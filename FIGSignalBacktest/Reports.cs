namespace FIGSignalBacktest;

public sealed record Candidate(string Id, string Json, string File);
public sealed record Result(Candidate Candidate, Ledger Ledger);
public static class Reports
{
    public const string PriceHeader = "Id,DataSetId,RawTime,Open,High,Low,Close,Volume";
    public static object[] PriceValues(Price p) => [p.Id, p.DataSetId, p.Time, p.Open, p.High, p.Low, p.Close, p.Volume];
    public static void Save(Result result, string directory)
    {
        var c = result.Candidate; var l = result.Ledger; var o = l.Options;
        using (var w = Format.Writer(Path.Combine(directory, c.Id + "-monthly.csv")))
        {
            w.WriteLine("MonthUTC,LastPriceRawTime,Bars,StartEquity,EndEquity,StartNAV,EndNAV,MonthlyNetProfit,MonthlyReturnPct,RealizedGrossProfit,UnrealizedProfit,Commissions,ClosedTrades,OpenContracts,DrawdownPctFromRunPeak,DrawdownNAVPoints");
            foreach (var m in l.Months) Format.Row(w, $"{m.Id / 12:D4}-{m.Id % 12 + 1:D2}", m.LastTime, m.Bars, m.Start, m.Equity, 100 * m.Start / o.Capital, 100 * m.Equity / o.Capital, m.Equity - m.Start, m.Start > 0 ? 100 * (m.Equity / m.Start - 1) : null, m.Realized, m.Unrealized, m.Fees, m.Trades, m.Position, m.Drawdown, m.DrawdownPoints);
        }
        using (var w = Format.Writer(Path.Combine(directory, c.Id + "-trades.csv")))
        {
            w.WriteLine("EntryRawTime,ExitRawTime,EntryPrice,ExitPrice,Contracts,Multiplier,GrossProfit,Commissions,NetProfit,ExitReason,Side");
            foreach (var t in l.Trades) Format.Row(w, t.EntryTime, t.ExitTime, t.Entry, t.Exit, t.Quantity, o.Multiplier, t.Gross, t.Fees, t.Gross - t.Fees, t.Reason, t.EntrySignal.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
        }
        File.WriteAllText(Path.Combine(directory, c.Id + "-parameters.json"), c.Json + "\n");
    }
    public static void Summary(IEnumerable<Result> results, string directory)
    {
        using var w = Format.Writer(Path.Combine(directory, "results.csv"));
        w.Write("TrainingProfitRank,RunId,NetProfit,FinalNAV,RealizedGrossProfit,UnrealizedProfit,Commissions,MaxDrawdownPct,MaxDrawdownNAVPoints,ClosedTrades,WinRatePct,ProfitFactor,OpenContracts");
        foreach (var name in new[] { "Train", "Validation", "Test" }) w.Write($",{name}Bars,{name}NetProfit,{name}ReturnPct,{name}MaxDrawdownPct,{name}ClosedTrades");
        w.WriteLine(",ParametersJSON"); int rank = 0;
        foreach (var r in results.OrderByDescending(r => r.Ledger.Segments[0].End - r.Ledger.Segments[0].Start))
        {
            var l = r.Ledger; var o = l.Options;
            double wins = 0, losses = 0; int winning = 0;
            foreach (var t in l.Trades) { var net = t.Gross - t.Fees; if (net > 0) { wins += net; winning++; } else losses -= net; }
            var fields = new List<object?> { ++rank, r.Candidate.Id, l.Equity - o.Capital, 100 * l.Equity / o.Capital, l.Realized, l.Unrealized, l.Fees, l.MaxDrawdown, l.MaxDrawdownPoints, l.Trades.Count, l.Trades.Count > 0 ? 100.0 * winning / l.Trades.Count : 0, losses > 0 ? wins / losses : null, l.Open ? l.Side * l.Quantity : 0 };
            foreach (var s in l.Segments) fields.AddRange([s.Bars, s.Bars > 0 ? s.End - s.Start : null, s.Bars > 0 && s.Start > 0 ? 100 * (s.End / s.Start - 1) : null, s.Bars > 0 ? s.Drawdown : null, s.Trades]);
            fields.Add(Format.Quote(r.Candidate.Json.Replace("\r", "").Replace("\n", ""))); Format.Row(w, fields.ToArray());
        }
    }
    public static void Inputs(Options o, Plugins plugins, PriceInput input, IReadOnlyList<Candidate> candidates, int first)
    {
        using (var w = Format.Writer(Path.Combine(o.OutputDirectory, "batch-settings.txt")))
        {
            void Set(string key, object value) => w.WriteLine(key + "=" + Format.N(value));
            Set("PriceSource", string.IsNullOrWhiteSpace(o.PricesCsv) ? "database" : "csv");
            if (!string.IsNullOrWhiteSpace(o.PricesCsv)) Set("PricesCsv", Path.GetFullPath(o.PricesCsv));
            Set("Collection", o.Collection); Set("CollectionDLL", plugins.PathName); Set("Signal", o.Signal); Set("Dataset", input.Bars[first].DataSetId); Set("IntervalMinutes", o.Interval);
            Set("InitialCapital", o.Capital); Set("Contracts", o.Quantity); Set("Multiplier", o.Multiplier); Set("SizingMode", o.Quantity == 0 ? "efs-aum-hlc3" : "fixed"); Set("QuantityPct", o.QuantityPct); Set("ReinvestAUM", 0);
            Set("CommissionPerContractPerSide", o.Commission); Set("SlippagePointsPerSide", o.Slippage); Set("ForceClose", o.ForceClose);
            Set("StartUTC", o.Start); Set("EndExclusiveUTC", o.End); Set("TrainEndExclusiveUTC", o.TrainEnd); Set("ValidationEndExclusiveUTC", o.ValidationEnd);
            Set("SourceRowsRead", input.SourceCount); Set("AggregatedBars", input.Bars.Count); Set("PriceCache", input.Status); Set("PriceCacheFile", input.CacheFile);
            Set("DatabaseRowsReadThisRun", input.Status is "off" or "database-snapshot" ? input.SourceCount : 0UL);
            Set("FirstRawTime", input.Bars[first].Time); Set("LastRawTime", input.Bars[^1].Time); Set("Candidates", candidates.Count);
            w.Write("Fill=signal bar close plus/minus configured slippage\nMonths=UTC\nRanking=training net profit only\nSegments=continuous positions/equity, marked at boundaries; not separate restarted strategies\nPartial first and final buckets included; prices fetched from live database are not a transaction snapshot\n");
        }
        using (var w = Format.Writer(Path.Combine(o.OutputDirectory, "prices.csv"))) { w.WriteLine(PriceHeader); foreach (var p in input.Bars) Format.Row(w, PriceValues(p)); }
        using (var w = Format.Writer(Path.Combine(o.OutputDirectory, "candidate-files.csv"))) { w.WriteLine("RunId,InputParameterFile"); foreach (var c in candidates) Format.Row(w, c.Id, Format.Quote(c.File)); }
    }
}
