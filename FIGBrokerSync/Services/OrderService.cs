using FIGBrokerSvc.DataAccess;
using FIGCommon.Exceptions;
using FIGCommon.Interfaces;
using FIGCommon.Models.FIGBroker;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Services;
using FIGCommon.Utilities;
using IBKRProvider.Service;
using System.Collections.Concurrent;

namespace FIGBrokerSvc.Services
{
    public class OrderService : IHostedService
    {
        private readonly ILogger<OrderService> _logger;
        private readonly IConfiguration _config;
        private readonly IServiceProvider _serviceProvider;

        //private readonly ConcurrentDictionary<string, BrokerAccountRS> _brokerAccountMap = new();
        private readonly ConcurrentDictionary<string, FIGCommon.Models.FIGBroker.TickerRS> _tickersMap = new();
        private readonly ConcurrentDictionary<string, Task> _activeOrderTasks = new();
        private CancellationTokenSource _serviceCts = new();

        //private object _orderProcessingLock = new object();
        private object _tickerProcessingLock = new object();
        private readonly static object _lockAccess = new object();

        private string _serviceId = string.Empty;

        public OrderService(
            ILogger<OrderService> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        protected string ServiceId
        {
            get
            {
                lock (_lockAccess)
                {
                    if (string.IsNullOrEmpty(_serviceId))
                    {
                        string key = "ControllerConfig:Id";
                        string defVal = "";
                        string? val = _config[key];

                        // trim from spaces and LF and CR and Tabs
                        _serviceId = (val ?? defVal).Trim(' ', '\r', '\n', '\t').ToUpper();
                    }
                    return _serviceId;
                }
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _serviceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Cancel all ongoing operations
            _serviceCts.Cancel();

            try
            {
                // Wait for all active order tasks to complete
                await Task.WhenAll(_activeOrderTasks.Values);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Order processing was cancelled during shutdown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while waiting for order tasks to complete");
            }
        }

        public async Task<OrderStatusDto?> PlaceOrder(OrderRequestDto orderInstruction, CancellationToken cancellationToken)
        {
            return await PlaceOrderBase(orderInstruction, cancellationToken);
        }
        public async Task<OrderStatusDto?> PlaceOrderNoWait(OrderRequestDto orderInstruction, CancellationToken cancellationToken)
        {
            return await PlaceOrderBase(orderInstruction, cancellationToken, false);
        }

        private async Task<OrderStatusDto?> PlaceOrderBase(OrderRequestDto orderInstruction, CancellationToken cancellationToken, bool waitForResponse = true)
        {
            OrderStatusDto defStatus = new OrderStatusDto()
            {
                OrderId = string.Empty,  // will be updated after we add the order record into database and get the order id
                BrokerRef = string.Empty, // will be updated after order is submitted to broker and we have the order id
                BrokerOrderId = string.Empty,
                OrderRefId = orderInstruction.OrderRefId,  // client order reference id is passed through the request
                BrokerParams = new(),   // will be updated after order is submitted to broker and we have the order response information
                OrderType = orderInstruction.OrderType.ToUpper().Trim(),
                Qty = orderInstruction.Qty,
                Price = orderInstruction.Price,
                FilledQty = 0,
                AvgFillPrice = 0,
                CommissionAndFees = 0,
                StatusCode = OrderStatusCodes.IGNORED,
                StatusTime = DateTimeUtil.CurrentUTCUnixTime()
            };
            try
            {
                // 1- Retrieve the BrokerAccountId (adapter.Type/adapter.BrokerId)
                BrokerAccountRS? brokerAccount = GetBrokerAccount(orderInstruction.AccountId);
                if (brokerAccount == null)
                {
                    _logger.LogError($"Broker account not found or disabled for AccountId: {orderInstruction.AccountId}");
                    defStatus.StatusCode = OrderStatusCodes.IGNORED_ACCOUNT_NOT_CONFIG;
                    return defStatus;
                }
                // check if service Id is matching
                string acctServiceId = brokerAccount.AccountServiceId.Trim(' ', '\r', '\n', '\t').ToUpper();
                if (acctServiceId != ServiceId)
                {
                    _logger.LogError($"Broker account service ID mismatch for AccountId: {orderInstruction.AccountId} - expected: {ServiceId}, actual: {acctServiceId}");
                    defStatus.StatusCode = OrderStatusCodes.IGNORED_ACCOUNT_NOT_CONFIG;
                    return defStatus;
                }

                // 2- retrieve the Broker Provider (IProviderService) to communicate with the configured Broker
                IProviderService? providerService = GetBrokerProvider(brokerAccount);
                if (providerService == null)
                {
                    _logger.LogError($"Broker provider not found or not configured for AccountId: {orderInstruction.AccountId}");
                    defStatus.StatusCode = OrderStatusCodes.IGNORED_BROKER_NOT_CONFIG;
                    return defStatus;
                }

                // 3- Retrieve Ticker from database, create a new record if does not exist
                int execTimeout = brokerAccount.acctParams?.ExecTimeoutSeconds ?? 10;
                TickerRS? ticker = await GetTicker(orderInstruction.Ticker, providerService, execTimeout, cancellationToken);
                if (ticker == null)
                {
                    _logger.LogError($"Ticker not found or not configured for AccountId: {orderInstruction.AccountId}");
                    defStatus.StatusCode = OrderStatusCodes.IGNORED_TICKER_NOT_CONFIG;
                    return defStatus;
                }
                defStatus.Ticker = ticker.ToTickerInfo();

                // 4- ensure that account has the ticker configured to trade
                bool allowed = brokerAccount.IsTickerAllowed(ticker.Symbol);
                if (!allowed)
                {
                    defStatus.StatusCode = OrderStatusCodes.IGNORED_TICKER_NOT_ALLOWED;
                    _logger.LogWarning($"Ticker {ticker.Symbol} is not allowed for trading in the account {brokerAccount.AccountId}");
                    return defStatus;
                }

                // 5- Check max/min limits
                string providerTrackingKey = NormalizeProviderKey(brokerAccount.AccountId, true);
                var positionTracker = _serviceProvider.GetKeyedService<IPositionTrackerService>(providerTrackingKey);
                PositionUpdate pos = positionTracker?.GetPositionForTicker(ticker.ToTickerInfo()) ?? new PositionUpdate { AccountId = brokerAccount.BrokerAccountId, Pos = 0, PendingPos = 0 }; ;
                bool isLong = orderInstruction.Qty > 0;
                int newPos = (int)(pos.Pos + pos.PendingPos);
                int netPos = isLong ? Math.Max((int)pos.Pos, newPos) : Math.Min((int)pos.Pos, newPos);
                int newExpectedPos = netPos + orderInstruction.Qty;
                if (pos.AccountId == brokerAccount.BrokerAccountId)
                {
                    // maxLong is expected to be positive numbers representing max long poisitions 
                    // maxShort is expected to be a negative number representing max short positions

                    int maxLong = brokerAccount.LongLimit(ticker.Symbol);
                    int maxShort = brokerAccount.ShortLimit(ticker.Symbol);  

                    if (newExpectedPos > maxLong || newExpectedPos < maxShort)
                    {
                        defStatus.StatusCode = newExpectedPos > maxLong ? OrderStatusCodes.IGNORED_LONG_POS_LIMIT : OrderStatusCodes.IGNORED_SHORT_POS_LIMIT;
                        _logger.LogWarning("Placing order with Qty {OrderQty} would exceed the max position limit {MaxPos} for the account {AccountId} with current position {CurrentPos} for ticker {Symbol}",
                            orderInstruction.Qty, (isLong ? maxLong : maxShort), brokerAccount.AccountId, netPos, ticker.Symbol);
                        return defStatus;
                    }
                }
            
                // 6- Check if similar successful order of the same OrderTagRef and OrderTag
                OrderRS? existingOrder = BrokerRepo.GetOrderByOrderTag(brokerAccount.Id, orderInstruction.OrderTagRef, orderInstruction.OrderTag);
                if (existingOrder != null)
                {
                    defStatus.StatusCode = OrderStatusCodes.IGNORED_DUPLICATE_ORDER;
                    _logger.LogWarning("Duplicate order detected for account {AccountId} with OrderTagRef {OrderTagRef} and OrderTag {OrderTag}", brokerAccount.AccountId, orderInstruction.OrderTagRef, orderInstruction.OrderTag);
                    return defStatus;
                }

                _logger.LogInformation($"Placing order for account {brokerAccount.AccountId}");
                // 7- Ready to place the order through the broker provider 
                //    but first add a record for this order in the database with status = NEW
                int oid = AddOrder(brokerAccount.Id, ticker.Id, orderInstruction);
                if (oid < 0)
                {
                    defStatus.StatusCode = OrderStatusCodes.IGNORED_DATABASE_ERROR;
                    return defStatus;
                }
                defStatus.OrderId = oid.ToString();

                // record is added to database and not IGNORED
                BrokerOrderRequestDto orderReq = new BrokerOrderRequestDto()
                {
                    AccountId = orderInstruction.AccountId,
                    TxnId = oid.ToString(),    // database orderId is the order reference at the broker side so that we can track in the future,
                    RefClientId = orderInstruction.OrderRefId, // client order reference id is passed through the request
                    Ticker = orderInstruction.Ticker,
                    OrderType = orderInstruction.OrderType.ToUpper().Trim(),
                    Qty = orderInstruction.Qty,
                    Price = orderInstruction.Price,
                    StopPrice = orderInstruction.StopPrice,
                    TimeInForce = orderInstruction.TimeInForce
                };
                try
                {
                    // 8- Send Order
                    int orderExecTimeout = brokerAccount.acctParams?.OrderExecTimeoutSeconds ?? 5;
                    defStatus = providerService.PlaceOrder(orderReq, (waitForResponse ? orderExecTimeout * 1000 : 0));
                    if (defStatus.StatusCode == OrderStatusCodes.FAIL_DUPLICATE_ID)
                    {
                        //attempting to place the same order again since orderId is already reset
                        defStatus = providerService.PlaceOrder(orderReq, orderExecTimeout * 1000);
                    }
                    // 9- No exception, mark record as in progress
                    BrokerRepo.UpdateOrderStatus(oid, defStatus);

                    // 10-Track order asyncroulsy if still in progress
                    if (OrderStatusCodes.IsInProgress(defStatus.StatusCode))
                    {
                        _logger?.LogInformation($"OrderId {oid} is still in progress after waiting for {orderExecTimeout} seconds, will continue tracking asynchronously");
                        //_orderTrackerSvc.Track(providerService, oid);    // manage tracking this order asynchronously
                    }
                }
                catch (BrokerConnectionException cex)
                {
                    // convert error into Roots Error and throw
                    defStatus.StatusCode = OrderStatusCodes.FAIL_CONNECTION;
                    defStatus.StatusTime = DateTimeUtil.CurrentUTCUnixTime();
                    _logger?.LogError(cex, $"Failed to place order for orderId:  {oid}");
                    BrokerRepo.UpdateOrderStatus(oid, defStatus);
                }
            }
            catch (Exception ex)
            {
                defStatus.StatusCode = OrderStatusCodes.IGNORED;
                _logger.LogError(ex, $"Error placing order {orderInstruction.OrderRefId}");
            }
            return defStatus;
        }


        public async Task<OrderStatusDto?> CancelOrder(OrderCancelRequestDto cancelRequest, CancellationToken cancellationToken, int timeoutMsec = -1)
        {
            OrderStatusDto defStatus = new OrderStatusDto()
            {
                OrderId = string.Empty,  // will be updated after we add the order record into database and get the order id
                BrokerRef = cancelRequest.BrokerRef,
                BrokerOrderId = string.Empty,
                OrderRefId = cancelRequest.OrderRefId,
                OrderType = OrderTypes.UNDEFINED,
                StatusCode = OrderStatusCodes.IGNORED,
                StatusTime = DateTimeUtil.CurrentUTCUnixTime()
            };
            try
            {
                OrderRS? order = null;
                // 1- Load existing order from database
                if (cancelRequest.BrokerRef.Length > 0) {
                    order = BrokerRepo.GetOrderByBrokerRef(cancelRequest.BrokerRef);
                }
                else if (cancelRequest.AccountId.Length > 0 && cancelRequest.OrderRefId.Length > 0)
                {
                    BrokerAccountRS? brkAcct = GetBrokerAccount(cancelRequest.AccountId);
                    if (brkAcct == null)
                    {
                        defStatus.StatusCode = OrderStatusCodes.IGNORED_BROKER_NOT_CONFIG;
                        return defStatus;
                    }
                    order = BrokerRepo.GetOrderByOrderRefId(brkAcct.Id, cancelRequest.OrderRefId);
                }
                if (order == null)
                {
                    return defStatus;
                }
                defStatus.OrderId = order.Id.ToString();
                defStatus.OrderRefId = order.OrderRefId;
                defStatus.BrokerOrderId = order.BrokerOrderId;
                defStatus.BrokerParamsJson = order.BrokerParams;
                defStatus.OrderType = order.OrderType;
                defStatus.Qty = order.Qty;
                defStatus.Price = order.Price;
                defStatus.FilledQty = order.FillQty;
                defStatus.AvgFillPrice = order.FillPrice;
                defStatus.CommissionAndFees = 0;
                defStatus.StatusCode = order.OrderStatus;
                defStatus.Ticker = order.ticker?.ToTickerInfo();

                // 2- Retrieve the BrokerAccountId (adapter.Type/adapter.BrokerId)
                BrokerAccountRS? brokerAccount = order.brokerAccount;
                if (brokerAccount == null)
                {
                    defStatus.StatusCode = OrderStatusCodes.IGNORED_ACCOUNT_NOT_CONFIG;
                    // do not update order.. .just return 
                    return defStatus;
                }
                // 3- retrieve the Broker Provider (IProviderService) to communicate with the configured Broker
                IProviderService? providerService = GetBrokerProvider(brokerAccount);
                if (providerService == null)
                {
                    defStatus.StatusCode = OrderStatusCodes.IGNORED_BROKER_NOT_CONFIG;
                    // do not update order.. .just return 
                    return defStatus;
                }
                try
                {
                    // 4- Send cancellation Order
                    int orderExecTimeout = timeoutMsec < 0 ? (brokerAccount.acctParams?.ExecTimeoutSeconds * 1000) ?? timeoutMsec : timeoutMsec;
                    orderExecTimeout = 5000;

                    // get orderId from order
                    var cancelStatus = providerService.CancelOrder(order.BrokerOrderId, order.BrokerRef, orderExecTimeout);
                    if (cancelStatus == null)
                    {
                        _logger.LogWarning($"CancelOrder returned null for broker orderId {order.BrokerOrderId} with BrokerRef {cancelRequest.BrokerRef}");
                        defStatus.StatusCode = OrderStatusCodes.IGNORED;
                        return defStatus;
                    }
                    // 6- No exception, mark record as in progress
                    OrderRS? updatedOrder = BrokerRepo.UpdateOrderCancelStatus(order.Id, cancelStatus.StatusCode);
                    if (updatedOrder != null)
                    {
                        defStatus.StatusCode = updatedOrder.OrderStatus;
                        defStatus.StatusTime = cancelStatus.StatusTime;
                    }

                    // 7-Track order asyncroulsy if still in progress
                    if (OrderStatusCodes.IsInProgress(defStatus.StatusCode))
                    {
                        _logger?.LogInformation($"OrderId {defStatus.OrderId} is still in progress after waiting for {orderExecTimeout} seconds, will continue tracking asynchronously");
                        //_orderTrackerSvc.Track(providerService, order.Id);    // manage tracking this order asynchronously
                    }
                }
                catch (BrokerConnectionException cex)
                {
                    // convert error into Roots Error and throw
                    defStatus.StatusCode = OrderStatusCodes.FAIL_CONNECTION;
                    defStatus.StatusTime = DateTimeUtil.CurrentUTCUnixTime();
                    _logger?.LogError(cex, $"Failed to cancel order for orderId:  {defStatus.OrderId}");
                }
                catch (TimeoutException cex)
                {
                    // convert error into Roots Error and throw
                    defStatus.StatusCode = OrderStatusCodes.FAIL_TIMEOUT;
                    defStatus.StatusTime = DateTimeUtil.CurrentUTCUnixTime();
                    _logger?.LogError(cex, $"Failed to cancel order for orderId:  {defStatus.OrderId}");
                }
                catch (Exception ex)
                {
                    defStatus.StatusCode = OrderStatusCodes.IGNORED;
                    defStatus.StatusTime = DateTimeUtil.CurrentUTCUnixTime();

                    _logger?.LogError(ex, $"Failed to cancel order for orderId: {defStatus.OrderId}");
                }
            }
            catch (Exception ex)
            {
                defStatus.StatusCode = OrderStatusCodes.IGNORED;
                defStatus.StatusTime = DateTimeUtil.CurrentUTCUnixTime();
                _logger?.LogError(ex, $"Failed to cancel order for orderId: {defStatus.OrderId}");
            }
            return defStatus;
        }

        private IProviderService? GetBrokerProvider(BrokerAccountRS brokerAccount)
        {
            try
            {
                var provider = _serviceProvider.GetKeyedService<IProviderService>(brokerAccount.AccountId.Trim().ToUpperInvariant());

                if (provider == null)
                {
                    _logger.LogError("No broker provider found for adapter {AdapterId}", brokerAccount.AccountId);
                    return null;
                }
                return provider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting broker provider for adapter {AccountId}", brokerAccount.AccountId);
                return null;
            }
        }

        private BrokerAccountRS? GetBrokerAccount(string accountId)
        {
            var acct = BrokerRepo.GetBrokerAccountByAccountId(accountId);
            if (acct == null || !acct.Enable)
            {
                _logger.LogError($"Broker account not found or disabled for BrokerAccountId: {accountId}");
                return null;
            }
            return acct;

            //lock (_orderProcessingLock)
            //{
            //    if (_brokerAccountMap.TryGetValue(accountId, out var cachedAccount))
            //    {
            //        return cachedAccount;
            //    }
            //    _logger.LogError($"Broker account not found or disabled for BrokerAccountId: {accountId}");
            //    return null;
            //}
        }

        private TickerRS? GetTickerInternally(TickerInfo ticker)
        {
            string key = $"{ticker.Symbol}@@@{ticker.Exchange}";

            lock (_tickerProcessingLock)
            {
                if (_tickersMap.TryGetValue(key, out var cachedTicker))
                {
                    return cachedTicker;
                }
            }
            // not found, try to find it from database
            TickerRS? tickerRS = null;
            try
            {
                tickerRS = BrokerRepo.QueryTicker(ticker.Symbol, ticker.Exchange, ticker.ContractMonth);
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Exception querying ticker \"{ticker.Symbol}\" from database for the Exchange \"{ticker.Exchange}\" - {ex.Message}");
                tickerRS = null;
            }
            if (tickerRS != null)
            {
                lock (_tickerProcessingLock)
                {
                    _tickersMap[key] = tickerRS;
                }
                return tickerRS;
            }
            return tickerRS;
        }


        private async Task<TickerRS?> GetTicker(TickerInfo ticker, IProviderService providerService, int execTimeout, CancellationToken cancellationToken)
        {
            string key = $"{ticker.Symbol}@@@{ticker.Exchange}";
            TickerRS? tickerRS = GetTickerInternally(ticker);
            if (tickerRS != null)
            {
                return tickerRS;
            }

            _logger.LogDebug($"Ticker \"{ticker.Symbol}\" not found in database for the Exchange \"{ticker.Exchange}\", will proceed to query the ticker information from broker");
            tickerRS = await QueryTicker(ticker, providerService, execTimeout, cancellationToken);

            if (tickerRS != null)
            {
                lock (_tickerProcessingLock)
                {
                    try
                    {
                        tickerRS = BrokerRepo.UpsertTicker(tickerRS);
                        _tickersMap[key] = tickerRS;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug($"Exception adding ticker \"{ticker.Symbol}\" to database for the Exchange \"{ticker.Exchange}\" - {ex.Message}");
                        tickerRS = null;
                    }
                }
                return tickerRS;
            }

            return null;
        }

        public async Task<TickerRS?> QueryTicker(TickerInfo ticker, IProviderService providerService, int execTimeout, CancellationToken cancellationToken)
        {
            int orderExecTimeout = execTimeout; // default timeout in seconds
            try
            {
                _logger.LogInformation($"Query ticker \"{ticker.Symbol}\" for the exchange \"{ticker.Exchange}\"");

                var timeout = TimeSpan.FromSeconds(execTimeout + 5);
                // for futures we will try to get the full contract details as the SymbolLookup is not supported by some providers for futures contracts
                ContractInfo? contractInfo = null;
                InstrumentLookupRequest reqFut = new InstrumentLookupRequest()
                {
                    Symbol = ticker.Symbol,
                    Exchange = ticker.Exchange,
                    Currency = ticker.Currency,
                    SecType = ticker.SecType,
                    LastTradeDateOrContractMonth = TypeConvertUtil.GetStringValue(ticker.ContractMonth) ?? ""
                };
                var placeTask = Task.Run(() => providerService.ContractInfo(reqFut, execTimeout * 1000), cancellationToken);
                var completed = await Task.WhenAny(placeTask, Task.Delay(timeout, cancellationToken));
                if (completed != placeTask)
                {
                    _logger.LogWarning("ContractInfo request for ticker {Symbol} timed out after {Timeout} seconds", ticker.Symbol, execTimeout);
                    return null;
                }
                contractInfo = await placeTask; // propagate exceptions if any
                if (contractInfo == null)
                {
                    _logger.LogWarning("ContractInfo request for ticker {Symbol} returned null", ticker.Symbol);
                    return null;
                }
                TickerRS tickerRS = new TickerRS()
                {
                    Symbol = contractInfo.Symbol,
                    LocalSymbol = contractInfo.LocalSymbol,
                    Name = contractInfo.Name,
                    SecurityType = contractInfo.SecType,
                    Currency = contractInfo.Currency,
                    Exchange = contractInfo.Exchange,
                    ExpiryDate = (int)(TypeConvertUtil.GetIntegerValue(contractInfo.LastTradeDateOrContractMonth)??0),
                    MinTick = contractInfo.MinTick,
                    ContractSize = (int)(TypeConvertUtil.GetIntegerValue(contractInfo.Multiplier) ?? 1)
                };
                return tickerRS;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning($"Query Ticker {ticker.Symbol}@{ticker.Exchange} was cancelled");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning($"Query Ticker {ticker.Symbol}@{ticker.Exchange} timed out after {execTimeout} seconds");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Query Ticker {ticker.Symbol}@{ticker.Exchange} Exception - {ex.Message}");
            }
            return null;
        }

        private int AddOrder(int brokerAccountId, int tickerId, OrderRequestDto orderInstruction)
        {
            OrderRS recOrder = new OrderRS()
            {
                BrokerAccountId = brokerAccountId,
                OrderRefId = orderInstruction.OrderRefId,
                TickerId = tickerId,
                OrderTagRef = orderInstruction.OrderTagRef,
                OrderTag = orderInstruction.OrderTag,
                OrderType = orderInstruction.OrderType,
                Qty = orderInstruction.Qty,
                Price = orderInstruction.Price,
                StopPrice = orderInstruction.StopPrice,
                OrderStatus = OrderStatusCodes.NEW,
                FillQty = 0,
                FillPrice = 0,
                BrokerRef = string.Empty,
                OrderTime = -1,
                CompletionTime = -1,
                LastUpdateTime = -1
            };
            try
            {
                recOrder = BrokerRepo.InsertOrder(recOrder);
                if (recOrder.Id <= 0)
                {
                    _logger.LogError("Failed to insert order record into database for OrderRefId {OrderRefId}", orderInstruction.OrderRefId);
                    return -1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting order record into database for OrderRefId {OrderRefId}", orderInstruction.OrderRefId);
                return -1;
            }
            return recOrder.Id;
        }

        public async Task<decimal?> GetPriceSnapshot(string accountId, int tickerId, CancellationToken ct)
        {
            // Create TickerInfo


            try
            {
                // 1- Retrieve the BrokerAccountId (adapter.Type/adapter.BrokerId)
                BrokerAccountRS? brokerAccount = GetBrokerAccount(accountId);
                if (brokerAccount == null)
                {
                    _logger.LogError($"Broker account not found or disabled for AccountId: {accountId}");
                    return null;
                }
                // 2- retrieve the Broker Provider (IProviderService) to communicate with the configured Broker
                IProviderService? providerService = GetBrokerProvider(brokerAccount);
                if (providerService == null)
                {
                    _logger.LogError($"Broker provider not found or not configured for AccountId: {accountId}");
                    return null;
                }

                // 3- Retrieve Ticker from database, create a new record if does not exist
                FIGCommon.Models.FIGBroker.TickerRS? ticker = BrokerRepo.GetTicker(tickerId);
                if (ticker == null)
                {
                    _logger.LogError($"Ticker not found or not configured for AccountId: {accountId}");
                    return null;
                }
                TickerInfo tickerInfo = ticker.ToTickerInfo();

                // 4- Get snapshot
                try
                {
                    decimal? price = providerService.TickerSnapshot(tickerInfo, 5000);
                    return price;
                }
                catch (BrokerConnectionException cex)
                {
                    // convert error into Roots Error and throw
                    _logger?.LogError(cex, $"Failed to get ticker snapshot for tickerId: {tickerId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting ticker snapshot for tickerId: {tickerId}");
            }
            return null;
        }
        private static string NormalizeProviderKey(string id, bool isTracking)
        {
            return (isTracking ? "TRACKING_" : string.Empty) + id.Trim().ToUpperInvariant();
        }

    }
}