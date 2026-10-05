using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using FIG.Studies;
using FIGCommon.Models;
using Microsoft.Extensions.Logging.Abstractions;
using FIGSignalExSvc.Plugins;
using FIGSignalExSvc.Services;
using Microsoft.Extensions.Configuration;

string Json<T>(T value) => JsonSerializer.Serialize(value, StudyStateJson.Options);
StudyColBase Create(string kind, int length = 50)
{
    IStudyCollectionFactory factory = kind == "ADXTrend" ? new ADXTrendFactory() : new L8L6Factory();
    string options = kind == "ADXTrend"
        ? Json(new ADXTrendOptions { PriceMALen = Math.Max(1, length / 5), GuideMALen = length, GuideStdDev = 2, ADXLen = 14, ADXSmooth = 14, ADXTrigger = 15 })
        : Json(new L8L6Options { L8Len = length, L6Len = Math.Max(1, length / 2) });
    return factory.Create(new StudyCollectionContext(new StudyColRS { Id = 1, ColType = kind, ColParams = options },
        new TickerRS(), new IntervalRS { IntervalLen = 60 }, NullLoggerFactory.Instance));
}
List<PriceDataRS> Prices(int count, bool flat = false) => Enumerable.Range(0, count).Select(i =>
{
    decimal value = flat ? 100 : 100 + (decimal)Math.Sin(i * .039) * 35 + (decimal)Math.Sin(i * .41) * 3;
    long time = 1_700_000_040 + i * 60L;
    return new PriceDataRS { RawTime = time, PriceDate = DateTimeOffset.FromUnixTimeSeconds(time).UtcDateTime,
        Open = value, High = flat ? value : value + 2, Low = flat ? value : value - 3, Close = value, Volume = 1000 + i };
}).ToList();
string Hash(IEnumerable<BarStudy> bars) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(bars, StudyStateJson.Options)));

if (args.Contains("--baseline"))
{
    foreach (string kind in new[] { "ADXTrend", "L8L6" })
        Console.WriteLine($"{kind}: {Hash(Create(kind).Calc(Prices(2000), 0))}");
    return;
}

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
    Console.WriteLine($"PASS {name}");
}
void Reject(Action action, string name)
{
    try { action(); }
    catch (InvalidDataException) { Check(true, name); return; }
    throw new Exception($"Expected rejection: {name}");
}
CollectionCheckpoint RoundTrip(CollectionCheckpoint value) => JsonSerializer.Deserialize<CollectionCheckpoint>(Json(value), StudyStateJson.Options)!;
void Restore(StudyColBase collection, CollectionCheckpoint saved, IEnumerable<PriceDataRS> prices)
{
    var window = collection.GetRequiredCheckpointPrices(saved)!;
    collection.RestoreCheckpoint(saved, prices.Where(p => p.RawTime >= window.FirstRawTime && p.RawTime <= window.LastRawTime).ToList());
}

