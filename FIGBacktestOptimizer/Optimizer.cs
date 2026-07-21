using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace FIGBacktestOptimizer;

public sealed class Optimizer
{
    private readonly OptimizerConfig config;
    private readonly List<Bar> bars;

    public Optimizer(OptimizerConfig config, List<Bar> bars)
    {
        this.config = config;
        this.bars = bars;
    }

    public List<OptimizationResult> Run()
    {
        var filteredBars = FilterBars(bars, config.Backtest.StartDate, config.Backtest.EndDate);
        if (filteredBars.Count < 500)
        {
            throw new InvalidOperationException("Not enough bars after date filtering. Need at least 500 bars for useful optimization.");
        }

        var (train, validation, test) = SplitBars(filteredBars);
        var candidates = ParameterGenerator.Generate(config.BaseParameters, config.ParameterRanges, config.Search).ToList();
        Console.WriteLine($"Bars: all={filteredBars.Count:n0}, train={train.Count:n0}, validation={validation.Count:n0}, test={test.Count:n0}");
        Console.WriteLine($"Evaluating {candidates.Count:n0} parameter sets...");

        var results = new ConcurrentBag<OptimizationResult>();
        var evaluation = 0;
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = config.Search.Parallelism > 0
                ? config.Search.Parallelism
                : Environment.ProcessorCount
        };

        Parallel.ForEach(candidates, options, parameters =>
        {
            var current = Interlocked.Increment(ref evaluation);
            var trainResult = RunSegment(train, parameters, "train");
            var validationResult = RunSegment(validation, parameters, "validation");
            var testResult = RunSegment(test, parameters, "test");
            var walkForward = config.Splits.UseWalkForward
                ? RunWalkForward(filteredBars, parameters)
                : new BacktestMetrics { Segment = "walk-forward", Score = 0 };

            var composite = (validationResult.Score * config.Objective.ValidationWeight)
                + (trainResult.Score * config.Objective.TrainWeight)
                + (walkForward.Score * config.Objective.WalkForwardWeight);

            results.Add(new OptimizationResult
            {
                Evaluation = current,
                CompositeScore = composite,
                Train = trainResult,
                Validation = validationResult,
                Test = testResult,
                WalkForward = walkForward,
                Parameters = parameters
            });

            if (current % 100 == 0 || current == candidates.Count)
            {
                Console.WriteLine($"  completed {current:n0}/{candidates.Count:n0}");
            }
        });

