using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using FIG.Studies;
using FIGCommon.Models;
using FIGCommon.Utilities;
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
StudyColBase Create(int step = 276) => new L2Factory().Create(new StudyCollectionContext(
    new StudyColRS { Id = 1, ColType = "L2", ColParams = Json(new L2Options
    { MinPeriod = step, MaxPeriod = step * 21, PeriodInc = step, StdDev1 = 1, StdDev2 = 2 }) },
    new TickerRS(), new IntervalRS { IntervalLen = 60 }, NullLoggerFactory.Instance));
List<PriceDataRS> Prices(int count, bool flat = false) => Enumerable.Range(0, count).Select(i =>
{
    decimal value = flat ? 100 : 100 + (decimal)Math.Sin(i * .039) * 35 + (decimal)Math.Sin(i * .41) * 3;
    long time = 1_700_000_040 + i * 60L;
    return new PriceDataRS { RawTime = time, PriceDate = DateTimeOffset.FromUnixTimeSeconds(time).UtcDateTime,
        Open = value, High = flat ? value : value + 2, Low = flat ? value : value - 3, Close = value, Volume = 1000 + i };
}).ToList();

// These fixtures are also run before editing the plugin to detect any change to trading calculations.
if (args.Contains("--baseline"))
{
    foreach (var (step, count) in new[] { (3, 450), (276, 31000) })
    {
        var collection = Create(step);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var prices = Prices(count);
        for (int i = 0; i < count; i += 100)
        {
            collection.Calc(prices.Skip(i).Take(100).ToList(), 0, bar =>
                hash.AppendData(JsonSerializer.SerializeToUtf8Bytes(bar, StudyStateJson.Options)));
            if ((i + 100) % 5000 == 0) Console.WriteLine($"Calculated {i + 100} bars");
        }
        Console.WriteLine($"BASELINE step={step} count={count} SHA256={Convert.ToHexString(hash.GetHashAndReset())}");
    }
    return;
}

CollectionCheckpoint RoundTrip(CollectionCheckpoint state)
    => JsonSerializer.Deserialize<CollectionCheckpoint>(Json(state), StudyStateJson.Options)!;
void Restore(StudyColBase collection, CollectionCheckpoint state, IEnumerable<PriceDataRS> prices)
{
    var window = collection.GetRequiredCheckpointPrices(state)!;
    collection.RestoreCheckpoint(state, prices.Where(p => p.RawTime >= window.FirstRawTime && p.RawTime <= window.LastRawTime).ToList());
}
void Reject(Action action, string name)
{
    try { action(); }
    catch (InvalidDataException) { Check(true, name); return; }
    throw new Exception($"Expected invalid checkpoint: {name}");
}
string HashBars(IEnumerable<BarStudy> bars)
{
    using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    foreach (var bar in bars) hash.AppendData(JsonSerializer.SerializeToUtf8Bytes(bar, StudyStateJson.Options));
    return Convert.ToHexString(hash.GetHashAndReset());
}