try
{
    var prices = Prices(2000);
    var hashes = new Dictionary<string, string> {
        ["ADXTrend"] = "AC1F5FC0EC9C9E1BB0FDF4EE8485CA03376366B3BA094D35A110A67AC4F18CA1",
        ["L8L6"] = "8D0192FBE8D9D82C2D8DD9A11EFB9888793A7A8DEB19292F825A027E47DF74D4"
    };
    foreach (string kind in hashes.Keys)
    {
        Check(Create(kind).SupportsCheckpoint, $"{kind} opts into the checkpoint contract");
        var original = Create(kind).Calc(prices, 0);
        Check(Hash(original) == hashes[kind], $"{kind}: all 2,000 outputs match the unmodified plugin, including price-buffer eviction");
        string sideKey = kind == "ADXTrend" ? "LBASEADX_SIDE" : "L8L6_SIDE";
        var sides = original.Where(b => b.Studies.ContainsKey(sideKey)).Select(b => Convert.ToDecimal(b.Studies[sideKey])).ToHashSet();
        Check(sides.Contains(0m) && sides.Contains(1m) && (kind == "ADXTrend" || sides.Contains(-1m)), $"{kind}: fixture covers open and closed positions");
        SparseCheckpointChecks.Run(() => Create(kind), prices.Take(450).ToList(), (ok, name) => Check(ok, $"{kind}: {name}"));

        foreach (int length in new[] { 1, 50, 720 })
        foreach (int split in new[] { 1, 2, 3, 9, 10, 14, 15, 28, 49, 50, 51, 90, 719, 720, 721, 1500 })
        {
            var continuous = Create(kind, length);
            continuous.Calc(prices.Take(split).ToList(), 0);
            var saved = RoundTrip(continuous.CaptureCheckpoint());
            var resumed = Create(kind, length); Restore(resumed, saved, prices);
            Check(Json(saved) == Json(resumed.CaptureCheckpoint()), $"{kind}: complete state round-trip (length {length}, split {split})");
            var tail = prices.Skip(split).Take(40).ToList();
            Check(Json(continuous.Calc(tail, 0)) == Json(resumed.Calc(tail, 0)), $"{kind}: identical continuation (length {length}, split {split})");
            Check(Json(continuous.CaptureCheckpoint()) == Json(resumed.CaptureCheckpoint()), $"{kind}: identical continued state");
        }
        foreach (int rewind in new[] { 1, 3, 8 })
        {
            var continuous = Create(kind); continuous.Calc(prices.Take(1500).ToList(), 0);
            var saved = RoundTrip(continuous.CaptureCheckpoint());
            var resumed = Create(kind); Restore(resumed, saved, prices);
            var revised = prices.Skip(1500 - rewind).Take(rewind + 5).Select(p => new PriceDataRS(p) { High = p.High + .75m, Close = p.Close + .75m }).ToList();
            var expected = continuous.Calc(revised, rewind);
            Check(Json(expected) == Json(resumed.Calc(revised, rewind)), $"{kind}: {rewind}-bar correction survives restart");
            Check(Json(expected) == Json(Create(kind).Calc(prices.Take(1500 - rewind).Concat(revised).ToList(), 0).TakeLast(revised.Count)), $"{kind}: corrected outputs match full replay");
        }
        var flatPrices = Prices(350, true);
        var flat = Create(kind); flat.Calc(flatPrices.Take(300).ToList(), 0);
        var flatCopy = Create(kind); Restore(flatCopy, RoundTrip(flat.CaptureCheckpoint()), flatPrices);
        Check(Json(flat.Calc(flatPrices.Skip(300).ToList(), 0)) == Json(flatCopy.Calc(flatPrices.Skip(300).ToList(), 0)), $"{kind}: flat prices survive restart");

        var mature = Create(kind); mature.Calc(prices.Take(1500).ToList(), 0);
        var checkpoint = RoundTrip(mature.CaptureCheckpoint());
        Check(checkpoint.FormatVersion == 2 && checkpoint.Prices.Count == 0 && checkpoint.PriceWindow!.Count == 60, $"{kind}: rolling prices use a bounded StudyHistory reference");
        Check(checkpoint.Bars.Count == 10 && checkpoint.Bars.All(b => b.Values.Count > 0), $"{kind}: recent outputs retained for correction replay");
        Check(checkpoint.Studies.All(s => s.Parameters.All(p => p.GetProperty("Price").EnumerateObject().Count() == 1)), $"{kind}: parameter prices use timestamp references");
        Reject(() => Create(kind, 51).GetRequiredCheckpointPrices(checkpoint), $"{kind}: changed configuration rejected");
        Reject(() => Create(kind).GetRequiredCheckpointPrices(checkpoint with { Identity = "old-build" }), $"{kind}: old build rejected");
        Reject(() => Create(kind).RestoreCheckpoint(checkpoint), $"{kind}: missing history prices rejected");
        Reject(() => Restore(Create(kind), checkpoint, prices.Where((p, i) => i != 1490)), $"{kind}: missing history row rejected");
        Reject(() => Restore(Create(kind), checkpoint, prices.Select((p, i) => i == 1490 ? new PriceDataRS(p) { Close = p.Close + 1 } : p)), $"{kind}: changed history prices rejected");
        var parameter = JsonNode.Parse(checkpoint.Studies[^1].Parameters[0].GetRawText())!.AsObject(); parameter.Remove("side");
        var damaged = RoundTrip(checkpoint); damaged.Studies[^1].Parameters[0] = JsonSerializer.SerializeToElement(parameter);
        Reject(() => Restore(Create(kind), damaged, prices), $"{kind}: missing strategy state rejected");

        var replayBatch = new StudySignalBatch(kind == "ADXTrend" ? ["LBASEADX"] : ["LL8L6", "SL8L6"], prices[1499].RawTime,
            _ => throw new Exception("Already processed bars must not look up or create signals"));
        replayBatch.Process(original.Skip(1470).Take(30).Select(b => new StudyHistoryRS { RawTime = b.Price.RawTime, rawStudies = b.Studies }), long.MaxValue);
        Check(replayBatch.Changes.Count == 0, $"{kind}: checkpoint replay emits no duplicate trade-driving signals");

        string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        string pluginDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../FIGSignalCollection", $"FIG.StudyCollections.{kind}", "bin", configuration, "net10.0"));
        var catalog = new StudyPluginCatalog(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["SingalProcessing:PluginPath"] = pluginDir }).Build(), NullLogger<StudyPluginCatalog>.Instance);
        Check(catalog.Registrations.Count == 1, $"{kind}: real plugin loader discovers the rebuilt plugin");
        var loaded = catalog.Create(kind, Create(kind).Context); Restore(loaded, checkpoint, prices);
        Check(Json(loaded.Calc(prices.Skip(1500).Take(30).ToList(), 0)) == Json(mature.Calc(prices.Skip(1500).Take(30).ToList(), 0)), $"{kind}: plugin load context restores identical outputs");
    }

    // Seed every protected cache with fractional values to detect truncation or omitted state.
    var strategy = new StudyLBaseADXEx("LADX", "Fast", "Guide", "Active", 15m);
    string[] runtimeFields = ["adxVal", "pdiVal", "ndiVal", "guideUB", "guideLB", "guideMA", "priceMA"];
    foreach (string field in runtimeFields)
        typeof(StudyLBaseADXEx).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(strategy, 123.456m);
    var state = JsonSerializer.Deserialize<StudyCheckpoint>(Json(strategy.CaptureCheckpoint()), StudyStateJson.Options)!;
    var copy = new StudyLBaseADXEx("LADX", "Fast", "Guide", "Active", 15m); copy.RestoreCheckpoint(state);
    Check(Json(state) == Json(copy.CaptureCheckpoint()), "ADXTrend: all fractional runtime caches round-trip exactly");
    var config = JsonNode.Parse(state.Identity.Split('|')[^1])!.AsObject();
    var runtimeNames = state.Runtime.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
    foreach (var field in typeof(StudyLBaseADXEx).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
        Check(config.ContainsKey(field.Name) || runtimeNames.Contains(field.Name), $"ADXTrend: configuration/runtime covers {field.Name}");
    foreach (var field in state.Runtime.EnumerateObject())
    {
        var damaged = JsonNode.Parse(state.Runtime.GetRawText())!.AsObject(); damaged.Remove(field.Name);
        Reject(() => copy.RestoreCheckpoint(state with { Runtime = JsonSerializer.SerializeToElement(damaged) }), $"ADXTrend: missing {field.Name} cache rejected");
    }
    Reject(() => new StudyLBaseADXEx("LADX", "Fast", "Guide", "Active", 16m).RestoreCheckpoint(state), "ADXTrend: changed trigger rejected");
    Check(!new UnknownADX().SupportsCheckpoint && !new UnknownL8L6().SupportsCheckpoint, "Unknown subclasses cannot inherit partial checkpoint support");
    // This optional study is not configured by ADXTrend. Its separate, non-rewindable
    // slope buffer has not been opted in, so enabling it cannot silently lose state.
    Check(!new StudySlopeTrend("Guide", 4, .1m, "SLGuide").SupportsCheckpoint, "Unused slope study continues to fail closed for checkpointing");
    Console.WriteLine($"All {checks} ADXTrend/L8L6 checkpoint checks passed.");
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    Environment.ExitCode = 1;
}

sealed class UnknownADX() : StudyLBaseADXEx("LADX", "Fast", "Guide", "Active", 15m);
sealed class UnknownL8L6 : StudyL8L6;