        return results
            .OrderByDescending(result => result.CompositeScore)
            .ThenBy(result => Math.Abs(result.Validation.MaxDrawdownPct))
            .ThenByDescending(result => result.Validation.NetProfitPct)
            .ToList();
    }

    public void WriteOutputs(IReadOnlyList<OptimizationResult> results)
    {
        Directory.CreateDirectory(config.OutputDirectory);
        var top = results.Take(Math.Max(1, config.Search.TopResults)).ToList();

        File.WriteAllText(
            Path.Combine(config.OutputDirectory, "optimizer-results.csv"),
            BuildResultsCsv(results),
            Encoding.UTF8);

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(
            Path.Combine(config.OutputDirectory, "top-parameters.json"),
            JsonSerializer.Serialize(top, jsonOptions),
            Encoding.UTF8);

        if (top.Count > 0)
        {
            var allBars = FilterBars(bars, config.Backtest.StartDate, config.Backtest.EndDate);
            var bestRun = new AdxBaseBacktester(allBars, config.Backtest, config.Objective).Run(top[0].Parameters, "all");
            File.WriteAllText(
                Path.Combine(config.OutputDirectory, "best-trades.csv"),
                BuildTradesCsv(bestRun.Trades),
                Encoding.UTF8);

            File.WriteAllText(
                Path.Combine(config.OutputDirectory, "summary.txt"),
                BuildSummary(top[0], bestRun.Metrics),
                Encoding.UTF8);
        }
    }

    private BacktestMetrics RunSegment(IReadOnlyList<Bar> segmentBars, AdxBaseParameters parameters, string name)
    {
        if (segmentBars.Count == 0)
        {
            return new BacktestMetrics { Segment = name };
        }

        return new AdxBaseBacktester(segmentBars, config.Backtest, config.Objective)
            .Run(parameters, name)
            .Metrics;
    }

    private BacktestMetrics RunWalkForward(IReadOnlyList<Bar> sourceBars, AdxBaseParameters parameters)
    {
        var segments = WalkForwardSegments(sourceBars).ToList();
        if (segments.Count == 0)
        {
            return new BacktestMetrics { Segment = "walk-forward" };
        }

        var metrics = segments
            .Select((segment, index) => RunSegment(segment, parameters, $"wf-{index + 1}"))
            .ToList();

        return new BacktestMetrics
        {
            Segment = "walk-forward",
            NetProfit = metrics.Sum(metric => metric.NetProfit),
            NetProfitPct = metrics.Average(metric => metric.NetProfitPct),
            MaxDrawdownPts = metrics.Min(metric => metric.MaxDrawdownPts),
            MaxDrawdownPct = metrics.Min(metric => metric.MaxDrawdownPct),
            Trades = metrics.Sum(metric => metric.Trades),
            WinRatePct = metrics.Average(metric => metric.WinRatePct),
            ProfitFactor = metrics.Average(metric => metric.ProfitFactor),
            AverageTrade = metrics.Average(metric => metric.AverageTrade),
            Score = metrics.Average(metric => metric.Score)
        };
    }

    private IEnumerable<IReadOnlyList<Bar>> WalkForwardSegments(IReadOnlyList<Bar> sourceBars)
    {
        if (sourceBars.Count == 0)
        {
            yield break;
        }

        var trainMonths = Math.Max(1, config.Splits.WalkForwardTrainMonths);
        var testMonths = Math.Max(1, config.Splits.WalkForwardTestMonths);
        var cursor = sourceBars[0].Time.Date;
        var lastTime = sourceBars[^1].Time;

        while (true)
        {
            var testStart = cursor.AddMonths(trainMonths);
            var testEnd = testStart.AddMonths(testMonths);
            if (testStart >= lastTime)
            {
                yield break;
            }

            var segment = sourceBars
                .Where(bar => bar.Time >= testStart && bar.Time < testEnd)
                .ToList();

            if (segment.Count > 100)
            {
                yield return segment;
            }

            cursor = cursor.AddMonths(testMonths);
        }
    }

    private (List<Bar> Train, List<Bar> Validation, List<Bar> Test) SplitBars(IReadOnlyList<Bar> sourceBars)
    {
        var trainRatio = Math.Clamp(config.Splits.TrainRatio, 0.05, 0.85);
        var validationMax = Math.Max(0.05, 0.95 - trainRatio);
        var validationRatio = Math.Clamp(config.Splits.ValidationRatio, 0.05, validationMax);
        var trainCount = (int)Math.Floor(sourceBars.Count * trainRatio);
        var validationCount = (int)Math.Floor(sourceBars.Count * validationRatio);
        var testCount = sourceBars.Count - trainCount - validationCount;

        if (testCount <= 0)
        {
            throw new InvalidOperationException("Split ratios leave no test data.");
        }

        return (
            sourceBars.Take(trainCount).ToList(),
            sourceBars.Skip(trainCount).Take(validationCount).ToList(),
            sourceBars.Skip(trainCount + validationCount).ToList());
    }

    private static List<Bar> FilterBars(IEnumerable<Bar> sourceBars, DateTime? start, DateTime? end)
    {
        return sourceBars
            .Where(bar => start is null || bar.Time >= start.Value)
            .Where(bar => end is null || bar.Time <= end.Value)
            .OrderBy(bar => bar.Time)
            .ToList();
    }

    private static string BuildResultsCsv(IEnumerable<OptimizationResult> results)
    {
        var builder = new StringBuilder();
        builder.Append("Evaluation,CompositeScore,");
        builder.Append(MetricHeader("Train"));
        builder.Append(',');
        builder.Append(MetricHeader("Validation"));
        builder.Append(',');
        builder.Append(MetricHeader("Test"));
        builder.Append(',');
        builder.Append(MetricHeader("WalkForward"));
        builder.Append(',');
        builder.AppendLine(ParameterReflection.ToCsvHeader());

        foreach (var result in results)
        {
            builder.Append(Invariant($"{result.Evaluation},{result.CompositeScore},"));
            builder.Append(MetricRow(result.Train));
            builder.Append(',');
            builder.Append(MetricRow(result.Validation));
            builder.Append(',');
            builder.Append(MetricRow(result.Test));
            builder.Append(',');
            builder.Append(MetricRow(result.WalkForward));
            builder.Append(',');
            builder.AppendLine(ParameterReflection.ToCsvRow(result.Parameters));
        }

        return builder.ToString();
    }

    private static string BuildTradesCsv(IEnumerable<Trade> trades)
    {
        var builder = new StringBuilder();
        builder.AppendLine("OpenTime,CloseTime,OpenPrice,ClosePrice,Quantity,BarsHeld,Profit,EntryMode,ExitReason");
        foreach (var trade in trades)
        {
            builder.AppendLine(Invariant($"{trade.OpenTime:O},{trade.CloseTime:O},{trade.OpenPrice},{trade.ClosePrice},{trade.Quantity},{trade.BarsHeld},{trade.Profit},{trade.EntryMode},{trade.ExitReason}"));
        }

        return builder.ToString();
    }

    private static string BuildSummary(OptimizationResult best, BacktestMetrics allData)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Best ADXBase parameter set");
        builder.AppendLine("==========================");
        builder.AppendLine(Invariant($"CompositeScore: {best.CompositeScore}"));
        builder.AppendLine(Invariant($"Validation NetProfitPct: {best.Validation.NetProfitPct}"));
        builder.AppendLine(Invariant($"Validation MaxDrawdownPct: {best.Validation.MaxDrawdownPct}"));
        builder.AppendLine(Invariant($"Validation Trades: {best.Validation.Trades}"));
        builder.AppendLine();
        builder.AppendLine("All-data rerun with best parameters");
        builder.AppendLine(Invariant($"NetProfit: {allData.NetProfit}"));
        builder.AppendLine(Invariant($"NetProfitPct: {allData.NetProfitPct}"));
        builder.AppendLine(Invariant($"MaxDrawdownPct: {allData.MaxDrawdownPct}"));
        builder.AppendLine(Invariant($"Trades: {allData.Trades}"));
        builder.AppendLine(Invariant($"ProfitFactor: {allData.ProfitFactor}"));
        builder.AppendLine();
        builder.AppendLine("Parameters");
        foreach (var pair in ParameterReflection.GetParameterValues(best.Parameters).OrderBy(pair => pair.Key))
        {
            builder.AppendLine(Invariant($"{pair.Key}: {pair.Value}"));
        }

        return builder.ToString();
    }

    private static string MetricHeader(string prefix)
    {
        return string.Join(',',
            $"{prefix}_NetProfit",
            $"{prefix}_NetProfitPct",
            $"{prefix}_MaxDrawdownPts",
            $"{prefix}_MaxDrawdownPct",
            $"{prefix}_Trades",
            $"{prefix}_WinRatePct",
            $"{prefix}_ProfitFactor",
            $"{prefix}_AverageTrade",
            $"{prefix}_Score");
    }

    private static string MetricRow(BacktestMetrics metric)
    {
        return Invariant($"{metric.NetProfit},{metric.NetProfitPct},{metric.MaxDrawdownPts},{metric.MaxDrawdownPct},{metric.Trades},{metric.WinRatePct},{metric.ProfitFactor},{metric.AverageTrade},{metric.Score}");
    }

    private static string Invariant(FormattableString text)
    {
        return text.ToString(CultureInfo.InvariantCulture);
    }
}

