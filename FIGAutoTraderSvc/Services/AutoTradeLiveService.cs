using FIGCommon.DataAccess;
using FIGCommon.Exceptions;
using FIGCommon.Extensions;
using FIGCommon.Models;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Services;
using FIGCommon.Tasks;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGAutoTradeExSvc.Services
{

    public class AutoTradeLiveService : IAutoTradeService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AutoTradeLiveService> _logger;
        private readonly ITaskSchedulerService _taskScheduler;
        private readonly IOrderManagementService _orderMgt;
        private AutoTradeRS? _autoTradeRec = null;
        private readonly object _lockAccess = new();
        private string _ownerId = "";
        private int openSignalExpiryMinutes = -1;
        private int closeSignalExpiryMinutes = -1;
        private int defaultSignalExpiryMinutes = 10020;

        public AutoTradeLiveService(ILogger<AutoTradeLiveService> logger,
                                    IConfiguration config,
                                    IOrderManagementService orderMgt,
                                    ITaskSchedulerService taskScheduler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _orderMgt = orderMgt ?? throw new ArgumentNullException(nameof(orderMgt));
            _taskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
            _logger.LogInformation("Service is created.");
        }

        #region Accessors
        public bool IsRunning
        {
            get { lock (_lockAccess) { return _autoTradeRec != null && _autoTradeRec.Status == (int)EAutoTradeStatus.Started; } }
        }

        public int OpenSignalExpiryMinutes
        {
            get
            {
                if (openSignalExpiryMinutes <= 0)
                {
                    openSignalExpiryMinutes = _config.GetValue<int>("AutoTradeSettings:OpenSignalExpiryMinutes");
                    openSignalExpiryMinutes = openSignalExpiryMinutes > 0 ? openSignalExpiryMinutes : defaultSignalExpiryMinutes; // default to 20 minutes if not set or invalid
                }
                return openSignalExpiryMinutes;
            }
        }
        public int CloseSignalExpiryMinutes
        {
            get
            {
                if (closeSignalExpiryMinutes <= 0)
                {
                    closeSignalExpiryMinutes = _config.GetValue<int>("AutoTradeSettings:CloseSignalExpiryMinutes");
                    closeSignalExpiryMinutes = closeSignalExpiryMinutes > 0 ? closeSignalExpiryMinutes : defaultSignalExpiryMinutes; // default to 20 minutes if not set or invalid
                }
                return closeSignalExpiryMinutes;
            }
        }

        #endregion Accessors

        public void Init(AutoTradeRS? autoTrade)
        {
            if (autoTrade == null)
            {
                throw new ArgumentNullException(nameof(autoTrade));
            }
            lock (_lockAccess)
            {
                _autoTradeRec = autoTrade;
                _logger?.LogDebug("Initializing AutoTrade instance for Id({0})...", _autoTradeRec.Id);

                if (_autoTradeRec == null)
                {
                    throw new ArgumentNullException(nameof(_autoTradeRec));
                }

                // onwerId for task scheduling
                _ownerId = $"AutoTradeService@{_autoTradeRec.Id}";
            }
        }

        public EAutoTradeStatus Start()
        {
            lock (_lockAccess)
            {
                // update status in case if it is not already in progress (1)
                if (_autoTradeRec != null)
                {
                    //if (_autoTradeRec.Status != (int)EAutoTradeStatus.Started)
                    //{
                        // schedule a task to be sched async
                        MainRepo.UpsertAutoTrade(_autoTradeRec);
                        _logger?.LogDebug("AutoTrade({0}) - Started", _autoTradeRec.Id);
                        _UpdateExecStatus(EAutoTradeStatus.Started);
                        _taskScheduler.ScheduleEventAsync(_ownerId, "Process", 10, Process);
                    //}
                    return (EAutoTradeStatus)_autoTradeRec.Status;
                }
            }
            return EAutoTradeStatus.Failed;
        }

        public EAutoTradeStatus Stop()
        {
            lock (_lockAccess)
            {
                _taskScheduler.StopAllTasksAsync(_ownerId).Wait();
                // mark the autotrade as stopped
                if (_autoTradeRec != null)
                {
                    MainRepo.UpsertAutoTrade(_autoTradeRec);
                    _logger?.LogDebug("AutoTrade({0}) - Stopped", _autoTradeRec.Id);
                    _UpdateExecStatus(EAutoTradeStatus.Stopped);
                }
                return EAutoTradeStatus.Stopped;
            }
        }

        protected Task Process(object? sender = null, TaskEventArgs? e = null)
        {
            int autoTradeId = _autoTradeRec?.Id ?? -1;
            int maxAttempts = 5;
            var lk = AutoTradeProcessLock.GetLock(autoTradeId);
            List<int> placeOrderList = new();
            bool dispatchOrders = false;

            // Global slot acquired first (before per-AutoTrade lock) to cap the number
            // of concurrent SQL transactions and prevent cross-AutoTrade deadlocks.
            AutoTradeProcessLock.AcquireGlobalSlot();
            try
            {
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    lk.WaitHighPriority();
                    try
                    {
                        ProcessInternal(out placeOrderList);
                        dispatchOrders = true;
                        break; // if processing is successful, break out of the retry loop
                    }
                    catch (SqlLockException ex)
                    {
                        _logger?.LogWarning($"AutoTrade({autoTradeId}) - Attempt {attempt + 1}/{maxAttempts} - Database lock encountered. Retrying... - {ex.Message}");
                        if (attempt == maxAttempts - 1)
                            break;

                        Thread.Sleep(500);
                    }
                    catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 1205)
                    {
                        _logger?.LogWarning($"AutoTrade({autoTradeId}) - Attempt {attempt + 1}/{maxAttempts} - SQL deadlock. Retrying... - {ex.Message}");
                        if (attempt == maxAttempts - 1)
                            break;

                        Thread.Sleep(200 * (attempt + 1)); // increasing backoff: 200, 400, 600, 800 ms
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, $"AutoTrade({autoTradeId}) - Attempt {attempt + 1}/{maxAttempts} - exception.");
                        break;
                    }
                    finally
                    {
                        lk.Release(); // released before any broker I/O
                    }
                }
            }
            finally
            {
                AutoTradeProcessLock.ReleaseGlobalSlot();
            }

            // Dispatch broker calls entirely outside the priority lock so they
            // never delay the next trade processing cycle.
            if (dispatchOrders)
            {
                foreach (var orderId in placeOrderList)
                    if (orderId > 0) _ = _orderMgt.PlaceOrderAsync(orderId);
            }

            return Task.CompletedTask;
        }
        protected void ProcessInternal(out List<int> placeOrderList)
        {
            placeOrderList = new();
            lock (_lockAccess)
            {
                int autoTradeId = _autoTradeRec?.Id ?? -1;
                string autoTradeName = _autoTradeRec?.Name ?? "Unknown";
                string strategyName = _autoTradeRec?.Strategy ?? "Unknown";
                // get current time in EPOCH UTC
                long currentTime = DateTimeUtil.CurrentUTCUnixTime();

                _logger?.LogDebug("AutoTrade({0}) - Processing Signal Event", autoTradeId);


                try
                {
                    // use the out parameters directly so they are populated before returning
                    placeOrderList = new();


                    // HANDLE PENDING SIGNALS
                    // ------------------------------------------------------------------------------------
                    var pendingAutoTradeSignal = MainRepo.GetTopAutoTradeSignal(autoTradeId);
                    SignalRS? pendingSignal = pendingAutoTradeSignal != null ? MainRepo.GetSignal(pendingAutoTradeSignal.SignalId) : null;
                    if (pendingAutoTradeSignal != null && pendingSignal != null)
                    {

                        bool isSignalHandled = false;

                        // Handle pending signal with no close status
                        // only interested in signals that are not canceled amd has not yet been closed
                        if (!pendingSignal.Canceled && pendingAutoTradeSignal.CloseStatus == OrderStatus.NONE && pendingSignal.Status == SignalStatus.Stop)
                        {
                            // Check if pending signal has open status as processing or new,
                            // if open is still not yet processed -  easy, cancel the signal and mark as canceled for both open and close
                            if (pendingAutoTradeSignal.OpenStatus == OrderStatus.NONE || pendingAutoTradeSignal.OpenStatus == OrderStatus.NEW)
                            {
                                pendingAutoTradeSignal.OpenStatusCode = OrderStatusCodes.FAIL_CLOSED_BEFORE_SUBMISSION;
                                pendingAutoTradeSignal.OpenStatus = OrderStatusCodes.GetOrderStatus(pendingAutoTradeSignal.OpenStatusCode);
                                pendingAutoTradeSignal.CloseStatusCode = OrderStatusCodes.FAIL_CLOSING_INCOMPLETE_ORDER;
                                pendingAutoTradeSignal.CloseStatus = OrderStatusCodes.GetOrderStatus(pendingAutoTradeSignal.CloseStatusCode);
                                pendingAutoTradeSignal.LastUpdated = -1;
                                _logger?.Alert($"AutoTrade({autoTradeId}) - Closing unprocessed signal for strategy {strategyName}, AutoTrade: {autoTradeName}");
                            }
                            // in the case when it is showing as processing... check the status of the order
                            // if order is still processing then we can cancel the order and mark the signal as canceled by system
                            else if (pendingAutoTradeSignal.OpenStatus == OrderStatus.PROCESSING)
                            {
                                pendingAutoTradeSignal.OpenStatusCode = OrderStatusCodes.INPROGRESS_CANCEL;
                                pendingAutoTradeSignal.OpenStatus = OrderStatusCodes.GetOrderStatus(pendingAutoTradeSignal.OpenStatusCode);
                                pendingAutoTradeSignal.CloseStatusCode = OrderStatusCodes.FAIL_CLOSING_INCOMPLETE_ORDER;
                                pendingAutoTradeSignal.CloseStatus = OrderStatusCodes.GetOrderStatus(pendingAutoTradeSignal.CloseStatusCode);
                                pendingAutoTradeSignal.LastUpdated = -1;
                                _logger?.Alert($"AutoTrade({autoTradeId}) - Canceling closing incomplete signal for strategy {strategyName}, AutoTrade: {autoTradeName}, Manual intervention is required, check if the order is filled and close it manually.");
                            }
                            else if (pendingAutoTradeSignal.OpenStatus == OrderStatus.FAILED || pendingAutoTradeSignal.OpenStatus == OrderStatus.CANCELED)
                            {
                                // this is the case where stop signal comes after start signal but start signal is already marked as failed,
                                // we can just mark this stop signal as failed as well
                                pendingAutoTradeSignal.CloseStatusCode = OrderStatusCodes.FAIL_CLOSING_INCOMPLETE_ORDER;
                                pendingAutoTradeSignal.CloseStatus = OrderStatusCodes.GetOrderStatus(pendingAutoTradeSignal.CloseStatusCode);
                                pendingAutoTradeSignal.LastUpdated = -1;
                            }
                            else
                            {
                                // here open status is filled partially or filled manually,
                                // in any of cases we can just process the stop signal 
                                if (pendingAutoTradeSignal.OpenStatus == OrderStatus.FILLED_PARTIALLY)
                                {
                                    // cancel the existing open order for the signal if still partially filled and mark it as partially filled - final
                                    //todo - Alert
                                }
                                pendingAutoTradeSignal.LastUpdated = -1;

                                // make a stop order with the qty 
                                int qty = -(pendingAutoTradeSignal.OpenStatus == OrderStatus.FILLED_MANUALLY ? pendingAutoTradeSignal.ManualQty : pendingAutoTradeSignal.FilledQty);
                                if (qty == 0)
                                {
                                    pendingAutoTradeSignal.CloseStatusCode = OrderStatusCodes.FAIL_NO_OPEN_POS;
                                    pendingAutoTradeSignal.CloseStatus = OrderStatusCodes.GetOrderStatus(pendingAutoTradeSignal.CloseStatusCode);
                                    pendingAutoTradeSignal.LastUpdated = -1;
                                }
                                else
                                {
                                    // before adding order, check if stop signal expired
                                    if (IsCloseSignalExpired(pendingSignal))
                                    {
                                        _logger?.LogDebug($"AutoTrade({autoTradeId}) - Stop Signal({pendingSignal.Id}) for strategy {pendingSignal.Strategy} is expired. Marking it as failed");
                                        pendingAutoTradeSignal.CloseStatusCode = OrderStatusCodes.FAIL_EXPIRED;
                                        pendingAutoTradeSignal.CloseStatus = OrderStatusCodes.GetOrderStatus(pendingAutoTradeSignal.CloseStatusCode);
                                        pendingAutoTradeSignal.LastUpdated = -1;
                                    }
                                    else
                                    {
                                        // add order for stop signal
                                        // Save originals before mutating so we can restore on tx failure
                                        var origCloseStatus = pendingAutoTradeSignal.CloseStatus;
                                        var origCloseStatusCode = pendingAutoTradeSignal.CloseStatusCode;
                                        pendingAutoTradeSignal.CloseStatusCode = OrderStatusCodes.INPROGRESS;
                                        pendingAutoTradeSignal.CloseStatus = OrderStatusCodes.GetOrderStatus(pendingAutoTradeSignal.CloseStatusCode);
                                        pendingAutoTradeSignal.LastUpdated = -1;
                                        var closeOrder = new AutoTradeSignalOrderRS()
                                        {
                                            Id = -1,
                                            AutoTradeSignalId = pendingAutoTradeSignal.Id,
                                            OrderTag = OrderTagDef.CLOSE,
                                            OrderTime = currentTime,
                                            Type = OrderTypes.MARKET,
                                            LimitPrice = pendingSignal.StopPrice ?? 0,
                                            Qty = qty,
                                            QtyFilled = 0,
                                            AvgFillPrice = 0M,
                                            Status = OrderStatus.NEW,
                                            StatusCode = -1,
                                            RequestRef = Guid.NewGuid().ToString(),
                                            BrokerRef = "",
                                            CancelRequest = false,
                                            LastUpdated = -1
                                        };
                                        //_logger?.Alert($"AutoTrade({autoTradeId}) - Closing AutoTrade for Signal: {strategyName}, AutoTrade: {autoTradeName}");
                                        SqlTransaction? tx = null;
                                        try
                                        {
                                            tx = MainRepo.OpenTransaction();
                                            if (tx == null)
                                            {
                                                _logger?.LogCritical($"AutoTrade({autoTradeId}) - Error opening database transaction - critical");
                                            }
                                            pendingAutoTradeSignal.LastOrder = MainRepo.UpsertAutoTradeSignalOrder(closeOrder, tx);
                                            MainRepo.UpsertAutoTradeSignal(pendingAutoTradeSignal, tx);
                                            MainRepo.CommitTransaction(tx);
                                            placeOrderList.Add(pendingAutoTradeSignal.LastOrder?.Id ?? -1);
                                            isSignalHandled = true;
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger?.LogCritical($"AutoTrade({autoTradeId}) - Error processing close signal: {ex.Message}");
                                            MainRepo.RollbackTransaction(tx);
                                            // Restore in-memory state so the fallthrough UpsertAutoTradeSignal
                                            // writes the original values, not the failed INPROGRESS state.
                                            pendingAutoTradeSignal.CloseStatus = origCloseStatus;
                                            pendingAutoTradeSignal.CloseStatusCode = origCloseStatusCode;
                                        }
                                    }
                                }
                            }
                        }
                        else if (pendingSignal.Canceled)
                        {
                            // Check if pending signal has open status as processing or new,
                            // if open is still not yet processed -  easy, cancel the signal and mark as canceled for both open and close
                            if (pendingAutoTradeSignal.OpenStatus == OrderStatus.NONE || pendingAutoTradeSignal.OpenStatus == OrderStatus.NEW)
                            {
                                pendingAutoTradeSignal.OpenStatusCode = OrderStatusCodes.FAIL_CANCELED_NOT_SUBMITTED;
                                pendingAutoTradeSignal.OpenStatus = OrderStatusCodes.GetOrderStatus(pendingAutoTradeSignal.CloseStatusCode);
                                pendingAutoTradeSignal.CloseStatusCode = OrderStatusCodes.FAIL_CANCELED_NOT_SUBMITTED;
                                pendingAutoTradeSignal.CloseStatus = OrderStatusCodes.GetOrderStatus(pendingAutoTradeSignal.CloseStatusCode);
                                pendingAutoTradeSignal.LastUpdated = -1;
                            }
                            // in the case when it is showing as processing... check the status of the order
                            // if order is still processing then we can cancel the order and mark the signal as canceled by system
                            else if (pendingAutoTradeSignal.OpenStatus == OrderStatus.PROCESSING)
                            {
                                //pendingAutoTradeSignal.OpenStatusCode = OrderStatusCodes.FAIL_CANCELED_NOT_SUBMITTED;
                                //pendingAutoTradeSignal.OpenStatus = OrderStatusCodes.GetOrderStatus(pendingAutoTradeSignal.CloseStatusCode);
                                pendingAutoTradeSignal.CloseStatusCode = OrderStatusCodes.FAIL_CANCELED_NOT_SUBMITTED;
                                pendingAutoTradeSignal.CloseStatus = OrderStatusCodes.GetOrderStatus(pendingAutoTradeSignal.CloseStatusCode);
                                pendingAutoTradeSignal.LastUpdated = -1;

                                // cancel order
                                //todo - Alert
                            }
                            else if (pendingAutoTradeSignal.OpenStatus == OrderStatus.FAILED || pendingAutoTradeSignal.OpenStatus == OrderStatus.CANCELED)
                            {
                                // this is the case where stop signal comes after start signal but start signal is already marked as failed,
                                // we can just mark this stop signal as failed as well
                                pendingAutoTradeSignal.CloseStatusCode = OrderStatusCodes.FAIL_CANCELED;
                                pendingAutoTradeSignal.CloseStatus = OrderStatusCodes.GetOrderStatus(pendingAutoTradeSignal.CloseStatusCode);
                                pendingAutoTradeSignal.LastUpdated = -1;
                            }
                            else
                            {
                                // here open status is filled partially or filled manually,
                                // leave the close status unchanged in case if the signal is going to be canceled
                                pendingAutoTradeSignal.LastUpdated = -1;
                            }
                        }
                        if (!isSignalHandled)
                        {
                            MainRepo.UpsertAutoTradeSignal(pendingAutoTradeSignal);
                        }
                    }


                    // HANDLE NEW SIGNALS
                    // ------------------------------------------------------------------------------------
                    // Get Last Signal for the AutoTrade
                    // get last signal for the strategy
                    SignalRS? lastSignal = MainRepo.QueryLastSignal(strategyName);
                    if (lastSignal != null && !lastSignal.Canceled)
                    {
                        if (lastSignal.Status == SignalStatus.Start)
                        {
                            // check if last signal is already processed 
                            if ((pendingAutoTradeSignal?.SignalId??-1) != lastSignal.Id)
                            {
                                int qty = _autoTradeRec?.Qty ?? 0;
                                _logger?.LogDebug($"AutoTrade({autoTradeId}) - Found new signal {lastSignal.Id} for strategy {strategyName}, status: {lastSignal.Status}");
                                // calculate Qty
                                AutoTradeSignalRS openAutoTradeSignal = new AutoTradeSignalRS()
                                {
                                    Id = -1,
                                    AutoTradeId = autoTradeId,
                                    SignalId = lastSignal.Id,
                                    Tag = Guid.NewGuid().ToString(),
                                    BotId = _autoTradeRec?.BotId ?? -1,
                                    TickerId = _autoTradeRec?.TickerId ?? -1,
                                    OpenStatus = OrderStatus.PROCESSING,
                                    OpenStatusCode = OrderStatusCodes.UNDEFINED,
                                    OpenAvgPrice = 0M,
                                    CloseStatus = OrderStatus.NONE,
                                    CloseStatusCode = OrderStatusCodes.UNDEFINED,
                                    CloseAvgPrice = 0M,
                                    OrigQty = lastSignal.Side > 0 ? qty : lastSignal.Side < 0 ? -qty : 0,
                                    FilledQty = 0,
                                    ManualQty = 0,
                                    LastUpdated = -1
                                };

                                // so we have a new signal which is not processed yet, check if it is still valid (not expired)
                                if (IsOpenSignalExpired(lastSignal))
                                {
                                    _logger?.LogDebug($"AutoTrade({autoTradeId}) - Signal({lastSignal.Id}) for strategy {strategyName} is expired. Will be added as Expired");
                                    // set AutoTradeSignal record with EXPIRED status
                                    openAutoTradeSignal.OpenStatusCode = OrderStatusCodes.FAIL_EXPIRED;
                                    openAutoTradeSignal.OpenStatus = OrderStatusCodes.GetOrderStatus(openAutoTradeSignal.CloseStatusCode);
                                    openAutoTradeSignal = MainRepo.UpsertAutoTradeSignal(openAutoTradeSignal);  // not through transaction, as we are not doing any other db operation for this signal
                                }
                                else
                                {
                                    // insert an order 
                                    SqlTransaction? tx = null;
                                    try
                                    {
                                        tx = MainRepo.OpenTransaction();
                                        if (tx == null)
                                        {
                                            _logger?.LogCritical($"AutoTrade({autoTradeId}) - Error opening database transaction - critical");
                                        }
                                        openAutoTradeSignal = MainRepo.UpsertAutoTradeSignal(openAutoTradeSignal, tx);
                                        var newOrder = new AutoTradeSignalOrderRS()
                                        {
                                            Id = -1,
                                            AutoTradeSignalId = openAutoTradeSignal.Id,
                                            OrderTag = OrderTagDef.OPEN,
                                            OrderTime = currentTime,
                                            Type = OrderTypes.MARKET,
                                            LimitPrice = lastSignal.StartPrice ?? 0,
                                            Qty = openAutoTradeSignal.OrigQty,
                                            QtyFilled = 0,
                                            AvgFillPrice = 0M,
                                            Status = OrderStatus.NEW,
                                            StatusCode = -1,
                                            RequestRef = Guid.NewGuid().ToString(),
                                            BrokerRef = "",
                                            CancelRequest = false,
                                            LastUpdated = -1
                                        };
                                        newOrder = MainRepo.UpsertAutoTradeSignalOrder(newOrder, tx);
                                        MainRepo.CommitTransaction(tx);
                                        placeOrderList.Add(newOrder?.Id ?? -1); // only after commit succeeds
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger?.LogCritical($"AutoTrade({autoTradeId}) - Error processing open signal: {ex.Message}");
                                        MainRepo.RollbackTransaction(tx);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"AutoTrade({autoTradeId}) - Error processing AutoTrade signals: {ex.Message}");
                    throw;
                }
            }
        }

        // unsafe method
        private void _UpdateExecStatus(EAutoTradeStatus status)
        {
            if (_autoTradeRec != null)
            {
                int liveStatus = _autoTradeRec.Status;
                bool isInProgress = (liveStatus == (int)EAutoTradeStatus.Started);
                switch (status)
                {
                    case EAutoTradeStatus.Started:
                        _autoTradeRec.ExecStatus.Started();
                        liveStatus = (int)EAutoTradeStatus.Started;
                        break;
                    case EAutoTradeStatus.Stopped:
                        _autoTradeRec.ExecStatus.Stopped();
                        liveStatus = (int)EAutoTradeStatus.Stopped;
                        break;
                    case EAutoTradeStatus.Failed:
                        _autoTradeRec.ExecStatus.Failed();
                        liveStatus = (int)EAutoTradeStatus.Failed;
                        break;
                    case EAutoTradeStatus.Completed:
                        _autoTradeRec.ExecStatus.Completed();
                        liveStatus = (int)EAutoTradeStatus.Completed;
                        break;
                }
                if (liveStatus != _autoTradeRec.Status)
                {
                    _autoTradeRec.Status = liveStatus;
                    MainRepo.UpdateAutoTradeStatus(_autoTradeRec.Id, liveStatus);
                }
            }
            else
            {
                return;
            }
        }

        public void WakeUp()
        {
            lock (_lockAccess)
            {
                if (_autoTradeRec != null && _autoTradeRec.Status == (int)EAutoTradeStatus.Started)
                {
                    _taskScheduler.ScheduleEventAsync(_ownerId, "Process", 10, Process);
                }
            }
        }

        public AutoTradeExecStatus? GetExecStatus()
        {
            lock (_lockAccess)
            {
                if (_autoTradeRec != null)
                {
                    return new(_autoTradeRec.ExecStatus);
                }
                return null;
            }
        }

        public bool IsOpenSignalExpired(SignalRS signal)
        {
            // get current time in EPOCH UTC
            long currentTime = DateTimeUtil.CurrentUTCUnixTime();

            // get Interval length in seconds
            int intervalSeconds = signal.StrategyConfig?.StudyCol?.Interval?.IntervalLen ?? 0;

            long signalExpiryTime = (signal.StartTime ?? 0) + intervalSeconds + (OpenSignalExpiryMinutes * 60);
            return signalExpiryTime < currentTime;
        }

        public bool IsCloseSignalExpired(SignalRS signal)
        {
            // get current time in EPOCH UTC
            long currentTime = DateTimeUtil.CurrentUTCUnixTime();

            // get Interval length in seconds
            int intervalSeconds = signal.StrategyConfig?.StudyCol?.Interval?.IntervalLen ?? 0;
            long signalExpiryTime = (signal.StopTime ?? 0) + intervalSeconds + (CloseSignalExpiryMinutes * 60);
            return signalExpiryTime < currentTime;
        }


    }
}
