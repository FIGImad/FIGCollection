using FIGCommon.Services;

namespace FIGSignalExSvc.Publication;

public sealed class SignalPublicationService(ISignalPublicationStore store, ISignalPublicationClient client,
    SignalPublicationOptions options, SignalPublicationWakeup wakeup, EventMonitorService events,
    ILogger<SignalPublicationService> logger) : BackgroundService
{
    public override async Task StartAsync(CancellationToken ct)
    {
        // Fails startup before PriceMonitorService if the migration is missing or the producer changed.
        await store.InitializeAsync(options.ProducerId, ct);
        events.Subscribe("SIGNAL", OnSignal);
        await base.StartAsync(ct);
    }
    private void OnSignal(FIGCommon.Models.EventRS? _) => wakeup.Notify();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextReconciliation = DateTimeOffset.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Retry dirty rows promptly; a bad strategy does not starve subsequent signals.
                await SweepAsync(true, stoppingToken);
                if (DateTimeOffset.UtcNow >= nextReconciliation)
                {
                    await SweepAsync(false, stoppingToken);
                    nextReconciliation = DateTimeOffset.UtcNow.AddSeconds(options.ReconciliationSeconds);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Signal synchronization scan failed; durable records remain pending"); }
            try { await wakeup.WaitAsync(TimeSpan.FromSeconds(options.RetrySeconds), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    public async Task SweepAsync(bool pendingOnly, CancellationToken ct)
    {
        int afterId = 0;
        while (!ct.IsCancellationRequested)
        {
            var batch = await store.ReadAsync(afterId, options.BatchSize, pendingOnly, ct);
            if (batch.Count == 0) return;
            foreach (var item in batch)
            {
                ct.ThrowIfCancellationRequested();
                afterId = item.SignalId;
                try
                {
                    var ack = await client.PublishAsync(item.Message, ct);
                    await store.AcknowledgeAsync(item, ack, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Signal sync failed for {Tag} revision {Revision}",
                        item.Message.Signal.Tag, item.Message.Revision);
                    await store.RecordFailureAsync(item, ex.Message, ct);
                }
            }
        }
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        events.Unsubscribe("SIGNAL", OnSignal);
        await base.StopAsync(ct);
    }
}
