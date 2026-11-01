using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

/// <summary>Retries only fully rolled-back deadlock victims. The action owns a fresh transaction on each call.</summary>
public static class SqlDeadlockRetry
{
    public const int MaximumRetries=3;
    public static async Task ExecuteAsync(Func<CancellationToken,Task> transaction,CancellationToken ct,Action<int,TimeSpan>? retrying=null)
    {
        for(int attempt=0;;attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try{await transaction(ct);return;}
            catch(SqlException ex)when(ex.Number==1205&&attempt<MaximumRetries&&!ct.IsCancellationRequested)
            {
                var delay=TimeSpan.FromMilliseconds(250*(1<<attempt)+Random.Shared.Next(100,300));
                try{retrying?.Invoke(attempt+1,delay);}catch{ /* Diagnostics must not prevent recovery. */ }
                await Task.Delay(delay,ct);
            }
        }
    }
}
