using FIGCommon.Models;

namespace FIGSignalExSvc.Services;

/// <summary>Selects one checkpoint interval per processing cycle with bounded look-ahead.</summary>
public sealed class StudyCheckpointPolicy
{
    public int LiveEveryNBars { get; }
    public int CatchUpEveryNBars { get; }
    public int LiveMaxBars { get; }

    public StudyCheckpointPolicy(int liveEveryNBars = 30, int catchUpEveryNBars = 100, int liveMaxBars = 10)
    {
        if (liveEveryNBars < 1) throw new ArgumentOutOfRangeException(nameof(liveEveryNBars));
        if (catchUpEveryNBars < 1) throw new ArgumentOutOfRangeException(nameof(catchUpEveryNBars));
        if (liveMaxBars < 1) throw new ArgumentOutOfRangeException(nameof(liveMaxBars));
        LiveEveryNBars = liveEveryNBars;
        CatchUpEveryNBars = catchUpEveryNBars;
        LiveMaxBars = liveMaxBars;
    }

    public IEnumerable<PriceDataRS> SelectInterval(IEnumerable<PriceDataRS> prices, StudyCheckpointCadence cadence,
        int rewind, Action<int>? selected = null)
    {
        if (rewind < 0) throw new ArgumentOutOfRangeException(nameof(rewind));
        using var iterator = prices.GetEnumerator();
        var prefix = new List<PriceDataRS>();
        // Count aggregated study bars, not source minute rows or time gaps. Replayed
        // correction bars are excluded; post-checkpoint recovery work still counts.
        long probeLimit = (long)LiveMaxBars + rewind + 1;
        while (prefix.Count < probeLimit && iterator.MoveNext()) prefix.Add(iterator.Current);
        if (prefix.Count == 0) yield break;
        int interval = prefix.Count - rewind > LiveMaxBars ? CatchUpEveryNBars : LiveEveryNBars;
        cadence.SetInterval(interval);
        selected?.Invoke(interval);
        foreach (var price in prefix) yield return price;
        while (iterator.MoveNext()) yield return iterator.Current;
    }
}
