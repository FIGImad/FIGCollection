using System.Text.Json;

namespace FIGBacktestOptimizer;

public static class SampleConfigWriter
{
    public static void Write(string path)
    {
        var config = new OptimizerConfig
        {
            DataPath = @"C:\Data\NQ_5min_10y.csv",
            OutputDirectory = @"C:\Temp\ADXBaseOptimizer",
            Backtest = new BacktestSettings
            {
                InitialCapital = 1_200_000,
                Multiplier = 20,
                CommissionPerContract = 1.60,
                Quantity = 0,
                QuantityPercent = 1.0,
                TickSize = 0.25,
                SlippageTicks = 1,
                MinTrades = 150
            },
            Splits = new SplitSettings
            {
                TrainRatio = 0.60,
                ValidationRatio = 0.20,
                UseWalkForward = true,
                WalkForwardTrainMonths = 24,
                WalkForwardTestMonths = 3
            },
            Search = new SearchSettings
            {
                MaxEvaluations = 5000,
                RandomSeed = 12345,
                TopResults = 50,
                Parallelism = 0,
                IncludeBaseParameters = true
            },
            Objective = new ObjectiveSettings
            {
                NetProfitPctWeight = 1.0,
                MaxDrawdownPctWeight = 2.0,
                TradeCountPenalty = 0.02,
                LowTradeCountPenalty = 0.50,
                ProfitFactorWeight = 0.25,
                ValidationWeight = 1.0,
                TrainWeight = 0.25,
                WalkForwardWeight = 0.50
            },
            BaseParameters = new AdxBaseParameters(),
            ParameterRanges = new Dictionary<string, ParameterRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["StartBias"] = new() { Values = [0.5, 1.0, 1.5, 2.0] },
                ["StopBias"] = new() { Values = [-1.5, -0.75, 0.0] },
                ["FailFastBars"] = new() { Values = [1, 2, 3] },
                ["ReEntryCooldownBars"] = new() { Values = [1, 2, 3, 4] },
                ["TrendReEntryBars"] = new() { Values = [0, 2, 4, 6] },
                ["TrendReEntryShockMax"] = new() { Values = [0.35, 0.50, 0.65] },
                ["ShockDropATR"] = new() { Values = [1.25, 1.50, 1.75, 2.00] },
                ["ShockArmedDropATR"] = new() { Values = [1.00, 1.15, 1.30, 1.50] },
                ["ShockArmUpATR"] = new() { Values = [0.75, 1.00, 1.25] },
                ["ShockRangeATR"] = new() { Values = [1.80, 2.20, 2.60] },
                ["GuideStdDev"] = new() { Values = [0.5, 0.6, 0.8] },
                ["GuideMALen"] = new() { Values = [20, 24, 30] }
            }
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(config, options));
    }
}
