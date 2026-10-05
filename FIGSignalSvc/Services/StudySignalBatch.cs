using FIGCommon.Models;

namespace FIGSignalExSvc.Services;

/// <summary>Signal transitions for one transaction, independent of calculation checkpoints.</summary>
public sealed class StudySignalBatch
{
    private readonly string[] strategies;
    private readonly Func<string, SignalRS?> readLatest;
    private readonly Dictionary<string, SignalRS?> latest = new(StringComparer.Ordinal);
    private readonly List<SignalRS> changes = [];
    public IReadOnlyList<SignalRS> Changes => changes;
    public long ProcessedThrough { get; private set; }

    public StudySignalBatch(IEnumerable<string> strategies, long processedThrough, Func<string, SignalRS?> readLatest)
    {
        this.strategies = strategies.Distinct(StringComparer.Ordinal).ToArray();
        ProcessedThrough = processedThrough;
        this.readLatest = readLatest;
    }

    public void Process(IEnumerable<StudyHistoryRS> history, long completedBefore)
    {
        foreach (var bar in history.OrderBy(b => b.RawTime))
        {
            // Inclusive: even an already processed flat bar cannot create a trade after correction/restart.
            if (bar.RawTime <= ProcessedThrough) continue;
            if (bar.RawTime >= completedBefore) break; // leave forming bars eligible for a later transaction
            foreach (var strategy in strategies)
            {
                if (bar.rawStudies == null || !bar.rawStudies.TryGetValue($"{strategy}_SIDE", out var value)) continue;
                if (!latest.TryGetValue(strategy, out var signal))
                {
                    var saved = readLatest(strategy);
                    signal = saved == null ? null : new SignalRS(saved);
                    latest[strategy] = signal; // retain closed signals as replay evidence too
                }
                if (signal != null && bar.RawTime <= Math.Max(signal.StartTime ?? -1, signal.StopTime ?? -1)) continue;
                decimal side = StudySignal.Get<decimal?>(value) ?? 0m;
                bool active = signal?.Status == SignalStatus.Start;
                if (active && (side == 0m || signal!.Side != side))
                {
                    signal!.Status = SignalStatus.Stop;
                    signal.StopTime = bar.RawTime;
                    signal.StopPrice = bar.Close;
                    signal.LastUpdated = -1;
                    if (!changes.Contains(signal)) changes.Add(signal);
                    active = false;
                }
                if (side != 0m && !active)
                {
                    signal = new SignalRS
                    {
                        Strategy = strategy, Tag = Guid.NewGuid().ToString(), Side = side,
                        Status = SignalStatus.Start, StartTime = bar.RawTime, StartPrice = bar.Close,
                        LastUpdated = -1
                    };
                    latest[strategy] = signal;
                    changes.Add(signal);
                }
            }
            // Persist progress even when no strategy emits a signal on this bar.
            ProcessedThrough = bar.RawTime;
        }
    }
}
