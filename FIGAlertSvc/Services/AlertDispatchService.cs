using FIGCommon.DataAccess;
using FIGCommon.Models.Alert;
using System.Runtime.Versioning;

namespace FIGAlertSvc.Services
{
    /// <summary>
    /// Background service that runs every minute, collects all unsent matched alerts
    /// from the database and dispatches them to subscribers via email and/or Pushover
    /// based on each recipient's subscription AlertMethods bitmask.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class AlertDispatchService : BackgroundService
    {
        private static readonly TimeSpan DispatchInterval = TimeSpan.FromMinutes(1);

        private const int MethodEmail       = 1;
        private const int MethodPushover    = 2;

        // One singleton owns the lock for both fetch/send/mark paths.
        private readonly SemaphoreSlim _dispatchLock = new(1, 1);

        private readonly ILogger<AlertDispatchService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly int _maxAgeMinutes;

        public AlertDispatchService(
            ILogger<AlertDispatchService> logger,
            IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _maxAgeMinutes = configuration.GetValue<int>("AlertDispatch:MaxAlertAgeMinutes", 30);
            if (_maxAgeMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(configuration), "MaxAlertAgeMinutes must be positive.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug("AlertDispatchService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DispatchPendingAlertsAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AlertDispatchService: unhandled error during dispatch cycle.");
                }

                await Task.Delay(DispatchInterval, stoppingToken);
            }

            _logger.LogDebug("AlertDispatchService stopped.");
        }

        internal Task DispatchPendingAlertsAsync(CancellationToken cancellationToken) =>
            DispatchAsync(false, cancellationToken);

        public Task DispatchImmediateAlertsAsync(CancellationToken cancellationToken) =>
            DispatchAsync(true, cancellationToken);

        private async Task DispatchAsync(bool immediate, CancellationToken cancellationToken)
        {
            await _dispatchLock.WaitAsync(cancellationToken);
            try
            {
                // Fetch inside the lock: a waiting caller must not use stale pending rows.
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var rows = immediate ? AlertRepo.GetPendingImmediateAlerts(lookbackMinutes: _maxAgeMinutes) : AlertRepo.GetPendingAlerts(_maxAgeMinutes);
                    // SQL filters disabled recipients; keep the same guard before sending or marking sent.
                    rows = rows.Where(row => row.RecipientEnabled).ToList();
                    if (rows.Count == 0) return;

                    bool allSent = await DispatchRowsAsync(rows, immediate, cancellationToken);
                    // Drain immediate batches, but leave failures for the minute fallback
                    // instead of repeatedly sending the same partially delivered alert.
                    if (!immediate || !allSent) return;
                }
            }
            finally
            {
                _dispatchLock.Release();
            }
        }

        private async Task<bool> DispatchRowsAsync(List<PendingAlertRS> pendingRows, bool immediate, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailSvc = scope.ServiceProvider.GetRequiredService<SmtpMessageService>();
            var pushoverSvc = scope.ServiceProvider.GetRequiredService<PushOverMessageService>();

            // An alert is complete only when every returned recipient row succeeds.
            var remaining = pendingRows.GroupBy(r => r.AlertId).ToDictionary(g => g.Key, g => g.Count());
            var failed = new HashSet<int>();

            var validRows = new List<PendingAlertRS>();
            foreach (var row in pendingRows)
            {
                try { AlertMessageFormatter.Format(row); validRows.Add(row); }
                catch (Exception ex)
                {
                    failed.Add(row.AlertId);
                    _logger.LogError(ex, "Formatting failed: Alert Id={AlertId}, Recipient Id={RecipientId}.", row.AlertId, row.RecipientId);
                }
            }
            foreach (var batch in AlertBatchBuilder.Build(validRows, immediate))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var first = batch.Rows[0];
                var ids = string.Join(",", batch.Rows.Select(r => r.AlertId));
                bool sent = await SendToRecipientAsync(first, ids,
                    (first.AlertMethods & MethodEmail) != 0,
                    (first.AlertMethods & MethodPushover) != 0,
                    batch.Rows.Count == 1 ? "FIG Alert Notification" : $"FIG Alert Notification ({batch.Rows.Count} alerts)",
                    batch.Message, emailSvc, pushoverSvc);
                foreach (var row in batch.Rows)
                {
                    if (!sent) failed.Add(row.AlertId);
                    if (--remaining[row.AlertId] == 0 && !failed.Contains(row.AlertId))
                    {
                        AlertRepo.MarkAlertSent(row.AlertId);
                        _logger.LogInformation("Alert Id={AlertId} marked as sent after all recipients succeeded.", row.AlertId);
                    }
                }
            }
            return failed.Count == 0;
        }

        private async Task<bool> SendToRecipientAsync(
            PendingAlertRS row, string alertIds,
            bool wantEmail,
            bool wantPushover,
            string subject,
            string messageBody,
            SmtpMessageService emailSvc,
            PushOverMessageService pushoverSvc)
        {
            using var logScope = _logger.BeginScope("AlertId={AlertIds} RecipientId={RecipientId}", alertIds, row.RecipientId);
            _logger.LogInformation("Dispatch attempt: Alert Ids={AlertIds}, Recipient Id={RecipientId}, Methods={Methods}.", alertIds, row.RecipientId, row.AlertMethods);
            bool success = wantEmail || wantPushover;
            if (wantEmail)
            {
                if (string.IsNullOrWhiteSpace(row.Email))
                {
                    success = false;
                    _logger.LogWarning(
                        "AlertDispatchService: Recipient Id={RecipientId} has EMAIL method but no email address — skipping.",
                        row.RecipientId);
                }
                else
                {
                    try
                    {
                        await emailSvc.Send(row.Email, subject, messageBody);
                        _logger.LogInformation(
                            "AlertDispatchService: Email sent to '{Email}' for Recipient Id={RecipientId}.",
                            row.Email, row.RecipientId);
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        _logger.LogError(ex,
                            "AlertDispatchService: Failed to send email to '{Email}' for Recipient Id={RecipientId}.",
                            row.Email, row.RecipientId);
                    }
                }
            }

            if (wantPushover)
            {
                if (string.IsNullOrWhiteSpace(row.PushoverKey))
                {
                    success = false;
                    _logger.LogWarning(
                        "AlertDispatchService: Recipient Id={RecipientId} has PUSHOVER method but no pushover key — skipping.",
                        row.RecipientId);
                }
                else
                {
                    try
                    {
                        await pushoverSvc.SendAlert(row.PushoverKey, subject, messageBody, alertIds, row.RecipientId);
                        _logger.LogInformation(
                            "AlertDispatchService: Pushover accepted for Alert Ids={AlertIds}, Recipient Id={RecipientId}.",
                            alertIds, row.RecipientId);
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        _logger.LogError(ex,
                            "AlertDispatchService: Failed to send Pushover to Recipient Id={RecipientId}.",
                            row.RecipientId);
                    }
                }
            }
            _logger.LogInformation("Dispatch result: Alert Ids={AlertIds}, Recipient Id={RecipientId}, Success={Success}.", alertIds, row.RecipientId, success);
            return success;
        }
    }
}
