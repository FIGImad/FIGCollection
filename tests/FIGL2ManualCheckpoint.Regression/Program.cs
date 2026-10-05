using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using FIG.Studies;
using FIGCommon.Models;
using FIGSignalExSvc.Services;
using FIGSignalExSvc.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

string Json<T>(T value) => JsonSerializer.Serialize(value, StudyStateJson.Options);
StudyColBase Create(int period = 130) => new L2ManualFactory().Create(new StudyCollectionContext(
    new StudyColRS { Id = 1, ColType = "L2Manual", ColParams = Json(new L2ManualOptions { MainPeriod = period, StdDev1 = 1, StdDev2 = 2 }) },
    new TickerRS(), new IntervalRS { IntervalLen = 60 }, NullLoggerFactory.Instance));
var prices = Enumerable.Range(0, 2200).Select(i =>
{
    decimal value = 100 + (decimal)Math.Sin(i * .039) * 35 + (decimal)Math.Sin(i * .41) * 3;
    long time = 1_700_000_040 + i * 60L;
    return new PriceDataRS { RawTime = time, PriceDate = DateTimeOffset.FromUnixTimeSeconds(time).UtcDateTime,
        Open = value, High = value + 2, Low = value - 3, Close = value, Volume = 1000 + i };
}).ToList();
string Hash(IEnumerable<BarStudy> bars) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(bars, StudyStateJson.Options)));
if (args.Contains("--baseline"))
{
    Console.WriteLine(Hash(Create().Calc(prices, 0)));
    return;
}

