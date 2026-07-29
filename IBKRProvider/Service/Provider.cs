using FIGCommon.Exceptions;
using FIGCommon.Interfaces;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Utilities;
using FIGCommon.Utilities.FIGProviderAPI;
using IBApi;
using IBKRProvider.Models;
using IBKRProvider.Tasks;
using IBKRProvider.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace IBKRProvider.Service
{
    public class Provider : IProviderService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ProviderOptions _options;
        private readonly ILogger<Provider> _logger;
        private readonly CancellationToken _appStoppingToken;
        private readonly ICallbackQueue _callbackQueue;
        private readonly IProviderConnectionSignal _connectionSignal;


        private EClientSocket? _clientSocket = null;
        private EWrapperImpl? _wrapper = null;
        private EReader? _reader = null;
        private ReaderSignal? _readerSignal = null;
        private ProviderTask? _task = null;
        private readonly object _connectionLock = new();
        private static readonly TimeSpan MinimumReconnectInterval = TimeSpan.FromSeconds(5);
        private DateTime _nextConnectionAttemptUtc = DateTime.MinValue;
        private ConnectionState _connectionState = ConnectionState.Disconnected;
        private bool _connectionUnavailableLogged;

        private enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected
        }

        public Provider(ILogger<Provider> logger,
                        IServiceProvider serviceProvider,
                        IHostApplicationLifetime appLifetime,
                        ProviderOptions options,
                        ICallbackQueue callbackQueue,
                        IProviderConnectionSignal connectionSignal)
        {
            _options = options;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _appStoppingToken = appLifetime.ApplicationStopping;
            _callbackQueue = callbackQueue ?? throw new ArgumentNullException(nameof(callbackQueue));
            _connectionSignal = connectionSignal ?? throw new ArgumentNullException(nameof(connectionSignal));
        }

        #region accessors

        public bool IsConnected => (_clientSocket?.IsConnected() ?? false); //&& (_wrapper?.ConnectionState.IsReady ?? false);
        public string Id => _options.Id;
        public ProviderOptions Options => _options;
        public string ClientId => _options.ClientId.ToString();
        #endregion accessors

        public void CreateConnection(bool forceReconnect = false)
        {
            lock (_connectionLock)
            {
                bool alreadyConnected = (_clientSocket?.IsConnected() ?? false); //&& (_wrapper?.ConnectionState.IsReady ?? false);
                if (alreadyConnected && !forceReconnect)
                {
                    _connectionState = ConnectionState.Connected;
                    return;
                }

                DateTime now = DateTime.UtcNow;
                if (!forceReconnect && now < _nextConnectionAttemptUtc)
                {
                    throw new BrokerConnectionException(
                        $"IBKR reconnect is already scheduled after {_nextConnectionAttemptUtc:O}");
                }

                bool connectionWasLost = !alreadyConnected && _connectionState == ConnectionState.Connected;
                if (connectionWasLost)
                {
                    _logger.LogWarning(
                        "IBKR connection lost for provider {ProviderId}; automatic reconnect attempts are starting",
                        _options.Id);
                }

                _connectionState = ConnectionState.Connecting;
                InternalDisconnect_NoLock();

                try
                {
                    _logger.LogDebug(
                        "CreateConnection: Establishing connection to IBKR TWS/Gateway at {Host}:{Port} with ClientId {ClientId}...",
                        _options.Host,
                        _options.Port,
                        _options.ClientId);

                    _readerSignal = new ReaderSignal(() => _task?.Enqueue(new TaskEvent("@PROV_EVT@", new JObject())));
                    _wrapper = new EWrapperImpl(_readerSignal, _logger, _callbackQueue, _connectionSignal);
                    _wrapper.RegisterResponseHandle("NEXT_VALID_ID");

                    _clientSocket = new EClientSocket(_wrapper, _readerSignal);
                    _wrapper.ClientSocket = _clientSocket;

                    _clientSocket.eConnect(_options.Host, _options.Port, _options.ClientId, _options.EnableFrozenData);

                    if (!_clientSocket.IsConnected())
                    {
                        throw new BrokerConnectionException("Failed to connect to IBKR TWS/Gateway");
                    }

                    _reader = new EReader(_clientSocket, _readerSignal);
                    _reader.Start();

                    if (_task == null)
                    {
                        _task = _serviceProvider.GetRequiredService<ProviderTask>();
                        _task.SetReader(_reader);
                        _task.StartTask();
                    }
                    else
                    {
                        _task.SetReader(_reader);
                        if (_task.Status != TaskStatus.Running)
                        {
                            _task.StartTask();
                        }
                    }
                    // Wait for OrderId
                    string eventId = "NEXT_VALID_ID";
                    int timeoutMs = 5000;
                    try
                    {
                        var respHandle = _wrapper?.FetchResponse("NEXT_VALID_ID");
                        if (respHandle != null)
                        {
                            int index = WaitHandle.WaitAny(new WaitHandle[] { respHandle.Event, _appStoppingToken.WaitHandle }, timeoutMs);

                            if (index == 1) // app stopping
                                throw new OperationCanceledException(_appStoppingToken);

                            if (index == WaitHandle.WaitTimeout)
                            {
                                _logger.LogDebug(
                                    "WaitForResponse: Timed out waiting for response for event {EventId} after {TimeoutMs} ms.",
                                    eventId,
                                    timeoutMs);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "WaitForResponse: Exception while waiting for response for event {EventId} - {Message}",
                            eventId,
                            ex.Message);
                    }

                    _connectionState = ConnectionState.Connected;
                    _nextConnectionAttemptUtc = DateTime.MinValue;
                    _connectionUnavailableLogged = false;
                    _logger.LogInformation(
                        "IBKR connection established for provider {ProviderId} at {Host}:{Port} with ClientId {ClientId}",
                        _options.Id,
                        _options.Host,
                        _options.Port,
                        _options.ClientId);
                }
                catch
                {
                    InternalDisconnect_NoLock();
                    _connectionState = ConnectionState.Disconnected;
                    _nextConnectionAttemptUtc = DateTime.UtcNow.Add(MinimumReconnectInterval);
                    if (!_connectionUnavailableLogged)
                    {
                        _logger.LogWarning(
                            "IBKR connection unavailable for provider {ProviderId}; retrying automatically",
                            _options.Id);
                        _connectionUnavailableLogged = true;
                    }
                    else
                    {
                        _logger.LogDebug(
                            "IBKR reconnect attempt failed for provider {ProviderId}; automatic retries remain active",
                            _options.Id);
                    }
                    throw;
                }
            }
        }

        private bool WaitForNextOrderId(int timeoutMS = 1000)
        {
            try
            {
                JToken? resp = WaitForResponse("NEXT_VALID_ID", timeoutMS);
                _wrapper?.ResetTracking("NEXT_VALID_ID");
                if (resp != null && resp.Type == JTokenType.Integer && _wrapper != null)
                {
                    _logger.LogInformation("nextValidId={OrderId}", _wrapper?.LastOrderId);
                    return true;
                }
                else
                {
                    _logger?.LogError("Could not retrieve NextValidId");
                    return false;
                }
            }
            catch(ResponseWaitException wex)
            {
                _logger?.LogError(wex, "Could not retrieve NextValidId - Wait exception");
                if (wex.ErrorCode == WaitForResponseErrorCodes.COMM)
                {
                    throw;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Could not retrieve NextValidId");
                return false;
            }
        }

        public void Disconnect()
        {
            lock (_connectionLock)
            {
                InternalDisconnect_NoLock();
            }
        }

        private void InternalDisconnect_NoLock()
        {
            bool hadSocket = _clientSocket != null;
            try
            {
                _clientSocket?.eDisconnect();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Disconnect: exception while disconnecting socket");
            }
            finally
            {
                _reader = null;
                _readerSignal = null;
                _clientSocket = null;

                _wrapper?.Dispose();
                _wrapper = null;
                if (hadSocket)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        public void Dispose()
        {
            Disconnect();
        }

        private bool CheckConnected()
        {
            if (_clientSocket?.IsConnected() ?? false) //&& (_wrapper?.ConnectionState.IsReady ?? false))
                return true;

            CreateConnection();
            return (_clientSocket?.IsConnected() ?? false); //&& (_wrapper?.ConnectionState.IsReady ?? false);
        }

        public JToken? WaitForResponse(string eventId, int timeoutMs)
        {
            var respHandle = _wrapper?.FetchResponse(eventId);
            if (respHandle is null || respHandle.Event == null || timeoutMs < -1)
            {
                throw new ResponseWaitException(WaitForResponseErrorCodes.NOT_ASSIGNED, "Failed to find response handle");
            }

            // Wait for either:
            // 0 = response event signaled
            // 1 = app stopping token signaled
            // or timeout = WaitTimeout
            try
            {
                int index = WaitHandle.WaitAny(new WaitHandle[] { respHandle.Event, _appStoppingToken.WaitHandle }, timeoutMs);

                if (index == 1) // app stopping
                    throw new OperationCanceledException(_appStoppingToken);

                if (index == WaitHandle.WaitTimeout)
                {
                    _logger?.LogDebug($"WaitForResponse: Timed out waiting for response for event {eventId} after {timeoutMs} ms.");
                    // get last result of response handle (if any)

                    throw new ResponseWaitException(WaitForResponseErrorCodes.TIMEOUT, $"Timed out waiting for {eventId}");
                }
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, $"WaitForResponse: Exception while waiting for response for event {eventId} - {ex.Message}");
                throw new ResponseWaitException(WaitForResponseErrorCodes.WAIT_EXCEPTION, $"Timed out waiting for {eventId}");
            }

            // response event signaled
            if (respHandle.Error != null)
            {
                var errDef = IBKRProviderErrorCodes.GetProvErrorDef(respHandle.Error.ErrorCode);
                bool isConnectionError =
                    respHandle.Error.ErrorMsg.Contains("forcibly closed", StringComparison.OrdinalIgnoreCase) ||
                    respHandle.Error.ErrorMsg.Contains("connection closed", StringComparison.OrdinalIgnoreCase) ||
                    Array.IndexOf(IBKRProviderErrorCodes.ServerErrors, respHandle.Error.ErrorMsg) >= 0;

                if (isConnectionError)
                {
                    Disconnect();
                    throw new ResponseWaitException(WaitForResponseErrorCodes.COMM, $"Connection error waiting for response {eventId}");
                }

                throw new ProviderException(errDef?.ErrCode ?? -1, errDef?.RootsErr ?? ProviderErrorCodes.FAIL, respHandle.Error.ErrorMsg);
            }

            return respHandle.Response?["Result"];
        }

        private int ReserveOrderId()
        {
            int lastOrderId = _wrapper?.LastOrderId ?? -1;
            if (lastOrderId > 0)
            {
                _wrapper?.IncrementOrderId();
                return _wrapper?.LastOrderId ?? -1;
            }
            // Check connection - will throw ProviderException(-1, ProviderErrorCodes.COMM_ERR, "Provider Connection Error"); if not connected
            CheckConnected();

            _clientSocket?.reqIds(-1);
            if (WaitForNextOrderId(1000))
            {
                return _wrapper?.LastOrderId ?? -1;
            }
            return -1;
        }

        public List<BarData>? HistoricalDataRequest(HistoricalDataRequest request, int timeoutInMsec = 60000)
        {
            // Check connection - will throw ProviderException(-1, ProviderErrorCodes.COMM_ERR, "Provider Connection Error"); if not connected
            CheckConnected();

            // End Date/Time: The date, time, or time-zone entered should ne in the format yyyymmdd hh:mm:ss xx/xxxx where yyyymmdd and xx/xxxx are optional. E.g.: 20031126 15:59:00 US/Eastern
            //   Note that there is a space between the date and time, and between the time and time-zone.
            //   If no date is specified, current date is assumed. If no time-zone is specified, local time-zone is assumed(deprecated).
            //   You can also provide yyyymmddd-hh:mm:ss time is in UTC. Note that there is a dash between the date and time in UTC notation.
            String queryTime = FIGCommon.Utilities.DateTimeUtil.ConvertUnixTimeToDateTime(request.StartRawTime).ToString("yyyyMMdd-HH:mm:ss");
            Contract contract = new Contract()
            {
                Symbol = request.Ticker.Symbol,
                SecType = request.Ticker.SecType,
                Currency = request.Ticker.Currency,
                Exchange = request.Ticker.Exchange,
                LastTradeDateOrContractMonth = request.Ticker.SecType == "CONTFUT" ? null : TypeConvertUtil.GetStringValue(request.Ticker.ContractMonth)
            };
            int reqId = _wrapper?.NextReqId??-1;
            string eventId = $"REQ-{reqId}";

            var mEvent = _wrapper?.RegisterResponseHandle(eventId);
            try
            {
                _clientSocket?.reqHistoricalData(reqId, contract, request.Ticker.SecType == "CONTFUT" ? "" : queryTime, request.Duration, request.IntervalId, "TRADES", 0, 2, false, new List<TagValue>());
                JToken? resp = WaitForResponse(eventId, timeoutInMsec);
                if (resp == null || resp.Type != JTokenType.Array)
                {
                    string errMsg = "Invalid Response";
                    _logger?.LogError("HistoricalDataRequest: {0}", errMsg);
                    throw new ProviderException(-1, ProviderErrorCodes.FAIL, errMsg);
                }
                List<BarData>? barDataLst = new();
                try
                {
                    List<Bar>? bars = resp.ToObject<List<Bar>>();
                    if (bars != null)
                    {
                        bars.ForEach(bar =>
                        {
                            int time = (int)(TypeConvertUtil.GetIntegerValue(bar.Time) ?? -1);
                            barDataLst.Add(new BarData(time, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume));
                        });
                    }
                    return barDataLst;
                }
                catch (Exception e)
                {
                    string errMsg = "Exception parsing HistoricalData";
                    _logger?.LogError("HistoricalDataRequest: {0} - {1}", errMsg, e.Message);
                    throw new ProviderException(-1, ProviderErrorCodes.FAIL, errMsg);
                }
            }
            catch (ProviderException exResponse)
            {
                // convert error into Roots Error and throw
                string errMsg = exResponse.Message;
                _logger?.LogError($"HistoricalDataRequest: {exResponse.ProviderErrorCode} - {exResponse.Message}");
                //if (_wrapper?.LastErrorCode == 326) // client is in use
                //{
                //    _options.ClientId = _options.ClientId + 1;
                //    _logger?.LogDebug($"HistoricalDataRequest: Incremented ClientId to {_options.ClientId}");
                //}
                throw;
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                _logger?.LogError("HistoricalDataRequest: {0}", ex.Message);
                //if (_wrapper?.LastErrorCode == 326) // client is in use
                //{
                //    _options.ClientId = _options.ClientId + 1;
                //    _logger?.LogDebug($"HistoricalDataRequest: Incremented ClientId to {_options.ClientId}");
                //}
                throw new ProviderException(-1, ProviderErrorCodes.FAIL, ex.Message);
            }
            finally
            {
                _wrapper?.UnRegisterResponseHandle(eventId);
            }
        }

        public decimal? TickerSnapshot(TickerInfo ticker, int timeoutInMsec = 10000)
        {
            // Check connection - will throw ProviderException(-1, ProviderErrorCodes.COMM_ERR, "Provider Connection Error"); if not connected
            CheckConnected();

            Contract contract = new Contract()
            {
                Symbol = ticker.Symbol,
                SecType = ticker.SecType,
                Currency = ticker.Currency,
                Exchange = ticker.Exchange,
                LastTradeDateOrContractMonth = ticker.SecType == "CONTFUT" ? null : TypeConvertUtil.GetStringValue(ticker.ContractMonth)
            };
            int reqId = _wrapper?.NextReqId ?? -1;
            string eventId = $"REQ-{reqId}";

            var mEvent = _wrapper?.RegisterResponseHandle(eventId);
            try
            {
                _clientSocket?.reqHistoricalData(reqId, contract, "" , "30 S", "1 secs", "TRADES", 0, 2, false, new List<TagValue>());
                JToken? resp = WaitForResponse(eventId, timeoutInMsec);
                if (resp == null || resp.Type != JTokenType.Array)
                {
                    string errMsg = "Invalid Response";
                    _logger?.LogError("HistoricalDataRequest: {0}", errMsg);
                    throw new ProviderException(-1, ProviderErrorCodes.FAIL, errMsg);
                }
                List<BarData>? barDataLst = new();
                try
                {
                    List<Bar>? bars = resp.ToObject<List<Bar>>();
                    if (bars != null)
                    {
                        bars.ForEach(bar =>
                        {
                            int time = (int)(TypeConvertUtil.GetIntegerValue(bar.Time) ?? -1);
                            barDataLst.Add(new BarData(time, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume));
                        });
                    }

                    return (decimal?) barDataLst.OrderByDescending(b => b.RawTime).FirstOrDefault()?.Close;
                }
                catch (Exception e)
                {
                    string errMsg = "Exception parsing HistoricalData";
                    _logger?.LogError("HistoricalDataRequest: {0} - {1}", errMsg, e.Message);
                    throw new ProviderException(-1, ProviderErrorCodes.FAIL, errMsg);
                }
            }
            catch (ProviderException exResponse)
            {
                // convert error into Roots Error and throw
                string errMsg = exResponse.Message;
                _logger?.LogError($"HistoricalDataRequest: {exResponse.ProviderErrorCode} - {exResponse.Message}");
                //if (_wrapper?.LastErrorCode == 326) // client is in use
                //{
                //    _options.ClientId = _options.ClientId + 1;
                //    _logger?.LogDebug($"HistoricalDataRequest: Incremented ClientId to {_options.ClientId}");
                //}
                throw;
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                _logger?.LogError("HistoricalDataRequest: {0}", ex.Message);
                //if (_wrapper?.LastErrorCode == 326) // client is in use
                //{
                //    _options.ClientId = _options.ClientId + 1;
                //    _logger?.LogDebug($"HistoricalDataRequest: Incremented ClientId to {_options.ClientId}");
                //}
                throw new ProviderException(-1, ProviderErrorCodes.FAIL, ex.Message);
            }
            finally
            {
                _wrapper?.UnRegisterResponseHandle(eventId);
            }
        }

        public decimal? TickerSnapshotOld(TickerInfo ticker, int timeoutInMsec = 5000)
        {
            // Check connection - will throw ProviderException(-1, ProviderErrorCodes.COMM_ERR, "Provider Connection Error"); if not connected
            CheckConnected();

            // End Date/Time: The date, time, or time-zone entered should ne in the format yyyymmdd hh:mm:ss xx/xxxx where yyyymmdd and xx/xxxx are optional. E.g.: 20031126 15:59:00 US/Eastern
            //   Note that there is a space between the date and time, and between the time and time-zone.
            //   If no date is specified, current date is assumed. If no time-zone is specified, local time-zone is assumed(deprecated).
            //   You can also provide yyyymmddd-hh:mm:ss time is in UTC. Note that there is a dash between the date and time in UTC notation.
            Contract contract = new Contract()
            {
                Symbol = ticker.Symbol,
                SecType = ticker.SecType,
                Currency = ticker.Currency,
                Exchange = ticker.Exchange,
                LastTradeDateOrContractMonth = ticker.SecType == "CONTFUT" ? null : TypeConvertUtil.GetStringValue(ticker.ContractMonth)
            };

            int reqId = _wrapper?.NextTickerId ?? -1;
            string eventId = $"TICKER-{reqId}";

            var mEvent = _wrapper?.RegisterResponseHandle(eventId);
            try
            {
                bool isFut = ticker.SecType == "CONTFUT" || ticker.SecType == "FUT";
                _clientSocket?.reqMktData(reqId, contract, "", true, !isFut, new List<TagValue>());
                JToken? resp = WaitForResponse(eventId, timeoutInMsec);
                if (resp == null)
                {
                    string errMsg = "Invalid Response";
                    _logger?.LogError("TickerSnapshot: {0}", errMsg);
                    throw new ProviderException(-1, ProviderErrorCodes.FAIL, errMsg);
                }
                try
                {
                    decimal? price = resp.ToObject<decimal?>();
                    return price;
                }
                catch (Exception e)
                {
                    string errMsg = "Exception parsing TickerSnapshot";
                    _logger?.LogError("TickerSnapshot: {0} - {1}", errMsg, e.Message);
                    throw new ProviderException(-1, ProviderErrorCodes.FAIL, errMsg);
                }
            }
            catch (ProviderException exResponse)
            {
                // convert error into Roots Error and throw
                string errMsg = exResponse.Message;
                _logger?.LogError($"TickerSnapshot: {exResponse.ProviderErrorCode} - {exResponse.Message}");
                throw;
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                _logger?.LogError("TickerSnapshot: {0}", ex.Message);
                throw new ProviderException(-1, ProviderErrorCodes.FAIL, ex.Message);
            }
            finally
            {
                _wrapper?.UnRegisterResponseHandle(eventId);
                _clientSocket?.cancelMktData(reqId);
            }
        }


        public List<ContractInfo>? SymbolLookup(string symbol, int timeoutInMsec = 30000)
        {
            // Check connection - will throw ProviderException(-1, ProviderErrorCodes.COMM_ERR, "Provider Connection Error"); if not connected
            CheckConnected();

            int reqId = _wrapper?.NextReqId ?? -1;
            string eventId = $"REQ-{reqId}";

            var mEvent = _wrapper?.RegisterResponseHandle(eventId);
            try
            {
                _clientSocket?.reqMatchingSymbols(reqId, symbol);
                JToken? resp = WaitForResponse(eventId, timeoutInMsec);
                if (resp == null || resp.Type != JTokenType.Array)
                {
                    string errMsg = "Invalid Response";
                    _logger?.LogError("SymbolLookup: {0}", errMsg);
                    throw new ProviderException(-1, ProviderErrorCodes.FAIL, errMsg);
                }
                List<ContractInfo> contractInfoLst = new();
                try
                {
                    List<ContractDescription>? contractDescriptions = resp.ToObject<List<ContractDescription>>();
                    if (contractDescriptions != null)
                    {
                        contractDescriptions.ForEach(contractDesc =>
                        {
                            contractInfoLst.Add(new ContractInfo()
                            {
                                ConId = contractDesc.Contract.ConId,
                                Symbol = contractDesc.Contract.Symbol,
                                SecType = contractDesc.Contract.SecType,
                                LastTradeDateOrContractMonth = contractDesc.Contract.LastTradeDateOrContractMonth,
                                Strike = contractDesc.Contract.Strike,
                                Right = contractDesc.Contract.Right,
                                Multiplier = contractDesc.Contract.Multiplier,
                                Exchange = contractDesc.Contract.Exchange,
                                Currency = contractDesc.Contract.Currency,
                                LocalSymbol = contractDesc.Contract.LocalSymbol,
                                PrimaryExch = contractDesc.Contract.PrimaryExch,
                                TradingClass = contractDesc.Contract.TradingClass,
                                IncludeExpired = contractDesc.Contract.IncludeExpired,
                                SecIdType = contractDesc.Contract.SecIdType,
                                SecId = contractDesc.Contract.SecId,
                                Description = contractDesc.Contract.Description,
                                IssuerId = contractDesc.Contract.IssuerId,
                                ComboLegsDescription = contractDesc.Contract.ComboLegsDescription
                            });
                        });
                    }
                    return contractInfoLst;
                }
                catch (Exception e)
                {
                    string errMsg = "Exception parsing SymbolLookup";
                    _logger?.LogError("SymbolLookup: {0} - {1}", errMsg, e.Message);
                    throw new ProviderException(-1, ProviderErrorCodes.FAIL, errMsg);
                }
            }
            catch (ProviderException exResponse)
            {
                // convert error into Roots Error and throw
                string errMsg = exResponse.Message;
                _logger?.LogError("SymbolLookup: {0}", exResponse.Message);
                //if (_wrapper?.LastErrorCode == 326) // client is in use
                //{
                //    _options.ClientId = _options.ClientId + 1;
                //    _logger?.LogDebug($"SymbolLookup: Incremented ClientId to {_options.ClientId}");
                //}
                throw;
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                _logger?.LogError("SymbolLookup: {0}", ex.Message);
                throw new ProviderException(-1, ProviderErrorCodes.FAIL, errMsg);
            }
            finally
            {
                _wrapper?.UnRegisterResponseHandle(eventId);
            }
        }

        public ContractInfo? ContractInfo(InstrumentLookupRequest contrReq, int timeoutInMsec = 30000)
        {
            // Check connection - will throw ProviderException(-1, ProviderErrorCodes.COMM_ERR, "Provider Connection Error"); if not connected
            CheckConnected();

            var curMonth = $"{DateTime.Now.Year}{DateTime.Now.Month:D2}";
            Contract contr = new Contract()
            {
                Symbol = contrReq.Symbol,
                SecType = contrReq.SecType,
                LastTradeDateOrContractMonth = contrReq.LastTradeDateOrContractMonth == "" ? curMonth : contrReq.LastTradeDateOrContractMonth,
                Exchange = contrReq.Exchange,
                Currency = contrReq.Currency,
            };

            int reqId = _wrapper?.NextReqId ?? -1;
            string eventId = $"REQ-{reqId}";

            var mEvent = _wrapper?.RegisterResponseHandle(eventId);
            try
            {
                _clientSocket?.reqContractDetails(reqId, contr);
                JToken? resp = WaitForResponse(eventId, timeoutInMsec);
                if (resp == null || resp.Type != JTokenType.Object)
                {
                    string errMsg = "Invalid Response";
                    _logger?.LogError("ContractInfo: {0}", errMsg);
                    throw new ProviderException(-1, ProviderErrorCodes.FAIL, errMsg);
                }
                try
                {
                    ContractDetails? contrDet = resp.ToObject<ContractDetails>();
                    if (contrDet != null)
                    {
                        return new ContractInfo()
                        {
                            ConId = contrDet.Contract.ConId,
                            Symbol = contrDet.Contract.Symbol,
                            Name = contrDet.LongName,
                            SecType = contrDet.Contract.SecType,
                            LastTradeDateOrContractMonth = contrDet.Contract.LastTradeDateOrContractMonth,
                            Strike = contrDet.Contract.Strike,
                            Right = contrDet.Contract.Right,
                            Multiplier = contrDet.Contract.Multiplier,
                            MinTick = (decimal) contrDet.MinTick,
                            Exchange = contrDet.Contract.Exchange,
                            Currency = contrDet.Contract.Currency,
                            LocalSymbol = contrDet.Contract.LocalSymbol,
                            PrimaryExch = contrDet.Contract.PrimaryExch,
                            TradingClass = contrDet.Contract.TradingClass,
                            IncludeExpired = contrDet.Contract.IncludeExpired,
                            SecIdType = contrDet.Contract.SecIdType,
                            SecId = contrDet.Contract.SecId,
                            Description = contrDet.Contract.Description,
                            IssuerId = contrDet.Contract.IssuerId,
                            ComboLegsDescription = contrDet.Contract.ComboLegsDescription
                        };
                    }
                    return null;
                }
                catch (Exception e)
                {
                    string errMsg = "Exception parsing";
                    _logger?.LogError("ContractInfo: {0} - {1}", errMsg, e.Message);
                    throw new ProviderException(-1, ProviderErrorCodes.FAIL, errMsg);
                }
            }
            catch (ProviderException exResponse)
            {
                // convert error into Roots Error and throw
                string errMsg = exResponse.Message;
                _logger?.LogError("ContractInfo: {0}", exResponse.Message);
                //if (_wrapper?.LastErrorCode == 326) // client is in use
                //{
                //    _options.ClientId = _options.ClientId + 1;
                //    _logger?.LogDebug($"ContractInfo: Incremented ClientId to {_options.ClientId}");
                //}
                throw;
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                _logger?.LogError("ContractInfo: {0}", ex.Message);
                throw new ProviderException(-1, ProviderErrorCodes.FAIL, errMsg);
            }
            finally
            {
                _wrapper?.UnRegisterResponseHandle(eventId);
            }

        }

        public OrderStatusDto PlaceOrder(BrokerOrderRequestDto orderReq, int timeoutInMsec = 30000)
        {
            // Check connection - will throw ProviderException(-1, ProviderErrorCodes.COMM_ERR, "Provider Connection Error"); if not connected
            CheckConnected();

            Contract contr = new Contract()
            {
                Symbol = orderReq.Ticker.Symbol,
                SecType = orderReq.Ticker.SecType,
                LastTradeDateOrContractMonth = TypeConvertUtil.GetStringValue(orderReq.Ticker.ContractMonth) ?? "",
                Exchange = orderReq.Ticker.Exchange,
                Currency = orderReq.Ticker.Currency
            };

            bool isLimit = orderReq.OrderType == OrderTypes.LIMIT
                            || orderReq.OrderType == OrderTypes.STOP_LIMIT;
            Order order = new Order()
            {
                Action = orderReq.Qty > 0 ? "BUY" : (orderReq.Qty < 0 ? "SELL" : ""),
                OrderType = orderReq.OrderType,
                TotalQuantity = Math.Abs(orderReq.Qty),
                LmtPrice = isLimit ? (double)orderReq.Price : 0.0D,
                Tif = orderReq.TimeInForce,
                Transmit = true,
                OrderRef = orderReq.TxnId
            };

            OrderStatusDto defStatus = new OrderStatusDto()
            {
                OrderId = orderReq.TxnId,
                BrokerRef = string.Empty,
                BrokerOrderId = string.Empty,
                OrderRefId = orderReq.RefClientId,
                BrokerParams = new(),
                OrderType = orderReq.OrderType.ToUpper().Trim(),
                Qty = orderReq.Qty,
                Price = orderReq.Price,
                FilledQty = 0,
                AvgFillPrice = 0,
                CommissionAndFees = 0,
                StatusCode = OrderStatusCodes.INPROGRESS,
                StatusTime = FIGCommon.Utilities.DateTimeUtil.CurrentUTCUnixTime()
            };

            // initial placing of order, tracking using OrderId
            OrderStatusDto? orderStatus = null;
            string reqIdStr = $"";
            try
            {
                int orderId = ReserveOrderId();
                if (orderId < 0)
                {
                    defStatus.StatusCode = OrderStatusCodes.IGNORED_ORDER_ID_ERROR;
                    defStatus.StatusTime = FIGCommon.Utilities.DateTimeUtil.CurrentUTCUnixTime();
                    return defStatus;
                }

                defStatus.BrokerOrderId = orderId.ToString();

                reqIdStr = $"ORDER-{orderId}";

                defStatus.BrokerParams.Add("OrderId", orderId.ToString());

                _wrapper?.RegisterResponseHandle(reqIdStr);

                _clientSocket?.placeOrder(orderId, contr, order);
            }
            catch (ResponseWaitException wex)
            {
                _logger?.LogError(wex, $"Exception while placing order for orderId: {orderReq.TxnId}");
                defStatus.StatusCode = OrderStatusCodes.FAIL_REJECTED;
                defStatus.StatusTime = FIGCommon.Utilities.DateTimeUtil.CurrentUTCUnixTime();
                if (wex.ErrorCode == WaitForResponseErrorCodes.COMM)
                {
                    defStatus.StatusCode = OrderStatusCodes.FAIL_CONNECTION;
                }
                return defStatus;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception while placing order for orderId: {orderReq.TxnId}");
                defStatus.StatusCode = OrderStatusCodes.FAIL_REJECTED;
                defStatus.StatusTime = FIGCommon.Utilities.DateTimeUtil.CurrentUTCUnixTime();
                return defStatus;
            }
            if (timeoutInMsec == 0) return defStatus;


            // Waiting for response...
            int timeout = timeoutInMsec;
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                JToken? resp = WaitForResponse(reqIdStr, timeout);
                orderStatus = resp?.ToObject<OrderStatusDto>();
                if (orderStatus is null || string.IsNullOrEmpty(orderStatus.BrokerRef))
                {
                    // received an invalid response, cannot track further
                    // mark as in-progress
                    defStatus.StatusCode = OrderStatusCodes.INPROGRESS;
                    defStatus.StatusTime = FIGCommon.Utilities.DateTimeUtil.CurrentUTCUnixTime();
                    return defStatus;
                }
            }
            catch (ResponseWaitException wex)
            {
                if (wex.ErrorCode == WaitForResponseErrorCodes.TIMEOUT)
                {
                    defStatus.StatusCode = OrderStatusCodes.INPROGRESS_TIMEOUT;
                    defStatus.StatusTime = FIGCommon.Utilities.DateTimeUtil.CurrentUTCUnixTime();
                    return defStatus;
                }
                else
                {
                    defStatus.StatusCode = OrderStatusCodes.INPROGRESS;
                    defStatus.StatusTime = FIGCommon.Utilities.DateTimeUtil.CurrentUTCUnixTime();
                    return defStatus;
                }
            }
            catch (ProviderException pex)
            {
                _logger?.LogError(pex, $"Failed to place order for orderId: {orderReq.TxnId} - Code({pex.RawErrorCode}), {pex.Message}");
                // failed to place order
                defStatus.StatusCode = OrderStatusCodes.FAIL_REJECTED;
                defStatus.StatusTime = FIGCommon.Utilities.DateTimeUtil.CurrentUTCUnixTime();
                if (pex.ProviderErrorCode == ProviderErrorCodes.DUPLICATE_ORDER)
                {
                    // reset NextOrderId to avoid future duplicate id issue
                    _wrapper?.ResetLastOrderId();
                    _logger?.LogDebug($"PlaceOrder: Reset NextOrderId to -1 due to duplicate order id error");
                    defStatus.StatusCode = OrderStatusCodes.FAIL_DUPLICATE_ID;
                }
                _wrapper?.UnRegisterResponseHandle(reqIdStr);  // final state for this call
                return defStatus;
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, $"Exception while placing order for orderId: {orderReq.TxnId} - {ex.Message}");
                // failed to place order
                defStatus.StatusCode = OrderStatusCodes.FAIL_OTHER;
                defStatus.StatusTime = FIGCommon.Utilities.DateTimeUtil.CurrentUTCUnixTime();
                _wrapper?.UnRegisterResponseHandle(reqIdStr);  // final state for this call
                return defStatus;
            }

            // now wait for final status update, tracking using BrokerRef which is more reliable
            // for tracking the order status after placement
            if (orderStatus != null && !string.IsNullOrEmpty(orderStatus.BrokerRef))
            {
                orderStatus.OrderRefId = defStatus.OrderRefId;

                if (isLimit) // stop tracking
                {
                    return orderStatus;
                }
                string brokerRefEventId = $"PERMID-{orderStatus.BrokerRef}";
                OrderStatusDto? statusUpdate = new OrderStatusDto(orderStatus);
                try
                {
                    timeout = timeoutInMsec - (int)sw.ElapsedMilliseconds;
                    timeout = timeout < 0 ? 0 : timeout;

                    while (timeout > 0 && !OrderStatusCodes.IsComplete(statusUpdate?.StatusCode?? OrderStatusCodes.INPROGRESS))
                    {
                        JToken? finalResp = WaitForResponse(brokerRefEventId, timeout);
                        statusUpdate = finalResp?.ToObject<OrderStatusDto>();
                        timeout = timeoutInMsec - (int)sw.ElapsedMilliseconds;
                        timeout = timeout < 0 ? 0 : timeout;
                    }
                    if (statusUpdate is null || string.IsNullOrEmpty(statusUpdate.BrokerRef))
                    {
                        return orderStatus;
                    }
                    orderStatus = statusUpdate;
                    orderStatus.OrderRefId = defStatus.OrderRefId;
                    return orderStatus;
                }
                catch (ProviderException pex)
                {
                    _logger?.LogError(pex, $"Failed to place order for orderId: {orderReq.TxnId} - Code({pex.RawErrorCode}), {pex.Message}");
                    // failed to place order
                    defStatus.StatusCode = OrderStatusCodes.FAIL_REJECTED;
                    defStatus.StatusTime = FIGCommon.Utilities.DateTimeUtil.CurrentUTCUnixTime();
                    _wrapper?.UnRegisterResponseHandle(reqIdStr);  // final state for this call
                    return defStatus;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Exception while placing order for orderId: {orderReq.TxnId} - {ex.Message}");
                    return orderStatus;
                }
                finally
                {
                    if (OrderStatusCodes.IsComplete(orderStatus.StatusCode))
                    {
                        _wrapper?.UnRegisterResponseHandle(brokerRefEventId);
                    }
                }
            }
            else
            {
                defStatus.StatusCode = OrderStatusCodes.INPROGRESS;
                defStatus.StatusTime = FIGCommon.Utilities.DateTimeUtil.CurrentUTCUnixTime();
                return defStatus;
            }
        }

        public OrderStatusDto? TrackOrder(string orderId, int timeoutInMsec = 30000)
        {
            return TrackEvent($"ORDER-{orderId}");
        }
        public OrderStatusDto? TrackBrokerRef(string brokerRef, int timeoutInMsec = 30000)
        {
            return TrackEvent($"PERMID-{brokerRef}");
        }

        public OrderStatusDto? TrackEvent(string eventId, int timeoutInMsec = 30000)
        {
            try
            {
                var respHandle = _wrapper?.FetchResponse(eventId);
                if (respHandle == null) return null;   // no need to wait, the handle is not registered

                _wrapper?.ResetTracking(eventId);
                JToken? resp = WaitForResponse(eventId, timeoutInMsec);
                OrderStatusDto? orderStatus = resp?.ToObject<OrderStatusDto>();
                if (orderStatus == null)
                {
                    _wrapper?.UnRegisterResponseHandle(eventId);
                    return null;
                }
                // if order is still in progress, return null to indicate no final status yet
                if (OrderStatusCodes.IsComplete(orderStatus.StatusCode))
                {
                    _wrapper?.UnRegisterResponseHandle(eventId);
                }
                return orderStatus;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Response error or timeout while tracking event {eventId} - {ex.Message}");
                throw;
            }
        }

        public void QueryOpenOrders()
        {
            CheckConnected();
            _clientSocket?.reqOpenOrders();
        }

        public void QueryAllCompletedOrders()
        {
            CheckConnected();
            _clientSocket?.reqCompletedOrders(true); 
        }

        public List<OrderStatusDto>? QueryAllOpenOrders(int timeoutInMsec = 5000)
        {
            CheckConnected();

            int reqId = _wrapper?.NextReqId ?? -1;
            string eventId = $"OO-{reqId}";

            var mEvent = _wrapper?.RegisterResponseHandle(eventId);
            try
            {
                _clientSocket?.reqAllOpenOrders();
                JToken? resp = WaitForResponse(eventId, timeoutInMsec);
                if (resp == null || resp.Type != JTokenType.Array)
                {
                    string errMsg = "Invalid Response";
                    _logger?.LogError("QueryAllOpenOrders: {0}", errMsg);
                    throw new ProviderException(-1, ProviderErrorCodes.FAIL, errMsg);
                }
                try
                {
                    return resp.ToObject<List<OrderStatusDto>>();
                }
                catch (Exception e)
                {
                    string errMsg = "Exception parsing SymbolLookup";
                    _logger?.LogError("QueryAllOpenOrders: {0} - {1}", errMsg, e.Message);
                    throw new ProviderException(-1, ProviderErrorCodes.FAIL, errMsg);
                }
            }
            catch (ProviderException exResponse)
            {
                // convert error into Roots Error and throw
                string errMsg = exResponse.Message;
                _logger?.LogError("SymbolLookup: {0}", exResponse.Message);
                //if (_wrapper?.LastErrorCode == 326) // client is in use
                //{
                //    _options.ClientId = _options.ClientId + 1;
                //    _logger?.LogDebug($"SymbolLookup: Incremented ClientId to {_options.ClientId}");
                //}
                throw;
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                _logger?.LogError("QueryAllOpenOrders: {0}", ex.Message);
                throw new ProviderException(-1, ProviderErrorCodes.FAIL, errMsg);
            }
            finally
            {
                _wrapper?.UnRegisterResponseHandle(eventId);
            }
        }

        public CancelStatusDto? CancelOrder(string orderId, string brokerRef, int timeoutInMsec = 30000)
        {
            // Check connection - will throw ProviderException(-1, ProviderErrorCodes.COMM_ERR, "Provider Connection Error"); if not connected
            CheckConnected();

            int oid = (int)(TypeConvertUtil.GetIntegerValue(orderId) ?? -1);
            if (oid <= 0) 
            {
                _logger?.LogError($"Invalid orderId {orderId} for cancellation");
                return null;
            }

            string reqIdStr = $"CANCEL-{oid}";
            _wrapper?.RegisterResponseHandle(reqIdStr);
            try
            {
                OrderCancel orderCancel = new IBApi.OrderCancel()
                {
                    ExtOperator = "SRV",
                    ManualOrderIndicator = 1,
                    // ManualOrderCancelTime is a string in UTC time in the format  “YYYYMMDD-HH:mm:ss” 
                    ManualOrderCancelTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss")
                };
                _clientSocket?.cancelOrder(oid, orderCancel);
                if (timeoutInMsec == 0)
                {
                    return new CancelStatusDto()
                    {
                        OrderId = orderId,
                        BrokerRef = brokerRef,
                        StatusCode = OrderStatusCodes.INPROGRESS_CANCEL,
                        StatusTime = FIGCommon.Utilities.DateTimeUtil.CurrentUTCUnixTime()
                    };
                }

                JToken? resp = WaitForResponse(reqIdStr, timeoutInMsec);
                return resp?.ToObject<CancelStatusDto>();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception while placing order for orderId: {orderId} - {ex.Message}");
                if (ex.Message.StartsWith("Timed out"))
                {
                    throw new TimeoutException();
                }
                return null;
            }
            finally
            {
                _wrapper?.UnRegisterResponseHandle(reqIdStr);
            }
        }

        public int SubscribeMarketData(TickerInfo request)
        {
            // Check connection - will throw ProviderException(-1, ProviderErrorCodes.COMM_ERR, "Provider Connection Error"); if not connected
            CheckConnected();

            Contract contract = new Contract()
            {
                Symbol = request.Symbol,
                SecType = request.SecType,
                Currency = request.Currency,
                Exchange = request.Exchange,
                LastTradeDateOrContractMonth = request.SecType == "CONTFUT" ? null : TypeConvertUtil.GetStringValue(request.ContractMonth)
            };
            int tickerId = _wrapper?.NextTickerId ?? -1;
            try
            {
                //_clientSocket?.reqMktData(tickerId, contract, "233", false, false, new List<TagValue>());
                _clientSocket?.reqTickByTickData(tickerId, contract, "Last", 0, false);

                return tickerId;
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                _logger?.LogError("SubscribeMarketData: {0}", ex.Message);
                //if (_wrapper?.LastErrorCode == 326) // client is in use
                //{
                //    _options.ClientId = _options.ClientId + 1;
                //    _logger?.LogDebug($"SubscribeMarketData: Incremented ClientId to {_options.ClientId}");
                //}
                throw new ProviderException(-1, ProviderErrorCodes.FAIL, ex.Message);
            }
        }

        public void SubscribePositions()
        {
            // Check connection - will throw ProviderException(-1, ProviderErrorCodes.COMM_ERR, "Provider Connection Error"); if not connected
            CheckConnected();
            try
            {
                _clientSocket?.reqPositions();
                return ;
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                _logger?.LogError("SubscribePositions: {0}", ex.Message);
                //if (_wrapper?.LastErrorCode == 326) // client is in use
                //{
                //    _options.ClientId = _options.ClientId + 1;
                //    _logger?.LogDebug($"SubscribePositions: Incremented ClientId to {_options.ClientId}");
                //}
                throw new ProviderException(-1, ProviderErrorCodes.FAIL, ex.Message);
            }
        }

    }
}

