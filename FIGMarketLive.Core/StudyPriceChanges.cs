namespace FIGMarketLive;

/// <summary>Coalesces dataset notifications without losing changes during calculation.</summary>
public sealed class StudyPriceChanges : IDisposable
{
    private readonly object gate=new();
    private readonly HashSet<int> pending=[];
    private readonly SemaphoreSlim ready=new(0,1);
    private bool disposed;
    public void Notify(int dataSetId)
    {lock(gate){if(disposed)return;pending.Add(dataSetId);if(ready.CurrentCount==0)ready.Release();}}
    public async Task<HashSet<int>?> WaitAsync(TimeSpan timeout,CancellationToken ct)
    {
        if(!await ready.WaitAsync(timeout,ct))return null;
        lock(gate){var result=new HashSet<int>(pending);pending.Clear();while(ready.Wait(0)){}return result;}
    }
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;ready.Dispose();}}
}