int checks = 0;
void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; Console.WriteLine($"PASS {name}"); }
CollectionCheckpoint RoundTrip(CollectionCheckpoint state) => JsonSerializer.Deserialize<CollectionCheckpoint>(Json(state), StudyStateJson.Options)!;
void Restore(StudyColBase collection, CollectionCheckpoint state, IEnumerable<PriceDataRS> input)
{
    var window = collection.GetRequiredCheckpointPrices(state)!;
    collection.RestoreCheckpoint(state, input.Where(p => p.RawTime >= window.FirstRawTime && p.RawTime <= window.LastRawTime).ToList());
}
void Reject(Action action, string name)
{
    try { action(); } catch (InvalidDataException) { Check(true, name); return; }
    throw new Exception($"Expected rejection: {name}");
}
try
{
    Check(Create().SupportsCheckpoint, "L2Manual supports checkpointing");
    var expected = Create().Calc(prices, 0);
    Check(Hash(expected) == "D53CF9D14A0F75CBBCBCCFBC2E8734507ADB6CB08B06F872836147D0E3B7BD90",
        "All 2,200 output bars match the original plugin, including bounded-buffer eviction");
    var positionPrices = prices.Take(450).Select(p => new PriceDataRS(p) { High = p.Close + .1m, Low = p.Close - .1m }).ToList();
    var positions = Create(25).Calc(positionPrices, 0);
    Check(positions.Any(b => Equals(b["L2Manual_SIDE"], 1m)) && positions.Any(b => Equals(b["L2Manual_SIDE"], 0m)), "Narrow-range fixture covers open and closed strategy states");
    foreach (int split in Enumerable.Range(3, positions.Count - 4).Where(i => !Equals(positions[i]["L2Manual_SIDE"], positions[i - 1]["L2Manual_SIDE"])).Take(12))
    {
        var active = Create(25); active.Calc(positionPrices.Take(split).ToList(), 0);
        var restoredPosition = Create(25); Restore(restoredPosition, RoundTrip(active.CaptureCheckpoint()), positionPrices);
        Check(Json(active.Calc(positionPrices.Skip(split).ToList(), 0)) == Json(restoredPosition.Calc(positionPrices.Skip(split).ToList(), 0)), "Restart at a strategy transition preserves subsequent signals");
    }
    SparseCheckpointChecks.Run(() => Create(), prices.Take(450).ToList(), Check);
    foreach (int period in new[] { 2, 130, 720 })
    foreach (int split in new[] { 1, 2, 3, 9, 10, period - 1, period, period + 1, 1000, 2000 }.Distinct())
    {
        var continuous = Create(period); continuous.Calc(prices.Take(split).ToList(), 0);
        var saved = RoundTrip(continuous.CaptureCheckpoint());
        var resumed = Create(period); Restore(resumed, saved, prices);
        Check(Json(saved) == Json(resumed.CaptureCheckpoint()), $"Complete state round-trip (period {period}, bar {split})");
        var tail = prices.Skip(split).Take(40).ToList();
        Check(Json(continuous.Calc(tail, 0)) == Json(resumed.Calc(tail, 0)), "Restored calculation matches uninterrupted outputs");
        Check(Json(continuous.CaptureCheckpoint()) == Json(resumed.CaptureCheckpoint()), "Continued internal state matches");
    }
    foreach (int rewind in new[] { 1, 3, 8 })
    {
        var continuous = Create(); continuous.Calc(prices.Take(1500).ToList(), 0);
        var saved = RoundTrip(continuous.CaptureCheckpoint());
        var resumed = Create(); Restore(resumed, saved, prices);
        var revised = prices.Skip(1500 - rewind).Take(rewind + 5).Select(p => new PriceDataRS(p) { Close = p.Close + .75m, High = p.High + .75m }).ToList();
        var output = continuous.Calc(revised, rewind);
        Check(Json(output) == Json(resumed.Calc(revised, rewind)), $"{rewind}-bar correction survives restart");
        Check(Json(output) == Json(Create().Calc(prices.Take(1500 - rewind).Concat(revised).ToList(), 0).TakeLast(revised.Count)), "Corrected outputs match full replay");
    }
    var flatPrices = prices.Take(350).Select(p => new PriceDataRS(p) { Open = 100, High = 100, Low = 100, Close = 100 }).ToList();
    var flat = Create(); flat.Calc(flatPrices.Take(300).ToList(), 0);
    var flatRestart = Create(); Restore(flatRestart, RoundTrip(flat.CaptureCheckpoint()), flatPrices);
    Check(Json(flat.Calc(flatPrices.Skip(300).ToList(), 0)) == Json(flatRestart.Calc(flatPrices.Skip(300).ToList(), 0)), "Flat price history restores identically");

    var mature = Create(); mature.Calc(prices.Take(1500).ToList(), 0);
    var checkpoint = RoundTrip(mature.CaptureCheckpoint());
    Check(checkpoint.FormatVersion == 2 && checkpoint.Prices.Count == 0 && checkpoint.PriceWindow!.Count == 140, "Rolling price window references StudyHistory instead of embedding OHLCV");
    Check(checkpoint.Bars.Count == 10 && checkpoint.Bars.All(b => b.Values.Count > 0), "Recent typed study outputs are retained");
    Check(checkpoint.Studies.All(s => s.Parameters.All(p => p.GetProperty("Price").EnumerateObject().Count() == 1)), "Parameter prices are timestamp references");
    Reject(() => Create(131).GetRequiredCheckpointPrices(checkpoint), "Changed parameters rejected");
    Reject(() => Create().GetRequiredCheckpointPrices(checkpoint with { Identity = "old-build" }), "Old plugin build rejected");
    Reject(() => Create().RestoreCheckpoint(checkpoint), "Missing history prices rejected");
    Reject(() => Restore(Create(), checkpoint, prices.Where((p, i) => i != 1490)), "Missing history row rejected");
    Reject(() => Restore(Create(), checkpoint, prices.Select((p, i) => i == 1490 ? new PriceDataRS(p) { Close = p.Close + 1 } : p)), "Changed historical OHLCV rejected");
    foreach (int index in new[] { 1, 2 })
    {
        var custom = checkpoint.Studies[index];
        foreach (var field in custom.Parameters[0].EnumerateObject().Where(p => p.Name != "Price"))
        {
            var damaged = RoundTrip(checkpoint);
            var parameter = JsonNode.Parse(custom.Parameters[0].GetRawText())!.AsObject(); parameter.Remove(field.Name);
            damaged.Studies[index].Parameters[0] = JsonSerializer.SerializeToElement(parameter);
            Reject(() => Restore(Create(), damaged, prices), $"Missing custom parameter rejected: study {index}, {field.Name}");
        }
    }
    Check(!new UnknownLine().SupportsCheckpoint && !new UnknownManual().SupportsCheckpoint, "Unknown subclasses cannot inherit partial checkpoint support");
    var line = new StudyLineTrendEx("L2");
    Reject(() => new StudyLineTrendEx("other").RestoreCheckpoint(line.CaptureCheckpoint()), "Changed line-trend label rejected");

    var history = new SortedDictionary<long, BarStudy>();
    var snapshots = new SortedDictionary<long, CollectionCheckpoint>();
    var selected = new List<int>();
    void Process(StudyColBase collection, StudyCheckpointCadence cadence, IEnumerable<PriceDataRS> input, int rewind = 0)
        => StudyCheckpointProcessor.CalculateAndCommit(collection,
            new StudyCheckpointPolicy().SelectInterval(input, cadence, rewind, selected.Add), 0, rewind, 7, long.MaxValue,
            (bars, batch) => {
                foreach (var b in bars) { history[b.Price.RawTime] = b; snapshots.Remove(b.Price.RawTime); }
                foreach (var p in batch.Items) snapshots[p.RawTime] = JsonSerializer.Deserialize<CollectionCheckpoint>(p.Json, StudyStateJson.Options)!;
            }, cadence: cadence);
    var live = Create(); var cadence = new StudyCheckpointCadence(30);
    Process(live, cadence, prices.Take(245));
    Check(snapshots.Keys.SequenceEqual(new[] { prices[99].RawTime, prices[199].RawTime }), "Catch-up writes checkpoints at 100-bar intervals");
    for (int end = 246; end <= 310; end++) Process(live, cadence, prices.Skip(end - 4).Take(4), 3);
    Check(selected[0] == 100 && selected.Skip(1).All(i => i == 30), "Live processing switches to 30-bar cadence");
    Check(snapshots.Keys.SequenceEqual(new[] { 100, 200, 243, 273, 303 }.Select(n => prices[n - 1].RawTime)), "Adaptive snapshots preserve progress and 30-bar live spacing");
    Check(Json(history.Values) == Json(expected.Take(310)), "Adaptive catch-up/live output matches full replay");
    var restart = Create(); var latest = snapshots.Values.Last(); Restore(restart, latest, history.Values.Select(b => b.Price));
    var restartCadence = new StudyCheckpointCadence(30); restartCadence.Restore(latest);
    var plan = StudyCheckpointReplayPlan.FromCheckpoint(latest, history.Keys.Last(), 3);
    Process(restart, restartCadence, plan.RequireStart(prices.Take(350).Where(p => p.RawTime >= plan.Start)), plan.Rewind);
    Check(Json(history.Values) == Json(expected.Take(350)), "Restart between adaptive checkpoints regenerates every intervening history row");
    var signals = new StudySignalBatch(["LongManual", "ShortManual"], prices[349].RawTime, _ => throw new Exception("Replayed signals must be skipped"));
    signals.Process(history.Values.Select(b => new StudyHistoryRS { RawTime = b.Price.RawTime, rawStudies = b.Studies }), long.MaxValue);
    Check(signals.Changes.Count == 0, "Replayed L2Manual history generates no duplicate trade-driving signals");

    var catalog = new StudyPluginCatalog(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["SingalProcessing:PluginPath"] = Path.GetDirectoryName(typeof(L2ManualFactory).Assembly.Location) }).Build(), NullLogger<StudyPluginCatalog>.Instance);
    Check(catalog.Registrations.Count == 1, "Host loader discovers the rebuilt L2Manual plugin");
    var loaded = catalog.Create("L2Manual", Create().Context); Restore(loaded, checkpoint, prices);
    Check(Json(loaded.Calc(prices.Skip(1500).Take(30).ToList(), 0)) == Json(mature.Calc(prices.Skip(1500).Take(30).ToList(), 0)), "Plugin load context restores identical outputs");
    Console.WriteLine($"All {checks} L2Manual checkpoint checks passed.");
}
catch (Exception ex) { Console.Error.WriteLine(ex); Environment.ExitCode = 1; }

sealed class UnknownLine() : StudyLineTrendEx("L2");
sealed class UnknownManual : Study_L2Manual;
