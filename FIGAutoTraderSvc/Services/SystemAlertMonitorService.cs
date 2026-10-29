using FIGCommon.DataAccess;
using FIGCommon.Models;
using FIGCommon.Models.Alert;
using FIGCommon.Models.FIGController;
using FIGCommon.Models.LogMonitor;
using FIGCommon.Services;
using FIGCommon.Services.LogMonitor;
using System.Globalization;
using System.Threading.Channels;

namespace FIGAutoTradeExSvc.Services;

public sealed class SystemAlertMonitorService : BackgroundService
{
    private readonly ILogger<SystemAlertMonitorService> _logger;
    private readonly EventMonitorService _events;
    private readonly LogAlertRouter _router;
    private readonly ControllerConfig _controller = new();
    private readonly Channel<bool> _wakeups = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    { SingleReader = true, FullMode = BoundedChannelFullMode.DropWrite });

    public SystemAlertMonitorService(ILogger<SystemAlertMonitorService> logger,
        EventMonitorService eventMonitor, LogAlertRouter router, IConfiguration configuration)
    {
        _logger = logger;
        _events = eventMonitor;
        _router = router;
        configuration.GetSection("ControllerConfig").Bind(_controller);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _events.Subscribe("SYSTEMALERT", OnChange);
        var polling = PollAsync(stoppingToken);
        _wakeups.Writer.TryWrite(true); // Recover pending rows after restart.
        try
        {
            await foreach (var wakeup in _wakeups.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var alerts = MainRepo.GetPendingSystemAlerts(100);
                        if (alerts.Count == 0) break;
                        bool failed = false;
                        foreach (var alert in alerts)
                        {
                            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                            timeout.CancelAfter(TimeSpan.FromSeconds(30));
                            var timestamp = DateTimeOffset.FromUnixTimeSeconds(alert.RawTime).AddMilliseconds(alert.MSec);
                            var message = new LogAlertMessage
                            {
                                SystemAlertId = alert.Id, ServiceId = _controller.Id, LogName = _controller.Name,
                                Source = AlertCategory.SystemAlertProducer, Level = "ALT",
                                RawTime = alert.RawTime, Milliseconds = alert.MSec,
                                Message = $"Source: {alert.Source}, Time: {timestamp.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}\n{alert.Message}"
                            };
                            if (!await _router.ForwardSystemAlertAsync(message, timeout.Token))
                            {
                                failed = true;
                                _logger.LogWarning("System alert {Id} remains pending; delivery will be retried.", alert.Id);
                                break;
                            }
                            MainRepo.MarkSystemAlertDelivered(alert.Id);
                        }
                        if (failed) break;
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "System alert delivery failed; pending rows will be retried.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            _events.Unsubscribe("SYSTEMALERT", OnChange);
            await polling;
        }
    }

    private void OnChange(EventRS? data)
    {
        if (data?.EventType == "SYSTEMALERT") _wakeups.Writer.TryWrite(true);
    }

    private async Task PollAsync(CancellationToken ct)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            while (await timer.WaitForNextTickAsync(ct)) _wakeups.Writer.TryWrite(true);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }
}
