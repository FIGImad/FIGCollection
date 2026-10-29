using System.Globalization;
namespace FIGSignalBacktest;

public sealed class Options
{
    public string Command = "--help", Collection = "LADX", Signal = "LADX_SIDE";
    public string Plugin = Path.Combine(AppContext.BaseDirectory, "Plugins", "LADX", "FIG.StudyCollections.LADX.dll");
    public string ConnectionString = "", ParametersDirectory = "", OutputDirectory = "", Output = "", Parameters = "";
    public string CacheDirectory = "", CacheMode = "off", Start = "2010-01-01", End = "", TrainEnd = "2023-01-01", ValidationEnd = "2025-01-01";
    public int Dataset, Interval = 5, Jobs = 2, MaxBars, Quantity = 15, Period = 14, Smoothing = 14;
    public string PricesCsv = "";
    public string Source = "close";
    public double Capital = 9255000, QuantityPct = 1, Multiplier = 20, Commission = 1.6, Slippage;
    public bool ForceClose, CollectionSpecified, FixedOutputDirectory;
    public static Options Parse(string[] args)
    {
        var o = new Options(); if (args.Length == 0) return o;
        o.Command = args[0]; if (o.Command == "--calculate-studies") o.Interval = 1;
        bool smoothing = false;
        for (int i = 1; i < args.Length; i += 2)
        {
            if (args[i] == "--fixed-output-dir") { o.FixedOutputDirectory = true; i--; continue; }
            if (i + 1 == args.Length) throw new ArgumentException("Missing option value: " + args[i]);
            var v = args[i + 1]; int Int() => int.Parse(v, CultureInfo.InvariantCulture); double Num() => double.Parse(v, CultureInfo.InvariantCulture);
            switch (args[i])
            {
                case "--collection-dll": o.Plugin = v; break;
                case "--collection": o.Collection = v; o.CollectionSpecified = true; break;
                case "--signal": o.Signal = v; break;
                case "--connection-string": o.ConnectionString = v; break;
                case "--parameters-dir": o.ParametersDirectory = v; break;
                case "--output-dir": o.OutputDirectory = v; break;
                case "--output": o.Output = v; break;
                case "--parameters": o.Parameters = v; break;
                case "--dataset": o.Dataset = Int(); break;
                case "--prices-csv": o.PricesCsv = Path.GetFullPath(v); break;
                case "--interval-minutes": o.Interval = Int(); break;
                case "--jobs": o.Jobs = Int(); break;
                case "--max-bars": o.MaxBars = Int(); break;
                case "--quantity": o.Quantity = Int(); break;
                case "--quantity-pct": o.QuantityPct = Num(); break;
                case "--capital": o.Capital = Num(); break;
                case "--multiplier": o.Multiplier = Num(); break;
                case "--commission": o.Commission = Num(); break;
                case "--slippage-points": o.Slippage = Num(); break;
                case "--force-close": o.ForceClose = v switch { "0" => false, "1" => true, _ => throw new ArgumentException("--force-close requires 0 or 1") }; break;
                case "--start": o.Start = v; break;
                case "--end": o.End = v; break;
                case "--train-end": o.TrainEnd = v; break;
                case "--validation-end": o.ValidationEnd = v; break;
                case "--price-cache-dir": o.CacheDirectory = v; break;
                case "--price-cache-mode": o.CacheMode = v; break;
                case "--period": o.Period = Int(); break;
                case "--adx-smoothing": o.Smoothing = Int(); smoothing = true; break;
                case "--source": o.Source = v; break;
                default: throw new ArgumentException("Unknown option: " + args[i]);
            }
        }
        if (!smoothing) o.Smoothing = o.Period;
        return o;
    }
    public void Validate(bool batch, bool requireData = true)
    {
        if (requireData && string.IsNullOrWhiteSpace(PricesCsv) && Dataset <= 0) throw new ArgumentException("--dataset must be a positive ID for database input; use --prices-csv FILE for CSV input.");
        if (requireData && string.IsNullOrWhiteSpace(PricesCsv) && string.IsNullOrWhiteSpace(ConnectionString)) throw new ArgumentException("--connection-string is required for database input.");
        if (Interval < 1 || Interval > int.MaxValue / 60 || MaxBars < 0 || Jobs < 1 || Jobs > 64 || Quantity < 0) throw new ArgumentException("Invalid interval/maximum/quantity/jobs");
        if (CacheMode is not ("off" or "use" or "refresh") || (CacheMode != "off" && string.IsNullOrWhiteSpace(CacheDirectory))) throw new ArgumentException("Invalid price cache configuration");
        if (!batch) return;
        if (!double.IsFinite(Capital) || Capital <= 0 || !double.IsFinite(Multiplier) || Multiplier <= 0 || !double.IsFinite(Commission) || Commission < 0 || !double.IsFinite(Slippage) || Slippage < 0 || !double.IsFinite(QuantityPct) || QuantityPct <= 0 || QuantityPct > 1) throw new ArgumentException("Invalid backtest financial settings");
        if (Format.Date(Start) >= Format.Date(TrainEnd) || Format.Date(TrainEnd) >= Format.Date(ValidationEnd) || (End != "" && Format.Date(End) <= Format.Date(Start))) throw new ArgumentException("Dates must satisfy start < train-end < validation-end and end > start");
        if (string.IsNullOrWhiteSpace(Collection) || string.IsNullOrWhiteSpace(Signal)) throw new ArgumentException("Collection and signal are required");
    }
}
