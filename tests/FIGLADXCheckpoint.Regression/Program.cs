using System.Text.Json;
using System.Text.Json.Nodes;
using FIG.Studies;
using FIG.StudyCollections.LADX;
using FIGCommon.Models;
using Microsoft.Extensions.Logging.Abstractions;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
    Console.WriteLine($"PASS {name}");
}
string Json<T>(T value) => JsonSerializer.Serialize(value, StudyStateJson.Options);
CollectionCheckpoint RoundTrip(CollectionCheckpoint checkpoint)
    => JsonSerializer.Deserialize<CollectionCheckpoint>(Json(checkpoint), StudyStateJson.Options)!;
StudyColBase Create(LADXOptions? options = null) => new LADXFactory().Create(new StudyCollectionContext(
    new StudyColRS { Id = 1, ColType = "LADX", ColParams = Json(options ?? new LADXOptions()) },
    new TickerRS(), new IntervalRS { IntervalLen = 60 }, NullLoggerFactory.Instance));
void Reject(Action action, string name)
{
    try { action(); }
    catch (InvalidDataException) { Check(true, name); return; }
    throw new Exception($"Expected invalid checkpoint: {name}");
}
List<PriceDataRS> Prices(int count, bool flat = false) => Enumerable.Range(0, count).Select(i =>
{
    decimal value = flat ? 100 : 100 + (decimal)Math.Sin(i * .039) * 35 + (decimal)Math.Sin(i * .41) * 3;
    long time = 1_700_000_040 + i * 60L;
    return new PriceDataRS { RawTime = time, PriceDate = DateTimeOffset.FromUnixTimeSeconds(time).UtcDateTime,
        Open = value, High = flat ? value : value + 2, Low = flat ? value : value - 3, Close = value, Volume = 1000 + i };
}).ToList();

if (args.Contains("--benchmark"))
{
    var collection = Create();
    var input = Prices(5200);
    collection.Calc(input.Take(200).ToList(), 0);
    long bytes = 0;
    var watch = System.Diagnostics.Stopwatch.StartNew();
    collection.Calc(input.Skip(200).ToList(), 0, _ =>
        bytes += System.Text.Encoding.UTF8.GetByteCount(Json(collection.CaptureCheckpoint())));
    watch.Stop();
    Console.WriteLine($"5000 bars: {watch.Elapsed.TotalSeconds:F3}s, {bytes / 5000} average checkpoint bytes, {bytes} total bytes");
    return;
}

var prices = Prices(450);
SparseCheckpointChecks.Run(() => Create(), prices, Check);
Check(Create().SupportsCheckpoint, "LADX opts into the host checkpoint contract");
var beforeDeletion = Create();
CollectionCheckpoint? lastRetained = null;
var allOutputs = beforeDeletion.Calc(prices, 0, bar =>
{
    if (bar.Price.RawTime == prices[^101].RawTime) lastRetained = RoundTrip(beforeDeletion.CaptureCheckpoint());
});
var afterDeletion = Create(); afterDeletion.RestoreCheckpoint(lastRetained!);
Check(Json(afterDeletion.Calc(prices.TakeLast(100).ToList(), 0)) == Json(allOutputs.TakeLast(100).ToList()),
    "Rebuilding 100 deleted bars from the surviving per-bar checkpoint matches full LADX replay");
foreach (var options in new[]
{
    new LADXOptions(),
    new LADXOptions { AdxLen = 1, AdxLenConf = 1, FastMALen = 1, SlowMALen = 1, GuideMALen = 1, ShockATRLen = 1, RevisionDepth = 1 },
    new LADXOptions { GuideMALen = 720, SlowMALen = 600, RevisionDepth = 5 }
})
{
    var input = options.GuideMALen > 500 ? Prices(1000) : prices;
    foreach (var split in new[] { 1, 2, 6, 21, 22, 23, 50, 100, 250, input.Count - 1 })
    {
        var baseline = Create(options);
        baseline.Calc(input.Take(split).ToList(), 0);
        var saved = RoundTrip(baseline.CaptureCheckpoint());
        var restarted = Create(options);
        restarted.RestoreCheckpoint(saved);
        Check(Json(saved) == Json(restarted.CaptureCheckpoint()), $"Complete state round trip (guide {options.GuideMALen}, split {split})");
        var tail = input.Skip(split).ToList();
        Check(Json(baseline.Calc(tail, 0)) == Json(restarted.Calc(tail, 0)), $"Identical resumed outputs (guide {options.GuideMALen}, split {split})");
        Check(Json(baseline.CaptureCheckpoint()) == Json(restarted.CaptureCheckpoint()), "Identical final internal state");
    }
}

// Restores while signals are open and cooling down, not just at fixed warm-up boundaries.
var streaming = Create();
bool sawLong = false, sawShort = false, sawCooldown = false;
foreach (var price in prices)
{
    var saved = streaming.CaptureCheckpoint();
    var resumed = Create();
    if (saved.Bars.Count > 0) resumed.RestoreCheckpoint(RoundTrip(saved));
    var expected = streaming.Calc([price], 0);
    if (Json(expected) != Json(resumed.Calc([price], 0))) throw new Exception("Per-bar restart mismatch");
    var values = expected[0].Studies;
    sawLong |= Equals(values["STrend_LBaseADX_SIDE"], 1m);
    sawShort |= Equals(values["STrend_SBaseADX_SIDE"], -1m);
    sawCooldown |= Equals(values["STrend_LBaseADX_StartBlockReason"], "COOLDOWN")
        || Equals(values["STrend_SBaseADX_StartBlockReason"], "COOLDOWN");
}
Check(sawLong && sawShort && sawCooldown, "Every-bar restarts cover open long/short trades and cooldown");