try
{
    var prices = Prices(450);
    Check(Create().SupportsCheckpoint, "L2 supports the host checkpoint contract");
    Check(HashBars(Create(3).Calc(prices, 0)) == "ABCBBA59AB59305D5EDC2F925C6C66DAA5EC7445C6F72576783D3E8D751E24F6",
        "All outputs match the original plugin (450 bars, short cycles)");
    SparseCheckpointChecks.Run(() => Create(3), prices, Check);

    // Audit every plugin study independently, including states a price fixture may never reach.
    Func<BaseStudy>[] factories = [
        () => new StudyCycleDir(3, "ohlc4", "C1", "1"), () => new StudyCrossSEMMA("C1"),
        () => new StudyCrossMAMA("C1"), () => new StudyMajorTrends("C21"),
        () => new StudyLowerUpper("C6", "C21", 3, 63, 3), () => new StudyFlag("C5", "C21"),
        () => new StudyLineTrendEx("C1"), () => new StudyTrend2SEM("C1", "C5"),
        () => new StudyTrend3SEM("C1", "C5"), () => new StudyTrend3MA("C5", "C1", "C2", "C3", "C4"),
        () => new StudyPivot2Cycles("C3", "L2"), () => new StudyPivotStatus("C1"),
        () => new StudyCycleTrendEx("C1"), () => new Study_BaseL1(), () => new Study_L2()
    ];
    var pluginTypes = typeof(StudyColL2).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(BaseStudy))).ToHashSet();
    Check(pluginTypes.SetEquals(factories.Select(f => f().GetType())), "Every L2 custom study is covered by the state audit");
    foreach (var factory in factories)
    {
        var study = factory();
        var buffer = (CircularBuffer<BaseParams>)typeof(BaseStudy).GetField("paramHist", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(study)!;
        for (int i = 0; i < 10; i++)
        {
            var parameters = study.GetParams()!;
            parameters.Price = new PriceDataRS(prices[i]);
            foreach (var field in parameters.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (!field.IsPublic) throw new Exception($"Unreviewed private parameter state: {field.Name}");
                var type = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
                field.SetValue(parameters, type == typeof(int) ? (object)(i + 17) : type == typeof(decimal) ? (object)(i + 13.25m)
                    : throw new Exception($"Unreviewed parameter type: {field.Name}"));
            }
            buffer.AddToFront(parameters);
        }
        foreach (var field in study.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            field.SetValue(study, field.FieldType == typeof(int) ? (object)7 : 123.75m);
        var saved = JsonSerializer.Deserialize<StudyCheckpoint>(Json(study.CaptureCheckpoint()), StudyStateJson.Options)!;
        var restored = factory(); restored.RestoreCheckpoint(saved);
        Check(Json(saved) == Json(restored.CaptureCheckpoint()), $"All ten parameter rows and runtime fields round-trip: {study.GetType().Name}");

        var config = JsonNode.Parse(saved.Identity.Split('|')[^1])!.AsObject();
        var runtimeNames = saved.Runtime.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var field in study.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            Check(config.ContainsKey(field.Name) || runtimeNames.Contains(field.Name), $"Checkpoint covers study field {study.GetType().Name}.{field.Name}");

        // Required parameter and strategy runtime fields must fail closed if a payload is incomplete.
        var incomplete = JsonNode.Parse(saved.Parameters[0].GetRawText())!.AsObject();
        incomplete.Remove(incomplete.First(p => p.Key != "Price").Key);
        var damagedParams = saved.Parameters.ToArray(); damagedParams[0] = JsonSerializer.SerializeToElement(incomplete);
        Reject(() => factory().RestoreCheckpoint(saved with { Parameters = damagedParams }), $"Missing parameters rejected: {study.GetType().Name}");
        foreach (var field in saved.Runtime.EnumerateObject())
        {
            var damaged = JsonNode.Parse(saved.Runtime.GetRawText())!.AsObject(); damaged.Remove(field.Name);
            Reject(() => factory().RestoreCheckpoint(saved with { Runtime = JsonSerializer.SerializeToElement(damaged) }), $"Missing runtime field rejected: {study.GetType().Name}.{field.Name}");
        }
    }
    Check(!new UnknownCycle().SupportsCheckpoint && !new UnknownStrategy().SupportsCheckpoint,
        "Unknown subclasses cannot inherit partial checkpoint support");

    foreach (int step in new[] { 3, 276 })
    foreach (int split in new[] { 1, 2, 3, 9, 10, 24, 25, 26, 64, 65, 66, 129, 130, 131, 169, 170, 171, 300, 447 })
    {
        var collection = Create(step); collection.Calc(prices.Take(split).ToList(), 0);
        var saved = RoundTrip(collection.CaptureCheckpoint());
        var resumed = Create(step); Restore(resumed, saved, prices);
        Check(Json(saved) == Json(resumed.CaptureCheckpoint()), $"Complete collection state round-trip (step {step}, bar {split})");
        var tail = prices.Skip(split).Take(35).ToList();
        Check(Json(collection.Calc(tail, 0)) == Json(resumed.Calc(tail, 0)), $"Identical resumed outputs (step {step}, bar {split})");
        Check(Json(collection.CaptureCheckpoint()) == Json(resumed.CaptureCheckpoint()), "Identical resumed internal state");
    }

    foreach (int rewind in new[] { 1, 3, 8 })
    {
        var collection = Create(3); collection.Calc(prices.Take(300).ToList(), 0);
        var saved = RoundTrip(collection.CaptureCheckpoint());
        var resumed = Create(3); Restore(resumed, saved, prices);
        var corrected = prices.Skip(300 - rewind).Take(rewind + 5)
            .Select(p => new PriceDataRS(p) { Close = p.Close + .75m, High = p.High + .75m }).ToList();
        var expected = collection.Calc(corrected, rewind);
        Check(Json(expected) == Json(resumed.Calc(corrected, rewind)), $"Restart preserves {rewind}-bar correction");
        Check(Json(expected) == Json(Create(3).Calc(prices.Take(300 - rewind).Concat(corrected).ToList(), 0).TakeLast(corrected.Count)),
            $"{rewind}-bar correction matches full historical calculation");
        var repeat = Create(3); Restore(repeat, RoundTrip(resumed.CaptureCheckpoint()), prices.Take(300 - rewind).Concat(corrected));
        Check(Json(resumed.Calc([corrected[^1]], 1)) == Json(repeat.Calc([corrected[^1]], 1)), "Repeated forming bar after restart matches");
    }

    var flatPrices = Prices(300, true);
    var flat = Create(3); flat.Calc(flatPrices.Take(250).ToList(), 0);
    var flatRestart = Create(3); Restore(flatRestart, RoundTrip(flat.CaptureCheckpoint()), flatPrices);
    Check(Json(flat.Calc(flatPrices.Skip(250).ToList(), 0)) == Json(flatRestart.Calc(flatPrices.Skip(250).ToList(), 0)), "Flat prices resume identically");

    var mature = Create(3); mature.Calc(prices.Take(300).ToList(), 0);
    var checkpoint = RoundTrip(mature.CaptureCheckpoint());
    Check(checkpoint.FormatVersion == 2 && checkpoint.Prices.Count == 0 && checkpoint.PriceWindow!.Count == 300,
        "Large price history is referenced from StudyHistory, not embedded in JSON");
    Check(checkpoint.Studies.All(s => s.Parameters.All(p => p.GetProperty("Price").EnumerateObject().Count() == 1)),
        "Per-study parameters contain only price timestamps");
    Check(checkpoint.Bars.Count == 10 && checkpoint.Bars.All(b => b.Values.Count > 0), "Recent typed outputs remain available for replay");
    Reject(() => Create(276).GetRequiredCheckpointPrices(checkpoint), "Changed collection settings rejected");
    Reject(() => Create(3).GetRequiredCheckpointPrices(checkpoint with { Identity = "old-build" }), "Changed plugin build rejected");
    Reject(() => Create(3).GetRequiredCheckpointPrices(checkpoint with { FormatVersion = 1 }), "Old checkpoint format rejected");
    Reject(() => Create(3).RestoreCheckpoint(checkpoint), "Missing StudyHistory prices rejected");
    Reject(() => Restore(Create(3), checkpoint, prices.Skip(1)), "Missing required history row rejected");
    Reject(() => Restore(Create(3), checkpoint, prices.Select((p, i) => i == 5 ? new PriceDataRS(p) { Close = p.Close + 1 } : p)),
        "Changed historical OHLCV rejected");

    // Longest fixed cycle, its warm-up boundary, and eviction beyond the old declared price capacity.
    var longPrices = Prices(31000);
    var longRun = Create();
    using var longHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    var boundaries = new[] { 722, 723, 724, 9383, 9384, 9385, 15179, 15180, 15181, 24563, 24564, 24565, 24575, 30565, 31000 };
    int previous = 0;
    foreach (int end in boundaries)
    {
        for (int start = previous; start < end; start += 100)
            longRun.Calc(longPrices.Skip(start).Take(Math.Min(100, end - start)).ToList(), 0,
                b => longHash.AppendData(JsonSerializer.SerializeToUtf8Bytes(b, StudyStateJson.Options)));
        var saved = RoundTrip(longRun.CaptureCheckpoint());
        Check(saved.PriceWindow!.Count == Math.Min(end, 24574), $"Bounded history window at bar {end}");
        var copy = Create(); Restore(copy, saved, longPrices);
        Check(Json(saved) == Json(copy.CaptureCheckpoint()), $"Full state restores at long-cycle boundary {end}");
        // A continuous instance and a restored instance both receive the host's correction window.
        var replay = longPrices.Skip(end - 3).Take(Math.Min(38, longPrices.Count - end + 3)).ToList();
        var expectedReplay = longRun.Calc(replay, 3);
        Check(Json(expectedReplay) == Json(copy.Calc(replay, 3)), $"Long-cycle restart and three-bar replay at {end}");
        // Return the baseline to its checkpoint before hashing the next uninterrupted segment.
        longRun = Create(); Restore(longRun, saved, longPrices);
        previous = end;
        Console.WriteLine($"Long-cycle validation reached {end} bars");
    }
    Check(Convert.ToHexString(longHash.GetHashAndReset()) == "077287B5FD1F6DB89869E1B4198CFD9097D1A269E388348AFA55D7B02372A100",
        "Every output through 31,000 bars matches the original plugin despite repeated checkpoint restores");

    // The real loader scans only this plugin's build directory.
    var pluginDirectory = Path.GetDirectoryName(typeof(L2Factory).Assembly.Location)!;
    var catalog = new StudyPluginCatalog(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["SingalProcessing:PluginPath"] = pluginDirectory }).Build(), NullLogger<StudyPluginCatalog>.Instance);
    Check(catalog.Registrations.Count == 1 && catalog.Registrations.Single().Factory.ColType == "L2", "Host discovers the rebuilt L2 plugin");
    var loaded = catalog.Create("L2", Create(3).Context);
    Restore(loaded, checkpoint, prices);
    Check(Json(loaded.Calc(prices.Skip(300).ToList(), 0)) == Json(mature.Calc(prices.Skip(300).ToList(), 0)),
        "Plugin load context restores identical L2 results using shared host assemblies");
    Console.WriteLine($"All {checks} L2 checkpoint checks passed.");
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    Environment.ExitCode = 1;
}

sealed class UnknownCycle() : StudyCycleDir(3, "ohlc4", "C1", "1");
sealed class UnknownStrategy : Study_L2;
