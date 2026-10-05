using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FIG.Studies;
using FIGCommon.Models;
using FIGCommon.Utilities;
using FIGSignalExSvc.Services;
using FIGSignalExSvc.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
    Console.WriteLine($"PASS {name}");
}
string Json<T>(T value) => JsonSerializer.Serialize(value, StudyStateJson.Options);
CollectionCheckpoint RoundTrip(CollectionCheckpoint state)
    => JsonSerializer.Deserialize<CollectionCheckpoint>(Json(state), StudyStateJson.Options)!;
void Restore(StudyColBase collection, CollectionCheckpoint checkpoint, IEnumerable<PriceDataRS> history)
{
    var window = collection.GetRequiredCheckpointPrices(checkpoint);
    collection.RestoreCheckpoint(checkpoint, window == null ? null : history
        .Where(p => p.RawTime >= window.FirstRawTime && p.RawTime <= window.LastRawTime)
        .Select(p => new PriceDataRS(p)).ToList());
}
StudyColBase Create(int period = 100, double deviation = 1.5) => new ClassicFactory().Create(new StudyCollectionContext(
    new StudyColRS { Id = 1, ColType = "Classic", ColParams = Json(new ClassicOptions { C21 = period, StdDev1 = deviation, StdDev2 = 2 }) },
    new TickerRS(), new IntervalRS { IntervalLen = 60 }, NullLoggerFactory.Instance));
void Reject(Action action, string name)
{
    try { action(); }
    catch (Exception ex) when (ex is InvalidDataException or JsonException) { Check(true, name); return; }
    throw new Exception($"Expected invalid checkpoint: {name}");
}
List<PriceDataRS> Prices(int count, bool flat = false) => Enumerable.Range(0, count).Select(i =>
{
    decimal value = flat ? 100 : 100 + (decimal)Math.Sin(i * .039) * 35 + (decimal)Math.Sin(i * .41) * 3;
    long time = 1_700_000_040 + i * 60L;
    return new PriceDataRS { RawTime = time, PriceDate = DateTimeOffset.FromUnixTimeSeconds(time).UtcDateTime,
        Open = value, High = flat ? value : value + 2, Low = flat ? value : value - 3, Close = value, Volume = 1000 + i };
}).ToList();
string OutputHash(List<BarStudy> bars) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Json(bars))));

if (args.Length == 2 && args[0] == "--plugin-folder")
{
    var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["SingalProcessing:PluginPath"] = Path.GetFullPath(args[1]) }).Build();
    var catalog = new StudyPluginCatalog(config, NullLogger<StudyPluginCatalog>.Instance);
    Check(catalog.Registrations.Count == 1, "Host discovers exactly the packaged Classic plugin");
    var context = new StudyCollectionContext(
        new StudyColRS { Id = 1, ColType = "Classic", ColParams = "{\"C21\":100,\"StdDev1\":1.5,\"StdDev2\":2}" },
        new TickerRS(), new IntervalRS { IntervalLen = 60 }, NullLoggerFactory.Instance);
    var loaded = catalog.Create("Classic", context);
    Check(loaded.SupportsCheckpoint, "Packaged plugin supports host checkpoints");
    loaded.Calc(Prices(30), 0);
    var saved = RoundTrip(loaded.CaptureCheckpoint());
    var restored = catalog.Create("Classic", context); Restore(restored, saved, Prices(30));
    Check(Json(restored.CaptureCheckpoint()) == Json(saved), "Packaged plugin restores state through the host loader");
    var next = Prices(31).TakeLast(1).ToList();
    Check(Json(loaded.Calc(next, 0)) == Json(restored.Calc(next, 0)), "Packaged plugin resumes identical outputs");
    return;
}

var prices = Prices(1200);
if (!args.Contains("--write-baseline")) SparseCheckpointChecks.Run(() => Create(), prices, Check);
var baselinePath = Path.Combine(Directory.GetCurrentDirectory(), "artifacts/classic-checkpoint/original-outputs.json");
var fixtures = new Dictionary<string, string>();
foreach (var period in new[] { 20, 100, 240, 2000 })
    fixtures[$"C21={period}"] = OutputHash(Create(period).Calc(prices, 0));
fixtures["flat"] = OutputHash(Create().Calc(Prices(1200, true), 0));
if (args.Contains("--write-baseline"))
{
    Directory.CreateDirectory(Path.GetDirectoryName(baselinePath)!);
    File.WriteAllText(baselinePath, Json(fixtures));
    Console.WriteLine("Saved original Classic output hashes for five scenarios.");
    return;
}
if (File.Exists(baselinePath))
    Check(Json(fixtures) == File.ReadAllText(baselinePath), "Checkpoint changes preserve original Classic outputs across five scenarios");

