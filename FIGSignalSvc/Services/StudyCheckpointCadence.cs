using FIG.Studies;
using FIGCommon.Models;

namespace FIGSignalExSvc.Services;

/// <summary>Counts distinct study bars, retaining positions for recent-bar corrections.</summary>
public sealed class StudyCheckpointCadence
{
    public int EveryNBars { get; private set; }
    private readonly int correctionCapacity;
    private sealed record Position(long Time, long Number, long PreviousCheckpoint, bool IsCheckpoint);
    private readonly List<Position> recent = [];
    // Keep these across removal of the correction window so an interval change
    // cannot erase a checkpoint that will be replayed later in that window.
    private readonly HashSet<long> checkpointTimes = [];

    public StudyCheckpointCadence(int everyNBars, int correctionCapacity = 10)
    {
        if (everyNBars < 1) throw new ArgumentOutOfRangeException(nameof(everyNBars));
        if (correctionCapacity < 1) throw new ArgumentOutOfRangeException(nameof(correctionCapacity));
        EveryNBars = everyNBars;
        this.correctionCapacity = correctionCapacity;
    }

    public void SetInterval(int everyNBars)
    {
        if (everyNBars < 1) throw new ArgumentOutOfRangeException(nameof(everyNBars));
        // Keep the last checkpoint anchor. Switching to live mode must not
        // discard the bars already counted since the last catch-up checkpoint.
        EveryNBars = everyNBars;
    }

    // A restored checkpoint is the cadence anchor, including legacy per-bar snapshots.
    public void Restore(CollectionCheckpoint checkpoint)
    {
        recent.Clear();
        checkpointTimes.Clear();
        checkpointTimes.Add(checkpoint.RawTime);
        var times = checkpoint.Bars.Take(correctionCapacity).Select(b => b.Price.RawTime).Reverse().ToArray();
        for (int i = 0; i < times.Length; i++)
        {
            long position = i - times.Length + 1;
            long remainder = ((position % EveryNBars) + EveryNBars) % EveryNBars;
            long previousCheckpoint = position - (remainder == 0 ? EveryNBars : remainder);
            recent.Add(new(times[i], position, previousCheckpoint, remainder == 0));
        }
    }

    public bool Advance(long rawTime, bool completed = true)
    {
        int index = recent.FindIndex(b => b.Time >= rawTime);
        long position, previousCheckpoint;
        if (index >= 0)
        {
            // Repeated or corrected bars retain their position instead of advancing the count.
            if (recent[index].Time == rawTime)
            {
                position = recent[index].Number;
            }
            else if (index > 0) position = recent[index - 1].Number + 1;
            else throw new InvalidDataException("Checkpoint cadence rewind exceeds retained history.");
            previousCheckpoint = index == 0 ? recent[index].PreviousCheckpoint : Anchor(recent[index - 1]);
            recent.RemoveRange(index, recent.Count - index);
        }
        else
        {
            position = recent.Count == 0 ? 1 : recent[^1].Number + 1;
            previousCheckpoint = recent.Count == 0 ? 0 : Anchor(recent[^1]);
        }
        bool capture = completed && (checkpointTimes.Contains(rawTime)
            || (position - previousCheckpoint >= EveryNBars && !checkpointTimes.Any(time => time > rawTime)));
        if (capture) checkpointTimes.Add(rawTime);
        recent.Add(new(rawTime, position, previousCheckpoint, capture));
        if (recent.Count > correctionCapacity) recent.RemoveAt(0);
        checkpointTimes.RemoveWhere(time => time < recent[0].Time);
        return capture;
    }

    private static long Anchor(Position bar) => bar.IsCheckpoint ? bar.Number : bar.PreviousCheckpoint;
}

public sealed record StudyCheckpointReplayPlan(long Start, int Rewind)
{
    public static StudyCheckpointReplayPlan FromCheckpoint(CollectionCheckpoint checkpoint, long historyTail, int correctionBars)
    {
        if (checkpoint.RawTime > historyTail || checkpoint.Bars.Count == 0 || correctionBars < 1)
            throw new InvalidDataException("Checkpoint is outside committed study history.");
        int rewind = Math.Min(correctionBars, checkpoint.Bars.Count);
        return new(checkpoint.Bars[rewind - 1].Price.RawTime, rewind);
    }

    public IEnumerable<PriceDataRS> RequireStart(IEnumerable<PriceDataRS> prices)
    {
        using var iterator = prices.GetEnumerator();
        if (!iterator.MoveNext() || iterator.Current.RawTime != Start)
            throw new InvalidDataException("Source prices do not cover the checkpoint correction window.");
        do { yield return iterator.Current; } while (iterator.MoveNext());
    }
}