foreach (var rewind in new[] { 1, 3, 5, 20 })
{
    var baseline = Create();
    baseline.Calc(prices.Take(300).ToList(), 0);
    var saved = RoundTrip(baseline.CaptureCheckpoint());
    var restarted = Create(); restarted.RestoreCheckpoint(saved);
    var revised = prices.Skip(300 - rewind).Take(rewind + 5).Select(p => new PriceDataRS(p) { Close = p.Close + .75m, High = p.High + .75m }).ToList();
    var expected = baseline.Calc(revised, rewind);
    Check(Json(expected) == Json(restarted.Calc(revised, rewind)), $"{rewind}-bar correction survives restart");
    var fullReplay = Create();
    var replay = fullReplay.Calc(prices.Take(300 - rewind).Concat(revised).ToList(), 0).TakeLast(revised.Count).ToList();
    Check(Json(expected) == Json(replay), $"{rewind}-bar correction matches full historical replay");
    var repeated = Create(); repeated.RestoreCheckpoint(RoundTrip(restarted.CaptureCheckpoint()));
    Check(Json(restarted.Calc([revised[^1]], 1)) == Json(repeated.Calc([revised[^1]], 1)), "Repeated current bar survives another restart");
    // Advance a disposable instance, then recover from the still-committed checkpoint.
    var afterFailure = Create(); afterFailure.RestoreCheckpoint(saved);
    Check(Json(expected) == Json(afterFailure.Calc(revised, rewind)), "Saved payload remains independent of later processing");
}

var flat = Create(); flat.Calc(Prices(300, true), 0);
var flatRestart = Create(); flatRestart.RestoreCheckpoint(RoundTrip(flat.CaptureCheckpoint()));
Check(Json(flat.Calc(Prices(350, true).Skip(300).ToList(), 0)) == Json(flatRestart.Calc(Prices(350, true).Skip(300).ToList(), 0)), "Zero true range survives restoration");

var mature = Create(); mature.Calc(prices.Take(300).ToList(), 0);
var checkpoint = RoundTrip(mature.CaptureCheckpoint());
var oldRevision = Create(); oldRevision.RestoreCheckpoint(checkpoint);
try { oldRevision.Calc(prices.Skip(270).Take(1).ToList(), 22); throw new Exception("Old revision was accepted"); }
catch (InvalidOperationException) { Check(true, "Trimmed history still rejects an unsupported old revision"); }
Reject(() => Create(new LADXOptions { StartBias = 2 }).RestoreCheckpoint(checkpoint), "Changed parameters rejected");
Reject(() => Create().RestoreCheckpoint(checkpoint with { Identity = "old-build" }), "Changed build rejected");
Reject(() => Create().RestoreCheckpoint(checkpoint with { FormatVersion = 999 }), "Unknown host schema rejected");

CollectionCheckpoint Damage(Action<JsonObject> change)
{
    var state = JsonNode.Parse(checkpoint.Studies[0].Runtime.GetRawText())!.AsObject();
    change(state);
    return checkpoint with { Studies = [checkpoint.Studies[0] with { Runtime = JsonSerializer.SerializeToElement(state) }] };
}
JsonObject LastState(JsonObject root) => root["Anchor"]!["State"]!.AsObject();
Reject(() => Create().RestoreCheckpoint(Damage(root => root["Version"] = 999)), "Unknown engine schema rejected");
Reject(() => Create().RestoreCheckpoint(Damage(root => root.Remove("Trimmed"))), "Missing trim marker rejected");
Reject(() => Create().RestoreCheckpoint(Damage(root => root["Options"]!["FastMALen"] = 9)), "Mismatched engine settings rejected");
Reject(() => Create().RestoreCheckpoint(Damage(root => root["Anchor"] = null)), "Missing anchor snapshot rejected");
Reject(() => Create().RestoreCheckpoint(Damage(root => root["ReplayBars"]![0]!["Time"] = root["Anchor"]!["Time"]!.GetValue<long>())), "Unordered engine snapshots rejected");
Reject(() => Create().RestoreCheckpoint(Damage(root => LastState(root)["Previous"]!["Price"]!["Time"] = 1)), "Snapshot timestamp mismatch rejected");
Reject(() => Create().RestoreCheckpoint(Damage(root => LastState(root)["Long"]!.AsObject().Remove("MaxImpulse"))), "Missing signal field rejected");
Reject(() => Create().RestoreCheckpoint(Damage(root => LastState(root)["ATR"]!.AsObject().Remove("Sum"))), "Missing smoothing seed rejected");
Reject(() => Create().RestoreCheckpoint(Damage(root => LastState(root)["Long"] = null)), "Null signal state rejected");
Reject(() => Create().RestoreCheckpoint(Damage(root => LastState(root)["MA"]!.AsArray().RemoveAt(0))), "Missing moving average rejected");
Reject(() => Create().RestoreCheckpoint(Damage(root => LastState(root)["Bands"]!["Values"] = null)), "Null rolling values rejected");
Console.WriteLine($"All {checks} LADX checkpoint checks passed.");
