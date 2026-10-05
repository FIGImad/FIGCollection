using System.Text.Json;
using FIG.Studies;
using FIGCommon.Models;
using FIGSignalExSvc.Services;

static class AdaptiveCheckpointChecks
{
    public static void Run(Func<StudyColBase> create, IReadOnlyList<PriceDataRS> prices, Action<bool, string> check)
    {
        string Json<T>(T value) => JsonSerializer.Serialize(value, StudyStateJson.Options);
        CollectionCheckpoint Read(StudyCheckpointPayload payload) => JsonSerializer.Deserialize<CollectionCheckpoint>(payload.Json, StudyStateJson.Options)!;
        var expected = create().Calc(prices.ToList(), 0);
        var policy = new StudyCheckpointPolicy();
        var history = new SortedDictionary<long, BarStudy>();
        var snapshots = new SortedDictionary<long, StudyCheckpointPayload>();
        var selections = new List<int>();
        int attempts = 0, failOnAttempt = 0;
        void Commit(IReadOnlyList<BarStudy> bars, StudyCheckpointBatch batch)
        {
            if (++attempts == failOnAttempt) throw new IOException("Simulated commit failure");
            foreach (var bar in bars) { history[bar.Price.RawTime] = bar; snapshots.Remove(bar.Price.RawTime); }
            foreach (var payload in batch.Items) snapshots[payload.RawTime] = payload;
        }
        void Process(StudyColBase collection, StudyCheckpointCadence cadence, IEnumerable<PriceDataRS> input,
            int rewind = 0, int batchSize = 7, long maxBytes = long.MaxValue)
            => StudyCheckpointProcessor.CalculateAndCommit(collection,
                policy.SelectInterval(input, cadence, rewind, interval => selections.Add(interval)),
                0, rewind, batchSize, maxBytes, Commit, cadence: cadence);
        (StudyColBase Collection, StudyCheckpointCadence Cadence, StudyCheckpointReplayPlan Plan) Restore()
        {
            var saved = Read(snapshots.Values.Last());
            var collection = create();
            var window = collection.GetRequiredCheckpointPrices(saved);
            collection.RestoreCheckpoint(saved, window == null ? null : prices
                .Where(p => p.RawTime >= window.FirstRawTime && p.RawTime <= window.LastRawTime).ToList());
            var cadence = new StudyCheckpointCadence(30); cadence.Restore(saved);
            return (collection, cadence, StudyCheckpointReplayPlan.FromCheckpoint(saved, history.Keys.Last(), 3));
        }
        void Clear() { history.Clear(); snapshots.Clear(); selections.Clear(); attempts = 0; failOnAttempt = 0; }

        foreach (int rewind in new[] { 0, 3 })
        foreach (int pending in new[] { 0, 1, 10, 11, 100 })
        {
            var cadence = new StudyCheckpointCadence(30);
            var input = prices.Take(rewind + pending).ToList();
            var emitted = policy.SelectInterval(input, cadence, rewind).ToList();
            check(emitted.SequenceEqual(input) && cadence.EveryNBars == (pending > 10 ? 100 : 30),
                $"Adaptive cadence selects {(pending > 10 ? 100 : 30)} for {pending} bars plus {rewind} corrections without losing input");
        }
        var configured = new StudyCheckpointCadence(30);
        new StudyCheckpointPolicy(7, 50, 2).SelectInterval(prices.Take(3), configured, 0).ToList();
        check(configured.EveryNBars == 50, "Catch-up interval and live threshold are configurable");
        new StudyCheckpointPolicy(7, 50, 2).SelectInterval(prices.Take(2), configured, 0).ToList();
        check(configured.EveryNBars == 7, "Configured live interval is selected at the exact threshold");
        bool Reject(Action action) { try { action(); return false; } catch (ArgumentOutOfRangeException) { return true; } }
        check(Reject(() => new StudyCheckpointPolicy(0)) && Reject(() => new StudyCheckpointPolicy(30, 0))
            && Reject(() => new StudyCheckpointPolicy(30, 100, 0)), "Invalid adaptive cadence settings rejected");

        var counted = new CountingCollection();
        Process(counted, new(30), prices);
        var catchUpTimes = prices.Where((_, i) => (i + 1) % 100 == 0 && i + 1 < prices.Count).Select(p => p.RawTime);
        check(snapshots.Keys.SequenceEqual(catchUpTimes), "Initial catch-up saves only every 100th completed bar");
        check(counted.Captures == (prices.Count - 1) / 100, "Catch-up avoids capture/serialization on the other 99 bars");
        check(Json(history.Values) == Json(expected), "Adaptive catch-up still writes every study history row identically");
        check(selections.SequenceEqual([100]), "A large processing cycle keeps catch-up cadence through its final short database batch");

        // The last catch-up snapshot is at 200. Switching after 245 is overdue
        // for a 30-bar live interval, so take the first completed replay bar (243).
        Clear();
        var live = create(); var liveCadence = new StudyCheckpointCadence(30);
        Process(live, liveCadence, prices.Take(245));
        for (int end = 246; end <= 310; end++) Process(live, liveCadence, prices.Skip(end - 4).Take(4), 3);
        check(selections[0] == 100 && selections.Skip(1).All(i => i == 30), "Subsequent one-bar cycles switch from 100 to 30");
        check(snapshots.Keys.SequenceEqual(new[] { 100, 200, 243, 273, 303 }.Select(n => prices[n - 1].RawTime)),
            "Live switch keeps counted progress, captures overdue state, then spaces checkpoints by 30");
        check(Json(history.Values) == Json(expected.Take(310)), "Catch-up-to-live transition preserves every output");
        // Now simulate a larger backlog, and then return to live mode again.
        Process(live, liveCadence, prices.Skip(307).Take(114), 3);
        check(selections[^1] == 100 && snapshots.ContainsKey(prices[402].RawTime), "A later backlog switches back to 100 from the last live checkpoint");
        for (int end = 422; end <= 440; end++) Process(live, liveCadence, prices.Skip(end - 4).Take(4), 3);
        check(snapshots.ContainsKey(prices[432].RawTime) && selections[^1] == 30, "Returning to live mode resumes 30-bar spacing from the catch-up checkpoint");

        // A cadence switch inside the correction window must preserve an existing snapshot.
        Clear(); live = create(); liveCadence = new(30);
        Process(live, liveCadence, prices.Take(201));
        Process(live, liveCadence, prices.Skip(198).Take(4), 3);
        check(snapshots.Keys.SequenceEqual(new[] { prices[99].RawTime, prices[199].RawTime }),
            "Changing cadence preserves the checkpoint replayed inside the correction window");
        for (int repeat = 0; repeat < 4; repeat++) Process(live, liveCadence, prices.Skip(199).Take(3), 3);
        check(snapshots.Keys.SequenceEqual(new[] { prices[99].RawTime, prices[199].RawTime }), "Repeated forming/corrected bars do not advance adaptive cadence");

        Clear(); live = create(); liveCadence = new(30);
        Process(live, liveCadence, prices.Take(100));
        check(snapshots.Count == 0, "The forming 100th catch-up bar is not checkpointed");
        Process(live, liveCadence, prices.Skip(97).Take(14), 3);
        check(snapshots.Keys.SequenceEqual([prices[99].RawTime]), "The 100th bar is checkpointed once its successor arrives during catch-up");

        foreach (int split in new[] { 105, 109, 110, 111, 195, 201, 245 })
        {
            Clear(); Process(create(), new(30), prices.Take(split));
            var resumed = Restore();
            // Include three correction bars plus every post-checkpoint bar, even
            // if StudyHistory already contains those rows from the previous run.
            int end = split + 1;
            Process(resumed.Collection, resumed.Cadence, resumed.Plan.RequireStart(prices.Take(end).Where(p => p.RawTime >= resumed.Plan.Start)), resumed.Plan.Rewind);
            check(Json(history.Values) == Json(expected.Take(end)), $"Restart after catch-up at {split} reconstructs the checkpoint gap identically");
            int remaining = end - (split / 100 * 100);
            check(selections[^1] == (remaining > 10 ? 100 : 30), $"Restart counts post-checkpoint recovery work at {split} when choosing cadence");
        }

        Clear(); Process(create(), new(30), prices.Take(450));
        foreach (long time in history.Keys.TakeLast(100).ToArray()) { history.Remove(time); snapshots.Remove(time); }
        var afterDeletion = Restore();
        Process(afterDeletion.Collection, afterDeletion.Cadence, afterDeletion.Plan.RequireStart(prices.Take(450).Where(p => p.RawTime >= afterDeletion.Plan.Start)), afterDeletion.Plan.Rewind);
        check(Json(history.Values) == Json(expected.Take(450)), "Deleting 100 bars rebuilds from a catch-up checkpoint without losing history");

        // Verify bounded look-ahead, one enumeration and disposal on failure.
        Clear(); int reads = 0, enumerations = 0; bool disposed = false;
        IEnumerable<PriceDataRS> Stream()
        {
            if (++enumerations != 1) throw new Exception("Price stream enumerated twice");
            try { foreach (var p in prices) { reads++; yield return p; } }
            finally { disposed = true; }
        }
        failOnAttempt = 2;
        try { Process(create(), new(30), Stream(), batchSize: 7); }
        catch (IOException) { }
        check(history.Count == 7 && reads <= 15 && enumerations == 1 && disposed, "Adaptive selection buffers only threshold+1 bars and stops/disposes on commit failure");

        Clear(); failOnAttempt = 32;
        try { Process(create(), new(30), prices, batchSize: 7); }
        catch (IOException) { }
        check(history.Count == 217 && snapshots.Keys.Last() == prices[199].RawTime, "Interrupted catch-up retains committed rows after the last 100-bar checkpoint");
        failOnAttempt = 0; var retry = Restore();
        Process(retry.Collection, retry.Cadence, retry.Plan.RequireStart(prices.Where(p => p.RawTime >= retry.Plan.Start)), retry.Plan.Rewind, maxBytes: 1);
        check(Json(history.Values) == Json(expected), "Retry from a catch-up checkpoint with a tiny byte cap regenerates identical history");

        // Page size and time gaps cannot inflate the number of study bars.
        foreach (int pageSize in new[] { 1, 7, 100 })
        {
            long alignedStart = prices[0].RawTime - prices[0].RawTime % 300;
            var minutePrices = prices.Take(50).Select((p, i) => new PriceDataRS(p)
            {
                RawTime = alignedStart + i * 60,
                PriceDate = DateTimeOffset.FromUnixTimeSeconds(alignedStart + i * 60).UtcDateTime
            }).ToList();
            var aggregate = StudyHistoryReplay.ReadBars(cursor => minutePrices.Where(p => p.RawTime >= cursor).Take(pageSize).ToList(), 300);
            var cadence = new StudyCheckpointCadence(100);
            var output = policy.SelectInterval(aggregate, cadence, 0).ToList();
            check(cadence.EveryNBars == 30 && output.Count <= 10, $"Adaptive selection counts aggregated bars across price pages ({pageSize})");
        }
        var gapped = prices.Where((_, i) => i % 20 == 0).Take(10).ToList();
        var gappedCadence = new StudyCheckpointCadence(100);
        policy.SelectInterval(gapped, gappedCadence, 0).ToList();
        check(gappedCadence.EveryNBars == 30, "Market time gaps do not turn ten study bars into a catch-up cycle");
    }
}
