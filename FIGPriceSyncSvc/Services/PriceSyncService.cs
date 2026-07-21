using FIGCommon.DataAccess;
using FIGCommon.Exceptions;
using FIGCommon.Interfaces;
using FIGCommon.Models;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Services;
using FIGCommon.Tasks;
using FIGCommon.Utilities;
using FIGCommon.Utilities.FIGProviderAPI;
using FIGPriceSyncSvc.Model;
using FIGPriceSyncSvc.Models;
using Newtonsoft.Json;
using System.Data;

namespace FIGPriceSyncSvc.Services
{
    public class PriceSyncService : IHostedService, IDisposable
    {
        private readonly ILogger<PriceSyncService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _config;
        private readonly IProviderService? defaultProvider;
        private readonly ITaskSchedulerService _taskScheduler;

        private const string SRC_NAME = "PriceSyncService";

        private readonly object _lockObj = new();
        private readonly object _lockMarketDataObj = new();
        private Dictionary<int, IBKRDataSet> mapDataSets = new Dictionary<int, IBKRDataSet>();
        private Dictionary<int, TickerRS> mapMarketDataTicker = new Dictionary<int, TickerRS>();
        private readonly ProvidersConfig _providersConfig = new ProvidersConfig();
        private readonly ProviderConfig? _defaultProviderConfig = new ProviderConfig();

        public PriceSyncService(
            ILogger<PriceSyncService> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ITaskSchedulerService taskScheduler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _taskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
            _config.GetSection("ProviderServices").Bind(_providersConfig);
            defaultProvider = GetActiveProvider();
            if (defaultProvider != null)
            {
                _defaultProviderConfig = _providersConfig.Providers.FirstOrDefault(p => p.Id == defaultProvider.Id);
            }
        }

        public IProviderService? GetActiveProvider()
        {
            // IProviderService is registered as a keyed singleton — resolve by its normalized key
            var providerKey = _providersConfig.ActiveProvider?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(providerKey))
            {
                _logger.LogError("No active provider configured");
                return null;
            }

            var activeProvider = _serviceProvider.GetKeyedService<IProviderService>(providerKey);
            if (activeProvider == null)
            {
                _logger.LogError("No active provider found for key {ProviderKey}", providerKey);
                return null;
            }
            return activeProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service Started");
            ReloadSubscriptions();
            _ = HandleRestart(null, null!);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service is Stopping");
            _taskScheduler.StopAllTasksAsync(SRC_NAME).Wait();
            return Task.CompletedTask;
        }