Check(Create().SupportsCheckpoint, "Classic and all configured studies support checkpoints");
foreach (var period in new[] { 20, 100, 240, 2000 })
{
    var longest = new Periods(period).mapPeriods.Values.Max();
    var splits = new[] { 1, 2, 3, 10, 75, Math.Min(longest - 1, 1190), Math.Min(longest, 1191), Math.Min(longest + 1, 1192), 1100 }.Distinct();
    foreach (var split in splits)
    {
        var baseline = Create(period);
        baseline.Calc(prices.Take(split).ToList(), 0);
        var saved = RoundTrip(baseline.CaptureCheckpoint());
        var restored = Create(period); Restore(restored, saved, prices);
        Check(Json(saved) == Json(restored.CaptureCheckpoint()), $"Complete checkpoint round trip (C21={period}, bar={split})");
        var tail = prices.Skip(split).Take(12).ToList();
        Check(Json(baseline.Calc(tail, 0)) == Json(restored.Calc(tail, 0)), "Resumed outputs match uninterrupted calculations");
        Check(Json(baseline.CaptureCheckpoint()) == Json(restored.CaptureCheckpoint()), "Resumed internal state matches uninterrupted calculations");
        Check(saved.Prices.Count == 0 && saved.PriceWindow is { Count: > 0 } && saved.PriceWindow.Count <= longest + 10,
            "Checkpoint references a bounded price window without embedding it");
    }
}

// Ten retained output bars must leave two preceding bars for Classic's reads.
foreach (var rewind in new[] { 1, 3, 8 })
{
    var baseline = Create(); baseline.Calc(prices.Take(1100).ToList(), 0);
    var saved = RoundTrip(baseline.CaptureCheckpoint());
    var restored = Create(); Restore(restored, saved, prices);
    var revised = prices.Skip(1100 - rewind).Take(rewind + 5)
        .Select(p => new PriceDataRS(p) { Close = p.Close + .75m, High = p.High + .75m }).ToList();
    var expected = baseline.Calc(revised, rewind);
    Check(Json(expected) == Json(restored.Calc(revised, rewind)), $"{rewind}-bar corrections survive restart with bounded prices");
    var replay = Create();
    var replayed = replay.Calc(prices.Take(1100 - rewind).Concat(revised).ToList(), 0).TakeLast(revised.Count).ToList();
    Check(Json(expected) == Json(replayed), "Corrected outputs match full recalculation");
    var repeated = Create(); Restore(repeated, RoundTrip(restored.CaptureCheckpoint()), prices.Take(1100 - rewind).Concat(revised));
    Check(Json(restored.Calc([revised[^1]], 1)) == Json(repeated.Calc([revised[^1]], 1)), "Repeated current bar survives a second restart");
}

var uninterrupted = Create();
CollectionCheckpoint? surviving = null;
var allOutputs = uninterrupted.Calc(prices, 0, bar =>
{
    if (bar.Price.RawTime == prices[^101].RawTime) surviving = RoundTrip(uninterrupted.CaptureCheckpoint());
});
var afterDeletion = Create(); Restore(afterDeletion, surviving!, prices.Take(prices.Count - 100));
Check(Json(afterDeletion.Calc(prices.TakeLast(100).ToList(), 0)) == Json(allOutputs.TakeLast(100).ToList()),
    "Deleting the newest 100 bars resumes exactly from the surviving per-bar checkpoint");

var batchCollection = Create();
batchCollection.Calc(prices.Take(1050).ToList(), 0);
var committed = new List<BarStudy>();
var savedBars = new List<StudyCheckpointPayload>();
StudyCheckpointProcessor.CalculateAndCommit(batchCollection, prices.Skip(1050), 0, 0, 7, 16 * 1024 * 1024,
    (bars, batch) => { committed.AddRange(bars); savedBars.AddRange(batch.Items); });
Check(Json(committed) == Json(allOutputs.Skip(1050).ToList()), "Host bounded batches preserve Classic outputs");
Check(savedBars.Count == 150 && savedBars.Select(p => p.RawTime).SequenceEqual(prices.Skip(1050).Select(p => p.RawTime)),
    "Host captures a checkpoint for every Classic bar");
foreach (var index in new[] { 0, 6, 7, 49, 148 })
{
    var restored = Create(); Restore(restored, JsonSerializer.Deserialize<CollectionCheckpoint>(savedBars[index].Json, StudyStateJson.Options)!, prices);
    Check(Json(restored.Calc(prices.Skip(1051 + index).Take(1).ToList(), 0)) == Json(allOutputs.Skip(1051 + index).Take(1).ToList()),
        "Intermediate batch checkpoints resume exactly");
}

