using FIGCommon.Models;
using FIGCommon.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Channels;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS: " + message);
}
static EventRS Row(int id, int seconds, string message = "state") => new()
{
    Id = id, EventType = "SIGNAL", EventId = id.ToString(), Message = message,
    Timestamp = new DateTime(2026, 10, 13, 11, 0, 0).AddSeconds(seconds)
};

using (var monitor = new TestMonitor())
{
    var calls = new List<EventRS?>();
    Action<EventRS?> handler = row => calls.Add(row);
    monitor.Subscribe("SIGNAL", handler);
    monitor.Subscribe("SIGNAL", handler);
    monitor.Snapshot([Row(1, 0)], false);
    monitor.Snapshot([Row(1, 0)], true);
    Check(calls.Count == 0, "Baseline and unchanged snapshots do not replay events");
    monitor.Snapshot([Row(1, 1), Row(2, 0)], true);
    Check(calls.Count == 2, "Update and insert publish once despite duplicate subscription");
    monitor.Snapshot([Row(1, 1, "changed"), Row(2, 0)], true);
    Check(calls.Count == 3, "Equal timestamp with different message publishes");
    monitor.Snapshot([Row(1, -1), Row(2, 0)], true);
    Check(calls.Count == 3, "Older timestamp remains ignored, matching notification mode");
    monitor.Snapshot([Row(2, 0)], true);
    Check(calls.Count == 4 && calls[^1] == null, "Deletion uses existing null callback convention");
    monitor.Unsubscribe("SIGNAL", handler);
    monitor.Snapshot([Row(2, 2)], true);
    Check(calls.Count == 4, "Unsubscribe prevents callbacks");
    monitor.Subscribe("SIGNAL", _ => throw new Exception("subscriber failure"));
    monitor.Subscribe("SIGNAL", handler);
    monitor.Snapshot([Row(2, 3)], true);
    Check(calls.Count == 5, "Failing subscriber does not suppress other subscribers");
}

using (var monitor = new TestMonitor())
{
    var observed = new TaskCompletionSource<EventRS?>(TaskCreationOptions.RunContinuationsAsynchronously);
    monitor.Subscribe("SIGNAL", row => observed.TrySetResult(row));
    await Task.WhenAll(monitor.StartMonitoringAsync(), monitor.StartMonitoringAsync());
    var baseline = await monitor.NextRead();
    baseline.SetResult([Row(1, 0)]);
    var failure = await monitor.NextRead();
    failure.SetException(new Exception("database unavailable"));
    var recovery = await monitor.NextRead();
    recovery.SetResult([Row(1, 1)]);
    var result = await observed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    Check(result?.Timestamp == Row(1, 1).Timestamp, "Polling survives read failure without resetting baseline");
    var pending = await monitor.NextRead();
    monitor.Dispose();
    await monitor.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
    Check(monitor.MaxConcurrentReads == 1, "Repeated startup creates only one loop; disposal cancels pending read");
}
foreach (var invalid in new[] { ("invalid", "10"), ("Polling", "0"), ("Polling", "-1") })
{
    bool rejected = false;
    try { using var monitor = new TestMonitor(invalid.Item1, invalid.Item2); }
    catch (ArgumentException) { rejected = true; }
    Check(rejected, $"Invalid configuration rejected: {invalid}");
}
Console.WriteLine("All event monitor regressions passed.");

sealed class TestMonitor : EventMonitorService
{
    private readonly Channel<TaskCompletionSource<List<EventRS>>> reads = Channel.CreateUnbounded<TaskCompletionSource<List<EventRS>>>();
    private int active;
    public int MaxConcurrentReads { get; private set; }
    public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TestMonitor(string mode = "Polling", string interval = "10") : base(
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EventMonitor:Mode"] = mode,
            ["EventMonitor:PollIntervalMilliseconds"] = interval,
            ["EventMonitor:ConnectionString"] = "Data Source=unused;Initial Catalog=unused;Integrated Security=true"
        }).Build(), NullLogger<EventMonitorService>.Instance, false) { }
    public void Snapshot(List<EventRS> rows, bool publish) => ApplyPollingSnapshot(rows, publish);
    public async Task<TaskCompletionSource<List<EventRS>>> NextRead() =>
        await reads.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
    protected override async Task<List<EventRS>> ReadPollingEventsAsync(CancellationToken token)
    {
        MaxConcurrentReads = Math.Max(MaxConcurrentReads, Interlocked.Increment(ref active));
        var completion = new TaskCompletionSource<List<EventRS>>(TaskCreationOptions.RunContinuationsAsynchronously);
        reads.Writer.TryWrite(completion);
        try { return await completion.Task.WaitAsync(token); }
        catch (OperationCanceledException) { Cancelled.TrySetResult(); throw; }
        finally { Interlocked.Decrement(ref active); }
    }
}