        public Task Refresh()
        {
            lock (_lockObj)
            {
                foreach (KeyValuePair<int, IBKRDataSet> datasetItem in mapDataSets)
                {
                    if (datasetItem.Value != null)
                    {
                        datasetItem.Value.lastSyncTime = 0L;
                    }
                }
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _taskScheduler.StopAllTasksAsync(SRC_NAME).Wait();
            GC.SuppressFinalize(this);
        }


        // Buisness Logic here
        private async Task GetDataSets()
        {
            if (_defaultProviderConfig == null)
            {
                return;
            }
            try
            {
                var dataSets = MainRepo.GetDataSets();
                if (dataSets == null || dataSets.Count == 0)
                {
                    _logger?.LogDebug("Dataset is not setup yet, trying again in 2 minutes seconds...");
                    await _taskScheduler.ScheduleEventAsync(SRC_NAME, "Restart", 120000, HandleRestart);
                }
                lock (_lockObj)
                {
                    mapDataSets.Clear();
                    dataSets?.ForEach(dataset =>
                    {
                        if (_defaultProviderConfig.DataSetIds.Contains(dataset.Id))
                        {
                            string ibkrInterval = IBKRIntervalDef.GetInterval(dataset.Interval.Id);
                            if (ibkrInterval != "")
                            {
                                var ibkrDataSet = new IBKRDataSet(dataset, ibkrInterval);
                                mapDataSets.Add(dataset.Id, ibkrDataSet);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogCritical("GetDataSets: Exception accessing database... trying again in 2 minutes seconds... - {0}", ex.Message);
                await _taskScheduler.ScheduleEventAsync(SRC_NAME, "Restart", 120000, HandleRestart);
                return;
            }

            // finally sched for synch
            foreach (KeyValuePair<int, IBKRDataSet> datasetItem in mapDataSets)
            {
                await _taskScheduler.ScheduleEventAsync(SRC_NAME, $"SyncDataSet_{datasetItem.Key}", 0, HandleSyncDataSet, datasetItem.Key);
            }
        }


        private bool SyncPrices(IBKRDataSet dataset, long lastPriceRawDate, bool updateOnly)
        {
            if (dataset.Dataset.Ticker.SecurityType == "FUT")
            {
                _logger?.LogDebug("SyncPrices{0}: Syncing prices for ticker ({1}), dataset ({2})", (updateOnly ? "(Update)" : ""),
                    dataset.Dataset.Ticker.Symbol, dataset.Dataset.Id);

                var bars = HistoricalData(dataset.Dataset, dataset.IBKRInterval, lastPriceRawDate);
                if (bars != null)
                {
                    List<PriceDataRS> updateBars = new();
                    List<PriceDataRS> newBars = new();
                    for (int ndx = 0; ndx < bars.Count; ndx++)
                    {
                        if (bars[ndx].PriceDate == DateTime.MinValue)
                        {
                            // get PriceDate from Unix RawTime (bars[ndx].RawTime) in UTC
                            bars[ndx].PriceDate = DateTimeUtil.ConvertUnixTimeToDateTime(bars[ndx].RawTime);
                        }
                        if (bars[ndx].RawTime <= dataset.lastSyncTime && bars[ndx].RawTime > lastPriceRawDate)
                        {
                            updateBars.Add(bars[ndx]);
                        }
                        else if (bars[ndx].RawTime > dataset.lastSyncTime)
                        {
                            newBars.Add(bars[ndx]);
                        }
                    }
                    try
                    {
                        if (updateBars.Count > 0)
                        {
                            MainRepo.UpsertPriceData(updateBars);
                        }
                        if (newBars.Count > 0 && !updateOnly)
                        {
                            MainRepo.BulkPriceAppendEx(newBars);
                            //dataset.lastSyncTime = newBars[newBars.Count - 1].RawTime;
                        }
                        try
                        {
                            dataset.lastSyncTime = MainRepo.GetLastPriceData(dataset.Dataset.Id)?.RawTime ?? 0L;
                        }
                        catch (Exception exLastSyncTime)
                        {
                            _logger?.LogError("SyncPrices: Exception retrieving lastSyncTime - {0}", exLastSyncTime.Message);
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError("SyncPrices: Exception occured - {0}", ex.Message);
                        return false;
                    }
                }
            }
            return false;
        }

        public List<PriceDataRS>? HistoricalData(DataSetRS dataset, string intervalProvider, long lastRawTime)
        {
            List<BarData> barData = new();
            try
            {
                long curRawTime = DateTimeUtil.ToUnixTime(DateTime.UtcNow);
                long deltaTime = curRawTime - lastRawTime;
                int durationInSec = (((int)deltaTime / dataset.Interval.IntervalLen) + 1) * dataset.Interval.IntervalLen;
                //durationInSec = durationInSec > 86400 ? 86400 : durationInSec;
                string intervalType;
                int intervalValue;
                IntervalRS.GetIntervalParams(dataset.Interval.Id, out intervalType, out intervalValue);
                string duration = $"{durationInSec} S";
                if (intervalType == "SEC" && intervalValue == 1 && durationInSec > 2000)
                {
                    duration = "2000 S";
                }
                else if (durationInSec > 86400)
                {
                    int durationInDays = durationInSec / 86400 + 1;
                    durationInDays = durationInDays > 200 ? 200 : durationInDays;
                    duration = $"{durationInDays} D";
                }

                HistoricalDataRequest request = new HistoricalDataRequest()
                {
                    Ticker =
                    {
                        Symbol = dataset.Ticker.LocalSymbol,
                        SecType = dataset.Ticker.SecurityType,
                        Currency = dataset.Ticker.Currency,
                        Exchange = dataset.Ticker.Exchange,
                        ContractMonth = dataset.Ticker.ExpiryDate
                    },
                    IntervalId = intervalProvider,
                    StartRawTime = curRawTime,
                    Duration = duration
                };
                if (defaultProvider != null)
                {
                    var barDataReq = defaultProvider.HistoricalDataRequest(request);
                    return barDataReq?.Select(bar =>
                    {
                        var newPrice = new PriceDataRS()
                        {
                            DataSetId = dataset.Id,
                            RawTime = bar.RawTime,
                            Open = (decimal)bar.Open,
                            High = (decimal)bar.High,
                            Low = (decimal)bar.Low,
                            Close = (decimal)bar.Close,
                            Volume = (int)bar.Volume
                        };
                        return newPrice;
                    }).ToList();
                }
            }
            catch (ProviderException ex)
            {
                if (ex.ProviderErrorCode == (int)ProviderErrorCodes.NO_DATA)
                {
                    return new List<PriceDataRS>();
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e.Message);
            }
            return null;
        }

        private int GetNextSchedTime(IBKRDataSet dataset)
        {
            //int lag = 500 + _providersConfig.TimeOffsetSec * 1000; // 500 msec + timeOffset msec
            //int intervalInSeconds = dataset.Dataset.Interval.IntervalLen;
            //int nextTime = (intervalInSeconds - (DateTime.Now.Second % dataset.Dataset.Interval.IntervalLen)) * 1000 - lag;
            //int nextNextTime = (intervalInSeconds * 2 - (DateTime.Now.Second % dataset.Dataset.Interval.IntervalLen)) * 1000 - lag;

            //return nextTime < 0 ? nextNextTime : nextTime;
            return GetNextSchedTime(_providersConfig.TimeOffsetSec, dataset.Dataset.Interval.IntervalLen);
        }

        private static int GetNextSchedTime(int timeOffsetSec, int intervalInSeconds)
        {
            var now = DateTime.UtcNow;

            int lagMs = 500 + timeOffsetSec * 1000;

            int currentMs = (now.Minute * 60 + now.Second) * 1000 + now.Millisecond;
            int intervalMs = intervalInSeconds * 1000;

            int nextBoundaryMs = ((currentMs / intervalMs) + 1) * intervalMs;

            int delay = nextBoundaryMs - currentMs - lagMs;

            if (delay < 0)
            {
                delay += intervalMs;
            }

            return delay;
        }

        public List<ContractInfo> LookupSymbol(string symbol)
        {
            List<ContractInfo> lst = new();
            try
            {
                var infoLst = defaultProvider?.SymbolLookup(symbol);
                if (infoLst == null || infoLst.Count == 0)
                {
                    return lst;
                }
                return infoLst;
            }
            catch (ProviderException eProvider)
            {
                _logger?.LogError("Exception in LookupSymbol - error code ({0})  {1}", eProvider.ProviderErrorCode, eProvider.Message);
                throw new AppErrorException(ErrorCodes.ApiService_General, eProvider.ProviderErrorCode.ToString(), eProvider.Message);
            }
            catch (Exception e)
            {
                _logger?.LogError("Exception in LookupSymbol - {0}", e.Message);
                throw new AppErrorException(ErrorCodes.ApiService_General, "", e.Message);
            }
        }

        public ContractInfo? FutureContractDetails(InstrumentLookupRequest lookupContract)
        {
            try
            {
                return defaultProvider?.ContractInfo(lookupContract);
            }
            catch (ProviderException eProvider)
            {
                _logger?.LogError("Exception in FutureContractDetails - error code ({0})  {1}", eProvider.ProviderErrorCode, eProvider.Message);
                throw new AppErrorException(ErrorCodes.ApiService_General, eProvider.ProviderErrorCode.ToString(), eProvider.Message);
            }
            catch (Exception e)
            {
                _logger?.LogError("Exception in FutureContractDetails - {0}", e.Message);
                throw new AppErrorException(ErrorCodes.ApiService_General, "", e.Message);
            }
        }

        public ProvidersConfig LoadConfig()
        {
            lock (_lockObj)
            {
                return new ProvidersConfig(_providersConfig);
            }
        }

        // Handlers
        private async Task HandleRestart(object? sender, TaskEventArgs e)
        {
            await _taskScheduler.StopAllTasksAsync(SRC_NAME);
            await GetDataSets();
        }

        public Task HandleSyncDataSet(object? sender, TaskEventArgs e)
        {
            var arg0 = e?.Args.Length > 0 ? e?.Args[0] : null;
            int key = (int)(TypeConvertUtil.GetIntegerValue(arg0 ?? -1) ?? -1);
            if (_defaultProviderConfig == null || arg0 == null || key == -1)
            {
                return Task.CompletedTask;
            }

            return Task.Run(() =>
            {
                IBKRDataSet? dataset;
                bool shouldSync = false;
                int datasetId = -1;
                int intervalLen = 0;
                bool needsInitialSeed = false;

                lock (_lockObj)
                {
                    if (this.mapDataSets.TryGetValue(key, out dataset) &&
                        _defaultProviderConfig.DataSetIds.Contains(dataset.Dataset.Id))
                    {
                        needsInitialSeed = dataset.lastSyncTime <= 0;
                        datasetId = dataset.Dataset.Id;
                        intervalLen = dataset.Dataset.Interval.IntervalLen;
                        shouldSync = true;
                    }
                }

                if (shouldSync && dataset != null)
                {
                    // Market-hours gate: skip I/O when the exchange is closed and
                    // reschedule directly to the next open instead of every minute.
                    int msUntilOpen = MarketScheduleHelper.GetMsUntilNextOpen(
                        _providersConfig.MarketSchedule, DateTime.UtcNow);

                    if (msUntilOpen > 0)
                    {
                        _logger?.LogDebug("SyncDataSet_{0}: Market closed, rescheduling in {1:N0} ms", key, msUntilOpen);
                        _taskScheduler.ScheduleEventAsync(SRC_NAME, $"SyncDataSet_{key}", msUntilOpen, HandleSyncDataSet, key);
                        return;
                    }

                    try
                    {
                        // Seed lastSyncTime from DB outside the lock (first run only)
                        if (needsInitialSeed)
                        {
                            long seeded = 0L;
                            try
                            {
                                seeded = MainRepo.GetLastPriceData(datasetId)?.RawTime ?? 0L;
                            }
                            catch
                            {
                                seeded = 0L;
                            }
                            lock (_lockObj) { dataset.lastSyncTime = seeded; }
                        }

                        long curRawTime = DateTimeUtil.ToUnixTime(DateTime.UtcNow);
                        long secondsToBackFill = _providersConfig.NumDaysToCatchUpWhenNoData * 86400L;
                        long lastSyncTime;
                        lock (_lockObj) { lastSyncTime = dataset.lastSyncTime; }
                        var fromPriceRawDate = lastSyncTime == 0L
                            ? (curRawTime - secondsToBackFill)
                            : lastSyncTime - (long)intervalLen * _providersConfig.NumPriceBarsToUpdate;
                        SyncPrices(dataset, fromPriceRawDate, false);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError("SyncDataSet: Exception occured - {0}", ex.Message);
                    }

                    int nextSchedTime = GetNextSchedTime(dataset);
                    _taskScheduler.ScheduleEventAsync(SRC_NAME, $"SyncDataSet_{key}", nextSchedTime, HandleSyncDataSet, key);
                    int nextUpdateTime = nextSchedTime + (_providersConfig.TimeOffsetSec) * 1000 + 500;
                    _taskScheduler.ScheduleEventAsync(SRC_NAME, $"UpdateDataSet_{key}", nextUpdateTime, HandleUpdateDataSet, key);
                }
            });
        }

        public Task HandleUpdateDataSet(object? sender, TaskEventArgs e)
        {
            var arg0 = e?.Args.Length > 0 ? e?.Args[0] : null;
            int key = (int)(TypeConvertUtil.GetIntegerValue(arg0 ?? -1) ?? -1);
            if (_defaultProviderConfig == null || arg0 == null || key == -1)
            {
                return Task.CompletedTask;
            }

            return Task.Run(() =>
            {
                IBKRDataSet? dataset;
                bool shouldSync = false;
                int intervalLen = 0;

                lock (_lockObj)
                {
                    if (this.mapDataSets.TryGetValue(key, out dataset) &&
                        _defaultProviderConfig.DataSetIds.Contains(dataset.Dataset.Id) &&
                        dataset.lastSyncTime > 0)
                    {
                        intervalLen = dataset.Dataset.Interval.IntervalLen;
                        shouldSync = true;
                    }
                }

                if (shouldSync && dataset != null)
                {
                    try
                    {
                        long lastSyncTime;
                        lock (_lockObj) { lastSyncTime = dataset.lastSyncTime; }
                        var fromPriceRawDate = lastSyncTime - (long)intervalLen * _providersConfig.NumPriceBarsToUpdate;
                        SyncPrices(dataset, fromPriceRawDate, true);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError("UpdateDataSet: Exception occured - {0}", ex.Message);
                    }
                }
            });
        }

        public void SubscribeMarketData(TickerRS ticker)
        {
            try
            {
                TickerInfo tickerInfo = new TickerInfo()
                {
                    Symbol = ticker.LocalSymbol,
                    SecType = ticker.SecurityType,
                    Currency = ticker.Currency,
                    Exchange = ticker.Exchange,
                    ContractMonth = ticker.ExpiryDate
                };
                if (defaultProvider != null)
                {
                    int tickerId = defaultProvider.SubscribeMarketData(tickerInfo);
                    // associate tickerId with ticker when needed for future reference
                    SetMarketDataTicker(tickerId, ticker);
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e.Message);
            }
        }

        private void SetMarketDataTicker(int tickerId, TickerRS ticker)
        {
            lock (_lockMarketDataObj)
            {
                mapMarketDataTicker[tickerId] = ticker;
            }

        }
        public TickerRS? GetMarketDataTicker(int tickerId)
        {
            lock (_lockMarketDataObj)
            {
                if (mapMarketDataTicker.TryGetValue(tickerId, out TickerRS? ticker))
                {
                    return new TickerRS(ticker);
                }
                else
                {
                     return null;
                }
            }
        }

        public void ReloadSubscriptions()
        {
            try
            {
                //todo: revise
                return;

                var setting = MainRepo.GetSetting("MarketData", "Subscription");
                if (setting != null)
                {
                    // convert setting.Value from string to List<int>
                    List<int>? subscribedTickers = JsonConvert.DeserializeObject<List<int>>(setting.Value);
                    subscribedTickers?.ForEach(tickerId =>
                    {
                        var ticker = MainRepo.GetTicker(tickerId);
                        if (ticker != null)
                        {
                            SubscribeMarketData(ticker);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("ReloadSubscriptions: Exception occured - {0}", ex.Message);
            }

        }
    }
}