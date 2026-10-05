using FIG.Studies;
using FIGCommon.Models;

namespace FIGSignalExSvc.Services;

public static class StudyCheckpointProcessor
{
    // The callback commits history, signals and checkpoints atomically for this batch.
    // A failed callback stops enumeration, so no later price pages are calculated.
    public static void CalculateAndCommit(StudyColBase collection, IEnumerable<PriceDataRS> prices,
        long retainFrom, int rewind, int batchSize, long maxPayloadBytes,
        Action<IReadOnlyList<BarStudy>, StudyCheckpointBatch> commit, Action<long>? progress = null,
        StudyCheckpointCadence? cadence = null)
    {
        if (batchSize < 1 || maxPayloadBytes < 1) throw new ArgumentOutOfRangeException(nameof(batchSize));
        using var checkpoints = new StudyCheckpointBatch();
        var bars = new List<BarStudy>();
        void Flush()
        {
            if (bars.Count == 0) return;
            commit(bars, checkpoints);
            bars.Clear(); checkpoints.Clear();
        }
        bool first = true;
        using var iterator = prices.GetEnumerator();
        bool hasPrice = iterator.MoveNext();
        while (hasPrice)
        {
            var price = iterator.Current;
            // A successor proves this study bar is complete, including across market gaps.
            // Defer a scheduled snapshot of the forming last bar until the next update.
            hasPrice = iterator.MoveNext();
            var bar = collection.Calc([price], first ? rewind : 0)[0];
            first = false;
            bool capture = cadence == null || cadence.Advance(price.RawTime, completed: hasPrice);
            progress?.Invoke(price.RawTime);
            if (price.RawTime < retainFrom) continue;
            var payload = capture ? StudyCheckpointPayload.Capture(collection.CaptureCheckpoint()) : null;
            if (payload != null && bars.Count > 0 && checkpoints.PayloadBytes + payload.Bytes > maxPayloadBytes) Flush();
            bars.Add(bar);
            if (payload != null) checkpoints.Add(payload);
            // An individual checkpoint larger than the cap is committed alone.
            if (bars.Count >= batchSize || checkpoints.PayloadBytes >= maxPayloadBytes) Flush();
        }
        Flush();
    }
}