var checkpoint = RoundTrip(uninterrupted.CaptureCheckpoint());
Reject(() => Restore(Create(120), checkpoint, prices), "Changed periods reject previous state");
Reject(() => Restore(Create(deviation: 1.6), checkpoint, prices), "Changed deviations reject previous state");
Reject(() => Restore(Create(), checkpoint with { Identity = "older build" }, prices), "Changed schema/build rejects previous state");
var invalid = RoundTrip(checkpoint);
var parameter = JsonNode.Parse(invalid.Studies[^1].Parameters[0].GetRawText())!.AsObject();
parameter.Remove("trigger_LC5dot1");
invalid.Studies[^1].Parameters[0] = JsonSerializer.SerializeToElement(parameter);
Reject(() => Restore(Create(), invalid, prices), "Missing strategy state is rejected");

var support = new StudySupport("test", ["C2", "C5"], new Periods(100))
{
    SEMCycleLongLabel = "C5", SEMCycleLongMA = 101.25m, SEMCycleLongSupport = 99.75m,
    SEMCycleShortLabel = "C2", SEMCycleShortMA = 103.5m, SEMCycleShortResistance = 105.25m
};
var supportState = JsonSerializer.Deserialize<StudyCheckpoint>(Json(support.CaptureCheckpoint()), StudyStateJson.Options)!;
var restoredSupport = new StudySupport("test", ["C2", "C5"], new Periods(100));
restoredSupport.RestoreCheckpoint(supportState);
Check(Json(restoredSupport.CaptureCheckpoint()) == Json(supportState), "All six support/resistance fields survive restoration");
Reject(() => new StudySupport("test", ["C2"], new Periods(100)).RestoreCheckpoint(supportState), "Changed support cycles reject previous state");
Reject(() => restoredSupport.RestoreCheckpoint(supportState with { Runtime = JsonSerializer.SerializeToElement(new { }) }),
    "Missing support runtime fields are rejected");

// This study is shipped by Classic even though the default collection does not register it.
var extra = new StudyUBLBCrossTrend("C21", new Periods(100));
var extraHistory = new CircularBuffer<BarStudy>(10);
var extraPrices = new CPriceList();
foreach (var bar in allOutputs.Take(30))
{
    extraPrices.Add(bar.Price); extraHistory.AddToFront(new BarStudy(bar));
    extra.Process(bar.Price, extraHistory, extraPrices);
}
var extraSaved = JsonSerializer.Deserialize<StudyCheckpoint>(Json(extra.CaptureCheckpoint()), StudyStateJson.Options)!;
var extraRestored = new StudyUBLBCrossTrend("C21", new Periods(100)); extraRestored.RestoreCheckpoint(extraSaved);
Check(Json(extraRestored.CaptureCheckpoint()) == Json(extraSaved), "The optional UBLB cross study supports checkpoints");
Check(!new UnknownCycleDir().SupportsCheckpoint, "Unknown subclasses do not inherit checkpoint support automatically");

// Exercise every parameter field with non-default values, including strategy
// states that the synthetic price series might not naturally enter.
BaseStudy[] customStudies = [
    new StudyCycleDir(5, "ohlc4", "C5", "1"), new StudyCycleTrend("C5", new Periods(100)),
    new StudyLineTrend("C5", new Periods(100)), new StudyMACSupportResistance("C5", "C2", new Periods(100)),
    new StudySEMCCycleAllSEMs("C8", new Periods(100)), new StudySEMCCycleSEMH("C1", "C5", new Periods(100)),
    new StudySupport("test", ["C2", "C5"], new Periods(100)), new StudyUBLBCrossTrend("C21", new Periods(100)),
    new Study_S100dot3(), new Study_LC5dot1()
];
foreach (var study in customStudies)
{
    var parameters = study.GetParams()!;
    parameters.Price = new PriceDataRS(prices[50]);
    parameters.IsValid = false;
    int value = 1;
    foreach (var field in parameters.GetType().GetFields())
    {
        var type = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
        field.SetValue(parameters, type == typeof(string) ? $"signal-{value++}"
            : type == typeof(int) ? (object)value++ : (decimal)value++ + .125m);
    }
    var saved = study.CaptureCheckpoint() with { Parameters = [JsonSerializer.SerializeToElement(parameters, parameters.GetType(), StudyStateJson.Options)] };
    study.RestoreCheckpoint(JsonSerializer.Deserialize<StudyCheckpoint>(Json(saved), StudyStateJson.Options)!);
    Check(Json(study.CaptureCheckpoint()) == Json(saved), $"Every {parameters.GetType().Name} field survives JSON restoration");
}

