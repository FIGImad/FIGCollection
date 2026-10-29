using System.Text.Json;
using FIG.Studies;
using FIGCommon.Models;
using Microsoft.Extensions.Logging;

var logger = new CaptureLogger();
foreach (var type in new[] { typeof(StudyTrend3SEMParams), typeof(StudyCrossSEMMAParams), typeof(StudyCycleDirParams), typeof(StudyExtendedParams) })
{
    var source = (BaseParams)Activator.CreateInstance(type)!;
    var target = (BaseParams)Activator.CreateInstance(type)!;
    var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);
    foreach (var field in fields)
    {
        if (field.FieldType == typeof(int)) field.SetValue(source, 17);
        if (field.FieldType == typeof(decimal) || field.FieldType == typeof(decimal?)) field.SetValue(source, 12.5m);
        if (field.FieldType == typeof(double)) field.SetValue(source, 42.25d);
        if (field.FieldType == typeof(string)) field.SetValue(source, "close");
    }
    var price = new PriceDataRS { RawTime = 123, Close = 12.5m };
    if (!target.TryCopyForNextBar(source, price) || fields.Any(f => !Equals(f.GetValue(source), f.GetValue(target))))
        throw new Exception("Incomplete reusable parameter copy: " + type.Name);
    if (ReferenceEquals(target.Price, source.Price) || ReferenceEquals(target.Price, price) || target.Price.RawTime != 123)
        throw new Exception("Reusable price is not independent: " + type.Name);
    long allocated = GC.GetAllocatedBytesForCurrentThread();
    for (int i = 0; i < 1000; i++) target.TryCopyForNextBar(source, price);
    if (GC.GetAllocatedBytesForCurrentThread() != allocated) throw new Exception("Reusable copy allocates: " + type.Name);
}
using var factory = LoggerFactory.Create(b => b.AddProvider(logger));
StudyColL2 Create(bool enabled)
{
    Environment.SetEnvironmentVariable("FIG_L2TEST_PROFILE", enabled ? "1" : null);
    Environment.SetEnvironmentVariable("FIG_L2TEST_PROFILE_EVERY_BARS", "100");
    return new StudyColL2(new StudyCollectionContext(
        new StudyColRS { Id = 7, ColType = "L2", ColParams = JsonSerializer.Serialize(new L2Options
        { MinPeriod = 3, MaxPeriod = 63, PeriodInc = 3, StdDev1 = 1, StdDev2 = 2 }) },
        new TickerRS(), new IntervalRS { IntervalLen = 60 }, factory));
}
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine($"PASS {message}");
}
var prices = Enumerable.Range(0, 450).Select(i =>
{
    decimal value = 100 + (decimal)Math.Sin(i * .039) * 35 + (decimal)Math.Sin(i * .41) * 3;
    long time = 1_700_000_040 + i * 60L;
    return new PriceDataRS { RawTime = time, PriceDate = DateTimeOffset.FromUnixTimeSeconds(time).UtcDateTime,
        Open = value, High = value + 2, Low = value - 3, Close = value, Volume = 1000 + i };
}).ToList();
string Json<T>(T value) => JsonSerializer.Serialize(value, StudyStateJson.Options);
var plain = Create(false);
var measured = Create(true);
AppContext.SetSwitch("FIG.Studies.DisableParameterReuse", true);
var referenceOutput = plain.Calc(prices, 0);
AppContext.SetSwitch("FIG.Studies.DisableParameterReuse", false);
var measuredOutput = measured.Calc(prices, 0);
Check(Json(referenceOutput) == Json(measuredOutput), "Parameter reuse and profiling preserve all output fields");
var consumerKeys = typeof(Study_L2).GetNestedType("FieldKeys", System.Reflection.BindingFlags.NonPublic)!
    .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
    .Where(f => f.Name.StartsWith("StudyTrend3SEM", StringComparison.Ordinal))
    .Select(f => (string)f.GetValue(null)!).ToArray();
Check(consumerKeys.Length == 36 && consumerKeys.All(k => measuredOutput[^1].Studies.ContainsKey(k)), "All consumed Trend3SEM keys are still published");
Check(measured.OmittedStudyKeys.Count > 600
    && measuredOutput.All(bar => measured.OmittedStudyKeys.All(key => !bar.Studies.ContainsKey(key))),
    "Unused outputs are absent on every bar");
Check(measuredOutput[^1].Studies.ContainsKey("L2_SIDE")
    && !measured.OmittedStudyKeys.Any(key => key.StartsWith("L2_", StringComparison.Ordinal) && key == "L2_SIDE"),
    "Final strategy signal remains published");

