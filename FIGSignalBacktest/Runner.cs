using System.Diagnostics;

namespace FIGSignalBacktest;

public static class Runner
{
    public static void Batch(Options o, CancellationToken cancellation = default)
    {
        o.Validate(true); var plugin = new Plugins(o.Plugin);
        var paths = Directory.GetFiles(o.ParametersDirectory, "*.json").Order(StringComparer.Ordinal).ToArray();
        if (paths.Length == 0) throw new ArgumentException("No JSON parameter files in parameters directory");
        var candidates = paths.Select((p, i) => new Candidate($"run-{i + 1:D4}", File.ReadAllText(p), p)).ToArray();
        foreach (var c in candidates) { using var validate = plugin.Create(o.Collection, c.Json, o.Interval); }
        if (string.IsNullOrWhiteSpace(o.OutputDirectory) || (!o.FixedOutputDirectory && Directory.Exists(o.OutputDirectory) && Directory.EnumerateFileSystemEntries(o.OutputDirectory).Any())) throw new ArgumentException("Choose a new or empty backtest output directory");
        var input = Prices.Load(o); if (input.SourceCount == 0) throw new InvalidDataException("No PriceData rows for dataset " + o.Dataset);
        var bars = input.Bars; int first = bars.FindIndex(p => p.Time >= Format.Date(o.Start)); if (first < 0) throw new InvalidDataException("No prices in selected backtest dates");
        foreach (var p in bars) p.Validate();
        cancellation.ThrowIfCancellationRequested();
        if (o.FixedOutputDirectory) FIG.IO.FixedOutputDirectory.Clear(o.OutputDirectory, o.ParametersDirectory, o.Plugin, o.CacheDirectory, o.PricesCsv);
        Directory.CreateDirectory(o.OutputDirectory); Reports.Inputs(o, plugin, input, candidates, first);
        Console.WriteLine($"{bars.Count} aggregated bars; {candidates.Length} candidates; {o.Jobs} workers.");
        var results = new Result[candidates.Length]; int done = 0;
        Parallel.For(0, candidates.Length, new ParallelOptions { MaxDegreeOfParallelism = o.Jobs, CancellationToken = cancellation }, (index, state) =>
        {
            try
            {
                var runTimer = Stopwatch.StartNew();
                var c = candidates[index]; using var study = plugin.Create(o.Collection, c.Json, o.Interval); var ledger = new Ledger(o);
                bool signalSeen = false; int signalWarmupBars = 0;
                using var chart = new FigDataWriter(Path.Combine(o.OutputDirectory, c.Id + "-chart.figdata"), bars.Count - first, o.Interval * 60, o.Collection);
                for (int n = 0; n < bars.Count; n++)
                {
                    cancellation.ThrowIfCancellationRequested(); if (state.IsExceptional) return;
                    var row = study.Process(bars[n]);
                    bool hasSignal = row.Numbers.TryGetValue(o.Signal, out var signal) && signal.HasValue;
                    if (hasSignal) signalSeen = true;
                    if (n < first) continue;
                    chart.Write(row);
                    if (!hasSignal)
                    {
                        if (signalSeen || row.Text.ContainsKey(o.Signal)) throw new InvalidDataException("Selected signal is missing or nonnumeric after initialization: " + o.Signal);
                        signal = 0; signalWarmupBars++;
                    }
                    ledger.Process(bars[n], signal, n + 1 == bars.Count);
                }
                if (!signalSeen) throw new InvalidDataException("Collection never provided selected signal: " + o.Signal);
                if (signalWarmupBars > 0) Console.WriteLine($"{c.Id}: stayed flat for {signalWarmupBars} initial bars before {o.Signal} became available.");
                chart.Finish(); results[index] = new(c, ledger); Reports.Save(results[index], o.OutputDirectory);
                var profilePath = Path.Combine(o.OutputDirectory, c.Id + "-profiling.csv");
                if (study.SaveProfiling(profilePath)) Console.WriteLine($"Profiling saved: {profilePath}");
                runTimer.Stop();
                Console.WriteLine($"Completed {Interlocked.Increment(ref done)}/{candidates.Length}: {c.Id} NAV={Format.N(100 * ledger.Equity / o.Capital)} Duration={runTimer.Elapsed.TotalSeconds:F3}s");
            }
            catch (Exception ex) { throw new InvalidOperationException($"Candidate {candidates[index].Id}: {ex.Message}", ex); }
        });
        Reports.Summary(results, o.OutputDirectory); Console.WriteLine("Batch complete. Reports: " + o.OutputDirectory);
    }
    public static void Studies(Options o)
    {
        o.Validate(false); if (string.IsNullOrWhiteSpace(o.Output)) throw new ArgumentException("--output is required");
        if (!o.CollectionSpecified && o.Parameters != "") throw new ArgumentException("--parameters requires --collection");
        using var study = o.CollectionSpecified ? new Plugins(o.Plugin).Create(o.Collection, o.Parameters == "" ? "{}" : File.ReadAllText(o.Parameters), o.Interval) : null;
        var diagnostics = study == null ? new Diagnostics(o) : null;
        var input = Prices.Load(o, true); if (input.Bars.Count == 0) throw new InvalidDataException("No prices found for dataset");
        using var writer = Format.Writer(o.Output); string[]? numbers = null, text = null;
        foreach (var p in input.Bars)
        {
            var row = study != null ? study.Process(p) : diagnostics!.Process(p);
            if (numbers == null) { numbers = row.Numbers.Keys.ToArray(); text = row.Text.Keys.ToArray(); writer.WriteLine(Reports.PriceHeader + "," + string.Join(',', numbers.Concat(text).Select(Format.Quote))); }
            if (!numbers.SequenceEqual(row.Numbers.Keys) || !text!.SequenceEqual(row.Text.Keys)) throw new InvalidDataException("Study schema changed");
            Format.Row(writer, Reports.PriceValues(p).Cast<object?>().Concat(numbers.Select(k => (object?)row.Numbers[k])).Concat(text!.Select(k => (object?)Format.Quote(row.Text[k]))).ToArray());
        }
        Console.WriteLine($"Wrote {input.Bars.Count} study bars to {o.Output}");
    }
}

