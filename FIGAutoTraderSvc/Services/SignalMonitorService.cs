using FIGCommon.Models;
using FIGCommon.Services;
using FIGCommon.Tasks;
using FIGCommon.Utilities;

namespace FIGAutoTradeExSvc.Services
{
    public class SignalMonitorService : IHostedService, IDisposable
    {
        private const string SRC_NAME = "SignalMonitorService";

        private readonly ILogger<SignalMonitorService> _logger;
        private readonly EventMonitorService _eventMonitor;
        private readonly IAutoTradeManagementService _autoTradeManagementService;
        private readonly ITaskSchedulerService _taskScheduler;

        private static string QUEUENAME = "SIGNAL";


        public SignalMonitorService(
            ILogger<SignalMonitorService> logger,
            EventMonitorService eventMonitor,
            IAutoTradeManagementService autoTradeManagementService,
            ITaskSchedulerService taskScheduler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventMonitor = eventMonitor ?? throw new ArgumentNullException(nameof(eventMonitor));
            _autoTradeManagementService = autoTradeManagementService ?? throw new ArgumentNullException(nameof(autoTradeManagementService));
            _taskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _eventMonitor.Subscribe(QUEUENAME, OnSignalChange);
            _logger.LogInformation($"Subscribed to {QUEUENAME} changes");

            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _eventMonitor.Unsubscribe(QUEUENAME, OnSignalChange);
            _logger.LogInformation($"Unsubscribed from {QUEUENAME} changes");

            return Task.CompletedTask;
        }

        private void OnSignalChange(EventRS? eventData)
        {
            try
            {
                _logger.LogInformation("Received change event from signal with strategy: {0}", eventData?.EventId ?? "Invalid_Strategy");

                // convert eventData EventId to int
                if (eventData != null && eventData.EventType == QUEUENAME)
                {
                    string strategyName = eventData?.EventId ?? "";
                    // convert eventData.EventId (string) to int as autoTradeId, use try parse
                    //var strategyConfigId = TypeConvertUtil.GetIntegerValue(eventData.EventId) ?? -1;
                    if (strategyName.Length > 0)
                    {
                        // wake up all autotrade task 
                        _taskScheduler.ScheduleEventAsync(SRC_NAME, $"WakeupCollection_{strategyName}", 50, WakeupAutoTrade, strategyName);
                    }
                    else
                    {
                        _logger.LogWarning($"OnSignalChange: Received invalid Strategy: {strategyName}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing EVENTA change");
            }
        }

        protected Task WakeupAutoTrade(object? sender = null, TaskEventArgs? e = null)
        {
            // let each autoTrade task decide if they need to do anything based on the signal changes
            _autoTradeManagementService.WakeupAll();
            return Task.CompletedTask;
        }


        public void Dispose()
        {
            _logger.LogInformation($"Unsubscribed from {QUEUENAME} changes");
        }

    }
}