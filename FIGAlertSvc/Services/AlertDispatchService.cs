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
        private const int MaxAlertsPerMessage = 20;

        // One singleton owns the lock for both fetch/send/mark paths.
        private readonly SemaphoreSlim _dispatchLock = new(1, 1);

        private readonly ILogger<AlertDispatchService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public AlertDispatchService(
            ILogger<AlertDispatchService> logger,
            IServiceProvider serviceProvider)
        {
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

        private Task DispatchPendingAlertsAsync(CancellationToken cancellationToken) =>
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
                    var rows = immediate ? AlertRepo.GetPendingImmediateAlerts() : AlertRepo.GetPendingAlerts();
                    // SQL filters disabled recipients; keep the same guard before sending or marking sent.
                    rows = rows.Where(row => row.RecipientEnabled).ToList();
                    if (rows.Count == 0) return;

                    bool allSent = await DispatchRowsAsync(rows, cancellationToken);
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

        private async Task<bool> DispatchRowsAsync(List<PendingAlertRS> pendingRows, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailSvc = scope.ServiceProvider.GetRequiredService<SmtpMessageService>();
            var pushoverSvc = scope.ServiceProvider.GetRequiredService<PushOverMessageService>();

            // An alert is complete only when every returned recipient row succeeds.
            var remaining = pendingRows.GroupBy(r => r.AlertId).ToDictionary(g => g.Key, g => g.Count());
            var failed = new HashSet<int>();

            // Preserve recipient batching while respecting each subscription's method mask.
            foreach (var recipientGroup in pendingRows.GroupBy(r => new { r.RecipientId, r.AlertMethods }))
            {
                var allRows = recipientGroup.ToList();
                int totalChunks = (int)Math.Ceiling(allRows.Count / (double)MaxAlertsPerMessage);
                for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var batch = allRows.Skip(chunkIndex * MaxAlertsPerMessage).Take(MaxAlertsPerMessage).ToList();
                    var firstRow = batch[0];
                    bool wantEmail = (firstRow.AlertMethods & MethodEmail) != 0;
                    bool wantPushover = (firstRow.AlertMethods & MethodPushover) != 0;
                    string subject = totalChunks == 1 && batch.Count == 1
                        ? "FIG Alert Notification"
                        : $"FIG Alert Notification ({chunkIndex * MaxAlertsPerMessage + 1}-{chunkIndex * MaxAlertsPerMessage + batch.Count} of {allRows.Count})";
                    string messageBody = string.Join("\n\n", batch.Select((r, i) =>
                        batch.Count > 1 ? $"[{chunkIndex * MaxAlertsPerMessage + i + 1}] {r.FriendlyMessage}" : r.FriendlyMessage));

                    bool sent = await SendToRecipientAsync(firstRow, wantEmail, wantPushover, subject, messageBody, emailSvc, pushoverSvc);
                    foreach (var row in batch)
                    {
                        if (!sent) failed.Add(row.AlertId);
                        if (--remaining[row.AlertId] == 0 && !failed.Contains(row.AlertId))
                        {
                            AlertRepo.MarkAlertSent(row.AlertId);
                            _logger.LogInformation("AlertDispatchService: Alert Id={AlertId} marked as sent.", row.AlertId);
                        }
                    }
                    if (chunkIndex < totalChunks - 1)
                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
            }
            return failed.Count == 0;
        }

        private async Task<bool> SendToRecipientAsync(
            PendingAlertRS row,
            bool wantEmail,
            bool wantPushover,
            string subject,
            string messageBody,
            SmtpMessageService emailSvc,
            PushOverMessageService pushoverSvc)
        {
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
                        await pushoverSvc.Send(row.PushoverKey, subject, messageBody);
                        _logger.LogInformation(
                            "AlertDispatchService: Pushover sent to Recipient Id={RecipientId}.",
                            row.RecipientId);
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
            return success;
        }
    }
}
