using FIGAutoTradeExSvc.Services;
using FIGAutoTraderSvc.Services;
using FIGCommon.DataAccess;
using FIGCommon.Exceptions;
using FIGCommon.Models;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Tasks;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;


namespace FIGCommon.Services
{
    public class OrderManagementService : IOrderManagementService
    {
        private const string SRC_NAME = "OrderManagementService";

        private readonly ILogger<OrderManagementService> _logger;
        private readonly ITaskSchedulerService _taskScheduler;
        private readonly FigBrokerAPIService _brokerSvc;
        private readonly CancellationTokenSource _serviceCts = new();

        private readonly object _lockObj = new object();

        private readonly static Dictionary<int, CacheItem<BotRS>> _tradeBotMap = new();
        private readonly static Dictionary<int, TickerRS> _tickersMap = new();
        private readonly static object _lockTradeBot = new object();
        private readonly static int expiryTimeout = 300000;   // expires in 5 minutes

        public OrderManagementService(ILogger<OrderManagementService> logger,
                                      IServiceProvider serviceProvider,
                                      ITaskSchedulerService taskScheduler,
                                      FigBrokerAPIService brokerSvc)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _taskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
            _brokerSvc = brokerSvc ?? throw new ArgumentNullException(nameof(brokerSvc));
            _logger.LogInformation("Service is created.");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service Started");
            lock (_lockObj)
            {
                InitializeService();
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service is Stopping");
            _taskScheduler.StopAllTasksAsync(SRC_NAME).Wait();

            _serviceCts.Cancel();

            return Task.CompletedTask;
        }

        private void InitializeService()
        {
            try
            {
                // Start OrderManagementService tasks
            }
            catch (Exception ex)
            {
                _logger.LogError("Error initializing service: {0}", ex.Message);
            }
        }

