using System.Text.Json;
using FIG.StudyCollections.LADX;
using FIG.Studies;
using FIGCommon.Models;
using Microsoft.Extensions.Logging.Abstractions;

var json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
if (args.Length == 3)
{
    var options = LADXOptions.FromJson(JsonDocument.Parse(File.ReadAllText(args[0])).RootElement);
    var inputBars = JsonSerializer.Deserialize<List<PriceDataRS>>(File.ReadAllText(args[1]), json)!;
    var engine = new LADXEngine(options);
    File.WriteAllText(args[2], JsonSerializer.Serialize(inputBars.Select(engine.Process)));
    return;
}
int checks = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; }
LADXOptions Parse(string text) => LADXOptions.FromJson(JsonDocument.Parse(text).RootElement);
void Invalid(string text) { try { Parse(text); } catch (ArgumentException) { checks++; return; } throw new Exception("Accepted " + text); }
Check(Parse("{}") == new LADXOptions(), "RUN0193 defaults");
Check(Parse("{\"StartBias\":2,\"StopBias\":-2}") is { StartBiasLongTrend: 2, StartBiasShortTrend: 2, StopBiasLongTrend: -2, StopBiasShortTrend: -2 }, "Legacy bias expands");
Check(Parse("{\"indicators\":{\"priceMaLength\":8},\"exit\":{\"exitOnGuideCrossShortTrend\":true}}") is { FastMALen: 8, ExitOnGuideCrossShortTrend: true }, "Grouped parameters");
foreach (var bad in new[] { "{\"StartBias\":1,\"entry\":{\"startBiasLongTrend\":2}}", "{\"indicators\":{\"priceMaLength\":2,\"fastMaLength\":2}}", "{\"ReEntryCooldownBars\":1.5}", "{\"AdxLen\":null}", "{\"exit\":{\"exitOnGuideCrossShortTrend\":1}}", "{\"marketTrendSettings\":{\"timeframe\":\"weekly\"}}", "{\"marketTrendSettings\":{\"timeZone\":\"Europe/London\"}}", "{\"marketTrendSettings\":{\"smaLength\":1,\"smaLength\":2}}", "{\"unknown\":1}" }) Invalid(bad);
var settings = new MarketTrendSettings { SmaLength = 3, SlopeLookback = 2, DmiLength = 2, AdxLength = 2, EmaLength = 3, AtrLength = 3 };
StudyColLADX Create(LADXOptions o) => new(new StudyCollectionContext(new StudyColRS { Id = 1, ColType = "LADX", ColParams = JsonSerializer.Serialize(o) }, new TickerRS(), new IntervalRS { IntervalLen = 300 }, NullLoggerFactory.Instance));
Check(Create(new LADXOptions()).GetMaxLen() == 1572, "Host requests hourly trend warmup in five-minute bars");
Check(Create(new LADXOptions { MarketTrendSettings = new MarketTrendSettings { Timeframe = "daily" } }).GetMaxLen() == 37728, "Host requests daily trend warmup" );
string Serialize<T>(T value) => JsonSerializer.Serialize(value, StudyStateJson.Options);
var bars = Enumerable.Range(0, 1800).Select(n => { decimal c = 100 + (decimal)(30 * Math.Sin(n * .024) + 4 * Math.Sin(n * .27)); return new PriceDataRS { RawTime = 1710000000 + n * 300L, Open = c, High = c + 2, Low = c - 3, Close = c }; }).ToList();
foreach (var timeframe in new[] { "hourly", "daily" })
{
    var o = new LADXOptions { AdxLen = 3, MarketTrendSettings = settings with { Timeframe = timeframe }, ExitOnGuideCrossShortTrend = true };
    var original = Create(o); var all = original.Calc(bars, 0);
    foreach (var split in new[] { 1, 22, 300, 1000, 1600 })
    {
        var left = Create(o); left.Calc(bars.Take(split).ToList(), 0);
        var saved = JsonSerializer.Deserialize<CollectionCheckpoint>(Serialize(left.CaptureCheckpoint()), StudyStateJson.Options)!;
        var right = Create(o); right.RestoreCheckpoint(saved);
        Check(Serialize(left.Calc(bars.Skip(split).ToList(), 0)) == Serialize(right.Calc(bars.Skip(split).ToList(), 0)), "Restart " + timeframe);
        Check(Serialize(left.CaptureCheckpoint()) == Serialize(right.CaptureCheckpoint()), "State " + timeframe);
    }
    var revised = bars.TakeLast(20).Select(b => new PriceDataRS(b) { Close = b.Close + 1 }).ToList();
    var correction = original.Calc(revised, 20);
    var full = Create(o).Calc(bars.SkipLast(20).Concat(revised).ToList(), 0).TakeLast(20).ToList();
    Check(Serialize(correction) == Serialize(full), "Correction " + timeframe);
    var repeat = original.Calc([revised[^1]], 1);
    Check(Serialize(repeat[0].Studies) == Serialize(correction[^1].Studies), "Live revision " + timeframe);
}
Console.WriteLine($"All {checks} parameter, restart and revision checks passed.");


