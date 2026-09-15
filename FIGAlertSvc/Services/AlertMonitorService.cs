using FIGCommon.Models;
using FIGCommon.Services;
using System.Runtime.Versioning;
using System.Threading.Channels;

namespace FIGAlertSvc.Services
{
    [SupportedOSPlatform("windows")]
    public sealed class AlertMonitorService : BackgroundService
    {
        private const string EventType = "ALERT_IMMEDIATE";
        private readonly EventMonitorService _eventMonitor;
        private readonly AlertDispatchService _dispatcher;
        private readonly ILogger<AlertMonitorService> _logger;
        private readonly Channel<bool> _wakeups = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.DropWrite
        });

        public AlertMonitorService(EventMonitorService eventMonitor, AlertDispatchService dispatcher,
            ILogger<AlertMonitorService> logger)
        {
            _eventMonitor = eventMonitor;
            _dispatcher = dispatcher;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _eventMonitor.Subscribe(EventType, OnChange);
            try
            {
                // Recover pending alerts after restart, without waiting for a new event.
                _wakeups.Writer.TryWrite(true);
                await foreach (var wakeup in _wakeups.Reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        await _dispatcher.DispatchImmediateAlertsAsync(stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Immediate alert processing failed; scheduled dispatch remains available.");
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown.
            }
            finally
            {
                _eventMonitor.Unsubscribe(EventType, OnChange);
                _wakeups.Writer.TryComplete();
            }
        }

        private void OnChange(EventRS? eventData)
        {
            // Keep SQL notification callbacks short; coalesce bursts without losing
            // a wake-up that arrives while the dispatcher is already running.
            if (eventData?.EventType == EventType && eventData.EventId == "-1")
                _wakeups.Writer.TryWrite(true);
        }
    }
}