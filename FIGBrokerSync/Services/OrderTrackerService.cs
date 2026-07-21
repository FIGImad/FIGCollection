using FIGBrokerSvc.DataAccess;
using FIGCommon.Extensions;
using FIGCommon.Interfaces;
using FIGCommon.Models.FIGBroker;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Utilities;
using FIGCommon.Utilities.FIGProviderAPI;

namespace FIGBrokerSvc.Services
{

    public class OrderTrackerService : BackgroundService, IOrderTrackerService
    {
        private readonly ILogger<OrderTrackerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ATSApi _apiATS;
        private readonly IConfiguration _config;

        private string _serviceId = string.Empty;
        private readonly static object _lockAccess = new object();

        public OrderTrackerService(ILogger<OrderTrackerService> logger,
                                   IServiceProvider serviceProvider,
                                   IConfiguration configuration,
                                   ATSApi apiATS)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _apiATS = apiATS ?? throw new ArgumentNullException(nameof(apiATS));
            _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OrderTrackerService starting");
            try
            {
                await Task.Delay(2000, stoppingToken);

                List<BrokerAccountRS> acctList = BrokerRepo.GetBrokerAccounts();

                var trackingTasks = new List<Task>();

                foreach (var acct in acctList)
                {
                    if (!acct.Enable /*|| !acct.TrackingOnly*/)
                    {
                        continue;
                    }

                    string acctServiceId = acct.AccountServiceId.Trim(' ', '\r', '\n', '\t').ToUpper();
                    if (ServiceId != acctServiceId)
                    {
                        _logger.LogInformation($"Broker account {acct.AccountId} does not match service ID, skipping");
                        continue;
                    }

                    var provider = _serviceProvider.GetKeyedService<IProviderService>(acct.AccountId.Trim().ToUpperInvariant());
                    if (provider == null)
                    {
                        _logger.LogWarning($"No broker provider registered for account {acct.AccountId}");
                        continue;
                    }

                    var callbackQueue = _serviceProvider.GetRequiredKeyedService<ICallbackQueue>(acct.AccountId.Trim().ToUpperInvariant());
                    trackingTasks.Add(RunProviderTrackingLoop(provider, stoppingToken));
                    trackingTasks.Add(RunOrderCallbackLoop(callbackQueue, stoppingToken));
                }

                await Task.WhenAll(trackingTasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("OrderTrackerService stopping due to cancellation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrderTrackerService encountered a fatal error");
            }

            _logger.LogInformation("OrderTrackerService TERMINATED");
        }

