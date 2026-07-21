using System.Text.Json;
using FIGBacktestOptimizer;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

try
{
    var command = args[0].Trim().ToLowerInvariant();
    switch (command)
    {
        case "optimize":
            if (args.Length < 2)
            {
                throw new ArgumentException("Missing config path.");
            }

            var config = LoadConfig(args[1]);
            var bars = CsvBarReader.ReadBars(config.DataPath);
            Console.WriteLine($"Loaded {bars.Count:n0} bars from {config.DataPath}");

            var optimizer = new Optimizer(config, bars);
            var results = optimizer.Run();
            optimizer.WriteOutputs(results);

            if (results.Count > 0)
            {
                var best = results[0];
                Console.WriteLine("Best result:");
                Console.WriteLine($"  Composite score: {best.CompositeScore:n2}");
                Console.WriteLine($"  Validation net: {best.Validation.NetProfitPct:n2}%");
                Console.WriteLine($"  Validation max DD: {best.Validation.MaxDrawdownPct:n2}%");
                Console.WriteLine($"  Validation trades: {best.Validation.Trades:n0}");
            }

            Console.WriteLine($"Outputs written to {Path.GetFullPath(config.OutputDirectory)}");
            return 0;

        case "write-sample-config":
            var outputPath = args.Length >= 2 ? args[1] : "adxbase-optimizer.sample.json";
            SampleConfigWriter.Write(outputPath);
            Console.WriteLine($"Sample config written to {Path.GetFullPath(outputPath)}");
            return 0;

        default:
            PrintUsage();
            return 1;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine(ex.ToString());
    return 2;
}

static OptimizerConfig LoadConfig(string path)
{
    if (!File.Exists(path))
    {
        throw new FileNotFoundException("Config file not found.", path);
    }

    var json = File.ReadAllText(path);
    var options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    return JsonSerializer.Deserialize<OptimizerConfig>(json, options)
        ?? throw new InvalidOperationException("Unable to parse optimizer config.");
}

static void PrintUsage()
{
    Console.WriteLine("FIGBacktestOptimizer");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project FIGBacktestOptimizer -- write-sample-config C:\\Temp\\adxbase-optimizer.json");
    Console.WriteLine("  dotnet run --project FIGBacktestOptimizer -- optimize C:\\Temp\\adxbase-optimizer.json");
}
