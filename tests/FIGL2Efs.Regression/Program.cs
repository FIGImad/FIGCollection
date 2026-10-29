using System.Text.Json;
using FIG.Studies;
using FIGCommon.Models;
using FIGCommon.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;

if (args[0] == "--base-signal")
{
    BaseSignalRegression.Run(args[1]);
    return;
}

if (args[0] == "--checkpoint")
{
    StudyColL2 Create() => new(new StudyCollectionContext(new StudyColRS { Id=1, ColType="L2",
        ColParams="{\"MinPeriod\":3,\"MaxPeriod\":63,\"PeriodInc\":3,\"StdDev1\":1,\"StdDev2\":2}" },
        new TickerRS(),new IntervalRS {IntervalLen=300},NullLoggerFactory.Instance));
    string Json<T>(T value) => JsonSerializer.Serialize(value,StudyStateJson.Options);
    var prices = Enumerable.Range(0,450).Select(i => {
        decimal close=100+(decimal)Math.Sin(i*.039)*35+(decimal)Math.Sin(i*.41)*3;
        return new PriceDataRS {RawTime=1700000040+i*300L,Open=close,High=close+2,Low=close-3,Close=close,Volume=1000+i};
    }).ToList();
    var uninterrupted=Create(); uninterrupted.Calc(prices.Take(300).ToList(),0);
    var checkpoint=JsonSerializer.Deserialize<CollectionCheckpoint>(Json(uninterrupted.CaptureCheckpoint()),StudyStateJson.Options)!;
    var restored=Create(); var window=restored.GetRequiredCheckpointPrices(checkpoint)!;
    restored.RestoreCheckpoint(checkpoint,prices.Where(p=>p.RawTime>=window.FirstRawTime && p.RawTime<=window.LastRawTime).ToList());
    if (Json(uninterrupted.Calc(prices.Skip(300).ToList(),0)) != Json(restored.Calc(prices.Skip(300).ToList(),0))) throw new Exception("Checkpoint continuation mismatch");
    if (Json(uninterrupted.CaptureCheckpoint()) != Json(restored.CaptureCheckpoint())) throw new Exception("Checkpoint state mismatch");
    var revised=prices.TakeLast(3).Select(p=>new PriceDataRS(p) {Close=p.Close+1}).ToList();
    var corrected=restored.Calc(revised,3);
    var fresh=Create().Calc(prices.SkipLast(3).Concat(revised).ToList(),0).TakeLast(3).ToList();
    if (Json(corrected)!=Json(fresh)) throw new Exception("Correction replay mismatch");
    Console.WriteLine("Checkpoint JSON roundtrip, continuation, state and correction replay passed.");
    return;
}

if (args[0] == "--replay")
{
    var col = new StudyColL2(new StudyCollectionContext(new StudyColRS { Id = 1, ColType = "L2",
        ColParams = "{\"MinPeriod\":276,\"MaxPeriod\":5796,\"PeriodInc\":276,\"StdDev1\":1,\"StdDev2\":2}" },
        new TickerRS(), new IntervalRS { IntervalLen = 300 }, NullLoggerFactory.Instance));
    using var output = new StreamWriter(args[2]);
    output.WriteLine("EntryRawTime,ExitRawTime,EntryPrice,ExitPrice,Side");
    decimal signal = 0, entry = 0; long entryTime = 0; int count = 0;
    var end = args.Length > 3 ? long.Parse(args[3]) : long.MaxValue;
    foreach (var line in File.ReadLines(args[1]).Skip(1))
    {
        var fields = line.Split(',');
        long time = long.Parse(fields[2]); if (time >= end) break;
        decimal V(int n) => decimal.Parse(fields[n], CultureInfo.InvariantCulture);
        var bar = col.Calc([new PriceDataRS { RawTime=time, Open=V(3), High=V(4), Low=V(5), Close=V(6), Volume=checked((int)V(7)) }],0)[0];
        decimal current = (decimal?)bar["L2_SIDE"] ?? 0;
        if (Math.Sign(current) != Math.Sign(signal))
        {
            if (signal != 0) output.WriteLine(FormattableString.Invariant($"{entryTime},{time},{entry},{bar.Price.Close},{signal:F4}"));
            signal = current; entry = bar.Price.Close; entryTime = time;
        }
        if (++count % 100000 == 0) Console.WriteLine($"Replayed {count} bars");
    }
    Console.WriteLine($"Replay complete: {count} bars");
    return;
}

var cases = JsonSerializer.Deserialize<List<Fixture>>(File.ReadAllText(args[0]), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
var results = cases.Select(f => new Probe().Run(f)).ToArray();
File.WriteAllText(args[1], JsonSerializer.Serialize(results));
record Fixture(string Method, decimal Trigger, decimal Basic, decimal Side, decimal Price, int Profile);
sealed class Probe : Study_L2
{
    public object Run(Fixture f)
    {
        var prior = new Study_L2Params { trigger_DL1 = f.Trigger, basicTrigger_DL1 = f.Basic, Side = f.Side };
        var current = new Study_L2Params(prior);
        paramHist.AddToFront(prior); paramHist.AddToFront(current);
        var bars = new CircularBuffer<BarStudy>(10);
        decimal Value(string cycle, string field)
        {
            if (f.Profile == 0) return 0;
            int hash = 0; foreach (char c in cycle + field) hash = (hash * 31 + c) % 997;
            return 50 + ((hash + f.Profile * 73) % 400) / 4m;
        }
        for (int i = 0; i < 3; i++)
        {
            var bar = new BarStudy(new PriceDataRS { RawTime = 1000 + i * 300, Close = f.Price, High = f.Price, Low = f.Price, Open = f.Price });
            foreach (string cycle in Enumerable.Range(1, 21).Select(n => "C" + n).Concat(new[] { "C34", "C55", "C89", "C110", "C165", "C276", "L1", "L2", "L3", "L4", "LL1", "LL2", "LL3", "H1", "H2" }))
            {
                bar[StudyMA.Label(cycle)] = Value(cycle, "MA");
                bar[StudySEM.Label(cycle)] = Value(cycle, "SEM");
                foreach (var band in new[] { "UB", "LB" }) foreach (var sd in new[] { "1", "2" })
                    bar[StudyBB.Label(cycle, sd, band)] = Value(cycle, band + sd);
            }
            bars.AddToFront(bar);
        }
        decimal result = 0;
        if (f.Method == "ResetSwitches") ResetSwitches(bars);
        else if (f.Method == "TestStart") result = CheckSignalStart(bars);
        else result = CheckSignalStop(bars) ? 1 : 0;
        return new { result, basic = current.basicTrigger_DL1, trigger = current.trigger_DL1, firsttime = current.firsttime, side = current.Side };
    }
}

