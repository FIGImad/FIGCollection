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
        private const int MaxMissingSignalRetries = 5;
        private readonly object _missingSignalRetryLock = new();
        private int _missingSignalId = -1;
        private int _missingSignalRetryCount = 0;
        private long _processInvocationSequence = 0;
        private int _activeProcessInvocations = 0;
        private readonly object _processStateLock = new();
        private bool _processRunning;
        private bool _processRerunRequested;

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
                // mark the autotrade as stopped
                if (_autoTradeRec != null)
                {
                    MainRepo.UpsertAutoTrade(_autoTradeRec);
                    _logger?.LogDebug("AutoTrade({0}) - Stopped", _autoTradeRec.Id);
                    _UpdateExecStatus(EAutoTradeStatus.Stopped);
                }
            }

            // Never wait for a running Process callback while holding _lockAccess;
            // ProcessInternal needs that lock in order to finish.
            _taskScheduler.StopAllTasksAsync(_ownerId).GetAwaiter().GetResult();
            return EAutoTradeStatus.Stopped;
        }

        protected async Task Process(object? sender = null, TaskEventArgs? e = null)
        {
            int autoTradeId = _autoTradeRec?.Id ?? -1;

            lock (_processStateLock)
            {
                if (_processRunning)
                {
                    _processRerunRequested = true;
                    _logger.LogDebug(
                        "AutoTrade({AutoTradeId}) - PROCESS_INVOCATION_COALESCED SchedulerTaskId={SchedulerTaskId}; one follow-up pass is pending",
                        autoTradeId,
                        e?.EventName ?? "DIRECT");
                    return;
                }

                _processRunning = true;
            }

            bool completedNormally = false;
            try
            {
                while (true)
                {
                    long invocationId = Interlocked.Increment(ref _processInvocationSequence);
                    int activeInvocations = Interlocked.Increment(ref _activeProcessInvocations);
                    using var logScope = _logger.BeginScope(new Dictionary<string, object>
                    {
                        ["AutoTradeId"] = autoTradeId,
                        ["ProcessInvocationId"] = invocationId
                    });

                    _logger.LogInformation(
                        "AutoTrade({AutoTradeId}) - PROCESS_INVOCATION_STARTED InvocationId={InvocationId}, ActiveInvocations={ActiveInvocations}, SchedulerTaskId={SchedulerTaskId}",
                        autoTradeId,
                        invocationId,
                        activeInvocations,
                        e?.EventName ?? "DIRECT");

                    try
                    {
                        await ProcessCore(invocationId);
                    }
                    finally
                    {
                        int remainingInvocations = Interlocked.Decrement(ref _activeProcessInvocations);
                        _logger.LogInformation(
                            "AutoTrade({AutoTradeId}) - PROCESS_INVOCATION_FINISHED InvocationId={InvocationId}, RemainingInvocations={RemainingInvocations}",
                            autoTradeId,
                            invocationId,
                            remainingInvocations);
                    }

                    lock (_processStateLock)
                    {
                        if (!_processRerunRequested)
                        {
                            _processRunning = false;
                            completedNormally = true;
                            return;
                        }

                        _processRerunRequested = false;
                    }

                    _logger.LogDebug(
                        "AutoTrade({AutoTradeId}) - PROCESS_COALESCED_RERUN starting one follow-up pass",
                        autoTradeId);
                }
            }
            finally
            {
                if (!completedNormally)
                {
                    bool rerunAfterFailure;
                    lock (_processStateLock)
                    {
                        // Clear the owner flag if an unexpected exception escapes
                        // ProcessCore so a later notification can recover processing.
                        rerunAfterFailure = _processRerunRequested;
                        _processRunning = false;
                        _processRerunRequested = false;
                    }

                    if (rerunAfterFailure)
                    {
                        _logger.LogDebug(
                            "AutoTrade({AutoTradeId}) - PROCESS_COALESCED_RERUN scheduled after failed pass",
                            autoTradeId);
                        await _taskScheduler.ScheduleEventAsync(_ownerId, "Process", 10, Process);
                    }
                }
            }
        }

        private async Task ProcessCore(long invocationId)
        {
            int autoTradeId = _autoTradeRec?.Id ?? -1;
            string strategyName = _autoTradeRec?.Strategy ?? "Unknown";
            int maxAttempts = 5;
            var lk = AutoTradeProcessLock.GetLock(autoTradeId);
            List<int> placeOrderList = new();
            bool processingSucceeded = false;
            bool retryProcessing = false;

            // Global slot acquired first (before per-AutoTrade lock) to cap the number
            // of concurrent SQL transactions and prevent cross-AutoTrade deadlocks.
            AutoTradeProcessLock.AcquireGlobalSlot();
            try
            {
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    int retryDelayMs = 0;
                    _logger.LogDebug(
                        "AutoTrade({AutoTradeId}) - PROCESS_LOCK_WAIT InvocationId={InvocationId}, Attempt={Attempt}",
                        autoTradeId,
                        invocationId,
                        attempt + 1);
                    lk.WaitHighPriority();
                    _logger.LogDebug(
                        "AutoTrade({AutoTradeId}) - PROCESS_LOCK_ACQUIRED InvocationId={InvocationId}, Attempt={Attempt}",
                        autoTradeId,
                        invocationId,
                        attempt + 1);

                    int suspectBotId = -1;
                    try
                    {
                        ProcessInternal(out placeOrderList, out suspectBotId);

                        var postcondition = VerifyLatestStartSignalPersisted(autoTradeId, strategyName);
                        if (!postcondition.Satisfied)
                        {
                            int missingRetryDelayMs = RegisterMissingSignalRetry(postcondition.SignalId);
                            if (missingRetryDelayMs > 0)
                            {
                                _logger.LogCritical(
                                    "AutoTrade({AutoTradeId}) - POSTCONDITION_FAILED: Signal({SignalId}) has no AutoTradeSignal. " +
                                    "Scheduling safeguard retry {RetryCount}/{MaxRetries} in {DelayMs} ms. Reason: {Reason}",
                                    autoTradeId,
                                    postcondition.SignalId,
                                    GetMissingSignalRetryCount(),
                                    MaxMissingSignalRetries,
                                    missingRetryDelayMs,
                                    postcondition.Reason);
                                retryProcessing = true;
                                retryDelayMs = missingRetryDelayMs;
                            }
                            else
                            {
                                DateTime timeUTC = DateTime.UtcNow;
                                string timeStr = timeUTC.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);

                                _logger.Alert(
                                    "Source: AUTOTRADE_SIGNAL, Time: {TimeStr}\nAutoTrade({AutoTradeId}) - POSTCONDITION_FAILED: Signal({SignalId}) is still missing after {MaxRetries} retries. Manual intervention required.",
                                    timeStr,
                                    autoTradeId,
                                    postcondition.SignalId,
                                    MaxMissingSignalRetries);

                                // mark bot as suspect
                                if (suspectBotId > 0)
                                {
                                    MainRepo.SetBotStatus(suspectBotId, "SUSPECT");
                                }
                            }
                            break;
                        }

                        ClearMissingSignalRetry(postcondition.SignalId);
                        processingSucceeded = true;
                        break; // if processing is successful, break out of the retry loop
                    }
                    catch (SqlLockException ex)
                    {
                        _logger?.LogWarning($"AutoTrade({autoTradeId}) - Attempt {attempt + 1}/{maxAttempts} - Database lock encountered. Retrying... - {ex.Message}");
                        if (attempt == maxAttempts - 1)
                        {
                            _logger!.Alert(
                                "AutoTrade({AutoTradeId}) - DATABASE_LOCK_RETRIES_EXHAUSTED after {MaxAttempts} attempts while updating AutoTradeSignal. " +
                                "Last error: {ErrorMessage}. Processing will be rescheduled; Manual intervention required if the alert repeats.",
                                autoTradeId,
                                maxAttempts,
                                ex.Message);
                            retryProcessing = true;
                            break;
                        }

                        retryDelayMs = 500;
                    }
                    catch (Microsoft.Data.SqlClient.SqlException ex) when (IsTransientSqlException(ex))
                    {
                        _logger.LogWarning(
                            ex,
                            "AutoTrade({AutoTradeId}) - Attempt {Attempt}/{MaxAttempts} - transient SQL error {SqlErrorNumber}. Retrying",
                            autoTradeId,
                            attempt + 1,
                            maxAttempts,
                            ex.Number);
                        if (attempt == maxAttempts - 1)
                        {
                            if (ex.Number == 1205)
                            {
                                _logger.Alert(
                                    "AutoTrade({AutoTradeId}) - SQL_DEADLOCK_RETRIES_EXHAUSTED after {MaxAttempts} attempts while updating AutoTradeSignal. " +
                                    "SQL error {SqlErrorNumber}: {ErrorMessage}. Processing will be rescheduled; Manual intervention required if the alert repeats.",
                                    autoTradeId,
                                    maxAttempts,
                                    ex.Number,
                                    ex.Message);
                            }
                            else
                            {
                                _logger.Alert(
                                    "AutoTrade({AutoTradeId}) - TRANSIENT_SQL_RETRIES_EXHAUSTED after {MaxAttempts} attempts while updating AutoTradeSignal. " +
                                    "SQL error {SqlErrorNumber}: {ErrorMessage}. Processing will be rescheduled; Manual intervention required if the alert repeats.",
                                    autoTradeId,
                                    maxAttempts,
                                    ex.Number,
                                    ex.Message);
                            }
                            retryProcessing = true;
                            break;
                        }

                        retryDelayMs = 500 * (attempt + 1); // increasing backoff: 500, 1000, 1500, 2000 ms
                    }
                    catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
                    {
                        // A second service instance may have inserted the same
                        // (AutoTradeId, SignalId) while this pass was running.
                        var postcondition = VerifyLatestStartSignalPersisted(autoTradeId, strategyName);
                        if (postcondition.Satisfied)
                        {
                            _logger.LogWarning(
                                "AutoTrade({AutoTradeId}) - Duplicate insert race for Signal({SignalId}) resolved by existing AutoTradeSignal",
                                autoTradeId,
                                postcondition.SignalId);
                            ClearMissingSignalRetry(postcondition.SignalId);
                            processingSucceeded = true;
                            break;
                        }

                        _logger.LogError(ex,
                            "AutoTrade({AutoTradeId}) - Attempt {Attempt}/{MaxAttempts}: duplicate-key conflict remains unresolved",
                            autoTradeId,
                            attempt + 1,
                            maxAttempts);

                        if (attempt == maxAttempts - 1)
                        {
                            _logger!.Alert(
                                "AutoTrade({AutoTradeId}) - DUPLICATE_SIGNAL_CONFLICT_UNRESOLVED after {MaxAttempts} attempts. " +
                                "SQL error {SqlErrorNumber}: {ErrorMessage}. Processing has stopped; manual intervention is required.",
                                autoTradeId,
                                maxAttempts,
                                ex.Number,
                                ex.Message);
                            //TryMarkBotSuspect(suspectBotId,
                            //    $"Duplicate-key retries were exhausted while closing AutoTrade {autoTradeId}");
                            break;
                        }

                        retryDelayMs = 500 * (attempt + 1);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, $"AutoTrade({autoTradeId}) - Attempt {attempt + 1}/{maxAttempts} - exception.");
                        if (attempt == maxAttempts - 1)
                        {
                            _logger!.Alert(
                                "AutoTrade({AutoTradeId}) - PROCESSING_RETRIES_EXHAUSTED after {MaxAttempts} attempts. " +
                                "Last error: {ErrorMessage}. Processing has stopped; manual intervention is required.",
                                autoTradeId,
                                maxAttempts,
                                ex.Message);
                            //TryMarkBotSuspect(suspectBotId,
                            //    $"Processing retries were exhausted while closing AutoTrade {autoTradeId}: {ex.Message}");
                            break;
                        }

                        retryDelayMs = 500 * (attempt + 1);
                    }
                    finally
                    {
                        lk.Release(); // released before any broker I/O
                    }

                    // Do not hold the per-AutoTrade lock or block a thread-pool thread
                    // during deadlock backoff. Broker callbacks can update order state
                    // while this processing pass waits to retry.
                    if (retryDelayMs > 0)
                        await Task.Delay(retryDelayMs);
                }
            }
            finally
            {
                AutoTradeProcessLock.ReleaseGlobalSlot();
            }

            // Dispatch broker calls entirely outside the priority lock so they
            // never delay the next trade processing cycle.
            if (processingSucceeded)
            {
                foreach (var orderId in placeOrderList)
                {
                    if (orderId > 0)
                    {
                        bool sent = await _orderMgt.PlaceOrderAsync(orderId);
                        if (!sent)
                        {
                            _logger?.LogError(
                                "AutoTrade({AutoTradeId}) - Order {OrderId} was committed but could not be sent to FIGBrokerSvc",
                                autoTradeId,
                                orderId);
                        }
                    }
                }

                _logger?.LogInformation(
                    "AutoTrade({AutoTradeId}) - PROCESS_COMPLETED for strategy {Strategy}; committed order IDs: [{OrderIds}]",
                    autoTradeId,
                    strategyName,
                    string.Join(",", placeOrderList));
            }
            else if (retryProcessing)
            {
                // A one-shot signal wake-up must not be the only opportunity to close a
                // position. If every database attempt fails, enqueue a fresh pass so the
                // signal is retried before its expiry window elapses.
                int delayMs = GetMissingSignalRetryDelayMsOrDefault(10000);
                _logger.LogWarning(
                    "AutoTrade({AutoTradeId}) - PROCESS_RETRY_SCHEDULED for strategy {Strategy} in {DelayMs} ms",
                    autoTradeId,
                    strategyName,
                    delayMs);
                await _taskScheduler.ScheduleEventAsync(_ownerId, "Process", delayMs, Process);
            }
        }

        private readonly record struct SignalPostconditionResult(bool Satisfied, int SignalId, string Reason);

        private SignalPostconditionResult VerifyLatestStartSignalPersisted(int autoTradeId, string strategyName)
        {
            SignalRS? latestSignal = MainRepo.QueryLastSignal(strategyName);
            if (latestSignal == null)
            {
                _logger.LogWarning(
                    "AutoTrade({AutoTradeId}) - PROCESS_OUTCOME=NO_LATEST_SIGNAL for strategy {Strategy}",
                    autoTradeId,
                    strategyName);
                return new SignalPostconditionResult(true, -1, "No latest signal");
            }

            _logger.LogInformation(
                "AutoTrade({AutoTradeId}) - LATEST_SIGNAL SignalId={SignalId}, Strategy={Strategy}, Status={Status}, StartTime={StartTime}, StopTime={StopTime}",
                autoTradeId,
                latestSignal.Id,
                strategyName,
                latestSignal.Status,
                latestSignal.StartTime,
                latestSignal.StopTime);

            if (latestSignal.Status != SignalStatus.Start)
            {
                _logger.LogInformation(
                    "AutoTrade({AutoTradeId}) - PROCESS_OUTCOME=LATEST_SIGNAL_NOT_START Signal({SignalId}), Status={Status}",
                    autoTradeId,
                    latestSignal.Id,
                    latestSignal.Status);
                return new SignalPostconditionResult(true, latestSignal.Id, $"Latest signal status is {latestSignal.Status}");
            }

            AutoTradeSignalRS? persistedSignal = MainRepo
                .GetAutoTradeSignalsFromSingalId(latestSignal.Id)
                .FirstOrDefault(signal => signal.AutoTradeId == autoTradeId);

            if (persistedSignal == null)
            {
                return new SignalPostconditionResult(
                    false,
                    latestSignal.Id,
                    "Eligible START signal was not persisted by the processing pass");
            }

            _logger.LogInformation(
                "AutoTrade({AutoTradeId}) - PROCESS_OUTCOME=SIGNAL_PERSISTED Signal({SignalId}), AutoTradeSignalId={AutoTradeSignalId}, OpenStatus={OpenStatus}, OpenStatusCode={OpenStatusCode}, CloseStatus={CloseStatus}, LastOrderId={LastOrderId}",
                autoTradeId,
                latestSignal.Id,
                persistedSignal.Id,
                persistedSignal.OpenStatus,
                persistedSignal.OpenStatusCode,
                persistedSignal.CloseStatus,
                persistedSignal.LastOrder?.Id ?? -1);

            return new SignalPostconditionResult(true, latestSignal.Id, "AutoTradeSignal exists");
        }

        private int RegisterMissingSignalRetry(int signalId)
        {
            lock (_missingSignalRetryLock)
            {
                if (_missingSignalId != signalId)
                {
                    _missingSignalId = signalId;
                    _missingSignalRetryCount = 0;
                }

                _missingSignalRetryCount++;
                if (_missingSignalRetryCount > MaxMissingSignalRetries)
                    return -1;

                return Math.Min(30000, 1000 * (1 << (_missingSignalRetryCount - 1)));
            }
        }

        private void ClearMissingSignalRetry(int signalId)
        {
            lock (_missingSignalRetryLock)
            {
                if (signalId == -1 || _missingSignalId == signalId)
                {
                    _missingSignalId = -1;
                    _missingSignalRetryCount = 0;
                }
            }
        }

        private int GetMissingSignalRetryCount()
        {
            lock (_missingSignalRetryLock)
            {
                return _missingSignalRetryCount;
            }
        }

        private int GetMissingSignalRetryDelayMsOrDefault(int defaultDelayMs)
        {
            lock (_missingSignalRetryLock)
            {
                if (_missingSignalRetryCount <= 0)
                    return defaultDelayMs;

                return Math.Min(30000, 1000 * (1 << (_missingSignalRetryCount - 1)));
            }
        }

        private static bool IsTransientSqlException(Microsoft.Data.SqlClient.SqlException ex)
        {
            return ex.Number is -2      // command timeout
                or 64                   // connection dropped
                or 233                  // connection initialization failure
                or 1205                 // deadlock victim
                or 4060                 // cannot open database
                or 10928 or 10929       // resource throttling
                or 40197 or 40501 or 40613
                or 49918 or 49919 or 49920
                or 10053 or 10054 or 10060;
        }
        protected void ProcessInternal(out List<int> placeOrderList, out int suspectBotId)
        {
            suspectBotId = -1;
            int suspectQty = 0;
            lock (_lockAccess)
            {
                int autoTradeId = _autoTradeRec?.Id ?? -1;
                string autoTradeName = _autoTradeRec?.Name ?? "Unknown";
                string strategyName = _autoTradeRec?.Strategy ?? "Unknown";
                int botId = _autoTradeRec?.BotId ?? -1;
                BotRS? bot = MainRepo.GetBot(botId);

                // get current time in EPOCH UTC
                long currentTime = DateTimeUtil.CurrentUTCUnixTime();

                _logger.LogInformation(
                    "AutoTrade({AutoTradeId}) - PROCESS_BEGIN Strategy={Strategy}, BotId={BotId}, BotStatus={BotStatus}",
                    autoTradeId,
                    strategyName,
                    botId,
                    bot?.Status ?? "NOT_FOUND");


                try
                {
                    // use the out parameters directly so they are populated before returning
                    placeOrderList = new();


                    // HANDLE PENDING SIGNALS
                    // ------------------------------------------------------------------------------------
                    var pendingAutoTradeSignal = MainRepo.GetTopAutoTradeSignal(autoTradeId);
                    SignalRS? pendingSignal = pendingAutoTradeSignal != null ? MainRepo.GetSignal(pendingAutoTradeSignal.SignalId) : null;
                    if (pendingAutoTradeSignal == null)
                    {
                        _logger.LogInformation("AutoTrade({AutoTradeId}) - PENDING_SIGNAL=NONE", autoTradeId);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "AutoTrade({AutoTradeId}) - PENDING_SIGNAL AutoTradeSignalId={AutoTradeSignalId}, SignalId={SignalId}, OpenStatus={OpenStatus}, OpenStatusCode={OpenStatusCode}, CloseStatus={CloseStatus}, CloseStatusCode={CloseStatusCode}, FilledQty={FilledQty}, ManualQty={ManualQty}",
                            autoTradeId,
                            pendingAutoTradeSignal.Id,
                            pendingAutoTradeSignal.SignalId,
                            pendingAutoTradeSignal.OpenStatus,
                            pendingAutoTradeSignal.OpenStatusCode,
                            pendingAutoTradeSignal.CloseStatus,
                            pendingAutoTradeSignal.CloseStatusCode,
                            pendingAutoTradeSignal.FilledQty,
                            pendingAutoTradeSignal.ManualQty);
                    }

                    if (pendingAutoTradeSignal != null && pendingSignal == null)
                    {
                        _logger.LogError(
                            "AutoTrade({AutoTradeId}) - PENDING_SIGNAL_DATA_MISSING: Signal({SignalId}) referenced by AutoTradeSignal({AutoTradeSignalId}) was not found",
                            autoTradeId,
                            pendingAutoTradeSignal.SignalId,
                            pendingAutoTradeSignal.Id);
                    }
                    if (pendingAutoTradeSignal != null && pendingSignal != null)
                    {

                        bool isSignalHandled = false;

                        // Handle pending signal with no close status
                        // only interested in signals that has not yet been closed
                        if (pendingAutoTradeSignal.CloseStatus == OrderStatus.NONE && pendingSignal.Status == SignalStatus.Stop)
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
                                _logger?.Alert($"AutoTrade({autoTradeId}) - Closing unprocessed signal id: {pendingSignal.Id}, for strategy {strategyName}, AutoTrade: {autoTradeName}, Manual intervention required.");
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
                                _logger?.Alert($"AutoTrade({autoTradeId}) - Failed closing incomplete signal for signal id: {pendingSignal.Id}, strategy {strategyName}, AutoTrade: {autoTradeName}, Manual intervention required, check if the order is filled and close it manually.");
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
                                    _logger?.Alert($"AutoTrade({autoTradeId}) - Partially filled position will be closed... Check transaction for signal id: {pendingSignal.Id},  {strategyName} signal, AutoTrade: {autoTradeName}, Manual intervention required.");
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
                                    suspectQty = qty;
                                    // before adding order, check if stop signal expired
                                    if (IsCloseSignalExpired(pendingSignal))
                                    {
                                        _logger?.LogDebug($"AutoTrade({autoTradeId}) - Stop Signal({pendingSignal.Id}) for strategy {pendingSignal.Strategy} is expired. Marking it as failed");
                                        pendingAutoTradeSignal.CloseStatusCode = OrderStatusCodes.FAIL_EXPIRED;
                                        pendingAutoTradeSignal.CloseStatus = OrderStatusCodes.GetOrderStatus(pendingAutoTradeSignal.CloseStatusCode);
                                        pendingAutoTradeSignal.LastUpdated = -1;
                                        // why is it expired... that's dangerous.. alert and set bot as suspect
                                        _logger?.Alert($"AutoTrade({autoTradeId}) - Failed to close autotrade signal id: {pendingSignal.Id}, - signal expired for strategy {strategyName}, AutoTrade: {autoTradeName}");
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
                                                throw new InvalidOperationException(
                                                    $"AutoTrade({autoTradeId}) - Could not open close-order database transaction");
                                            }
                                            pendingAutoTradeSignal.LastOrder = MainRepo.UpsertAutoTradeSignalOrder(closeOrder, tx);
                                            MainRepo.UpsertAutoTradeSignal(pendingAutoTradeSignal, tx);
                                            MainRepo.CommitTransaction(tx);
                                            placeOrderList.Add(pendingAutoTradeSignal.LastOrder?.Id ?? -1);
                                            isSignalHandled = true;
                                            _logger?.LogInformation(
                                                "AutoTrade({AutoTradeId}) - CLOSE_ORDER_COMMITTED Signal({SignalId}), AutoTradeSignalId={AutoTradeSignalId}, OrderId={OrderId}, Qty={Qty}",
                                                autoTradeId,
                                                pendingSignal.Id,
                                                pendingAutoTradeSignal.Id,
                                                pendingAutoTradeSignal.LastOrder?.Id ?? -1,
                                                qty);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger?.LogCritical(ex, $"AutoTrade({autoTradeId}) - Error processing close signal");
                                            try
                                            {
                                                MainRepo.RollbackTransaction(tx);
                                            }
                                            catch (Exception rollbackEx)
                                            {
                                                _logger?.LogError(rollbackEx,
                                                    "AutoTrade({AutoTradeId}) - Error rolling back close-order transaction",
                                                    autoTradeId);
                                            }
                                            // Restore in-memory state before the outer retry begins.
                                            pendingAutoTradeSignal.CloseStatus = origCloseStatus;
                                            pendingAutoTradeSignal.CloseStatusCode = origCloseStatusCode;
                                            // Preserve SqlException 1205 (and SqlLockException) so Process can
                                            // execute its retry policy. If it is swallowed here, the committed
                                            // order list stays empty and nothing can be dispatched to the broker.
                                            suspectBotId = suspectQty != 0 ? botId : -1;
                                            throw;
                                        }
                                    }
                                }
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
                    if (lastSignal == null)
                    {
                        _logger?.LogWarning(
                            "AutoTrade({AutoTradeId}) - NEW_SIGNAL_CHECK=NO_SIGNAL for strategy {Strategy}",
                            autoTradeId,
                            strategyName);
                    }
                    else
                    {
                        _logger?.LogInformation(
                            "AutoTrade({AutoTradeId}) - NEW_SIGNAL_CHECK SignalId={SignalId}, Status={Status}, PendingSignalId={PendingSignalId}",
                            autoTradeId,
                            lastSignal.Id,
                            lastSignal.Status,
                            pendingAutoTradeSignal?.SignalId ?? -1);
                    }
                    if (lastSignal != null)
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
                                    openAutoTradeSignal.OpenStatus = OrderStatusCodes.GetOrderStatus(openAutoTradeSignal.OpenStatusCode);
                                    openAutoTradeSignal = MainRepo.UpsertAutoTradeSignal(openAutoTradeSignal);  // not through transaction, as we are not doing any other db operation for this signal
                                    _logger?.LogInformation(
                                        "AutoTrade({AutoTradeId}) - OPEN_SIGNAL_RECORDED_EXPIRED Signal({SignalId}), AutoTradeSignalId={AutoTradeSignalId}",
                                        autoTradeId,
                                        lastSignal.Id,
                                        openAutoTradeSignal.Id);
                                }
                                else if (bot == null || !bot.IsActive())
                                {
                                    _logger?.Alert($"AutoTrade({autoTradeId}) - Inactive BOT \"{bot?.Status??"NULL"}\" for strategy {strategyName}. Signal will not open. Manual intervention required");
                                    // set AutoTradeSignal record with NEW status
                                    openAutoTradeSignal.OpenStatusCode = OrderStatusCodes.IGNORED_BOT_INACTIVE;
                                    openAutoTradeSignal.OpenStatus = OrderStatusCodes.GetOrderStatus(openAutoTradeSignal.OpenStatusCode);
                                    openAutoTradeSignal = MainRepo.UpsertAutoTradeSignal(openAutoTradeSignal);  // not through transaction, as we are not doing any other db operation for this signal
                                    _logger?.LogWarning(
                                        "AutoTrade({AutoTradeId}) - OPEN_SIGNAL_BLOCKED_BY_BOT Signal({SignalId}), AutoTradeSignalId={AutoTradeSignalId}, BotId={BotId}, BotStatus={BotStatus}",
                                        autoTradeId,
                                        lastSignal.Id,
                                        openAutoTradeSignal.Id,
                                        botId,
                                        bot?.Status ?? "NOT_FOUND");
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
                                            throw new InvalidOperationException(
                                                $"AutoTrade({autoTradeId}) - Could not open open-order database transaction");
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
                                        _logger?.LogInformation(
                                            "AutoTrade({AutoTradeId}) - OPEN_ORDER_COMMITTED Signal({SignalId}), AutoTradeSignalId={AutoTradeSignalId}, OrderId={OrderId}, Qty={Qty}, RequestRef={RequestRef}",
                                            autoTradeId,
                                            lastSignal.Id,
                                            openAutoTradeSignal.Id,
                                            newOrder?.Id ?? -1,
                                            newOrder?.Qty ?? 0,
                                            newOrder?.RequestRef ?? string.Empty);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger?.LogCritical(ex, $"AutoTrade({autoTradeId}) - Error processing open signal");
                                        try
                                        {
                                            MainRepo.RollbackTransaction(tx);
                                        }
                                        catch (Exception rollbackEx)
                                        {
                                            _logger?.LogError(rollbackEx,
                                                "AutoTrade({AutoTradeId}) - Error rolling back open-order transaction",
                                                autoTradeId);
                                        }
                                        throw;
                                    }
                                }
                            }
                            else
                            {
                                _logger?.LogInformation(
                                    "AutoTrade({AutoTradeId}) - OPEN_SIGNAL_ALREADY_RECORDED Signal({SignalId}), AutoTradeSignalId={AutoTradeSignalId}",
                                    autoTradeId,
                                    lastSignal.Id,
                                    pendingAutoTradeSignal?.Id ?? -1);
                            }
                        }
                        else
                        {
                            _logger?.LogInformation(
                                "AutoTrade({AutoTradeId}) - NEW_SIGNAL_SKIPPED_STATUS Signal({SignalId}), Status={Status}",
                                autoTradeId,
                                lastSignal.Id,
                                lastSignal.Status);
                        }
                    }
                    else if (lastSignal != null)
                    {
                        _logger?.LogInformation(
                            "AutoTrade({AutoTradeId}) - NEW_SIGNAL_SKIPPED_CANCELED Signal({SignalId})",
                            autoTradeId,
                            lastSignal.Id);
                    }
                    // All close-side database work completed and its orders can now
                    // be dispatched. A later terminal close failure is handled by
                    // OrderManagementService/usp_autotrade_signal_upsert.
                    suspectBotId = -1;
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"AutoTrade({autoTradeId}) - Error processing AutoTrade signals: {ex.Message}");
                    throw;
                }
            }
        }

        private void TryMarkBotSuspect(int botId, string reason)
        {
            if (botId <= 0)
                return;

            try
            {
                int changed = MainRepo.SetBotStatus(botId, "SUSPECT");
                if (changed > 0)
                    // usp_bot_set_status writes the durable CRITICAL SystemAlert.
                    // Keep this operational log below the alert-routing threshold.
                    _logger.LogWarning("Bot {BotId} marked SUSPECT: {Reason}", botId, reason);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to mark bot {BotId} SUSPECT: {Reason}", botId, reason);
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