        private static BotRS? GetTradeBot(int botId)
        {
            {
                BotRS? tradeBot = null;
                try
                {
                    lock (_lockTradeBot)
                    {
                        if (_tradeBotMap.ContainsKey(botId))
                        {
                            tradeBot = _tradeBotMap[botId].IsExpired() ? null : _tradeBotMap[botId].GetItem();
                            if (tradeBot == null)
                            {
                                _tradeBotMap.Remove(botId);
                            }
                        }
                        if (tradeBot == null)
                        {
                            tradeBot = MainRepo.GetBot(botId);
                            if (tradeBot != null)
                            {
                                _tradeBotMap.Add(botId, new CacheItem<BotRS>(tradeBot, expiryTimeout));
                            }
                        }
                        return tradeBot;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
        private static TickerRS? GetTicker(int tickerId)
        {
            {
                TickerRS? ticker = null;
                try
                {
                    lock (_lockTradeBot)
                    {
                        if (_tickersMap.ContainsKey(tickerId))
                        {
                            ticker = _tickersMap[tickerId];
                        }
                        if (ticker == null)
                        {
                            ticker = MainRepo.GetTicker(tickerId);
                            if (ticker != null)
                            {
                                _tickersMap.Add(tickerId, ticker);
                            }
                        }
                        return ticker;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }


        public async Task<bool> PlaceOrderAsync(int? id, int attempt = 0)
        {
            if (id == null)
            {
                _logger.LogDebug("Invalid order id: {0}", id);
                return false;
            }
            AutoTradeSignalOrderRS? order = null;
            AutoTradeSignalRS? autoTradeSignal = null;
            try
            {
                //1- Get order
                order = MainRepo.GetAutoTradeSignalOrder(id.Value);
                if (order == null)
                {
                    _logger.LogDebug("Order not found for id: {0}", id);
                    return false;
                }
                //2- Get Signal
                autoTradeSignal = MainRepo.GetAutoTradeSignal(order.AutoTradeSignalId);
                if (autoTradeSignal == null)
                {
                    _logger.LogDebug("Could not find signal for id: {0}", order.AutoTradeSignalId);
                    UpdateOrderStatus(order, autoTradeSignal, OrderStatusCodes.IGNORED_DATA_INVALID);
                    return false;
                }
                //3- Find BOT
                BotRS? bot = GetTradeBot(autoTradeSignal.BotId);
                if (bot == null)
                {
                    _logger.LogDebug("Could not find bot for botId: {0}", autoTradeSignal.BotId);
                    UpdateOrderStatus(order, autoTradeSignal, OrderStatusCodes.IGNORED_DATA_INVALID);
                    return false;
                }

                //4- Get Ticker
                TickerRS? ticker = GetTicker(autoTradeSignal.TickerId);
                if (ticker == null)
                {
                    _logger.LogDebug("Could not find Ticker information for TickerId: {0}", autoTradeSignal.TickerId);
                    UpdateOrderStatus(order, autoTradeSignal, OrderStatusCodes.IGNORED_DATA_INVALID);
                    return false;
                }
                TickerInfo tickerInfo = new TickerInfo()
                {
                    Symbol = ticker.LocalSymbol,
                    SecType = ticker.SecurityType,
                    Currency = ticker.Currency,
                    Exchange = ticker.Exchange,
                    ContractMonth = ticker.ExpiryDate
                };

                // check if order is marked as new before placing order
                if (order.Status == OrderStatus.NEW)
                {
                    OrderRequestDto req = new OrderRequestDto()
                    {
                        AccountId = bot.AccountId,
                        BrokerRef = string.Empty,
                        OrderRefId = order.RequestRef,
                        OrderTagRef = autoTradeSignal.Tag,
                        OrderTag = order.OrderTag,
                        Ticker = tickerInfo,
                        OrderType = order.Type,
                        Qty = order.Qty,
                        Price = order.LimitPrice,
                        StopPrice = 0, // need to update when stop order is supported
                        TimeInForce = "GTC" // need to update when different time in force is supported
                    };
                    int millisecondsTimeout = 60000;  // wait for a minute, mainly for communication, however order is sent with no wait at the broker side
                    OrderStatusDto? status = await _brokerSvc.PlaceOrderAsync(req, bot.BrokerServiceId, millisecondsTimeout, _serviceCts.Token);
                    // Update order status based on response from broker
                    if (status != null)
                    {
                        UpdateOrderStatus(order, autoTradeSignal, status.StatusCode, status.FilledQty, status.AvgFillPrice, status.BrokerRef);
                        // schedule check for order status update in case we do not receive any update from broker within certain time, this is to
                        // handle the scenario where order is placed successfully at broker but we do not receive the response due to communication issue,
                        // in that case we want to make sure that we update the order status to timeout after certain time so that it does not stay in new
                        // status indefinitely
                        await _taskScheduler.ScheduleEventAsync(
                           SRC_NAME,
                           $"CheckOrderStatus-{order.Id}",
                           5000,
                           (sender, e) => CheckOrderStatus(e), order.Id);
                        return true;
                    }

                    UpdateOrderStatus(order, autoTradeSignal, OrderStatusCodes.FAIL_CONNECTION);
                }
            }
            catch (AppErrorException ex)
            {
                _logger?.LogError(ex, "AppErrorException occured");
                if (order != null)
                {
                    if (ex.errorCode == ErrorCodes.SysError_GatewayTimeout)
                    {
                        UpdateOrderStatus(order, autoTradeSignal, OrderStatusCodes.FAIL_TIMEOUT);
                    }
                    else if (ex.errorCode == ErrorCodes.AuthError_Login ||
                             ex.errorCode == ErrorCodes.AuthError_Unauthorized ||
                             ex.errorCode == ErrorCodes.AuthError_Authentication ||
                             ex.errorCode == ErrorCodes.AuthError_InvalidCred)
                    {
                        UpdateOrderStatus(order, autoTradeSignal, OrderStatusCodes.FAIL_AUTHORIZATION);
                    }
                    else if (ex.errorCode == ErrorCodes.SysError_InternalServerError)
                    {
                        UpdateOrderStatus(order, autoTradeSignal, OrderStatusCodes.FAIL_CONNECTION);
                    }
                    else
                    {
                        UpdateOrderStatus(order, autoTradeSignal, OrderStatusCodes.FAIL_OTHER);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order for id {OrderId}", id);
                if (order != null)
                {
                    UpdateOrderStatus(order, autoTradeSignal, OrderStatusCodes.FAIL_OTHER);
                }
            }
            return false;
        }

        public async Task CancelOrderAsync(int? id, int attempt = 0)
        {
            if (id == null)
            {
                _logger.LogDebug("Invalid order id: {0}", id);
                return;
            }
            AutoTradeSignalOrderRS? order = null;
            try
            {
                //1- Get order
                order = MainRepo.GetAutoTradeSignalOrder(id ?? -1);
                if (order == null)
                {
                    _logger.LogDebug("Order not found for id: {0}", id);
                    return;
                }
                //2- Get Signal
                AutoTradeSignalRS? signal = MainRepo.GetAutoTradeSignal(order.AutoTradeSignalId);
                if (signal == null)
                {
                    _logger.LogDebug("Could not find signal for id: {0}", order.AutoTradeSignalId);
                    return;
                }
                //3- Find BOT
                BotRS? bot = GetTradeBot(signal.BotId);
                if (bot == null)
                {
                    _logger.LogDebug("Could not find bot for botId: {0}", signal.BotId);
                    return;
                }

                //4- Get Ticker
                TickerRS? ticker = GetTicker(signal.TickerId);
                if (ticker == null)
                {
                    _logger.LogDebug("Could not find Ticker information for TickerId: {0}", signal.TickerId);
                    return;
                }
                TickerInfo tickerInfo = new TickerInfo()
                {
                    Symbol = ticker.Symbol,
                    SecType = ticker.SecurityType,
                    Currency = ticker.Currency,
                    Exchange = ticker.Exchange,
                    ContractMonth = ticker.ExpiryDate
                };

                // check if order is already submitted
                if (OrderStatus.IsSubmitted(order.Status))
                {
                    // update order record to indicate that cancel request is made
                    order.CancelRequest = true;
                    MainRepo.UpsertAutoTradeSignalOrder(order);

                    OrderCancelRequestDto req = new OrderCancelRequestDto()
                    {
                        AccountId = bot.AccountId,
                        BrokerRef = order.BrokerRef ?? ""
                    };
                    int millisecondsTimeout = 60000;  // wait for a minute, mainly for communication, however order is sent with no wait at the broker side
                    OrderStatusDto? status = await _brokerSvc.CancelOrder(req, bot.BrokerServiceId, millisecondsTimeout, _serviceCts.Token);
                    // Update order status based on response from broker
                    if (status != null)
                    {
                        UpdateOrderStatus(order, signal, status.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order for id {OrderId}", id);
                if (order != null)
                {
                    order.CancelRequest = true;
                    MainRepo.UpsertAutoTradeSignalOrder(order);
                }
            }
        }


        public bool UpdateOrderStatus(OrderStatusDto orderStatus)
        {
            if (orderStatus == null || string.IsNullOrEmpty(orderStatus.OrderRefId))
            {
                _logger.LogDebug("Invalid order status update received");
                return false;
            }
            AutoTradeSignalOrderRS? order = MainRepo.GetAutoTradeSignalOrderByRequestRef(orderStatus.OrderRefId);
            if (order == null)
            {
                _logger.LogDebug("Could not find order for OrderRefId: {0}", orderStatus.OrderRefId);
                return false;
            }
            AutoTradeSignalRS? signal = MainRepo.GetAutoTradeSignal(order.AutoTradeSignalId);
            if (signal == null)
            {
                _logger.LogDebug("Could not find signal for id: {0}", order.AutoTradeSignalId);
                return false;
            }
            return UpdateOrderStatus(order, signal, orderStatus.StatusCode, orderStatus.FilledQty, orderStatus.AvgFillPrice, orderStatus.BrokerRef);
        }

        private bool UpdateOrderStatus(AutoTradeSignalOrderRS order, AutoTradeSignalRS? autoTradeSignal, int statusCode, int? qtyFilled = null, decimal? avgFillPrice = null, string? brokerRef = null)
        {
            int maxAttempts = 5;
            int autoTradeId = autoTradeSignal?.AutoTradeId ?? -1;
            var lk = AutoTradeProcessLock.GetLock(autoTradeId);
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                lk.WaitLowPriority();
                try
                {
                    return UpdateOrderStatusInternal(order, autoTradeSignal, statusCode, qtyFilled, avgFillPrice, brokerRef);
                }
                catch (SqlLockException ex)
                {
                    _logger?.LogWarning($"UpdateOrderStatus - AutoTrade({autoTradeId}) - Attempt {attempt + 1}/{maxAttempts} - Database lock encountered. Retrying... - {ex.Message}");
                    if (attempt == maxAttempts - 1)
                        break;

                    Thread.Sleep(500);
                }
                catch (SqlException ex) when (ex.Number == 1205)
                {
                    _logger?.LogWarning($"UpdateOrderStatus - AutoTrade({autoTradeId}) - Attempt {attempt + 1}/{maxAttempts} - SQL deadlock. Retrying... - {ex.Message}");
                    if (attempt == maxAttempts - 1)
                        break;

                    Thread.Sleep(200 * (attempt + 1)); // increasing backoff: 200, 400, 600, 800 ms
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"UpdateOrderStatus - AutoTrade({autoTradeId}) - Attempt {attempt + 1}/{maxAttempts} - exception.");
                    break;
                }
                finally
                {
                    lk.Release();
                }
            }
            return false;
        }

        private bool UpdateOrderStatusInternal(AutoTradeSignalOrderRS order, AutoTradeSignalRS? autoTradeSignal, int statusCode, int? qtyFilled = null, decimal? avgFillPrice = null, string? brokerRef = null)
        {
            int orderId = (int)(TypeConvertUtil.GetIntegerValue(order.Id) ?? -1);
            if (orderId == -1)
            {
                _logger.LogDebug("Invalid order id in status update");
                return false;
            }

                SqlTransaction? tx = null;
                try
                {
                    tx = MainRepo.OpenTransaction();
                    if (tx == null)
                    {
                        _logger.LogError($"UpdateOrderStatus: Error opening database transaction - critical");
                        return false;
                    }
                    order.QtyFilled = qtyFilled ?? order.QtyFilled;
                    order.AvgFillPrice = avgFillPrice ?? order.AvgFillPrice;
                    order.Status = OrderStatusCodes.GetOrderStatus(statusCode);
                    order.StatusCode = statusCode;
                    order.BrokerRef = brokerRef ?? order.BrokerRef;

                    var newOrder = MainRepo.UpsertAutoTradeSignalOrder(order, tx);
                    if (newOrder == null)
                    {
                        _logger.LogError($"UpdateOrderStatus: Error opening database transaction - critical");
                        MainRepo.RollbackTransaction(tx);
                        return false;
                    }
                    // based on the status code, we might want to update AutoTradeSignal status as well. 
                    int autoTradeSignalId = (int)(TypeConvertUtil.GetIntegerValue(autoTradeSignal?.SignalId) ?? -1);
                    if (autoTradeSignal != null && autoTradeSignalId != -1)
                    {
                        // update AutoTradeSignal
                        if (order.OrderTag == OrderTagDef.OPEN)
                        {
                            int qtyF = qtyFilled ?? order.QtyFilled;
                            if (qtyF != 0 && autoTradeSignal.FilledQty != qtyF)
                            {
                                autoTradeSignal.FilledQty = qtyF;
                            }
                            if (!OrderStatus.IsCompleted(autoTradeSignal.OpenStatus))
                            {
                                autoTradeSignal.OpenStatus = order.Status;
                                autoTradeSignal.OpenStatusCode = order.StatusCode;
                                autoTradeSignal.OpenAvgPrice = order.AvgFillPrice;
                                autoTradeSignal.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                var newrec = MainRepo.UpsertAutoTradeSignal(autoTradeSignal, tx);
                                if (newrec == null)
                                {
                                    _logger.LogError($"UpdateOrderStatus: Failed to update OPEN AutoTradeSignal for signalId {autoTradeSignal.SignalId}");
                                    MainRepo.RollbackTransaction(tx);
                                    return false;
                                }
                            }
                            else
                            {
                                // This is to handle the scenario where order status is updated to completed directly without going through processing state, in that case we want to make sure that AutoTradeSignal is also updated to completed status
                                // no change in OpenStatus since it is already completed
                                // but we want to make sure that filled qty and avg fill price are updated in AutoTradeSignal in case they are not updated before
                                if (order.AvgFillPrice > 0 && autoTradeSignal.OpenAvgPrice != order.AvgFillPrice)
                                {
                                    autoTradeSignal.OpenAvgPrice = order.AvgFillPrice;
                                }
                                autoTradeSignal.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                var newrec = MainRepo.UpsertAutoTradeSignal(autoTradeSignal, tx);
                                if (newrec == null)
                                {
                                    _logger.LogError($"UpdateOrderStatus: Failed to update OPEN AutoTradeSignal for signalId {autoTradeSignal.SignalId}");
                                    MainRepo.RollbackTransaction(tx);
                                    return false;
                                }
                            }
                        }
                        else if (order.OrderTag == OrderTagDef.CLOSE)
                        {
                            if (!OrderStatus.IsCompleted(autoTradeSignal.CloseStatus))
                            {
                                autoTradeSignal.CloseStatus = order.Status;
                                autoTradeSignal.CloseStatusCode = order.StatusCode;
                                autoTradeSignal.CloseAvgPrice = order.AvgFillPrice;
                                autoTradeSignal.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                var newrec = MainRepo.UpsertAutoTradeSignal(autoTradeSignal, tx);
                                if (newrec == null)
                                {
                                    _logger.LogError($"UpdateOrderStatus: Failed to update CLOSE AutoTradeSignal for signalId {autoTradeSignal.SignalId}");
                                    MainRepo.RollbackTransaction(tx);
                                    return false;
                                }
                            }
                            else
                            {
                                // This is to handle the scenario where order status is updated to completed directly without going through processing state, in that case we want to make sure that AutoTradeSignal is also updated to completed status
                                // no change in CloseStatus since it is already completed
                                // but we want to make sure that filled qty and avg fill price are updated in AutoTradeSignal in case they are not updated before
                                if (order.AvgFillPrice > 0 && autoTradeSignal.CloseAvgPrice != order.AvgFillPrice)
                                {
                                    autoTradeSignal.CloseAvgPrice = order.AvgFillPrice;
                                }
                                autoTradeSignal.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                var newrec = MainRepo.UpsertAutoTradeSignal(autoTradeSignal, tx);
                                if (newrec == null)
                                {
                                    _logger.LogError($"UpdateOrderStatus: Failed to update CLOSE AutoTradeSignal for signalId {autoTradeSignal.SignalId}");
                                    MainRepo.RollbackTransaction(tx);
                                    return false;
                                }
                            }
                        }
                    }
                    MainRepo.CommitTransaction(tx);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"UpdateOrderStatus: Exception while updating order status for orderId {order.Id}");
                    MainRepo.RollbackTransaction(tx);
                    return false;
                }
        }

        private async Task CheckOrderStatus(TaskEventArgs e)
        {
            // Scheduled fallback: fires after a delay when a broker acknowledgment has not arrived.
            // If the order is already in a completed state the broker update arrived in time — bail out.
            // Otherwise poll the broker directly and either apply the real status or mark FAIL_TIMEOUT.
            if (e.Args.Length == 0 || e.Args[0] is not int orderId)
            {
                _logger.LogWarning("CheckOrderStatus: missing or invalid orderId in event args");
                return;
            }

            AutoTradeSignalOrderRS? order = null;
            try
            {
                order = MainRepo.GetAutoTradeSignalOrder(orderId);
                if (order == null)
                {
                    _logger.LogDebug("CheckOrderStatus: order not found for id {OrderId}", orderId);
                    return;
                }

                // Broker update already arrived while the timer was counting down — nothing to do
                if (OrderStatus.IsCompleted(order.Status))
                {
                    _logger.LogDebug("CheckOrderStatus: order {OrderId} already in terminal state {Status} — skipping", orderId, order.Status);
                    return;
                }

                AutoTradeSignalRS? signal = MainRepo.GetAutoTradeSignal(order.AutoTradeSignalId);
                if (signal == null)
                {
                    _logger.LogWarning("CheckOrderStatus: signal not found for order {OrderId}", orderId);
                    return;
                }

                BotRS? bot = GetTradeBot(signal.BotId);
                if (bot == null)
                {
                    _logger.LogWarning("CheckOrderStatus: bot not found for order {OrderId}", orderId);
                    return;
                }

                // Poll the broker for the current order state
                OrderStatusRequestDto req = new()
                {
                    AccountId = bot.AccountId,
                    BrokerRef = order.BrokerRef ?? string.Empty,
                    OrderRef  = order.RequestRef
                };

                OrderStatusDto? status = await _brokerSvc.GetOrderStatus(req, bot.BrokerServiceId);

                if (status != null)
                {
                    _logger.LogDebug("CheckOrderStatus: broker returned status {StatusCode} for order {OrderId}", status.StatusCode, orderId);
                    UpdateOrderStatus(order, signal, status.StatusCode, status.FilledQty, status.AvgFillPrice, status.BrokerRef);
                    if (!OrderStatus.IsCompleted(order.Status))
                    {
                        await _taskScheduler.ScheduleEventAsync(
                               SRC_NAME,
                               $"CheckOrderStatus-{order.Id}",
                               10000,
                               async (sender, e) => await CheckOrderStatus(e), order.Id);
                    }
                }
                else
                {
                    // No response from broker within the scheduled window - reschedulling
                    _logger.LogWarning("CheckOrderStatus: no broker response for order {OrderId} — reschedulling", orderId);
                    await _taskScheduler.ScheduleEventAsync(
                           SRC_NAME,
                           $"CheckOrderStatus-{order.Id}",
                           30000,
                           async (sender, e) => await CheckOrderStatus(e), order.Id);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckOrderStatus: exception for order {OrderId}", orderId);
            }
        }
    }
}