// Fill a much longer rolling window without keeping thousands of output dictionaries in memory.
foreach (var period in new[] { 240, 2000 })
{
    var longest = new Periods(period).mapPeriods.Values.Max();
    var input = Prices(longest + 40);
    var longRun = Create(period);
    foreach (var price in input.Take(input.Count - 5)) longRun.Calc([price], 0);
    var saved = RoundTrip(longRun.CaptureCheckpoint());
    Check(saved.Prices.Count == 0 && saved.PriceWindow!.Count == longest + 10, $"C21={period} checkpoint refers to the required price window");
    var restored = Create(period); Restore(restored, saved, input);
    var tail = input.TakeLast(8).Select(p => new PriceDataRS(p) { Close = p.Close + .25m }).ToList();
    Check(Json(longRun.Calc(tail, 3)) == Json(restored.Calc(tail, 3)), $"C21={period} long-window correction matches uninterrupted processing");
    Check(Json(longRun.CaptureCheckpoint()) == Json(restored.CaptureCheckpoint()), "Long-window corrected internal states match");
}

Check(checkpoint.Studies.All(s => s.Parameters.All(p => p.GetProperty("Price").EnumerateObject().Single().Name == "RawTime")),
    "Study parameter histories reference prices by timestamp instead of repeating OHLCV");
Reject(() => Create().RestoreCheckpoint(checkpoint), "Referenced checkpoints require their historical inputs");
Reject(() => Restore(Create(), checkpoint, prices.Skip(600)), "Missing preceding price rows reject restoration");
Reject(() => Restore(Create(), checkpoint, prices.Where((_, i) => i != 900)), "A missing middle row rejects restoration");
Reject(() => Restore(Create(), checkpoint, prices.Concat([prices[900]])), "Duplicate or unordered rows reject restoration");
foreach (var change in new Action<PriceDataRS>[] { p => p.Open += 1, p => p.High += 1, p => p.Low -= 1, p => p.Close += 1, p => p.Volume += 1 })
{
    var changed = prices.Select(p => new PriceDataRS(p)).ToList(); change(changed[900]);
    Reject(() => Restore(Create(), checkpoint, changed), "Changed historical OHLCV rejects stale rolling state");
}
var badWindow = checkpoint with { PriceWindow = checkpoint.PriceWindow! with { Count = int.MaxValue } };
Reject(() => Create().GetRequiredCheckpointPrices(badWindow), "Oversized price-window requests fail before querying history");
Reject(() => Restore(Create(), checkpoint with { PriceWindow = null }, prices), "Missing price-window metadata is rejected");
Reject(() => Restore(Create(), checkpoint with { PriceWindow = checkpoint.PriceWindow! with { Algorithm = "unknown" } }, prices),
    "Unknown price checksum versions are rejected");
var badBar = RoundTrip(checkpoint); badBar.Bars[0].Price.Close += 1;
Reject(() => Restore(Create(), badBar, prices), "Retained bar prices must match the validated history");

var rolling = new CPriceList(20);
foreach (var price in prices.Take(70))
{
    rolling.Add(price);
    foreach (var size in new[] { 1, 7, 20 }) rolling.CapturePriceWindow(size).ValidatePrices(rolling.CapturePrices(size));
}
foreach (var price in prices.Skip(65).Take(10).Select(p => new PriceDataRS(p) { Close = p.Close + .25m }))
{
    rolling.Add(price); rolling.CapturePriceWindow(7).ValidatePrices(rolling.CapturePrices(7));
}
rolling.Clear(); rolling.Add(prices[0]); rolling.CapturePriceWindow(7).ValidatePrices([prices[0]]);
Check(true, "Incremental fingerprints handle advancing, eviction, correction rewind and clear");
var scaled = new CPriceList();
scaled.Add(new PriceDataRS { RawTime = 1, Open = 1.25m, High = 2m, Low = 1m, Close = 1.5m, Volume = 2 });
scaled.CapturePriceWindow(1).ValidatePrices([new PriceDataRS { Id = 99, DataSetId = 2, RawTime = 1,
    Open = 1.2500m, High = 2.0000m, Low = 1.0000m, Close = 1.5000m, Volume = 2 }]);
Check(true, "History fingerprints ignore decimal scale and source database IDs");

Console.WriteLine($"{checks} checks passed; steady-state checkpoint: {Encoding.UTF8.GetByteCount(Json(checkpoint)):N0} UTF-8 bytes, {checkpoint.Prices.Count} embedded rolling-window prices, {checkpoint.PriceWindow!.Count} referenced prices.");

sealed class UnknownCycleDir() : StudyCycleDir(5, "ohlc4", "test", "1");
