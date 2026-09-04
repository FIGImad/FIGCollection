using FIGCommon.DataAccess;
using FIGCommon.Models;
using FIGCommon.Services;
using FIGCommon.Extensions;
using FIGCommon.Utilities;

namespace FIGAutoTradeExSvc.Services
{
    public class SystemAlertMonitorService : IHostedService, IDisposable
    {
        private const string SRC_NAME = "SystemAlertMonitorService";

        private readonly ILogger<SystemAlertMonitorService> _logger;
        private readonly EventMonitorService _eventMonitor;
        private readonly ITaskSchedulerService _taskScheduler;

        private static string QUEUENAME = "SYSTEMALERT";


        public SystemAlertMonitorService(
            ILogger<SystemAlertMonitorService> logger,
            EventMonitorService eventMonitor,
            ITaskSchedulerService taskScheduler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventMonitor = eventMonitor ?? throw new ArgumentNullException(nameof(eventMonitor));
            _taskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _eventMonitor.Subscribe(QUEUENAME, OnChange);
            _logger.LogInformation($"Subscribed to {QUEUENAME} changes");

            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _eventMonitor.Unsubscribe(QUEUENAME, OnChange);
            _logger.LogInformation($"Unsubscribed from {QUEUENAME} changes");

            return Task.CompletedTask;
        }

        private void OnChange(EventRS? eventData)
        {
            try
            {
                _logger.LogInformation("Received change event from SYSTEMALERT");

                // convert eventData EventId to int
                if (eventData != null && eventData.EventType == QUEUENAME)
                {
                    List<SystemAlertRS> alerts = MainRepo.GetPendingSystemAlerts(100);
                    alerts.ForEach(alert =>
                    {
                        // alert time in UTC
                        DateTime timeUTC = DateTimeUtil.ConvertUnixTimeToDateTime(alert.RawTime);
                        string timeStr = timeUTC.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
                        _logger.Alert($"Source: {alert.Source}, Time: {timeStr}\n{alert.Message}");
                    } );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SYSTEMALERT change");
            }
        }


        public void Dispose()
        {
            _logger.LogInformation($"Unsubscribed from {QUEUENAME} changes");
        }

    }
}