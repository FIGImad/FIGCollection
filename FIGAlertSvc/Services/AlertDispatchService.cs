using FIGCommon.DataAccess;
using FIGCommon.Models.Alert;
using System.Runtime.Versioning;

namespace FIGAlertSvc.Services
{
    /// <summary>
    /// Background service that runs every 2 minutes, collects all unsent matched alerts
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

        private async Task DispatchPendingAlertsAsync(CancellationToken cancellationToken)
        {
            List<PendingAlertRS> pendingRows = AlertRepo.GetPendingAlerts();

            if (pendingRows.Count == 0)
                return;

            _logger.LogDebug("AlertDispatchService: {Count} pending alert-recipient rows to process.", pendingRows.Count);

            using var scope = _serviceProvider.CreateScope();
            var emailSvc    = scope.ServiceProvider.GetRequiredService<SmtpMessageService>();
            var pushoverSvc = scope.ServiceProvider.GetRequiredService<PushOverMessageService>();

            // Load all recipients (with their Subscriptions) keyed by recipient id.
            var recipients = AlertRepo.GetRecipients()
                .ToDictionary(r => r.Id);

            // Group by recipient so each person gets one batched message (up to MaxAlertsPerMessage).
            var byRecipient = pendingRows.GroupBy(r => r.RecipientId);

            // Track every alert that was attempted so we mark them sent regardless of
            // individual channel failures (errors are logged; retrying forever is worse).
            var attemptedAlertIds = new HashSet<int>();

            foreach (var recipientGroup in byRecipient)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var allRows = recipientGroup.ToList();

                // Use the first row for recipient metadata — all rows share the same recipient fields.
                var firstRow = allRows[0];

                // Verify the recipient exists and has a loaded subscription list.
                if (!recipients.TryGetValue(firstRow.RecipientId, out var recipientRecord))
                {
                    _logger.LogWarning(
                        "AlertDispatchService: Recipient Id={RecipientId} not found — skipping.",
                        firstRow.RecipientId);
                    foreach (var row in allRows)
                        attemptedAlertIds.Add(row.AlertId);
                    continue;
                }

                var subscriptionMap = recipientRecord.Subscriptions
                    .ToDictionary(s => s.AlertRuleId);

                // Keep only rows whose rule the recipient is actively subscribed to.
                var subscribedRows = allRows
                    .Where(r => subscriptionMap.ContainsKey(r.RuleId))
                    .ToList();

                // Any unsubscribed alerts are still marked sent so they are not re-queued.
                foreach (var row in allRows.Where(r => !subscriptionMap.ContainsKey(r.RuleId)))
                {
                    _logger.LogDebug(
                        "AlertDispatchService: Recipient Id={RecipientId} is not subscribed to RuleId={RuleId} — skipping alert Id={AlertId}.",
                        row.RecipientId, row.RuleId, row.AlertId);
                    attemptedAlertIds.Add(row.AlertId);
                }

                if (subscribedRows.Count == 0)
                    continue;

                // Resolve AlertMethods from the subscription for the first subscribed row;
                // rows within the same chunk share the same rule, so the bitmask is consistent.
                allRows = subscribedRows;

                bool wantEmail    = subscribedRows.Any(r => (subscriptionMap[r.RuleId].AlertMethods & MethodEmail)    == MethodEmail);
                bool wantPushover = subscribedRows.Any(r => (subscriptionMap[r.RuleId].AlertMethods & MethodPushover) == MethodPushover);

                // Send all alerts in chunks of MaxAlertsPerMessage, 2 s between each chunk.
                int totalChunks = (int)Math.Ceiling(allRows.Count / (double)MaxAlertsPerMessage);
                for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var batch = allRows.Skip(chunkIndex * MaxAlertsPerMessage).Take(MaxAlertsPerMessage).ToList();

                    string subject = totalChunks == 1 && batch.Count == 1
                        ? "FIG Alert Notification"
                        : $"FIG Alert Notification ({chunkIndex * MaxAlertsPerMessage + 1}-{chunkIndex * MaxAlertsPerMessage + batch.Count} of {allRows.Count})";

                    string messageBody = string.Join("\n\n", batch.Select((r, i) =>
                        batch.Count > 1 ? $"[{chunkIndex * MaxAlertsPerMessage + i + 1}] {r.FriendlyMessage}" : r.FriendlyMessage));

                    await SendToRecipientAsync(firstRow, wantEmail, wantPushover, subject, messageBody, emailSvc, pushoverSvc);

                    foreach (var row in batch)
                        attemptedAlertIds.Add(row.AlertId);

                    if (chunkIndex < totalChunks - 1)
                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
            }

            // Mark every attempted alert as sent so they are not picked up again.
            foreach (int alertId in attemptedAlertIds)
            {
                AlertRepo.MarkAlertSent(alertId);
                _logger.LogInformation("AlertDispatchService: Alert Id={AlertId} marked as sent.", alertId);
            }
        }

        private async Task SendToRecipientAsync(
            PendingAlertRS row,
            bool wantEmail,
            bool wantPushover,
            string subject,
            string messageBody,
            SmtpMessageService emailSvc,
            PushOverMessageService pushoverSvc)
        {
            if (wantEmail)
            {
                if (string.IsNullOrWhiteSpace(row.Email))
                {
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
                        _logger.LogError(ex,
                            "AlertDispatchService: Failed to send Pushover to Recipient Id={RecipientId}.",
                            row.RecipientId);
                    }
                }
            }
        }
    }
}
