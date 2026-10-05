using System.Text.Json;
using FIG.Studies;
using FIGCommon.Models;
using FIGSignalExSvc.Services;

static class SparseCheckpointChecks
{
    public static void Run(Func<StudyColBase> create, IReadOnlyList<PriceDataRS> prices, Action<bool, string> check)
    {
        string Json<T>(T value) => JsonSerializer.Serialize(value, StudyStateJson.Options);
        CollectionCheckpoint Read(StudyCheckpointPayload payload)
            => JsonSerializer.Deserialize<CollectionCheckpoint>(payload.Json, StudyStateJson.Options)!;
        var expected = create().Calc(prices.ToList(), 0);
        bool ResumesIdentically(StudyCheckpointPayload payload)
        {
            var state = Read(payload);
            var collection = create();
            var window = collection.GetRequiredCheckpointPrices(state);
            collection.RestoreCheckpoint(state, window == null ? null : prices
                .Where(p => p.RawTime >= window.FirstRawTime && p.RawTime <= window.LastRawTime).ToList());
            var next = prices.Where(p => p.RawTime > state.RawTime).Take(5).ToList();
            return Json(collection.Calc(next, 0)) == Json(expected.Where(b => b.Price.RawTime > state.RawTime).Take(5));
        }
        var history = new SortedDictionary<long, BarStudy>();
        var snapshots = new SortedDictionary<long, StudyCheckpointPayload>();
        void Commit(IReadOnlyList<BarStudy> bars, StudyCheckpointBatch batch)
        {
            foreach (var bar in bars)
            {
                history[bar.Price.RawTime] = bar;
                snapshots.Remove(bar.Price.RawTime);
            }
            foreach (var payload in batch.Items) snapshots[payload.RawTime] = payload;
        }
        void Process(StudyColBase collection, StudyCheckpointCadence cadence,
            IEnumerable<PriceDataRS> input, int rewind = 0, long bytes = long.MaxValue)
            => StudyCheckpointProcessor.CalculateAndCommit(collection, input, 0, rewind, 7, bytes, Commit, cadence: cadence);
        (StudyColBase Collection, StudyCheckpointCadence Cadence, StudyCheckpointReplayPlan Plan) Restore()
        {
            var checkpoint = Read(snapshots.Values.Last());
            var collection = create();
            var window = collection.GetRequiredCheckpointPrices(checkpoint);
            collection.RestoreCheckpoint(checkpoint, window == null ? null : history.Values
                .Where(b => b.Price.RawTime >= window.FirstRawTime && b.Price.RawTime <= window.LastRawTime)
                .Select(b => b.Price).ToList());
            var cadence = new StudyCheckpointCadence(30);
            cadence.Restore(checkpoint);
            return (collection, cadence, StudyCheckpointReplayPlan.FromCheckpoint(checkpoint, history.Keys.Last(), 3));
        }

        Process(create(), new(30), prices);
        check(Json(history.Values) == Json(expected), "30-bar cadence still commits every history bar with identical outputs");
        var expectedTimes = prices.Where((_, i) => (i + 1) % 30 == 0 && i + 1 < prices.Count).Select(p => p.RawTime).ToArray();
        check(snapshots.Keys.SequenceEqual(expectedTimes), "Only every 30th completed study bar has a snapshot");

        history.Clear(); snapshots.Clear();
        var live = create();
        var liveCadence = new StudyCheckpointCadence(30);
        int previous = 0;
        foreach (int end in new[] { 29, 30, 30, 31, 33, 55, 60, 60, 61, 89, 90, 91, 95, prices.Count })
        {
            int rewind = Math.Min(3, previous);
            Process(live, liveCadence, prices.Skip(previous - rewind).Take(end - previous + rewind), rewind);
            previous = end;
            if (end == 30) check(snapshots.Count == 0, "Repeated forming bar 30 defers its checkpoint without advancing cadence");
        }
        check(Json(history.Values) == Json(expected), "Live updates and repeated correction windows preserve every output");
        check(snapshots.Keys.SequenceEqual(expectedTimes), "Live updates keep the same checkpoint positions");
        // Rewinding a full ring buffer can shorten its oldest retained history;
        // verify equivalent continuation, rather than requiring identical cache lengths.
        check(snapshots.Values.All(ResumesIdentically), "Every live-update checkpoint resumes identical future outputs");

        foreach (int split in new[] { 29, 31, 59, 60, 61, 95, 120 })
        {
            history.Clear(); snapshots.Clear();
            Process(create(), new(30), prices.Take(split));
            long tail = history.Keys.Last();
            if (snapshots.Count == 0)
            {
                Process(create(), new(30), prices);
                check(Json(history.Values) == Json(expected), "Restart before the first checkpoint rebuilds all available prices");
                continue;
            }
            long checkpointTime = snapshots.Keys.Last();
            // Simulate stale study results after the snapshot; restart must regenerate these rows too.
            foreach (var key in history.Keys.Where(t => t > checkpointTime).ToArray())
                history[key] = new BarStudy(history[key].Price);
            var recovery = Restore();
            check(recovery.Plan.Start < checkpointTime && recovery.Plan.Start < tail,
                $"Restart at bar {split} starts at the checkpoint correction window, not the newest history rows");
            Process(recovery.Collection, recovery.Cadence, prices.Where(p => p.RawTime >= recovery.Plan.Start), recovery.Plan.Rewind);
            check(Json(history.Values) == Json(expected), $"Restart at bar {split} regenerates all post-checkpoint history and new bars");
            check(snapshots.Keys.SequenceEqual(expectedTimes) && ResumesIdentically(snapshots.Values.Last()),
                "Restart preserves snapshot positions and resumable state");
        }

        // Tail deletion may leave up to 29 retained rows after the newest surviving snapshot.
        foreach (var key in history.Keys.TakeLast(100).ToArray()) { history.Remove(key); snapshots.Remove(key); }
        var deletedRecovery = Restore();
        Process(deletedRecovery.Collection, deletedRecovery.Cadence,
            prices.Where(p => p.RawTime >= deletedRecovery.Plan.Start), deletedRecovery.Plan.Rewind);
        check(Json(history.Values) == Json(expected), "Deleting 100 bars recovers from a sparse checkpoint with identical regenerated history");

        history.Clear(); snapshots.Clear();
        int attempts = 0;
        try
        {
            StudyCheckpointProcessor.CalculateAndCommit(create(), prices, 0, 0, 7, long.MaxValue, (bars, batch) =>
            {
                if (++attempts == 16) throw new IOException("Simulated rollback");
                Commit(bars, batch);
            }, cadence: new(30));
            throw new Exception("Expected failed batch");
        }
        catch (IOException) { }
        check(history.Count == 105 && snapshots.Keys.Last() == prices[89].RawTime,
            "Failed batch retains committed history beyond the last durable checkpoint");
        var retry = Restore();
        Process(retry.Collection, retry.Cadence, prices.Where(p => p.RawTime >= retry.Plan.Start), retry.Plan.Rewind);
        check(Json(history.Values) == Json(expected), "Retry regenerates committed checkpoint-free bars and the failed batch exactly");

        history.Clear(); snapshots.Clear();
        Process(create(), new(30), prices, bytes: 1);
        check(Json(history.Values) == Json(expected) && snapshots.Keys.SequenceEqual(expectedTimes),
            "Oversized sparse snapshots remain atomic without dropping checkpoint-free history batches");

        // Correct an interval boundary and its two predecessors, then compare with a full fresh calculation.
        var corrected = prices.Select(p => new PriceDataRS(p)).ToList();
        for (int i = 57; i < 60; i++) { corrected[i].Close += .75m; corrected[i].High += .75m; }
        history.Clear(); snapshots.Clear();
        live = create(); liveCadence = new(30);
        Process(live, liveCadence, prices.Take(60));
        Process(live, liveCadence, corrected.Skip(57), 3);
        check(Json(history.Values) == Json(create().Calc(corrected, 0)),
            "Corrections around a scheduled snapshot match full replay");
        var correctedRecovery = Restore();
        Process(correctedRecovery.Collection, correctedRecovery.Cadence,
            corrected.Where(p => p.RawTime >= correctedRecovery.Plan.Start), correctedRecovery.Plan.Rewind);
        check(Json(history.Values) == Json(create().Calc(corrected, 0)),
            "Corrected sparse checkpoint remains restorable");
    }
}
