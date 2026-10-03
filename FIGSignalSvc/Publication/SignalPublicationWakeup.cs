namespace FIGSignalExSvc.Publication;

public sealed class SignalPublicationWakeup
{
    private readonly SemaphoreSlim pending = new(0, 1);
    public void Notify()
    {
        try { pending.Release(); }
        catch (SemaphoreFullException) { /* A single wakeup drains all pending signals. */ }
    }
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken ct) => pending.WaitAsync(timeout, ct);
}