// Shared studies default to full output outside this collection. Selection must
// also survive cloning and respect the Extended study's mutable public label.
var extended = new StudyExtended(3, "close", 1, 2, "Audit");
extended.SelectStudyOutputs(new HashSet<string> { "MAAudit", "MAChanged" });
var extendedClone = extended.Clone();
var history = new FIGCommon.Utilities.CircularBuffer<BarStudy>(10);
var priceHistory = new CPriceList(20);
foreach (var price in prices.Take(10))
{
    priceHistory.Add(price);
    history.AddToFront(new BarStudy(price));
    extendedClone.Process(price, history, priceHistory);
}
Check(history.Items[0]!.Studies.Keys.SequenceEqual(new[] { "MAAudit" }), "Selected shared-study outputs survive cloning");
extendedClone.label = "Changed";
priceHistory.Add(prices[10]);
history.AddToFront(new BarStudy(prices[10]));
extendedClone.Process(prices[10], history, priceHistory);
Check(history.Items[0]!.Studies.Keys.SequenceEqual(new[] { "MAChanged" }), "Publication refreshes after mutable label change");
Check(plain.GetProfilingSnapshot().Count == 0 && plain.ProfiledBars == 0, "Disabled profiling records nothing");
var rows = measured.GetProfilingSnapshot();
Check(rows.All(r => r.Study != "StudyCrossMAMA"), "Unused StudyCrossMAMA is not instantiated");
Check(rows.All(r => r.Study != "Study_BaseL1"), "Unused Study_BaseL1 is not instantiated");
Check(rows.Count > 100 && rows.All(r => r.Calls == 450), "Every configured study is measured once per bar");
Check(rows.All(r => r.TotalMs >= r.MaxMs && r.AverageMs >= 0 && r.AllocatedBytes >= 0), "Valid timing and allocation metrics");
Check(rows.Zip(rows.Skip(1)).All(p => p.First.TotalMs >= p.Second.TotalMs), "Report sorted by total cost");
Check(logger.Summaries == 4, "Periodic summaries emitted at configured interval");
measured.LogProfilingSummary();
Check(logger.Summaries == 5, "Final partial interval can be reported");
Check(Json(plain.CaptureCheckpoint()) == Json(measured.CaptureCheckpoint()), "Profiling preserves checkpoint state and identities");
Check(Json(plain.Calc(prices.TakeLast(3).ToList(), 3)) == Json(measured.Calc(prices.TakeLast(3).ToList(), 3)), "Correction replay outputs unchanged");
Check(measured.GetProfilingSnapshot().All(r => r.Calls == 453), "Correction replay included in counts");
for (int pass = 0; pass < 3; pass++)
    Check(Json(plain.Calc(prices.TakeLast(10).ToList(), 10)) == Json(measured.Calc(prices.TakeLast(10).ToList(), 10)), "Full history rollback/replay unchanged");
var restored = Create(false);
var checkpoint = measured.CaptureCheckpoint();
var window = restored.GetRequiredCheckpointPrices(checkpoint)!;
restored.RestoreCheckpoint(checkpoint, prices.Where(p => p.RawTime >= window.FirstRawTime && p.RawTime <= window.LastRawTime).ToList());
var next = new PriceDataRS(prices[^1]) { RawTime = prices[^1].RawTime + 60 };
Check(Json(plain.Calc([next], 0)) == Json(restored.Calc([next], 0)), "Parameter reuse survives checkpoint restore");
var buffer = new FIGCommon.Utilities.CircularBuffer<int>(3);
var view = buffer.Items; buffer.AddToFront(7);
Check(ReferenceEquals(view, buffer.Items) && view[0] == 7, "Cached buffer view stays live without wrapper allocations");
var copiedBuffer = new FIGCommon.Utilities.CircularBuffer<int>(buffer); buffer.AddToFront(8);
Check(copiedBuffer.Items[0] == 7 && !ReferenceEquals(view, copiedBuffer.Items), "Copied buffer owns an independent view");
foreach (var row in rows.Take(10)) Console.WriteLine($"{row.Study}: {row.TotalMs:F2} ms, {row.AllocatedBytes:N0} bytes");

sealed class CaptureLogger : ILoggerProvider, ILogger
{
    public int Summaries;
    public ILogger CreateLogger(string categoryName) => this;
    public void Dispose() { }
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (formatter(state, exception).Contains("Cumulative elapsed time")) Summaries++;
    }
}
