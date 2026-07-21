using FIGCommon.Interfaces;
using FIGCommon.Models.FIGBroker;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Utilities;
using IBApi;
using IBKRProvider.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace IBKRProvider.Utilities
{
    public class EWrapperImpl : EWrapper, IDisposable
    {
        public EReaderSignal Signal { get; }
        public EClientSocket ClientSocket { get; set; }

        private readonly ILogger? _logger;
        private readonly ConcurrentDictionary<string, ResponseHandle> _responseHandlerMap = new();
        private readonly List<OrderStatusDto> _orderStatusList = new();
        protected Dictionary<int, List<Bar>> historicalDataMap = new();
        private readonly ICallbackQueue _callbackQueue;
        private readonly IProviderConnectionSignal _connectionSignal;
        private readonly object _callbackLock = new();

        private readonly object _lastErrorCodeLock = new();
        private readonly object _nextReqIdLock = new();
        private readonly object _nextTickerIdLock = new();
        private readonly object _lastOrderIdLock = new();

        private static int _nextReqIdBase = 99000000;
        private int _nextReqId = _nextReqIdBase;
        private static int _nextTickerIdBase = 89000000;
        private int _nextTickerId = _nextTickerIdBase;
        private int _lastErrorCode = 0;
        private int _lastOrderId = -1;


        public EWrapperImpl(EReaderSignal signal, ILogger? logger, ICallbackQueue queue, IProviderConnectionSignal connectionSignal)
        {
            Signal = signal;
            ClientSocket = new EClientSocket(this, Signal);
            _logger = logger;
            _callbackQueue = queue;
            _connectionSignal = connectionSignal;
        }

        public void OnCallback(BrokerCallbackMessage message)
        {
            lock (_callbackLock)
            {
                _callbackQueue.TryEnqueue(message);
            }
        }

        #region accessors
        public int NextReqId
        {
            get
            {
                lock (_nextReqIdLock)
                {
                    return ++_nextReqId;
                }
            }
        }
        public int NextTickerId
        {
            get
            {
                lock (_nextTickerIdLock)
                {
                    return ++_nextTickerId;
                }
            }
        }
        public int LastErrorCode
        {
            get
            {
                lock (_lastErrorCodeLock)
                {
                    return _lastErrorCode;
                }
            }
        }

        public int LastOrderId
        {
            get
            {
                lock (_lastOrderIdLock)
                {
                    return _lastOrderId;
                }
            }
            private set
            {
                lock (_lastOrderIdLock)
                {
                    _lastOrderId = value;
                }
            }
        }
        public void IncrementOrderId()
        {
            lock (_lastOrderIdLock)
            {
                _lastOrderId = _lastOrderId > 0 ? _lastOrderId + 1 : _lastOrderId;
            }
        }
        public void ResetLastOrderId()
        {
            lock (_lastOrderIdLock)
            {
                _lastOrderId = -1;
            }
        }
        public void ObserveOrderId(int orderId)
        {
            if (orderId <= 0)
                return;

            lock (_lastOrderIdLock)
            {
                if (orderId > _lastOrderId)
                    _lastOrderId = orderId;
            }
        }

        #endregion accessors

        public ResponseHandle RegisterResponseHandle(string reqId, IDisposable? data = null)
        {
            var newHandle = new ResponseHandle(reqId, data);

            if (_responseHandlerMap.TryRemove(reqId, out ResponseHandle? oldHandle))
            {
                oldHandle.Dispose();
            }

            _responseHandlerMap[reqId] = newHandle;
            return newHandle;
        }

        public void UnRegisterResponseHandle(string reqId)
        {
            if (_responseHandlerMap.TryRemove(reqId, out ResponseHandle? handle))
            {
                handle.Dispose();
            }
        }

        public bool IsResponseHandleRegistered(string reqId)
        {
            return _responseHandlerMap.ContainsKey(reqId);
        }

        public ResponseHandle? FetchResponse(string reqId)
        {
            return _responseHandlerMap.TryGetValue(reqId, out ResponseHandle? handle) ? handle : null;
        }

        public ResponseHandle? FetchFirstResponseStartingWith(string prefix)
        {
            foreach (var kvp in _responseHandlerMap)
            {
                if (kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        public void ResetTracking(string reqId)
        {
            if (_responseHandlerMap.TryGetValue(reqId, out ResponseHandle? handle))
            {
                handle.ResetResponse();
            }
        }

        public void CompleteResponse(string reqId, JObject jsonObject)
        {
            if (_responseHandlerMap.TryGetValue(reqId, out ResponseHandle? handle))
            {
                handle.CompleteResponse(jsonObject);
            }
        }

        public void TouchResponse(string reqId, JObject jsonObject, bool signalEvent = false)
        {
            if (_responseHandlerMap.TryGetValue(reqId, out ResponseHandle? handle))
            {
                handle.TouchResponse(jsonObject, signalEvent);
            }
        }

        public void SignalResponse(string reqId)
        {
            if (_responseHandlerMap.TryGetValue(reqId, out ResponseHandle? handle))
            {
                handle.Signal();
            }
        }

        public virtual void error(Exception e)
        {
            _logger?.LogDebug("Exception thrown: " + e.Message);
            foreach (var kvp in _responseHandlerMap)
            {
                kvp.Value.ErrorResponse(-1, e.Message);
            }
        }

        public virtual void error(string str)
        {
            _logger?.LogDebug("IBKR Error: " + str);
        }

        public virtual void error(int id, long errorTime, int errorCode, string errorMsg, string advancedOrderRejectJson)
        {
            string errMsg = "";
            if (!Util.StringIsEmpty(advancedOrderRejectJson))
            {
                errMsg = string.Format("Error. Id: {0}, Code: {1}, Msg: {2}, AdvancedOrderRejectJson: {3}", id, errorCode, errorMsg, advancedOrderRejectJson);
            }
            else
            {
                errMsg = string.Format("Error. Id: {0}, Code: {1}, Msg: {2}", id, errorCode, errorMsg);
            }
            lock (_lastErrorCodeLock)
            {
                _lastErrorCode = errorCode;
            }
            _logger?.LogDebug(errMsg);
            string eventId = $"REQ-{id}";
            if (id < _nextTickerIdBase && id > 0)
            {
                // order request errors have request Ids (orderId) > 0 and < _nextTickerIdBase,
                eventId = $"ORDER-{id}";
                var errDef = IBKRProviderErrorCodes.GetProvErrorDef(errorCode);
                bool isConnectionError =
                    errorMsg.Contains("forcibly closed", StringComparison.OrdinalIgnoreCase) ||
                    errorMsg.Contains("connection closed", StringComparison.OrdinalIgnoreCase) ||
                    Array.IndexOf(IBKRProviderErrorCodes.ServerErrors, errorMsg) >= 0;

                if (errDef != null)
                {
                    ErrorResponseDto errResponse = new ErrorResponseDto()
                    {
                        BrokerOrderId = id.ToString(),
                        BrokerErrorCode = errDef.ErrCode,
                        ErrorCode = errDef.RootsErr,
                        Message = errorMsg
                    };
                    JObject jsonResult = JObject.FromObject(errResponse);
                    OnCallback(new BrokerCallbackMessage
                    {
                        Type = BrokerCallbackMessageType.Error,
                        DataJson = jsonResult,
                        Ttl = isConnectionError ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(30)
                    });
                }
            }
            else if (id >= _nextTickerIdBase && id < _nextReqIdBase)
            {
                // market data request errors have request Ids (tickerId) > _nextTickerIdBase and < _nextReqIdBase,
                eventId = $"TICKER-{id}";
                var errDef = IBKRProviderErrorCodes.GetProvErrorDef(errorCode);
                bool isConnectionError =
                    errorMsg.Contains("forcibly closed", StringComparison.OrdinalIgnoreCase) ||
                    errorMsg.Contains("connection closed", StringComparison.OrdinalIgnoreCase) ||
                    Array.IndexOf(IBKRProviderErrorCodes.ServerErrors, errorMsg) >= 0;

                if (errDef != null)
                {
                    ErrorResponseDto errResponse = new ErrorResponseDto()
                    {
                        BrokerOrderId = id.ToString(),
                        BrokerErrorCode = errDef.ErrCode,
                        ErrorCode = errDef.RootsErr,
                        Message = errorMsg
                    };
                    JObject jsonResult = JObject.FromObject(errResponse);
                    OnCallback(new BrokerCallbackMessage
                    {
                        Type = BrokerCallbackMessageType.Error,
                        DataJson = jsonResult,
                        Ttl = isConnectionError ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(30)
                    });
                }
            }
            else
            {
            }
            if (_responseHandlerMap.TryGetValue(eventId, out ResponseHandle? handle))
            {
                handle.ErrorResponse(errorCode, errMsg);
            }
        }

        public virtual void connectionClosed()
        {
            _logger?.LogDebug("Connection Closed");
            _connectionSignal.NotifyDisconnected();
            foreach (var kvp in _responseHandlerMap)
            {
                kvp.Value.ErrorResponse(LastErrorCode, "Connection Closed");
            }
        }

        public virtual void currentTime(long time)
        {
            _logger?.LogDebug("IBKR current time" + time );
        }

        public virtual void connectAck()
        {
            _connectionSignal.NotifyConnected();
            if (ClientSocket.AsyncEConnect)
            {
                ClientSocket.startApi();
            }
        }

        public virtual void tickPrice(int tickerId, int field, double price, TickAttrib attribs)
        {
            //Console.WriteLine("Tick Price. Ticker Id:" + tickerId + ", Field: " + field + ", Price: " + Util.DoubleMaxString(price) + ", CanAutoExecute: " + attribs.CanAutoExecute +
            //    ", PastLimit: " + attribs.PastLimit + ", PreOpen: " + attribs.PreOpen);
            JObject jsonResult = JObject.FromObject(price);
            JObject jsonObject = new JObject();
            jsonObject["Result"] = jsonResult;
            if (tickerId > 0)
            {
                // in case if this is the initial tracking using orderId (where PermId is not yet available)
                CompleteResponse($"TICKER-{tickerId}", jsonObject);
            }
        }

        public virtual void tickSize(int tickerId, int field, decimal size)
        {
            //Console.WriteLine("Tick Size. Ticker Id:" + tickerId + ", Field: " + field + ", Size: " + Util.DecimalMaxString(size));
        }

        public virtual void tickString(int tickerId, int tickType, string value)
        {
            if (tickType != 48) return;

            var parts = value.Split(';');

            var price = decimal.Parse(parts[0]);
            var size = decimal.Parse(parts[1]);
            var timestampMs = long.Parse(parts[2]);

            var time = DateTimeOffset
                .FromUnixTimeMilliseconds(timestampMs)
                .UtcDateTime;

            MarketDataDto data = new MarketDataDto()
            {
                TickerId = tickerId,
                Price = price,
                Volume = (int)size,
                DataTime = timestampMs
            };
            JObject jsonResult = JObject.FromObject(data);
            OnCallback(new BrokerCallbackMessage
            {
                Type = BrokerCallbackMessageType.MarketData,
                DataJson = jsonResult,
                Ttl = TimeSpan.FromSeconds(30)
            });
        }

        public virtual void tickGeneric(int tickerId, int field, double value)
        {
            //Console.WriteLine("Tick Generic. Ticker Id:" + tickerId + ", Field: " + field + ", Value: " + Util.DoubleMaxString(value));
        }

        public virtual void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureLastTradeDate, double dividendImpact, double dividendsToLastTradeDate)
        {
            //Console.WriteLine("TickEFP. " + tickerId + ", Type: " + tickType + ", BasisPoints: " + Util.DoubleMaxString(basisPoints) + ", FormattedBasisPoints: " + formattedBasisPoints +
            //    ", ImpliedFuture: " + Util.DoubleMaxString(impliedFuture) + ", HoldDays: " + Util.IntMaxString(holdDays) + ", FutureLastTradeDate: " + futureLastTradeDate +
            //    ", DividendImpact: " + Util.DoubleMaxString(dividendImpact) + ", DividendsToLastTradeDate: " + Util.DoubleMaxString(dividendsToLastTradeDate));
        }

        public void deltaNeutralValidation(int reqId, DeltaNeutralContract deltaNeutralContract) { }

        public virtual void tickOptionComputation(int tickerId, int field, int tickAttrib, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
        {
            //Console.WriteLine("TickOptionComputation. TickerId: " + tickerId + ", field: " + field + ", TickAttrib: " + Util.IntMaxString(tickAttrib) + ", ImpliedVolatility: " + Util.DoubleMaxString(impliedVolatility) +
            //    ", Delta: " + Util.DoubleMaxString(delta) + ", OptionPrice: " + Util.DoubleMaxString(optPrice) + ", pvDividend: " + Util.DoubleMaxString(pvDividend) +
            //    ", Gamma: " + Util.DoubleMaxString(gamma) + ", Vega: " + Util.DoubleMaxString(vega) + ", Theta: " + Util.DoubleMaxString(theta) + ", UnderlyingPrice: " + Util.DoubleMaxString(undPrice));
        }

        public virtual void tickSnapshotEnd(int tickerId)
        {
            //Console.WriteLine("TickSnapshotEnd: " + tickerId);
        }

        public virtual void nextValidId(int orderId)
        {
            _logger?.LogDebug($"IBKR next valid Id: {orderId}");

            LastOrderId = orderId;

            JObject jsonObject = new JObject
            {
                ["Result"] = orderId
            };

            TouchResponse("NEXT_VALID_ID", jsonObject, true);
        }

        public virtual void managedAccounts(string accountsList)
        {
            //Console.WriteLine("Account list: " + accountsList);
        }

        public virtual void accountSummary(int reqId, string account, string tag, string value, string currency)
        {
            //Console.WriteLine("Acct Summary. ReqId: " + reqId + ", Acct: " + account + ", Tag: " + tag + ", Value: " + value + ", Currency: " + currency);
        }


        public virtual void accountSummaryEnd(int reqId)
        {
            //Console.WriteLine("AccountSummaryEnd. Req Id: " + reqId + "\n");
        }

        public virtual void updateAccountValue(string key, string value, string currency, string accountName)
        {
            //Console.WriteLine("UpdateAccountValue. Key: " + key + ", Value: " + value + ", Currency: " + currency + ", AccountName: " + accountName);
        }
        public virtual void updatePortfolio(Contract contract, decimal position, double marketPrice, double marketValue, double averageCost, double unrealizedPNL, double realizedPNL, string accountName)
        {
            //Console.WriteLine("UpdatePortfolio. " + contract.Symbol + ", " + contract.SecType + " @ " + contract.Exchange
            //    + ": Position: " + Util.DecimalMaxString(position) + ", MarketPrice: " + Util.DoubleMaxString(marketPrice) + ", MarketValue: " + Util.DoubleMaxString(marketValue) +
            //    ", AverageCost: " + Util.DoubleMaxString(averageCost) + ", UnrealizedPNL: " + Util.DoubleMaxString(unrealizedPNL) + ", RealizedPNL: " + Util.DoubleMaxString(realizedPNL) +
            //    ", AccountName: " + accountName);
        }

        public virtual void updateAccountTime(string timestamp)
        {
            //Console.WriteLine("UpdateAccountTime. Time: " + timestamp + "\n");
        }

        public virtual void accountDownloadEnd(string account)
        {
            //Console.WriteLine("Account download finished: " + account + "\n");
        }

        public virtual void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
        {
            ObserveOrderId(orderId);
            OrderStatusDto status;
            try
            {
                status = ExtractOrderStatus(orderId, contract, order, orderState);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"openOrder: failed to extract status for orderId={orderId}: {ex.Message}");
                return;
            }
            JObject jsonResult = JObject.FromObject(status);
            OnCallback(new BrokerCallbackMessage
            {
                Type = BrokerCallbackMessageType.OpenOrder,
                DataJson = jsonResult,
                Ttl = TimeSpan.FromSeconds(30)
            });

            JObject jsonObject = new JObject();
            jsonObject["Result"] = jsonResult;

            if (orderId > 0)
            {
                // in case if this is the initial tracking using orderId (where PermId is not yet available)
                CompleteResponse($"ORDER-{orderId}", jsonObject);
                ResponseHandle? respHPermId = FetchResponse($"PERMID-{order.PermId}");
                if (respHPermId == null) RegisterResponseHandle($"PERMID-{order.PermId}");
            }
            var allOrderRequests = FetchFirstResponseStartingWith("OO");
            if (allOrderRequests != null)
            {
                _orderStatusList.Add(status);
            }

        }

        public void orderStatus(int orderId, string status, decimal filled, decimal remaining, double avgFillPrice, long permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice)
        {
            ObserveOrderId(orderId);
            _logger?.LogDebug(
                "OrderStatus. Id={OrderId}, Status={Status}, Filled={Filled}, Remaining={Remaining}, AvgFillPrice={AvgFillPrice}, PermId={PermId}, ParentId={ParentId}, LastFillPrice={LastFillPrice}, ClientId={ClientId}, WhyHeld={WhyHeld}, MktCapPrice={MktCapPrice}",
                orderId, status, filled, remaining, avgFillPrice, permId, parentId, lastFillPrice, clientId, whyHeld, mktCapPrice);
            try
            {
                int qty = (int)(filled + remaining);
                bool fullyFilled = remaining == 0;
                int filledQty = (int)filled; // check if negative when it is a sell
                int statusCode = ProviderStatusCode(status, fullyFilled);
                string brokerRef = permId.ToString();

                ResponseHandle? respH = FetchResponse($"ORDER-{orderId}");
                OrderStatusDto? orderStatus = respH?.Response?["Result"]?.ToObject<OrderStatusDto>();
                if (orderStatus != null)
                {
                    orderStatus.StatusCode = statusCode;
                    orderStatus.AvgFillPrice = (decimal)avgFillPrice;
                    orderStatus.FilledQty = (int)(orderStatus.Qty < 0 ? -filled : filled);
                    orderStatus.BrokerRef = orderStatus.BrokerRef.Length == 0 ? permId.ToString() : orderStatus.BrokerRef;
                    orderStatus.BrokerOrderId = orderId.ToString();
                    JObject jsonResult = JObject.FromObject(orderStatus);
                    JObject jsonObject = new JObject();
                    jsonObject["Result"] = jsonResult;
                    TouchResponse($"PERMID-{permId}", jsonObject, true);
                }
                ResponseHandle? respCancel = FetchResponse($"CANCEL-{orderId}");
                if (respCancel != null)
                {
                    CancelStatusDto orderCancelStatus = new()
                    {
                        StatusCode = statusCode,
                        OrderId = orderId.ToString(),
                        BrokerRef = brokerRef,
                        StatusTime = FIGCommon.Utilities.DateTimeUtil.CurrentUTCUnixTime()
                    };
                    JObject jsonResult = JObject.FromObject(orderCancelStatus);
                    JObject jsonObject = new JObject();
                    jsonObject["Result"] = jsonResult;
                    TouchResponse($"CANCEL-{orderId}", jsonObject, true);
                }

                OrderStatusUpdate statusUpdate = new OrderStatusUpdate()
                {
                    BrokerParams = new Dictionary<string, string>
                    {
                        { "ClientId", clientId.ToString() },
                     },
                    BrokerRef = brokerRef,
                    BrokerOrderId = orderId.ToString(),
                    QtyFilled = (int)filled,
                    QtyRemaining = (int)remaining,
                    AvgFillPrice = (decimal)avgFillPrice,
                    StatusCode = statusCode,
                    StatusTime = DateTimeUtil.CurrentUTCUnixTime()
                };

                OnCallback(new BrokerCallbackMessage
                {
                    Type = BrokerCallbackMessageType.OrderStatus,
                    DataJson = JObject.FromObject(statusUpdate),
                    Ttl = TimeSpan.FromSeconds(30)
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error in orderStatus callback for OrderId: {orderId}");
            }
        }

        public virtual void completedOrder(Contract contract, Order order, OrderState orderState)
        {
            try
            {
                OrderStatusDto? status = ExtractCompletedOrderStatus(0, contract, order, orderState);
                if (status != null)
                {
                    JObject jsonStatusResult = JObject.FromObject(status);
                    OnCallback(new BrokerCallbackMessage
                    {
                        Type = BrokerCallbackMessageType.CompletedOrder,
                        DataJson = jsonStatusResult
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error in completedOrder callback");
            }
        }
        public virtual void completedOrdersEnd()
        {
        }

        public OrderStatusDto ExtractOrderStatus(int orderId, Contract contract, Order order, OrderState orderState)
        {
            string action = order.Action.ToUpper();
            //order.TotalQuantity >= int.MaxValue ? 0 :
            //    order.FilledQuantity >= int.MaxValue ? 0 :
            int totalQty = 0;
            int filledQty = 0;
            try
            {
                totalQty = (int)Math.Abs(order.TotalQuantity);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error parsing quantities for orderId={orderId}, TotalQuantity={order.TotalQuantity}");
                totalQty = 0;
            }
            try
            {
                filledQty = (int)Math.Abs(order.FilledQuantity);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error parsing quantities for orderId={orderId}, FilledQuantity={order.FilledQuantity}");
                filledQty = 0;
            }
            int qty = action == "BUY" ? totalQty : action == "SELL" ? -totalQty : 0;
            int qtyFilled = action == "BUY" ? filledQty : action == "SELL" ? -filledQty : 0;
            bool fullyFilled = qtyFilled == qty;
            int orderStatus = ProviderStatusCode(orderState.Status, fullyFilled);

            string orderType = order.OrderType.ToUpper();
            orderType = orderType == "MKT" ? OrderTypes.MARKET : orderType == "LMT" ? OrderTypes.LIMIT
                : orderType == "STP" ? OrderTypes.STOP : orderType == "STP LMT" ? OrderTypes.STOP_LIMIT : OrderTypes.UNDEFINED;

            decimal price = orderType == OrderTypes.LIMIT ? (decimal)order.LmtPrice : 0M;

            TickerInfo? ticker = new TickerInfo()
            {
                Symbol = contract.Symbol,
                SecType = contract.SecType,
                Exchange = contract.Exchange,
                Currency = contract.Currency,
                ContractMonth = int.TryParse(contract.LastTradeDateOrContractMonth, out int contractMonth) ? contractMonth : 0
            };
            OrderStatusDto status = new OrderStatusDto()
            {
                OrderId = order.OrderRef ?? "",
                BrokerRef = order.PermId.ToString(),
                BrokerOrderId = orderId > 0 ? orderId.ToString() : order.OrderId.ToString(),
                BrokerParams = new(),
                OrderRefId = string.Empty,
                OrderType = orderType,
                Qty = qty,
                Price = price,
                FilledQty = qtyFilled,
                AvgFillPrice = 0,  // not populated here it is populated in orderStatus callback
                CommissionAndFees = (decimal)orderState.CommissionAndFees,  // not populated here it is populated in orderStatus callback
                StatusCode = orderStatus,
                StatusTime = DateTimeUtil.CurrentUTCUnixTime(),
                Ticker = ticker
            };
            if (order.ClientId > 0)
            {
                status.BrokerParams["ClientId"] = order.ClientId.ToString();
            }
            _logger?.LogDebug($"Extracted OrderStatus for orderId={orderId}: OrderType={orderType}, Qty={qty}, Price={price}, FilledQty={qtyFilled}, StatusCode={orderStatus}");
            return status;
        }

        public OrderStatusDto ExtractCompletedOrderStatus(int orderId, Contract contract, Order order, OrderState orderState)
        {
            string action = order.Action.ToUpper();
            int totalQty = order.TotalQuantity >= int.MaxValue ? int.MaxValue : (int)Math.Abs(order.TotalQuantity);  // cannot rely on this
            int filledQty = order.FilledQuantity >= int.MaxValue ? int.MaxValue : (int)Math.Abs(order.FilledQuantity);
            bool fullyFilled = orderState.Status.ToUpper() == "FILLED";
            totalQty = fullyFilled ? filledQty : totalQty;
            int qty = action == "BUY" ? totalQty : action == "SELL" ? -totalQty : 0;
            int qtyFilled = action == "BUY" ? filledQty : action == "SELL" ? -filledQty : 0;
            int orderStatus = ProviderStatusCode(orderState.Status, fullyFilled);
            string orderType = order.OrderType.ToUpper();
            orderType = orderType == "MKT" ? OrderTypes.MARKET : orderType == "LMT" ? OrderTypes.LIMIT
                : orderType == "STP" ? OrderTypes.STOP : orderType == "STP LMT" ? OrderTypes.STOP_LIMIT : OrderTypes.UNDEFINED;


            decimal price = orderType == OrderTypes.LIMIT ? (decimal)order.LmtPrice : 0M;
            TickerInfo? ticker = new TickerInfo()
            {
                Symbol = contract.Symbol,
                SecType = contract.SecType,
                Exchange = contract.Exchange,
                Currency = contract.Currency,
                ContractMonth = int.TryParse(contract.LastTradeDateOrContractMonth, out int contractMonth) ? contractMonth : 0
            };
            OrderStatusDto status = new OrderStatusDto()
            {
                OrderId = order.OrderRef ?? "",
                BrokerRef = order.PermId.ToString(),
                BrokerOrderId = orderId > 0 ? orderId.ToString() : order.OrderId.ToString(),
                BrokerParams = new(),
                OrderRefId = string.Empty,
                OrderType = orderType,
                Qty = qty,
                Price = price,
                FilledQty = qtyFilled,
                AvgFillPrice = 0,  // not populated here it is populated in orderStatus callback
                CommissionAndFees = (decimal)orderState.CommissionAndFees,  // not populated here it is populated in orderStatus callback
                StatusCode = orderStatus,
                StatusTime = DateTimeUtil.CurrentUTCUnixTime(),
                Ticker = ticker
            };
            if (order.ClientId > 0)
            {
                status.BrokerParams["ClientId"] = order.ClientId.ToString();
            }
            return status;
        }



        public int ProviderStatusCode(string ibkrStatus, bool fullyFilled)
        {
            ibkrStatus = ibkrStatus.ToUpper().Trim();

            int orderStatus = OrderStatusCodes.NEW;
            if (ibkrStatus == "API_PENDING" || ibkrStatus == "PRESUBMITTED" || ibkrStatus == "PENDING_SUBMIT")
            {
                orderStatus = OrderStatusCodes.INPROGRESS;
            }
            else if (ibkrStatus == "SUBMITTED")
            {
                orderStatus = OrderStatusCodes.INPROGRESS_SUBMITTED;
            }
            else if (ibkrStatus == "APICANCELLED" || ibkrStatus == "CANCELLED")
            {
                orderStatus = OrderStatusCodes.FAIL_CANCELED;
            }
            else if (ibkrStatus == "INACTIVE")
            {
                orderStatus = OrderStatusCodes.INPROGRESS_INACTIVE;
            }
            else if (ibkrStatus == "FILLED")
            {
                orderStatus = fullyFilled ? OrderStatusCodes.FILLED : OrderStatusCodes.PARTIALLY_FILLED;
            }
            return orderStatus;
        }

        public virtual void openOrderEnd()
        {
            JObject jsonObject = new JObject();
            try
            {
                JToken jsonResult = JToken.FromObject(_orderStatusList);
                jsonObject["Result"] = jsonResult;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in openOrderEnd callback while processing order status list");
            }

            //iterate through _responseHandlerMap, need the key and ResponseHandle,
            //if the key starts with "OO-" then complete the response with _orderStatusList
            foreach (var kvp in _responseHandlerMap)
            {
                if (kvp.Key.StartsWith("OO-"))
                {
                    kvp.Value.CompleteResponse(jsonObject);
                }
            }
            _orderStatusList.Clear();
        }

        public virtual void contractDetails(int reqId, ContractDetails contractDetails)
        {
            //Console.WriteLine("ContractDetails begin. ReqId: " + reqId);
            //printContractMsg(contractDetails.Contract);
            //printContractDetailsMsg(contractDetails);
            //Console.WriteLine("ContractDetails end. ReqId: " + reqId);

            JObject jsonResult = JObject.FromObject(contractDetails);
            JObject jsonObject = new JObject();
            jsonObject["Result"] = jsonResult;

            CompleteResponse($"REQ-{reqId}", jsonObject);
        }

        //public void printContractMsg(Contract contract)
        //{
        //    Console.WriteLine("\tConId: " + contract.ConId);
        //    Console.WriteLine("\tSymbol: " + contract.Symbol);
        //    Console.WriteLine("\tSecType: " + contract.SecType);
        //    Console.WriteLine("\tLastTradeDateOrContractMonth: " + contract.LastTradeDateOrContractMonth);
        //    Console.WriteLine("\tStrike: " + Util.DoubleMaxString(contract.Strike));
        //    Console.WriteLine("\tRight: " + contract.Right);
        //    Console.WriteLine("\tMultiplier: " + contract.Multiplier);
        //    Console.WriteLine("\tExchange: " + contract.Exchange);
        //    Console.WriteLine("\tPrimaryExchange: " + contract.PrimaryExch);
        //    Console.WriteLine("\tCurrency: " + contract.Currency);
        //    Console.WriteLine("\tLocalSymbol: " + contract.LocalSymbol);
        //    Console.WriteLine("\tTradingClass: " + contract.TradingClass);
        //}

        //public void printContractDetailsMsg(ContractDetails contractDetails)
        //{
        //    Console.WriteLine("\tMarketName: " + contractDetails.MarketName);
        //    Console.WriteLine("\tMinTick: " + Util.DoubleMaxString(contractDetails.MinTick));
        //    Console.WriteLine("\tPriceMagnifier: " + Util.IntMaxString(contractDetails.PriceMagnifier));
        //    Console.WriteLine("\tOrderTypes: " + contractDetails.OrderTypes);
        //    Console.WriteLine("\tValidExchanges: " + contractDetails.ValidExchanges);
        //    Console.WriteLine("\tUnderConId: " + Util.IntMaxString(contractDetails.UnderConId));
        //    Console.WriteLine("\tLongName: " + contractDetails.LongName);
        //    Console.WriteLine("\tContractMonth: " + contractDetails.ContractMonth);
        //    Console.WriteLine("\tIndystry: " + contractDetails.Industry);
        //    Console.WriteLine("\tCategory: " + contractDetails.Category);
        //    Console.WriteLine("\tSubCategory: " + contractDetails.Subcategory);
        //    Console.WriteLine("\tTimeZoneId: " + contractDetails.TimeZoneId);
        //    Console.WriteLine("\tTradingHours: " + contractDetails.TradingHours);
        //    Console.WriteLine("\tLiquidHours: " + contractDetails.LiquidHours);
        //    Console.WriteLine("\tEvRule: " + contractDetails.EvRule);
        //    Console.WriteLine("\tEvMultiplier: " + Util.DoubleMaxString(contractDetails.EvMultiplier));
        //    Console.WriteLine("\tAggGroup: " + Util.IntMaxString(contractDetails.AggGroup));
        //    Console.WriteLine("\tUnderSymbol: " + contractDetails.UnderSymbol);
        //    Console.WriteLine("\tUnderSecType: " + contractDetails.UnderSecType);
        //    Console.WriteLine("\tMarketRuleIds: " + contractDetails.MarketRuleIds);
        //    Console.WriteLine("\tRealExpirationDate: " + contractDetails.RealExpirationDate);
        //    Console.WriteLine("\tLastTradeTime: " + contractDetails.LastTradeTime);
        //    Console.WriteLine("\tStock Type: " + contractDetails.StockType);
        //    Console.WriteLine("\tMinSize: " + Util.DecimalMaxString(contractDetails.MinSize));
        //    Console.WriteLine("\tSizeIncrement: " + Util.DecimalMaxString(contractDetails.SizeIncrement));
        //    Console.WriteLine("\tSuggestedSizeIncrement: " + Util.DecimalMaxString(contractDetails.SuggestedSizeIncrement));
        //    printContractDetailsSecIdList(contractDetails.SecIdList);
        //}

        //public void printContractDetailsSecIdList(List<TagValue> secIdList)
        //{
        //    if (secIdList != null && secIdList.Count > 0)
        //    {
        //        Console.Write("\tSecIdList: {");
        //        foreach (TagValue tagValue in secIdList)
        //        {
        //            Console.Write(tagValue.Tag + "=" + tagValue.Value + ";");
        //        }
        //        Console.WriteLine("}");
        //    }
        //}

        //public void printBondContractDetailsMsg(ContractDetails contractDetails)
        //{
        //    Console.WriteLine("\tSymbol: " + contractDetails.Contract.Symbol);
        //    Console.WriteLine("\tSecType: " + contractDetails.Contract.SecType);
        //    Console.WriteLine("\tCusip: " + contractDetails.Cusip);
        //    Console.WriteLine("\tCoupon: " + Util.DoubleMaxString(contractDetails.Coupon));
        //    Console.WriteLine("\tMaturity: " + contractDetails.Maturity);
        //    Console.WriteLine("\tIssueDate: " + contractDetails.IssueDate);
        //    Console.WriteLine("\tRatings: " + contractDetails.Ratings);
        //    Console.WriteLine("\tBondType: " + contractDetails.BondType);
        //    Console.WriteLine("\tCouponType: " + contractDetails.CouponType);
        //    Console.WriteLine("\tConvertible: " + contractDetails.Convertible);
        //    Console.WriteLine("\tCallable: " + contractDetails.Callable);
        //    Console.WriteLine("\tPutable: " + contractDetails.Putable);
        //    Console.WriteLine("\tDescAppend: " + contractDetails.DescAppend);
        //    Console.WriteLine("\tExchange: " + contractDetails.Contract.Exchange);
        //    Console.WriteLine("\tCurrency: " + contractDetails.Contract.Currency);
        //    Console.WriteLine("\tMarketName: " + contractDetails.MarketName);
        //    Console.WriteLine("\tTradingClass: " + contractDetails.Contract.TradingClass);
        //    Console.WriteLine("\tConId: " + contractDetails.Contract.ConId);
        //    Console.WriteLine("\tMinTick: " + Util.DoubleMaxString(contractDetails.MinTick));
        //    Console.WriteLine("\tOrderTypes: " + contractDetails.OrderTypes);
        //    Console.WriteLine("\tValidExchanges: " + contractDetails.ValidExchanges);
        //    Console.WriteLine("\tNextOptionDate: " + contractDetails.NextOptionDate);
        //    Console.WriteLine("\tNextOptionType: " + contractDetails.NextOptionType);
        //    Console.WriteLine("\tNextOptionPartial: " + contractDetails.NextOptionPartial);
        //    Console.WriteLine("\tNotes: " + contractDetails.Notes);
        //    Console.WriteLine("\tLong Name: " + contractDetails.LongName);
        //    Console.WriteLine("\tEvRule: " + contractDetails.EvRule);
        //    Console.WriteLine("\tEvMultiplier: " + Util.DoubleMaxString(contractDetails.EvMultiplier));
        //    Console.WriteLine("\tAggGroup: " + Util.IntMaxString(contractDetails.AggGroup));
        //    Console.WriteLine("\tMarketRuleIds: " + contractDetails.MarketRuleIds);
        //    Console.WriteLine("\tLastTradeTime: " + contractDetails.LastTradeTime);
        //    Console.WriteLine("\tTimeZoneId: " + contractDetails.TimeZoneId);
        //    Console.WriteLine("\tMinSize: " + Util.DecimalMaxString(contractDetails.MinSize));
        //    Console.WriteLine("\tSizeIncrement: " + Util.DecimalMaxString(contractDetails.SizeIncrement));
        //    Console.WriteLine("\tSuggestedSizeIncrement: " + Util.DecimalMaxString(contractDetails.SuggestedSizeIncrement));
        //    printContractDetailsSecIdList(contractDetails.SecIdList);
        //}


        public virtual void contractDetailsEnd(int reqId)
        {
            //Console.WriteLine("ContractDetailsEnd. " + reqId + "\n");
        }

        public virtual void execDetails(int reqId, Contract contract, Execution execution)
        {
            _logger?.LogDebug(
                "ExecDetails. ReqId={ReqId}, Symbol={Symbol}, SecType={SecType}, Currency={Currency}, ExecId={ExecId}, OrderId={OrderId}, Shares={Shares}, CumQty={CumQty}, LastLiquidity={LastLiquidity}",
                reqId, contract.Symbol, contract.SecType, contract.Currency, execution.ExecId, execution.OrderId, execution.Shares, execution.CumQty, execution.LastLiquidity);
            try
            {
                ResponseHandle? respH = FetchResponse($"ORDER-{execution.OrderId}");
                OrderStatusDto? orderStatus = respH?.Response?["Result"]?.ToObject<OrderStatusDto>();
                if (orderStatus == null) return;


                // add execution.ExecId to BrokerRef if not already present
                if (!orderStatus.BrokerParams.ContainsKey("ExecId"))
                {
                    orderStatus.BrokerParams.Add("ExecId", execution.ExecId);
                }

                JObject jsonResult = JObject.FromObject(orderStatus);
                JObject jsonObject = new JObject();
                jsonObject["Result"] = jsonResult;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error in execDetails callback for OrderId: {execution.OrderId}");
            } 
        }

        public virtual void execDetailsEnd(int reqId)
        {
            //Console.WriteLine("ExecDetailsEnd. " + reqId + "\n");
        }

        public virtual void commissionAndFeesReport(CommissionAndFeesReport commissionAndFeesReport)
        {
            Console.WriteLine($"CommissionReport. {commissionAndFeesReport.ExecId} - {Util.DoubleMaxString(commissionAndFeesReport.CommissionAndFees)} "
                 + $" {commissionAndFeesReport.Currency} RPNL {Util.DoubleMaxString(commissionAndFeesReport.RealizedPNL)}");

        }

        public virtual void fundamentalData(int reqId, string data)
        {
            //Console.WriteLine("FundamentalData. " + reqId + "" + data + "\n");
        }

        public virtual void historicalData(int reqId, Bar bar)
        {
            List<Bar> bars = new List<Bar>();
            if (historicalDataMap.ContainsKey(reqId))
            {
                bars = historicalDataMap[reqId];
            }
            else
            {
                historicalDataMap.Add(reqId, bars);
            }
            bars.Add(bar);
            _logger?.LogDebug($"HistoricalData - reqId: {reqId} - Time: {bar.Time}, Open: {Util.DoubleMaxString(bar.Open)},  High: {Util.DoubleMaxString(bar.High)}, Low: {Util.DoubleMaxString(bar.Low)}, Close: {Util.DoubleMaxString(bar.Close)}");
        }
        public virtual void historicalDataEnd(int reqId, string startDate, string endDate)
        {
            List<Bar> bars = new List<Bar>();
            if (historicalDataMap.ContainsKey(reqId))
            {
                bars = historicalDataMap[reqId];
            }
            else
            {
                historicalDataMap.Add(reqId, bars);
            }

            //TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            //string timeZone = "EST";
            //if (startDate.Contains("US/Central"))
            //{
            //    timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            //}

            //for (int ndx = 0; ndx < bars.Count; ndx++)
            //{
            //    // Convert the string `Time` to a long Unix timestamp
            //    if (long.TryParse(bars[ndx].Time, out long unixTime))
            //    {
            //        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTime);

            //        // Convert to TimeZone
            //        DateTime dateTimeUTC = TimeZoneInfo.ConvertTime(dateTimeOffset, timeZoneInfo).UtcDateTime;

            //        bars[ndx].Time = new DateTimeOffset(dateTimeUTC).ToUnixTimeSeconds().ToString();
            //    }
            //}

            JArray jsonArray = JArray.FromObject(bars);
            JObject jsonObject = new JObject();
            jsonObject["Result"] = jsonArray;

            CompleteResponse($"REQ-{reqId}", jsonObject);
        }


        public virtual void marketDataType(int reqId, int marketDataType)
        {
            //Console.WriteLine("MarketDataType. " + reqId + ", Type: " + marketDataType + "\n");
        }

        public virtual void updateMktDepth(int tickerId, int position, int operation, int side, double price, decimal size)
        {
            //Console.WriteLine("UpdateMarketDepth. " + tickerId + " - Position: " + position + ", Operation: " + operation + ", Side: " + side + ", Price: " + Util.DoubleMaxString(price) + ", Size: " + Util.DecimalMaxString(size));
        }

        public virtual void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, decimal size, bool isSmartDepth)
        {
            //Console.WriteLine("UpdateMarketDepthL2. " + tickerId + " - Position: " + position + ", Operation: " + operation + ", Side: " + side + ", Price: " + Util.DoubleMaxString(price) + ", Size: " + Util.DecimalMaxString(size) + ", isSmartDepth: " + isSmartDepth);
        }

        public virtual void updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
        {
            //Console.WriteLine("News Bulletins. " + msgId + " - Type: " + msgType + ", Message: " + message + ", Exchange of Origin: " + origExchange + "\n");
        }

        public int ToContractDateInt(string? lastTradeDateOrContractMonth)
        {
            if (string.IsNullOrWhiteSpace(lastTradeDateOrContractMonth))
                return 0;

            try
            {
                var value = lastTradeDateOrContractMonth.Trim();

                if (value.Length == 6)
                {
                    // YYYYMM -> YYYYMM00
                    if (!DateTime.TryParseExact(
                            value,
                            "yyyyMM",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None,
                            out _))
                    {
                        throw new ArgumentException(
                            $"Invalid contract month: {lastTradeDateOrContractMonth}",
                            nameof(lastTradeDateOrContractMonth));
                    }

                    return int.Parse(value + "00");
                }

                if (value.Length == 8)
                {
                    // YYYYMMDD -> YYYYMMDD
                    if (!DateTime.TryParseExact(
                            value,
                            "yyyyMMdd",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None,
                            out _))
                    {
                        throw new ArgumentException(
                            $"Invalid last trade date: {lastTradeDateOrContractMonth}",
                            nameof(lastTradeDateOrContractMonth));
                    }

                    return int.Parse(value);
                }

                throw new ArgumentException(
                    $"Expected YYYYMM or YYYYMMDD but got: {lastTradeDateOrContractMonth}",
                    nameof(lastTradeDateOrContractMonth));
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, $"Error parsing date for lastTradeDateOrContractMonth: {lastTradeDateOrContractMonth}");
            }
            return 0;
        }

        public virtual void position(string account, Contract contract, decimal pos, double avgCost)
        {
            //Console.WriteLine("Position. " + account + " - Symbol: " + contract.Symbol + ", SecType: " + contract.SecType + ", Currency: " + contract.Currency +
            //    ", Position: " + Util.DecimalMaxString(pos) + ", Avg cost: " + Util.DoubleMaxString(avgCost));
            PositionUpdate posUpdate;
            try
            {
                posUpdate = new PositionUpdate()
                {
                    AccountId = account,
                    Ticker = new TickerInfo() {
                        Symbol = contract.Symbol,
                        SecType = contract.SecType,
                        Currency = contract.Currency,
                        Exchange = contract.Exchange,
                        ContractMonth = ToContractDateInt(contract.LastTradeDateOrContractMonth)
                    },
                    Pos = pos,
                    AvgCost = avgCost,
                    UpdateTime = DateTimeUtil.CurrentUTCUnixTime()
                };
                JObject jsonResult = JObject.FromObject(posUpdate);
                OnCallback(new BrokerCallbackMessage
                {
                    Type = BrokerCallbackMessageType.PositionUpdate,
                    DataJson = jsonResult
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"position: failed to extract status for account={account}, contract={contract.Symbol}:{contract.SecType}: {ex.Message}");
                return;
            }
        }

        public virtual void positionEnd()
        {
            //Console.WriteLine("PositionEnd \n");
        }

        public virtual void realtimeBar(int reqId, long time, double open, double high, double low, double close, decimal volume, decimal WAP, int count)
        {
            //Console.WriteLine("RealTimeBars. " + reqId + " - Time: " + Util.LongMaxString(time) + ", Open: " + Util.DoubleMaxString(open) + ", High: " + Util.DoubleMaxString(high) +
            //    ", Low: " + Util.DoubleMaxString(low) + ", Close: " + Util.DoubleMaxString(close) + ", Volume: " + Util.DecimalMaxString(volume) + ", Count: " + Util.IntMaxString(count) +
            //    ", WAP: " + Util.DecimalMaxString(WAP));
        }

        public virtual void scannerParameters(string xml)
        {
            //Console.WriteLine("ScannerParameters. " + xml + "\n");
        }

        public virtual void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
        {
            //Console.WriteLine("ScannerData. " + reqId + " - Rank: " + rank + ", Symbol: " + contractDetails.Contract.Symbol + ", SecType: " + contractDetails.Contract.SecType + ", Currency: " + contractDetails.Contract.Currency
            //    + ", Distance: " + distance + ", Benchmark: " + benchmark + ", Projection: " + projection + ", Legs String: " + legsStr);
        }

        public virtual void scannerDataEnd(int reqId)
        {
            //Console.WriteLine("ScannerDataEnd. " + reqId);
        }

        public virtual void receiveFA(int faDataType, string faXmlData)
        {
            //Console.WriteLine("Receing FA: " + faDataType + " - " + faXmlData);
        }

        public virtual void bondContractDetails(int requestId, ContractDetails contractDetails)
        {
            //Console.WriteLine("BondContractDetails begin. ReqId: " + requestId);
            //printBondContractDetailsMsg(contractDetails);
            //Console.WriteLine("BondContractDetails end. ReqId: " + requestId);
        }

        public virtual void verifyMessageAPI(string apiData)
        {
            //Console.WriteLine("verifyMessageAPI: " + apiData);
        }
        public virtual void verifyCompleted(bool isSuccessful, string errorText)
        {
            //Console.WriteLine("verifyCompleted. IsSuccessfule: " + isSuccessful + " - Error: " + errorText);
        }
        public virtual void verifyAndAuthMessageAPI(string apiData, string xyzChallenge)
        {
            //Console.WriteLine("verifyAndAuthMessageAPI: " + apiData + " " + xyzChallenge);
        }
        public virtual void verifyAndAuthCompleted(bool isSuccessful, string errorText)
        {
            //Console.WriteLine("verifyAndAuthCompleted. IsSuccessful: " + isSuccessful + " - Error: " + errorText);
        }
        //! [displaygrouplist]
        public virtual void displayGroupList(int reqId, string groups)
        {
            //Console.WriteLine("DisplayGroupList. Request: " + reqId + ", Groups" + groups);
        }
        //! [displaygrouplist]

        //! [displaygroupupdated]
        public virtual void displayGroupUpdated(int reqId, string contractInfo)
        {
            //Console.WriteLine("displayGroupUpdated. Request: " + reqId + ", ContractInfo: " + contractInfo);
        }
        //! [displaygroupupdated]

        //! [positionmulti]
        public virtual void positionMulti(int reqId, string account, string modelCode, Contract contract, decimal pos, double avgCost)
        {
            //Console.WriteLine("Position Multi. Request: " + reqId + ", Account: " + account + ", ModelCode: " + modelCode + ", Symbol: " + contract.Symbol + ", SecType: " + contract.SecType +
            //    ", Currency: " + contract.Currency + ", Position: " + Util.DecimalMaxString(pos) + ", Avg cost: " + Util.DoubleMaxString(avgCost) + "\n");
        }
        //! [positionmulti]

        //! [positionmultiend]
        public virtual void positionMultiEnd(int reqId)
        {
            //Console.WriteLine("Position Multi End. Request: " + reqId + "\n");
        }
        //! [positionmultiend]

        //! [accountupdatemulti]
        public virtual void accountUpdateMulti(int reqId, string account, string modelCode, string key, string value, string currency)
        {
            //Console.WriteLine("Account Update Multi. Request: " + reqId + ", Account: " + account + ", ModelCode: " + modelCode + ", Key: " + key + ", Value: " + value + ", Currency: " + currency + "\n");
        }
        //! [accountupdatemulti]

        //! [accountupdatemultiend]
        public virtual void accountUpdateMultiEnd(int reqId)
        {
            //Console.WriteLine("Account Update Multi End. Request: " + reqId + "\n");
        }
        //! [accountupdatemultiend]

        //! [securityDefinitionOptionParameter]
        public void securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass, string multiplier, HashSet<string> expirations, HashSet<double> strikes)
        {
            //Console.WriteLine("Security Definition Option Parameter. Reqest: {0}, Exchange: {1}, Undrelying contract id: {2}, Trading class: {3}, Multiplier: {4}, Expirations: {5}, Strikes: {6}",
            //                  reqId, exchange, Util.IntMaxString(underlyingConId), tradingClass, multiplier, string.Join(", ", expirations), string.Join(", ", strikes));
        }
        //! [securityDefinitionOptionParameter]

        //! [securityDefinitionOptionParameterEnd]
        public void securityDefinitionOptionParameterEnd(int reqId)
        {
            //Console.WriteLine("Security Definition Option Parameter End. Request: " + reqId + "\n");
        }
        //! [securityDefinitionOptionParameterEnd]


        //! [softDollarTiers]
        public void softDollarTiers(int reqId, SoftDollarTier[] tiers)
        {
            //Console.WriteLine("Soft Dollar Tiers:");

            //foreach (var tier in tiers)
            //{
            //    Console.WriteLine(tier.DisplayName);
            //}
        }
        //! [softDollarTiers]

        //! [familyCodes]
        public void familyCodes(FamilyCode[] familyCodes)
        {
            //Console.WriteLine("Family Codes:");

            //foreach (var familyCode in familyCodes)
            //{
            //    Console.WriteLine("Account ID: {0}, Family Code Str: {1}", familyCode.AccountID, familyCode.FamilyCodeStr);
            //}
        }
        //! [familyCodes]

        //! [symbolSamples]
        public void symbolSamples(int reqId, ContractDescription[] contractDescriptions)
        {
            string derivSecTypes;
            //Console.WriteLine("Symbol Samples. Request Id: {0}", reqId);

            foreach (var contractDescription in contractDescriptions)
            {
                derivSecTypes = "";
                foreach (var derivSecType in contractDescription.DerivativeSecTypes)
                {
                    derivSecTypes += derivSecType;
                    derivSecTypes += " ";
                }
                //Console.WriteLine("Contract: conId - {0}, symbol - {1}, secType - {2}, primExchange - {3}, currency - {4}, derivativeSecTypes - {5}, description - {6}, issuerId - {7}",
                //    contractDescription.Contract.ConId, contractDescription.Contract.Symbol, contractDescription.Contract.SecType,
                //    contractDescription.Contract.PrimaryExch, contractDescription.Contract.Currency, derivSecTypes, contractDescription.Contract.Description, contractDescription.Contract.IssuerId);

            }
            JArray jsonArray = JArray.FromObject(contractDescriptions);
            JObject jsonObject = new JObject();
            jsonObject["Result"] = jsonArray;
            historicalDataMap.Remove(reqId);

            CompleteResponse($"REQ-{reqId}", jsonObject);

        }
        //! [symbolSamples]

        //! [mktDepthExchanges]
        public void mktDepthExchanges(DepthMktDataDescription[] depthMktDataDescriptions)
        {
            //Console.WriteLine("Market Depth Exchanges:");

            //foreach (var depthMktDataDescription in depthMktDataDescriptions)
            //{
            //    Console.WriteLine("Depth Market Data Description: Exchange: {0}, Security Type: {1}, Listing Exch: {2}, Service Data Type: {3}, Agg Group: {4}",
            //        depthMktDataDescription.Exchange, depthMktDataDescription.SecType,
            //        depthMktDataDescription.ListingExch, depthMktDataDescription.ServiceDataType,
            //        Util.IntMaxString(depthMktDataDescription.AggGroup));
            //}
        }
        //! [mktDepthExchanges]

        //! [tickNews]
        public void tickNews(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData)
        {
            //Console.WriteLine("Tick News. Ticker Id: {0}, Time Stamp: {1}, Provider Code: {2}, Article Id: {3}, headline: {4}, extraData: {5}", tickerId, Util.LongMaxString(timeStamp), providerCode, articleId, headline, extraData);
        }
        //! [tickNews]

        //! [smartcomponents]
        public void smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap)
        {
            //StringBuilder sb = new StringBuilder();

            //sb.AppendFormat("==== Smart Components Begin (total={0}) reqId = {1} ====\n", theMap.Count, reqId);

            //foreach (var item in theMap)
            //{
            //    sb.AppendFormat("bit number: {0}, exchange: {1}, exchange letter: {2}\n", item.Key, item.Value.Key, item.Value.Value);
            //}

            //sb.AppendFormat("==== Smart Components Begin (total={0}) reqId = {1} ====\n", theMap.Count, reqId);

            //Console.WriteLine(sb);
        }
        //! [smartcomponents]

        //! [tickReqParams]
        public void tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions)
        {
            //Console.WriteLine("id={0} minTick = {1} bboExchange = {2} snapshotPermissions = {3}", tickerId, Util.DoubleMaxString(minTick), bboExchange, Util.IntMaxString(snapshotPermissions));
        }
        //! [tickReqParams]

        //! [newsProviders]
        public void newsProviders(NewsProvider[] newsProviders)
        {
            //Console.WriteLine("News Providers:");

            //foreach (var newsProvider in newsProviders)
            //{
            //    Console.WriteLine("News provider: providerCode - {0}, providerName - {1}",
            //        newsProvider.ProviderCode, newsProvider.ProviderName);
            //}
        }
        //! [newsProviders]

        //! [newsArticle]
        public void newsArticle(int requestId, int articleType, string articleText)
        {
            //Console.WriteLine("News Article. Request Id: {0}, ArticleType: {1}", requestId, articleType);
            //if (articleType == 0)
            //{
            //    Console.WriteLine("News Article Text: {0}", articleText);
            //}
            //else if (articleType == 1)
            //{
            //    Console.WriteLine("News Article Text: article text is binary/pdf and cannot be displayed");
            //}
        }
        //! [newsArticle]

        //! [historicalNews]
        public void historicalNews(int requestId, string time, string providerCode, string articleId, string headline)
        {
            //Console.WriteLine("Historical News. Request Id: {0}, Time: {1}, Provider Code: {2}, Article Id: {3}, headline: {4}", requestId, time, providerCode, articleId, headline);
        }
        //! [historicalNews]

        //! [historicalNewsEnd]
        public void historicalNewsEnd(int requestId, bool hasMore)
        {
            //Console.WriteLine("Historical News End. Request Id: {0}, Has More: {1}", requestId, hasMore);
        }
        //! [historicalNewsEnd]

        //! [headTimestamp]
        public void headTimestamp(int reqId, string headTimestamp)
        {
            //Console.WriteLine("Head time stamp. Request Id: {0}, Head time stamp: {1}", reqId, headTimestamp);
        }
        //! [headTimestamp]

        //! [histogramData]
        public void histogramData(int reqId, HistogramEntry[] data)
        {
            //Console.WriteLine("Histogram data. Request Id: {0}, data size: {1}", reqId, data.Length);
            //data.ToList().ForEach(i => Console.WriteLine("\tPrice: {0}, Size: {1}", Util.DoubleMaxString(i.Price), Util.DecimalMaxString(i.Size)));
        }
        //! [histogramData]

        //! [historicalDataUpdate]
        public void historicalDataUpdate(int reqId, Bar bar)
        {
            //Console.WriteLine("HistoricalDataUpdate. " + reqId + " - Time: " + bar.Time + ", Open: " + Util.DoubleMaxString(bar.Open) + ", High: " + Util.DoubleMaxString(bar.High) +
            //    ", Low: " + Util.DoubleMaxString(bar.Low) + ", Close: " + Util.DoubleMaxString(bar.Close) + ", Volume: " + Util.DecimalMaxString(bar.Volume) +
            //    ", Count: " + Util.IntMaxString(bar.Count) + ", WAP: " + Util.DecimalMaxString(bar.WAP));
            // todo.. maybe we want this
        }
        //! [historicalDataUpdate]

        //! [rerouteMktDataReq]
        public void rerouteMktDataReq(int reqId, int conId, string exchange)
        {
            //Console.WriteLine("Re-route market data request. Req Id: {0}, ConId: {1}, Exchange: {2}", reqId, conId, exchange);
        }
        //! [rerouteMktDataReq]

        //! [rerouteMktDepthReq]
        public void rerouteMktDepthReq(int reqId, int conId, string exchange)
        {
            //Console.WriteLine("Re-route market depth request. Req Id: {0}, ConId: {1}, Exchange: {2}", reqId, conId, exchange);
        }
        //! [rerouteMktDepthReq]

        //! [marketRule]
        public void marketRule(int marketRuleId, PriceIncrement[] priceIncrements)
        {
            //Console.WriteLine("Market Rule Id: " + marketRuleId);
            //foreach (var priceIncrement in priceIncrements)
            //{
            //    Console.WriteLine("Low Edge: {0}, Increment: {1}", Util.DoubleMaxString(priceIncrement.LowEdge), Util.DoubleMaxString(priceIncrement.Increment));
            //}
        }
        //! [marketRule]

        //! [pnl]
        public void pnl(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL)
        {
            //Console.WriteLine("PnL. Request Id: {0}, Daily PnL: {1}, Unrealized PnL: {2}, Realized PnL: {3}", reqId, Util.DoubleMaxString(dailyPnL), Util.DoubleMaxString(unrealizedPnL), Util.DoubleMaxString(realizedPnL));
        }
        //! [pnl]

        //! [pnlsingle]
        public void pnlSingle(int reqId, decimal pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value)
        {
            //Console.WriteLine("PnL Single. Request Id: {0}, Pos {1}, Daily PnL {2}, Unrealized PnL {3}, Realized PnL: {4}, Value: {5}", reqId, Util.DecimalMaxString(pos), Util.DoubleMaxString(dailyPnL), Util.DoubleMaxString(unrealizedPnL),
            //    Util.DoubleMaxString(realizedPnL), Util.DoubleMaxString(value));
        }
        //! [pnlsingle]

        //! [historicalticks]
        public void historicalTicks(int reqId, HistoricalTick[] ticks, bool done)
        {
            //foreach (var tick in ticks)
            //{
            //    Console.WriteLine("Historical Tick. Request Id: {0}, Time: {1}, Price: {2}, Size: {3}", reqId, Util.UnixSecondsToString(tick.Time, "yyyyMMdd-HH:mm:ss"), Util.DoubleMaxString(tick.Price), Util.DecimalMaxString(tick.Size));
            //}
        }
        //! [historicalticks]

        //! [historicalticksbidask]
        public void historicalTicksBidAsk(int reqId, HistoricalTickBidAsk[] ticks, bool done)
        {
            //foreach (var tick in ticks)
            //{
            //    Console.WriteLine("Historical Tick Bid/Ask. Request Id: {0}, Time: {1}, Price Bid: {2}, Price Ask: {3}, Size Bid: {4}, Size Ask: {5}, Bid/Ask Tick Attribs: {6} ",
            //        reqId, Util.UnixSecondsToString(tick.Time, "yyyyMMdd-HH:mm:ss"), Util.DoubleMaxString(tick.PriceBid), Util.DoubleMaxString(tick.PriceAsk),
            //        Util.DecimalMaxString(tick.SizeBid), Util.DecimalMaxString(tick.SizeAsk), tick.TickAttribBidAsk);
            //}
        }
        //! [historicalticksbidask]

        //! [historicaltickslast]
        public void historicalTicksLast(int reqId, HistoricalTickLast[] ticks, bool done)
        {
            //foreach (var tick in ticks)
            //{
            //    Console.WriteLine("Historical Tick Last. Request Id: {0}, Time: {1}, Price: {2}, Size: {3}, Exchange: {4}, Special Conditions: {5}, Last Tick Attribs: {6} ",
            //        reqId, Util.UnixSecondsToString(tick.Time, "yyyyMMdd-HH:mm:ss"), Util.DoubleMaxString(tick.Price), Util.DecimalMaxString(tick.Size), tick.Exchange, tick.SpecialConditions, tick.TickAttribLast);
            //}
        }
        //! [historicaltickslast]

        public void tickByTickAllLast(int reqId, int tickType, long time, double price, decimal size, TickAttribLast tickAttribLast, string exchange, string specialConditions)
        {
            MarketDataDto data = new MarketDataDto()
            {
                TickerId = reqId,
                Price = (decimal) price,
                Volume = (int)size,
                DataTime = time
            };
            JObject jsonResult = JObject.FromObject(data);
            OnCallback(new BrokerCallbackMessage
            {
                Type = BrokerCallbackMessageType.MarketData,
                DataJson = jsonResult
            });


            //Console.WriteLine("Tick-By-Tick. Request Id: {0}, TickType: {1}, Time: {2}, Price: {3}, Size: {4}, Exchange: {5}, Special Conditions: {6}, PastLimit: {7}, Unreported: {8}",
            //    reqId, tickType == 1 ? "Last" : "AllLast", Util.UnixSecondsToString(time, "yyyyMMdd-HH:mm:ss"), Util.DoubleMaxString(price), Util.DecimalMaxString(size), exchange, specialConditions, tickAttribLast.PastLimit, tickAttribLast.Unreported);
        }

        //! [tickbytickbidask]
        public void tickByTickBidAsk(int reqId, long time, double bidPrice, double askPrice, decimal bidSize, decimal askSize, TickAttribBidAsk tickAttribBidAsk)
        {
            //Console.WriteLine("Tick-By-Tick. Request Id: {0}, TickType: BidAsk, Time: {1}, BidPrice: {2}, AskPrice: {3}, BidSize: {4}, AskSize: {5}, BidPastLow: {6}, AskPastHigh: {7}",
            //    reqId, Util.UnixSecondsToString(time, "yyyyMMdd-HH:mm:ss"), Util.DoubleMaxString(bidPrice), Util.DoubleMaxString(askPrice), Util.DecimalMaxString(bidSize), Util.DecimalMaxString(askSize), tickAttribBidAsk.BidPastLow, tickAttribBidAsk.AskPastHigh);
        }
        //! [tickbytickbidask]

        //! [tickbytickmidpoint]
        public void tickByTickMidPoint(int reqId, long time, double midPoint)
        {
            //Console.WriteLine("Tick-By-Tick. Request Id: {0}, TickType: MidPoint, Time: {1}, MidPoint: {2}",
            //    reqId, Util.UnixSecondsToString(time, "yyyyMMdd-HH:mm:ss"), Util.DoubleMaxString(midPoint));
        }
        //! [tickbytickmidpoint]

        //! [orderbound]
        public void orderBound(long orderId, int apiClientId, int apiOrderId)
        {
            Console.WriteLine("Order bound. Order Id: {0}, Api Client Id: {1}, Api Order Id: {2}", Util.LongMaxString(orderId), Util.IntMaxString(apiClientId), Util.IntMaxString(apiOrderId));
        }
        //! [orderbound]

        //! [replacefaend]
        public virtual void replaceFAEnd(int reqId, string text)
        {
            //Console.WriteLine("Replace FA End. ReqId: " + reqId + ", Text: " + text + "\n");
        }
        //! [replacefaend]

        //! [wshMetaData]
        public void wshMetaData(int reqId, string dataJson)
        {
            //Console.WriteLine($"WSH Meta Data. Request Id: {reqId}, Data JSON: {dataJson}\n");
        }
        //! [wshMetaData]

        //! [wshEventData]
        public void wshEventData(int reqId, string dataJson)
        {
            //Console.WriteLine($"WSH Event Data. Request Id: {reqId}, Data JSON: {dataJson}\n");
        }
        //! [wshEventData]

        //! [historicalSchedule]
        public void historicalSchedule(int reqId, string startDateTime, string endDateTime, string timeZone, HistoricalSession[] sessions)
        {
            //Console.WriteLine($"Historical Schedule. ReqId: {reqId}, Start: {startDateTime}, End: {endDateTime}, Time Zone: {timeZone}");

            //foreach (var session in sessions)
            //{
            //    Console.WriteLine($"\tSession. Start: {session.StartDateTime}, End: {session.EndDateTime}, Ref Date: {session.RefDate}");
            //}
        }
        //! [historicalSchedule]

        //! [userInfo]
        public void userInfo(int reqId, string whiteBrandingId)
        {
            //Console.WriteLine($"User Info. ReqId: {reqId}, WhiteBrandingId: {whiteBrandingId}");
        }
        //! [userInfo]

        public void currentTimeInMillis(long timeInMillis)
        {
            // throw new NotImplementedException();
        }

        public void orderStatusProtoBuf(IBApi.protobuf.OrderStatus orderStatusProto)
        {
            // throw new NotImplementedException();
        }

        public void openOrderProtoBuf(IBApi.protobuf.OpenOrder openOrderProto)
        {
            // throw new NotImplementedException();
        }

        public void openOrdersEndProtoBuf(IBApi.protobuf.OpenOrdersEnd openOrdersEndProto)
        {
            // throw new NotImplementedException();
        }

        public void errorProtoBuf(IBApi.protobuf.ErrorMessage errorMessageProto)
        {
            // throw new NotImplementedException();
        }

        public void execDetailsProtoBuf(IBApi.protobuf.ExecutionDetails executionDetailsProto)
        {
            // throw new NotImplementedException();
        }

        public void execDetailsEndProtoBuf(IBApi.protobuf.ExecutionDetailsEnd executionDetailsEndProto)
        {
            // throw new NotImplementedException();
        }

        public void completedOrderProtoBuf(IBApi.protobuf.CompletedOrder completedOrderProto)
        {
            _logger?.LogDebug(
                "CompletedOrder. Symbol={Symbol}, SecType={SecType}, Currency={Currency}, ExecId={ExecId}, OrderId={OrderId}",
                completedOrderProto.Contract.Symbol, completedOrderProto.Contract.SecType, completedOrderProto.Contract.Currency, 
                completedOrderProto.Order, completedOrderProto.Order.OrderId);
            // throw new NotImplementedException();
        }

        public void completedOrdersEndProtoBuf(IBApi.protobuf.CompletedOrdersEnd completedOrdersEndProto)
        {
            // throw new NotImplementedException();
        }

        public void orderBoundProtoBuf(IBApi.protobuf.OrderBound orderBoundProto)
        {
            // throw new NotImplementedException();
        }

        public void contractDataProtoBuf(IBApi.protobuf.ContractData contractDataProto)
        {
            // throw new NotImplementedException();
        }

        public void bondContractDataProtoBuf(IBApi.protobuf.ContractData contractDataProto)
        {
            // throw new NotImplementedException();
        }

        public void contractDataEndProtoBuf(IBApi.protobuf.ContractDataEnd contractDataEndProto)
        {
            // throw new NotImplementedException();
        }

        public void tickPriceProtoBuf(IBApi.protobuf.TickPrice tickPriceProto)
        {
            // throw new NotImplementedException();
        }

        public void tickSizeProtoBuf(IBApi.protobuf.TickSize tickSizeProto)
        {
            // throw new NotImplementedException();
        }

        public void tickOptionComputationProtoBuf(IBApi.protobuf.TickOptionComputation tickOptionComputationProto)
        {
            // throw new NotImplementedException();
        }

        public void tickGenericProtoBuf(IBApi.protobuf.TickGeneric tickGenericProto)
        {
            // throw new NotImplementedException();
        }

        public void tickStringProtoBuf(IBApi.protobuf.TickString tickStringProto)
        {
            // throw new NotImplementedException();
        }

        public void tickSnapshotEndProtoBuf(IBApi.protobuf.TickSnapshotEnd tickSnapshotEndProto)
        {
            // throw new NotImplementedException();
        }

        public void updateMarketDepthProtoBuf(IBApi.protobuf.MarketDepth marketDepthProto)
        {
            // throw new NotImplementedException();
        }

        public void updateMarketDepthL2ProtoBuf(IBApi.protobuf.MarketDepthL2 marketDepthL2Proto)
        {
            // throw new NotImplementedException();
        }

        public void marketDataTypeProtoBuf(IBApi.protobuf.MarketDataType marketDataTypeProto)
        {
            // throw new NotImplementedException();
        }

        public void tickReqParamsProtoBuf(IBApi.protobuf.TickReqParams tickReqParamsProto)
        {
            // throw new NotImplementedException();
        }

        public void updateAccountValueProtoBuf(IBApi.protobuf.AccountValue accountValueProto)
        {
            // throw new NotImplementedException();
        }

        public void updatePortfolioProtoBuf(IBApi.protobuf.PortfolioValue portfolioValueProto)
        {
            // throw new NotImplementedException();
        }

        public void updateAccountTimeProtoBuf(IBApi.protobuf.AccountUpdateTime accountUpdateTimeProto)
        {
            // throw new NotImplementedException();
        }

        public void accountDataEndProtoBuf(IBApi.protobuf.AccountDataEnd accountDataEndProto)
        {
            // throw new NotImplementedException();
        }

        public void managedAccountsProtoBuf(IBApi.protobuf.ManagedAccounts managedAccountsProto)
        {
            // throw new NotImplementedException();
        }

        public void positionProtoBuf(IBApi.protobuf.Position positionProto)
        {
            // throw new NotImplementedException();
        }

        public void positionEndProtoBuf(IBApi.protobuf.PositionEnd positionEndProto)
        {
            // throw new NotImplementedException();
        }

        public void accountSummaryProtoBuf(IBApi.protobuf.AccountSummary accountSummaryProto)
        {
            // throw new NotImplementedException();
        }

        public void accountSummaryEndProtoBuf(IBApi.protobuf.AccountSummaryEnd accountSummaryEndProto)
        {
            // throw new NotImplementedException();
        }

        public void positionMultiProtoBuf(IBApi.protobuf.PositionMulti positionMultiProto)
        {
            // throw new NotImplementedException();
        }

        public void positionMultiEndProtoBuf(IBApi.protobuf.PositionMultiEnd positionMultiEndProto)
        {
            // throw new NotImplementedException();
        }

        public void accountUpdateMultiProtoBuf(IBApi.protobuf.AccountUpdateMulti accountUpdateMultiProto)
        {
            // throw new NotImplementedException();
        }

        public void accountUpdateMultiEndProtoBuf(IBApi.protobuf.AccountUpdateMultiEnd accountUpdateMultiEndProto)
        {
            // throw new NotImplementedException();
        }

        public void historicalDataProtoBuf(IBApi.protobuf.HistoricalData historicalDataProto)
        {
            // throw new NotImplementedException();
        }

        public void historicalDataUpdateProtoBuf(IBApi.protobuf.HistoricalDataUpdate historicalDataUpdateProto)
        {
            // throw new NotImplementedException();
        }

        public void historicalDataEndProtoBuf(IBApi.protobuf.HistoricalDataEnd historicalDataEndProto)
        {
            // throw new NotImplementedException();
        }

        public void realTimeBarTickProtoBuf(IBApi.protobuf.RealTimeBarTick realTimeBarTickProto)
        {
            // throw new NotImplementedException();
        }

        public void headTimestampProtoBuf(IBApi.protobuf.HeadTimestamp headTimestampProto)
        {
            // throw new NotImplementedException();
        }

        public void histogramDataProtoBuf(IBApi.protobuf.HistogramData histogramDataProto)
        {
            // throw new NotImplementedException();
        }

        public void historicalTicksProtoBuf(IBApi.protobuf.HistoricalTicks historicalTicksProto)
        {
            // throw new NotImplementedException();
        }

        public void historicalTicksBidAskProtoBuf(IBApi.protobuf.HistoricalTicksBidAsk historicalTicksBidAskProto)
        {
            // throw new NotImplementedException();
        }

        public void historicalTicksLastProtoBuf(IBApi.protobuf.HistoricalTicksLast historicalTicksLastProto)
        {
            // throw new NotImplementedException();
        }

        public void tickByTickDataProtoBuf(IBApi.protobuf.TickByTickData tickByTickDataProto)
        {
            // throw new NotImplementedException();
        }

        public void updateNewsBulletinProtoBuf(IBApi.protobuf.NewsBulletin newsBulletinProto)
        {
            // throw new NotImplementedException();
        }

        public void newsArticleProtoBuf(IBApi.protobuf.NewsArticle newsArticleProto)
        {
            // throw new NotImplementedException();
        }

        public void newsProvidersProtoBuf(IBApi.protobuf.NewsProviders newsProvidersProto)
        {
            // throw new NotImplementedException();
        }

        public void historicalNewsProtoBuf(IBApi.protobuf.HistoricalNews historicalNewsProto)
        {
            // throw new NotImplementedException();
        }

        public void historicalNewsEndProtoBuf(IBApi.protobuf.HistoricalNewsEnd historicalNewsEndProto)
        {
            // throw new NotImplementedException();
        }

        public void wshMetaDataProtoBuf(IBApi.protobuf.WshMetaData wshMetaDataProto)
        {
            // throw new NotImplementedException();
        }

        public void wshEventDataProtoBuf(IBApi.protobuf.WshEventData wshEventDataProto)
        {
            // throw new NotImplementedException();
        }

        public void tickNewsProtoBuf(IBApi.protobuf.TickNews tickNewsProto)
        {
            // throw new NotImplementedException();
        }

        public void scannerParametersProtoBuf(IBApi.protobuf.ScannerParameters scannerParametersProto)
        {
            // throw new NotImplementedException();
        }

        public void scannerDataProtoBuf(IBApi.protobuf.ScannerData scannerDataProto)
        {
            // throw new NotImplementedException();
        }

        public void fundamentalsDataProtoBuf(IBApi.protobuf.FundamentalsData fundamentalsDataProto)
        {
            // throw new NotImplementedException();
        }

        public void pnlProtoBuf(IBApi.protobuf.PnL pnlProto)
        {
            // throw new NotImplementedException();
        }

        public void pnlSingleProtoBuf(IBApi.protobuf.PnLSingle pnlSingleProto)
        {
            // throw new NotImplementedException();
        }

        public void receiveFAProtoBuf(IBApi.protobuf.ReceiveFA receiveFAProto)
        {
            // throw new NotImplementedException();
        }

        public void replaceFAEndProtoBuf(IBApi.protobuf.ReplaceFAEnd replaceFAEndProto)
        {
            // throw new NotImplementedException();
        }

        public void commissionAndFeesReportProtoBuf(IBApi.protobuf.CommissionAndFeesReport commissionAndFeesReportProto)
        {
            // throw new NotImplementedException();
        }

        public void historicalScheduleProtoBuf(IBApi.protobuf.HistoricalSchedule historicalScheduleProto)
        {
            // throw new NotImplementedException();
        }

        public void rerouteMarketDataRequestProtoBuf(IBApi.protobuf.RerouteMarketDataRequest rerouteMarketDataRequestProto)
        {
            // throw new NotImplementedException();
        }

        public void rerouteMarketDepthRequestProtoBuf(IBApi.protobuf.RerouteMarketDepthRequest rerouteMarketDepthRequestProto)
        {
            // throw new NotImplementedException();
        }

        public void secDefOptParameterProtoBuf(IBApi.protobuf.SecDefOptParameter secDefOptParameterProto)
        {
            // throw new NotImplementedException();
        }

        public void secDefOptParameterEndProtoBuf(IBApi.protobuf.SecDefOptParameterEnd secDefOptParameterEndProto)
        {
            // throw new NotImplementedException();
        }

        public void softDollarTiersProtoBuf(IBApi.protobuf.SoftDollarTiers softDollarTiersProto)
        {
            // throw new NotImplementedException();
        }

        public void familyCodesProtoBuf(IBApi.protobuf.FamilyCodes familyCodesProto)
        {
            // throw new NotImplementedException();
        }

        public void symbolSamplesProtoBuf(IBApi.protobuf.SymbolSamples symbolSamplesProto)
        {
            // throw new NotImplementedException();
        }

        public void smartComponentsProtoBuf(IBApi.protobuf.SmartComponents smartComponentsProto)
        {
            // throw new NotImplementedException();
        }

        public void marketRuleProtoBuf(IBApi.protobuf.MarketRule marketRuleProto)
        {
            // throw new NotImplementedException();
        }

        public void userInfoProtoBuf(IBApi.protobuf.UserInfo userInfoProto)
        {
            // throw new NotImplementedException();
        }

        public void nextValidIdProtoBuf(IBApi.protobuf.NextValidId nextValidIdProto)
        {
            // throw new NotImplementedException();
        }

        public void currentTimeProtoBuf(IBApi.protobuf.CurrentTime currentTimeProto)
        {
            // throw new NotImplementedException();
        }

        public void currentTimeInMillisProtoBuf(IBApi.protobuf.CurrentTimeInMillis currentTimeInMillisProto)
        {
            // throw new NotImplementedException();
        }

        public void verifyMessageApiProtoBuf(IBApi.protobuf.VerifyMessageApi verifyMessageApiProto)
        {
            // throw new NotImplementedException();
        }

        public void verifyCompletedProtoBuf(IBApi.protobuf.VerifyCompleted verifyCompletedProto)
        {
            // throw new NotImplementedException();
        }

        public void displayGroupListProtoBuf(IBApi.protobuf.DisplayGroupList displayGroupListProto)
        {
            // throw new NotImplementedException();
        }

        public void displayGroupUpdatedProtoBuf(IBApi.protobuf.DisplayGroupUpdated displayGroupUpdatedProto)
        {
            // throw new NotImplementedException();
        }

        public void marketDepthExchangesProtoBuf(IBApi.protobuf.MarketDepthExchanges marketDepthExchangesProto)
        {
            // throw new NotImplementedException();
        }

        public void configResponseProtoBuf(IBApi.protobuf.ConfigResponse configResponseProto)
        {
            // throw new NotImplementedException();
        }

        public void updateConfigResponseProtoBuf(IBApi.protobuf.UpdateConfigResponse updateConfigResponseProto)
        {
            // throw new NotImplementedException();
        }


        public void Dispose()
        {
            foreach (var kvp in _responseHandlerMap)
            {
                kvp.Value.Dispose();
            }

            _responseHandlerMap.Clear();
        }
    }
}
