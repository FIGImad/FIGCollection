using System.Globalization;
using FIGSignalBacktest;
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
try
{
    var options = Options.Parse(args);
    using var cancellation = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
    switch (options.Command)
    {
        case "--backtest-batch": Runner.Batch(options, cancellation.Token); break;
        case "--calculate-studies": Runner.Studies(options); break;
        case "--list-collections": foreach (var name in new Plugins(options.Plugin).Types) Console.WriteLine(name); break;
        case "--test-collection": SelfTests.Plugin(options); break;
        case "--self-test": SelfTests.Run(); break;
        case "--help": Console.WriteLine("""
FIGSignalBacktest (.NET 10, x64)
  --list-collections --collection-dll MANAGED.dll
  --test-collection --collection-dll MANAGED.dll [--collection LADX] [--parameters FILE.json] [--interval-minutes 5]
  --backtest-batch --collection-dll MANAGED.dll --collection LADX --signal LADX_SIDE
    --parameters-dir DIR --output-dir DIR [--fixed-output-dir]
    (--dataset ID --connection-string SQL_CONNECTION | --prices-csv FILE) [--interval-minutes 5 --jobs 2 --max-bars 0]
    [--capital 9255000 --quantity 15 --quantity-pct 1 --multiplier 20 --commission 1.60]
    [--slippage-points 0 --force-close 0]
    [--start 2010-01-01 --end YYYY-MM-DD --train-end 2023-01-01 --validation-end 2025-01-01]
    [--price-cache-dir DIR --price-cache-mode off|use|refresh]
  --calculate-studies --collection-dll MANAGED.dll --collection LADX --output FILE.csv
    (--dataset ID --connection-string SQL_CONNECTION | --prices-csv FILE) [--parameters FILE.json --interval-minutes 1 --max-bars 0]
  --self-test
Omit --collection in study export for the legacy ADXDiagnostics report:
  [--period 14 --adx-smoothing PERIOD --source close]
Dates and aggregation buckets are UTC; end boundaries are exclusive. Warmup uses earlier prices.
Quantity 0 uses initial-capital/HLC3 sizing. Binary charts use FIGDATA v1.
Plugins implement FIG.Studies.IStudyCollectionFactory. Native adapter DLLs are not accepted.
CSV requires RawTime (UTC Unix seconds), Open, High, Low, Close, Volume; Id and DataSetId are optional.
CSV input does not require database settings and bypasses the database price cache.
"""); break;
        default: throw new ArgumentException("Unknown command; use --help");
    }
    return 0;
}
catch (Exception ex)
{
    var error = ex is AggregateException aggregate ? aggregate.Flatten().InnerExceptions[0] : ex;
    Console.Error.WriteLine("FIGSignalBacktest fatal: " + error.Message); return 1;
}