        private async Task RunOrderCallbackLoop(ICallbackQueue callbackQueue, CancellationToken stoppingToken)
        {
            await foreach (var message in callbackQueue.Subscribe(
                BrokerCallbackMessageType.OpenOrder,
                BrokerCallbackMessageType.OrderStatus,
                BrokerCallbackMessageType.CompletedOrder,
                BrokerCallbackMessageType.Execution,
                BrokerCallbackMessageType.Error)
                .WithCancellation(stoppingToken))
            {
                try
                {
                    await HandleMessageAsync(message, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed handling callback message. Type={Type}", message.Type);
                }
            }
        }

        private async Task RunProviderTrackingLoop(IProviderService provider, CancellationToken stoppingToken)
        {
            bool isFirstCall = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("TrackProviderOrders starting for provider account {ProviderId}", provider.Id);

                    List<OrderRS> orderLst = BrokerRepo.GetActiveOrdersByServiceId(ServiceId);

                    if (orderLst.Count > 0)
                    {
                        if (isFirstCall)
                        {
                            _logger.LogInformation("TrackProviderOrders first call for provider account {ProviderId}", provider.Id);
                            provider.QueryOpenOrders();
                        }
                        _logger.LogInformation("TrackProviderOrders querying completed orders for provider account {ProviderId}", provider.Id);
                        provider.QueryAllCompletedOrders();
                    }

                    isFirstCall = false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to query orders for provider={ProviderId}", provider.Id);
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        //private async Task TrackProviderOrders(IProviderService provider, CancellationToken stoppingToken, bool isFirstCall)
        //{
        //    _logger.LogInformation($"TrackProviderOrders starting for provider account {provider.Id}");

        //    try
        //    {
        //        // get all in-progress (active) orders
        //        List<OrderRS> orderLst = BrokerRepo.GetActiveOrders("" /*provider.Id*/);   // get it for all accounts
        //        if (orderLst.Count > 0)
        //        {
        //            if (isFirstCall)
        //            {
        //                provider.QueryAllOpenOrders(); // query provider for all orders to start tracking
        //            }
        //            provider.QueryAllCompletedOrders(); // query provider for all orders to start tracking
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to query open orders for provider={ProviderId}", provider.Id);
        //    }

        //    // track orders in 1 min
        //    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        //    _ = TrackProviderOrders(provider, stoppingToken, false);
        //}


        //protected override async Task ExecuteAsyncOld(CancellationToken stoppingToken)
        //{
        //    try
        //    {
        //        _logger.LogInformation("OrderTrackerService starting");
        //        await Task.Delay(2000, stoppingToken);

        //        var providerServices = _serviceProvider.GetServices<IProviderService>();
        //        List<BrokerAccountRS> acctList = BrokerRepo.GetBrokerAccounts();
        //        // loop through providers
        //        foreach (var provider in providerServices)
        //        {
        //            // get broker account (BrokerAccountRS) from acctList where BrokerAccountRS.AccountId == provider.Id case insentive search
        //            BrokerAccountRS? acct = acctList.FirstOrDefault(a => a.AccountId.Equals(provider.Id, StringComparison.OrdinalIgnoreCase));
        //            if (acct == null)
        //            {
        //                _logger.LogWarning("No broker account found for provider {ProviderId}", provider.Id);
        //                continue;
        //            }
        //            // check if account is enabled and marked as TrackingOnly
        //            if (!acct.Enable || !acct.TrackingOnly)
        //            {
        //                _logger.LogInformation("Broker account {AccountId} for provider {ProviderId} is not enabled or not marked as TrackingOnly, skipping", acct.AccountId, provider.Id);
        //                continue;
        //            }
        //            await TrackProviderOrders(provider, stoppingToken, true); // track all active orders for this provider
        //        }

        //        await foreach (var message in _callbackQueue.ReadAllAsync(stoppingToken))
        //        {
        //            await HandleMessageAsync(message, stoppingToken);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "OrderTrackerService encountered an error: {Message}", ex.Message);
        //    }

        //    _logger.LogInformation("OrderTrackerService TERMINATED");


        //    // do startup work here
        //    await Task.CompletedTask;
        //}


        private async Task HandleMessageAsync(BrokerCallbackMessage message, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Received callback message. Type={message.Type}");

            switch (message.Type)
            {
                case BrokerCallbackMessageType.OpenOrder:
                    // handle order status update
                    OrderStatusDto? orderStatus = message.DataJson?.ToObject<OrderStatusDto>();
                    await HandleOpenOrder(orderStatus);
                    break;
                case BrokerCallbackMessageType.OrderStatus:
                    // handle order status update
                    OrderStatusUpdate? statusUpdate = message.DataJson?.ToObject<OrderStatusUpdate>();
                    _logger.LogInformation(
                        $"Received order status update. OrderId={statusUpdate?.BrokerOrderId ?? "N/A"}, BrokerRefId={statusUpdate?.BrokerRef ?? "N/A"}, Status={statusUpdate?.StatusCode.ToString() ?? "N/A"}");
                    await HandleStatusUpdate(statusUpdate);
                    break;
                case BrokerCallbackMessageType.CompletedOrder:
                    // handle order status update
                    OrderStatusDto? completedOrderUpdate = message.DataJson?.ToObject<OrderStatusDto>();
                    _logger.LogInformation(
                        $"Received order status complete. OrderId={completedOrderUpdate?.BrokerOrderId ?? "N/A"}, BrokerRefId={completedOrderUpdate?.BrokerRef ?? "N/A"}, Status={completedOrderUpdate?.StatusCode.ToString() ?? "N/A"}");
                    await HandleCompletedOrder(completedOrderUpdate);
                    break;
                case BrokerCallbackMessageType.Error:
                    // handle order status error
                    ErrorResponseDto? err = message.DataJson?.ToObject<ErrorResponseDto>();
                    await HandleError(err);
                    break;
                default:
                    //_logger.LogWarning("Received unhandled message type: {MessageType}", message.Type);
                    break;
            }
        }

        private async Task HandleError(ErrorResponseDto? err)
        {
            if (err == null) return;

            // load order from database
            OrderRS? order = null;
            if (err.BrokerOrderId.Length > 0)
            {
                order = BrokerRepo.GetOrderByOrderId(err.BrokerOrderId);
            }
            if (order is null)
            {
                _logger.LogDebug($"Could not load order from database for Order.Id: {err.BrokerOrderId}");
                return;
            }
            // order is already completed, update status in database
            if (OrderStatusCodes.IsInProgress(order.OrderStatus))
            {
                int statusCode = OrderStatusCodes.FAIL_REJECTED;
                long statusTime = FIGCommon.Utilities.DateTimeUtil.CurrentUTCUnixTime();
                if (err.ErrorCode == ProviderErrorCodes.DUPLICATE_ORDER)
                {
                    statusCode = OrderStatusCodes.FAIL_DUPLICATE_ID;
                }
                OrderStatusDto statusUpdate = new OrderStatusDto
                {
                    OrderId = order.Id.ToString(),
                    BrokerRef = order.BrokerRef,
                    BrokerOrderId = order.OrderRefId,
                    BrokerParams = order.BrokerParamsMap,
                    OrderRefId = order.OrderRefId,
                    OrderType = order.OrderType,
                    Qty = order.Qty,
                    Price = order.Price,
                    FilledQty = order.FillQty,
                    AvgFillPrice = order.FillPrice,
                    CommissionAndFees = 0,
                    StatusCode = statusCode,
                    StatusTime = statusTime
                };
                BrokerRepo.UpdateOrderStatusSimple(order.Id, statusUpdate);
                await _apiATS.SendStatusUpdate(statusUpdate);
            }
        }

        private async Task HandleStatusUpdate(OrderStatusUpdate? statusUpdate)
        {
            if (statusUpdate == null) return;

            // load order from database
            OrderRS? order = null;
            if (statusUpdate.BrokerRef.Length > 0)
            {
                order = BrokerRepo.GetOrderByBrokerRef(statusUpdate.BrokerRef);
            }
            if (order is null && statusUpdate.BrokerOrderId.Length > 0)
            {
                order = BrokerRepo.GetOrderByOrderId(statusUpdate.BrokerOrderId);
            }
            if (order is null)
            {
                _logger.LogDebug($"Could not load order from database for Order.Id: {statusUpdate.BrokerOrderId}, BrokerRef: {statusUpdate.BrokerRef}");
                return;
            }
            // order is already completed, update status in database
            bool isInProgress = OrderStatusCodes.IsInProgress(order.OrderStatus);
            bool isFinalState = OrderStatusCodes.IsComplete(order.OrderStatus);
            if (isInProgress || order.HasStatusChanged(statusUpdate))
            {
                int filledQty = Math.Abs(statusUpdate.QtyFilled);
                int orderFilledQty = Math.Abs(order.FillQty);
                bool canUpdateFillQty = filledQty > orderFilledQty;
                bool canUpdateFillPrice = statusUpdate.AvgFillPrice != order.FillPrice && statusUpdate.AvgFillPrice != 0;
                Dictionary<string, string> newBrokerParams = order.BrokerParamsMap;
                foreach (var kvp in statusUpdate.BrokerParams)
                {
                    if (!newBrokerParams.ContainsKey(kvp.Key))
                    {
                        newBrokerParams.Add(kvp.Key, kvp.Value);
                    }
                }
                OrderStatusDto orderStatus = new OrderStatusDto
                {
                    OrderId = order.Id.ToString(),
                    BrokerRef = string.IsNullOrEmpty(order.BrokerRef) ? statusUpdate.BrokerRef : order.BrokerRef,
                    BrokerOrderId = string.IsNullOrEmpty(order.BrokerOrderId) ? statusUpdate.BrokerOrderId : order.BrokerOrderId,
                    BrokerParams = newBrokerParams,
                    OrderRefId = order.OrderRefId,
                    OrderType = order.OrderType,
                    Qty = order.Qty,
                    Price = order.Price,
                    FilledQty = canUpdateFillQty ? (order.Qty > 0 ? filledQty : -filledQty) : order.FillQty,
                    AvgFillPrice = canUpdateFillPrice ? statusUpdate.AvgFillPrice : order.FillPrice,
                    CommissionAndFees = 0,
                    StatusCode = isFinalState ? order.OrderStatus : statusUpdate.StatusCode,
                    StatusTime = statusUpdate.StatusTime,
                    Ticker = order.ticker?.ToTickerInfo() 
                };
                BrokerRepo.UpdateOrderStatusSimple(order.Id, orderStatus);
                await _apiATS.SendStatusUpdate(orderStatus);
            }
        }

        private async Task HandleOpenOrder(OrderStatusDto? orderStatus)
        {
            await HandleOrderStatusUpdate(orderStatus, false);
        }

        private async Task HandleCompletedOrder(OrderStatusDto? statusUpdate)
        {
            await HandleOrderStatusUpdate(statusUpdate, true);
        }

        private async Task HandleOrderStatusUpdate(OrderStatusDto? orderStatus, bool isCompleted)
        {
            if (orderStatus == null) return;

            // load order from database
            OrderRS? order = null;
            if (orderStatus.OrderId.Length > 0)
            {
                int id = (int)(TypeConvertUtil.GetIntegerValue(orderStatus.OrderId) ?? 0);
                if (id > 0)
                {
                    order = BrokerRepo.GetOrder(id);
                }
            }
            if (order is null && orderStatus.BrokerRef.Length > 0)
            {
                // try load using broker ref
                order = BrokerRepo.GetOrderByBrokerRef(orderStatus.OrderId);
            }
            if (order is null)
            {
                _logger.LogDebug($"Could not load order from database for Order.Id: {orderStatus.OrderId}, BrokerRef: {orderStatus.BrokerRef}");
                return;
            }
            orderStatus.OrderRefId = order.OrderRefId; // ensure OrderRefId is set in case it was missing in the callback

            Dictionary<string, string> newBrokerParams = order.BrokerParamsMap;
            foreach (var kvp in orderStatus.BrokerParams)
            {
                if (!newBrokerParams.ContainsKey(kvp.Key))
                {
                    newBrokerParams.Add(kvp.Key, kvp.Value);
                }
            }
            orderStatus.BrokerParams = newBrokerParams;

            // order is already completed, update status in database
            bool isInProgress = OrderStatusCodes.IsInProgress(order.OrderStatus);
            bool isUpdateFinalState = OrderStatusCodes.IsComplete(orderStatus.StatusCode);
            if (isInProgress || order.HasStatusChanged(orderStatus))
            {
                // get string representation in json of the order status for logging
                string statusStr = System.Text.Json.JsonSerializer.Serialize(orderStatus);
                _logger.LogInformation(
                    "Updating order status for OrderId={OrderId}, BrokerRef={BrokerRef}. InProgress={InProgress}, FinalState={FinalState}, StatusChanged={StatusChanged}, Status={Status}",
                    order.Id, order.BrokerRef, isInProgress, isUpdateFinalState, order.HasStatusChanged(orderStatus), statusStr);
                int filledQty = Math.Abs(orderStatus.FilledQty);
                int orderFilledQty = Math.Abs(order.FillQty);
                bool canUpdateFillQty = filledQty > orderFilledQty;
                bool canUpdateFillPrice = orderStatus.AvgFillPrice != order.FillPrice && orderStatus.AvgFillPrice != 0;
                bool canUpdateStatus = isInProgress && isUpdateFinalState;

                orderStatus.BrokerRef = string.IsNullOrEmpty(order.BrokerRef) ? orderStatus.BrokerRef : order.BrokerRef;
                orderStatus.BrokerOrderId = string.IsNullOrEmpty(order.BrokerOrderId) ? orderStatus.BrokerOrderId : order.BrokerOrderId;
                orderStatus.FilledQty = canUpdateFillQty ? (order.Qty > 0 ? filledQty : -filledQty) : order.FillQty;
                orderStatus.AvgFillPrice = canUpdateFillQty ? orderStatus.AvgFillPrice : order.FillPrice;
                orderStatus.StatusCode = canUpdateFillPrice || canUpdateStatus ? orderStatus.StatusCode : order.OrderStatus;
                BrokerRepo.UpdateOrderStatusSimple(order.Id, orderStatus);
                await _apiATS.SendStatusUpdate(orderStatus);
            }
        }
    }
}
