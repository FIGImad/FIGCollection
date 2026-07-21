using FIGAutoTraderAdminSvc.Services;
using FIGCommon.DataAccess;
using FIGCommon.Models;
using FIGCommon.Services;
using FIGCommon.Tasks;

namespace FIGAutoTradeExSvc.Services
{
    public class DeviceMonitorService : IHostedService, IDisposable
    {
        private const string SRC_NAME = "DeviceMonitorService";

        private readonly ILogger<DeviceMonitorService> _logger;
        private readonly EventMonitorService _eventMonitor;
        private readonly ATSMonitorService _atsMonitorService;
        private readonly ITaskSchedulerService _taskScheduler;

        private static string QUEUENAME = "DEVICE";


        public DeviceMonitorService(
            ILogger<DeviceMonitorService> logger,
            EventMonitorService eventMonitor,
            ATSMonitorService atsMonitorService,
            ITaskSchedulerService taskScheduler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventMonitor = eventMonitor ?? throw new ArgumentNullException(nameof(eventMonitor));
            _atsMonitorService = atsMonitorService ?? throw new ArgumentNullException(nameof(atsMonitorService));
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
                _logger.LogInformation("Received change event from study id: {0}", eventData?.EventId ?? "-1");

                // convert eventData EventId to int
                if (eventData != null && eventData.EventType == QUEUENAME)
                {
                    var deviceId = eventData.EventId ?? "";
                    if (!string.IsNullOrEmpty(deviceId))
                    {
                        // wake up all device monitor task 
                        _taskScheduler.ScheduleEventAsync(SRC_NAME, $"ReportUpdate_{deviceId}", 50, ReportDeviceStatus, deviceId);
                    }
                    else
                    {
                        _logger.LogWarning($"OnSignalChange: Received invalid deviceId: {deviceId}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing EVENTA change");
            }
        }

        protected Task ReportDeviceStatus(object? sender = null, TaskEventArgs? e = null)
        {
            // let each device monitor task decide if they need to do anything based on the signal changes
            var deviceId = e?.Args != null && e.Args.Length > 0 ? e.Args[0]?.ToString() : null;
            try
            {
                // get device id from event args
                if (!string.IsNullOrEmpty(deviceId)) 
                {
                    var device = MainRepo.GetDevice(deviceId);
                    if (device != null)
                    {
                        _atsMonitorService.ReportStatus(device, device.ConnectionId.Length > 0, true);
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error reporting device status for device Id: {DeviceId}", deviceId ?? "");
            }
            return Task.CompletedTask;
        } 


        public void Dispose()
        {
            _logger.LogInformation($"Unsubscribed from {QUEUENAME} changes");
        }

    }
}