public static class ParameterGenerator
{
    public static IEnumerable<AdxBaseParameters> Generate(AdxBaseParameters baseParameters, Dictionary<string, ParameterRange> ranges, SearchSettings settings)
    {
        var options = ranges
            .Select(pair => (Name: pair.Key, Values: pair.Value.Expand()))
            .Where(pair => pair.Values.Count > 0)
            .OrderBy(pair => pair.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (settings.IncludeBaseParameters)
        {
            var clone = baseParameters.Clone();
            seen.Add(clone.ToKey());
            yield return clone;
        }

        if (options.Count == 0)
        {
            yield break;
        }

        var total = options.Aggregate(1.0, (current, option) => current * option.Values.Count);
        if (total <= settings.MaxEvaluations)
        {
            foreach (var candidate in EnumerateGrid(baseParameters, options, 0))
            {
                if (seen.Add(candidate.ToKey()))
                {
                    yield return candidate;
                }
            }

            yield break;
        }

        var random = new Random(settings.RandomSeed);
        var attempts = 0;
        var maxAttempts = Math.Max(settings.MaxEvaluations * 20, settings.MaxEvaluations + 100);
        while (seen.Count < settings.MaxEvaluations && attempts++ < maxAttempts)
        {
            var candidate = baseParameters.Clone();
            foreach (var option in options)
            {
                var value = option.Values[random.Next(option.Values.Count)];
                ParameterReflection.SetParameter(candidate, option.Name, value);
            }

            if (seen.Add(candidate.ToKey()))
            {
                yield return candidate;
            }
        }
    }

    private static IEnumerable<AdxBaseParameters> EnumerateGrid(AdxBaseParameters baseParameters, List<(string Name, IReadOnlyList<double> Values)> options, int index)
    {
        if (index >= options.Count)
        {
            yield return baseParameters.Clone();
            yield break;
        }

        var option = options[index];
        foreach (var value in option.Values)
        {
            ParameterReflection.SetParameter(baseParameters, option.Name, value);
            foreach (var candidate in EnumerateGrid(baseParameters, options, index + 1))
            {
                yield return candidate;
            }
        }
    }
}
