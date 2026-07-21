using System.Globalization;
using System.Text.Json.Serialization;

namespace FIGBacktestOptimizer;

public sealed record Bar(DateTime Time, double Open, double High, double Low, double Close, double Volume);

public sealed class OptimizerConfig
{
    public string DataPath { get; set; } = "";
    public string OutputDirectory { get; set; } = "optimizer-output";
    public BacktestSettings Backtest { get; set; } = new();
    public SplitSettings Splits { get; set; } = new();
    public SearchSettings Search { get; set; } = new();
    public ObjectiveSettings Objective { get; set; } = new();
    public AdxBaseParameters BaseParameters { get; set; } = new();
    public Dictionary<string, ParameterRange> ParameterRanges { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class BacktestSettings
{
    public double InitialCapital { get; set; } = 1_200_000;
    public double Multiplier { get; set; } = 20;
    public double CommissionPerContract { get; set; } = 1.60;
    public int Quantity { get; set; } = 0;
    public double QuantityPercent { get; set; } = 1.0;
    public double TickSize { get; set; } = 0.25;
    public int SlippageTicks { get; set; } = 1;
    public int MinTrades { get; set; } = 80;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public sealed class SplitSettings
{
    public double TrainRatio { get; set; } = 0.60;
    public double ValidationRatio { get; set; } = 0.20;
    public bool UseWalkForward { get; set; } = true;
    public int WalkForwardTrainMonths { get; set; } = 24;
    public int WalkForwardTestMonths { get; set; } = 3;
}

public sealed class SearchSettings
{
    public int MaxEvaluations { get; set; } = 5000;
    public int RandomSeed { get; set; } = 12345;
    public int TopResults { get; set; } = 50;
    public int Parallelism { get; set; } = 0;
    public bool IncludeBaseParameters { get; set; } = true;
}

public sealed class ObjectiveSettings
{
    public double NetProfitPctWeight { get; set; } = 1.0;
    public double MaxDrawdownPctWeight { get; set; } = 2.0;
    public double TradeCountPenalty { get; set; } = 0.02;
    public double LowTradeCountPenalty { get; set; } = 0.50;
    public double ProfitFactorWeight { get; set; } = 0.25;
    public double ValidationWeight { get; set; } = 1.0;
    public double TrainWeight { get; set; } = 0.25;
    public double WalkForwardWeight { get; set; } = 0.50;
}

public sealed class ParameterRange
{
    public double? Start { get; set; }
    public double? End { get; set; }
    public double? Step { get; set; }
    public double[]? Values { get; set; }

    public IReadOnlyList<double> Expand()
    {
        if (Values is { Length: > 0 })
        {
            return Values;
        }

        if (Start is null || End is null || Step is null || Step <= 0)
        {
            throw new InvalidOperationException("Parameter range requires either Values or Start/End/Step.");
        }

        var values = new List<double>();
        var start = Start.Value;
        var end = End.Value;
        var step = Step.Value;
        var epsilon = step / 1_000_000.0;

        for (var value = start; value <= end + epsilon; value += step)
        {
            values.Add(Math.Round(value, 10));
        }

        return values;
    }
}

public sealed class AdxBaseParameters
{
    public int AdxLen { get; set; } = 50;
    public int AdxLenConf { get; set; } = 6;
    public double AdxTriggerConf { get; set; } = 15;
    public int FastMALen { get; set; } = 3;
    public int SlowMALen { get; set; } = 3;
    public int GuideMALen { get; set; } = 24;
    public double GuideStdDev { get; set; } = 0.6;
    public double StartBias { get; set; } = 1.0;
    public double StopBias { get; set; } = -0.75;
    public int FailFastBars { get; set; } = 2;
    public double FailFastBuffer { get; set; } = 0;
    public int ReEntryCooldownBars { get; set; } = 3;
    public int TrendReEntryBars { get; set; } = 2;
    public double TrendReEntryShockMax { get; set; } = 0.40;
    public int ShockATRLen { get; set; } = 14;
    public double ShockDropATR { get; set; } = 1.50;
    public double ShockArmedDropATR { get; set; } = 1.15;
    public double ShockArmUpATR { get; set; } = 1.00;
    public double ShockRangeATR { get; set; } = 2.20;

    public AdxBaseParameters Clone() => (AdxBaseParameters)MemberwiseClone();

    public string ToKey()
    {
        var values = ParameterReflection.GetParameterValues(this)
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => string.Create(CultureInfo.InvariantCulture, $"{pair.Key}={pair.Value}"));
        return string.Join("|", values);
    }
}

public sealed class BacktestMetrics
{
    public string Segment { get; set; } = "";
    public double NetProfit { get; set; }
    public double NetProfitPct { get; set; }
    public double MaxDrawdownPts { get; set; }
    public double MaxDrawdownPct { get; set; }
    public int Trades { get; set; }
    public int Winners { get; set; }
    public int Losers { get; set; }
    public double WinRatePct { get; set; }
    public double ProfitFactor { get; set; }
    public double AverageTrade { get; set; }
    public double Score { get; set; }
}

public sealed class OptimizationResult
{
    public int Evaluation { get; set; }
    public double CompositeScore { get; set; }
    public BacktestMetrics Train { get; set; } = new();
    public BacktestMetrics Validation { get; set; } = new();
    public BacktestMetrics Test { get; set; } = new();
    public BacktestMetrics WalkForward { get; set; } = new();
    public AdxBaseParameters Parameters { get; set; } = new();
}

public sealed class Trade
{
    public DateTime OpenTime { get; set; }
    public DateTime CloseTime { get; set; }
    public double OpenPrice { get; set; }
    public double ClosePrice { get; set; }
    public int Quantity { get; set; }
    public double Profit { get; set; }
    public int BarsHeld { get; set; }
    public string EntryMode { get; set; } = "";
    public string ExitReason { get; set; } = "";
}

public sealed class BacktestRun
{
    public BacktestMetrics Metrics { get; set; } = new();
    public List<Trade> Trades { get; } = [];
}
