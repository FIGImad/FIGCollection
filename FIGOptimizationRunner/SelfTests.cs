using System.Globalization;
using FIGOptimizationRunner.Configuration;
using FIGOptimizationRunner.Execution;
using FIGOptimizationRunner.Search;

namespace FIGOptimizationRunner;

internal static class SelfTests
{
    private static void Check(bool ok, string label)
    {
        if (!ok) throw new InvalidOperationException($"Self-test failed: {label}");
    }
    private static void Reject(Action action, string label)
    {
        try { action(); } catch (ArgumentException) { return; }
        throw new InvalidOperationException($"Self-test expected rejection: {label}");
    }
    public static async Task RunAsync()
    {
        NestedParameterTests.Run();
        var generator = new RandomCandidateGenerator();
        var baseline = new ParameterSet(new Dictionary<string, decimal?> { ["StartBias"] = 1m, ["AdxLen"] = 50 });
        var c = new SearchConfiguration { DataSetId = 3, ConnectionStrings = "Data Source=localhost;Initial Catalog=Test;Integrated Security=True", Samples = 3, Ranges = new() { ["StartBias"] = [0, 1, 2, 3] } };
        Reject(() => new SearchConfiguration().Validate(), "database requires dataset");
        var csvConfig = c with { DataSetId = 0, PricesCsv = "prices with spaces.csv" };
        csvConfig.Validate();
        var csvArgs = NativeBatchExecutor.Arguments(csvConfig, "parameters", "reports");
        Check(!csvArgs.Contains("--connection-string"), "CSV does not forward connection");
        var databaseArgs = NativeBatchExecutor.Arguments(c, "parameters", "reports").ToList();
        Check(databaseArgs[databaseArgs.IndexOf("--connection-string") + 1] == c.ConnectionStrings && !databaseArgs.Contains("--settings"), "connection forwarded as a single argument without settings");
        Reject(() => (c with { ConnectionStrings = "" }).Validate(), "database requires connection string");
        Check(csvArgs.Contains("--prices-csv") && csvArgs.Contains(Path.GetFullPath(csvConfig.PricesCsv)) && !csvArgs.Contains("--dataset") && !csvArgs.Contains("--settings"), "CSV source forwarded without database requirements");
        var a = generator.Generate(c, baseline, default).ToArray();
        var b = generator.Generate(c, baseline, default).ToArray();
        Check(a.Length == 4 && a[0].Identity == baseline.Identity && a.Select(x => x.Identity).Distinct().Count() == 4, "small space exhaustion");
        Check(a.Select(x => x.Identity).SequenceEqual(b.Select(x => x.Identity)), "determinism");
        var outside = baseline.With(new Dictionary<string, decimal> { ["StartBias"] = 4 });
        Check(generator.Generate(c with { Samples = 4 }, outside, default).Count() == 5, "baseline outside ranges");
        Reject(() => generator.Generate(c with { Samples = 4 }, baseline, default).ToArray(), "oversubscribed space");
        Reject(() => generator.Generate(c with { Ranges = new() { ["StartBias"] = [1, 1.0m] } }, baseline, default).ToArray(), "duplicate numeric values");
        Reject(() => generator.Generate(c with { Ranges = new() { ["AdxLenConf"] = [1] } }, baseline, default).ToArray(), "inactive range");
        Reject(() => generator.Generate(c with { Ranges = new() { ["AdxLen"] = [1.5m] } }, baseline, default).ToArray(), "fractional period");
        var ldaxBase = new ParameterSet(new Dictionary<string, decimal?>
        {
            ["TrendReEntryShockMax"] = 4, ["ReEntryCooldownBars"] = 2,
            ["ContinuationMaxShockATR"] = 1, ["ContinuationEntryBars"] = 3
        });
        var ldax = c with
        {
            Collection = "ldax", Signal = "STrend_LADX_LongSide", CollectionDll = "dll with spaces/LDAX.dll",
            Ranges = new() { ["TrendReEntryShockMax"] = [3, 4, 5, 6] }
        };
        var ldaxCandidates = generator.Generate(ldax, ldaxBase, default).ToArray();
        Check(ldaxCandidates.Length == 4 && ldaxCandidates[0].Identity == ldaxBase.Identity, "LDAX baseline and active reentry threshold");
        Check(ldaxCandidates.All(p => p.Values["ReEntryCooldownBars"] == 2 && p.Values["ContinuationMaxShockATR"] == 1), "LDAX inactive values held fixed");
        foreach (var inactive in new[] { "ReEntryCooldownBars", "ContinuationMaxShockATR", "ContinuationEntryBars" })
            Reject(() => generator.Generate(ldax with { Ranges = new() { [inactive] = [0, 1, 2, 3] } }, ldaxBase, default).ToArray(), "LDAX inactive range " + inactive);
        Reject(() => generator.Generate(ldax with { Ranges = new() { ["TrendReEntryShockMax"] = [-1, 0, 1, 4] } }, ldaxBase, default).ToArray(), "LDAX negative shock threshold");
        Reject(() => generator.Generate(ldax with { Collection = "ADXBase" }, ldaxBase, default).ToArray(), "ADX inactive reentry threshold unchanged");
        var customBase = new ParameterSet(new Dictionary<string, decimal?> { ["Threshold"] = 0 });
        Check(generator.Generate(c with { Collection = "Custom", Ranges = new() { ["Threshold"] = [-1, 0, 1, 2] } }, customBase, default).Count() == 4, "custom plugin baseline schema");
        var ldaxArgs = NativeBatchExecutor.Arguments(ldax, "parameters", "reports").ToList();
        Check(ldaxArgs[ldaxArgs.IndexOf("--collection-dll") + 1] == Path.GetFullPath(ldax.CollectionDll), "DLL path forwarded as one argument");
        Check(ldaxArgs[ldaxArgs.IndexOf("--collection") + 1] == "ldax" && ldaxArgs[ldaxArgs.IndexOf("--signal") + 1] == "STrend_LADX_LongSide", "LDAX collection and normalized signal forwarded");
        var defaultArgs = NativeBatchExecutor.Arguments(c, "parameters", "reports").ToList();
        Check(!defaultArgs.Contains("--collection-dll") && defaultArgs[defaultArgs.IndexOf("--collection") + 1] == "ADXBase" && defaultArgs[defaultArgs.IndexOf("--signal") + 1] == "STrend_LBaseADX_Side", "default ADX selection preserved");
        Reject(() => (ldax with { Collection = " " }).Validate(), "empty collection");
        Reject(() => (ldax with { Signal = "" }).Validate(), "empty signal");
        var large = c with { Samples = 10, Ranges = new() { ["AdxLen"] = Enumerable.Range(1, 100001).Select(n => (ParameterValue)n).ToArray() } };
        Check(generator.Generate(large, baseline, default).Select(p => p.Identity).Distinct().Count() == 11, "large space");
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        try { generator.Generate(c, baseline, cancel.Token).ToArray(); throw new Exception("Missing generator cancellation"); }
        catch (OperationCanceledException) { }
        Reject(() => (c with { PriceCacheMode = "automatic" }).Validate(), "invalid cache mode");
        var cachedArgs = NativeBatchExecutor.Arguments(c with { PriceCacheMode = "use", PriceCacheDirectory = "cache with spaces" }, "parameters", "reports").ToList();
        Check(cachedArgs[cachedArgs.IndexOf("--price-cache-mode") + 1] == "use", "cache mode forwarded");
        Check(cachedArgs[cachedArgs.IndexOf("--price-cache-dir") + 1] == Path.GetFullPath("cache with spaces"), "cache directory forwarded");
        Check(!NativeBatchExecutor.Arguments(c, "parameters", "reports").Contains("--price-cache-mode"), "cache off compatible with older native builds");
        (c with { Quantity = 0, QuantityPct = .5m, InitialCapital = 1200000 }).Validate();
        Reject(() => (c with { Quantity = -1 }).Validate(), "negative quantity");
        Reject(() => (c with { QuantityPct = 0 }).Validate(), "zero allocation");
        Reject(() => (c with { QuantityPct = 1.01m }).Validate(), "excess allocation");
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var args = NativeBatchExecutor.Arguments(c, "parameters with spaces", "reports with spaces");
            Check(args[args.ToList().IndexOf("--commission") + 1] == "1.60", "culture-invariant native arguments");
            Check(args.Contains(Path.GetFullPath("parameters with spaces")), "paths remain single arguments");
            var sized = NativeBatchExecutor.Arguments(c with { Quantity = 0, QuantityPct = .5m }, "parameters", "reports").ToList();
            Check(sized[sized.IndexOf("--quantity-pct") + 1] == "0.5", "invariant sizing allocation");
            Check(sized[sized.IndexOf("--quantity") + 1] == "0", "dynamic sizing selected");
        }
        finally { CultureInfo.CurrentCulture = previousCulture; }

        // Files remain in the printed test directory for inspection; no database or native process is used.
        var root = Directory.CreateTempSubdirectory("FIGOptimizationRunner-tests-").FullName;
        File.WriteAllText(Path.Combine(root, "base.json"), baseline.ToJson());
        File.WriteAllText(Path.Combine(root, "native.exe"), "fake executable for checksum test");
        File.WriteAllText(Path.Combine(root, "settings.json"), "{}");
        c = c with { BaseParameters = Path.Combine(root, "base.json"), Executable = Path.Combine(root, "native.exe"), OutputRoot = root };
        var fake = new FakeExecutor();
        var runner = new OptimizationRunner(generator, fake);
        var fixedConfig = c with { FixedOutputDirectory = true, OutputRoot = Path.Combine(root, "fixed-output") };
        Check(NativeBatchExecutor.Arguments(fixedConfig, "parameters", "reports").Contains("--fixed-output-dir"), "fixed output forwarded");
        Check(!NativeBatchExecutor.Arguments(c, "parameters", "reports").Contains("--fixed-output-dir"), "default arguments unchanged");
        var fixedBatch = runner.Prepare(fixedConfig, null, default);
        Check(fixedBatch == fixedConfig.OutputRoot && OptimizationRunner.Load(fixedBatch).Configuration.FixedOutputDirectory, "fixed path and persisted option");
        await runner.ExecuteAsync(fixedBatch, default);
        Check(File.Exists(Path.Combine(fixedBatch, "results.csv")) && File.Exists(Path.Combine(fixedBatch, "top-candidates.json")) && !Directory.Exists(Path.Combine(fixedBatch, ".reports-staging")), "fixed reports at root");
        Directory.CreateDirectory(Path.Combine(fixedBatch, "stale"));
        File.WriteAllText(Path.Combine(fixedBatch, "stale", "old.txt"), "old");
        runner.Prepare(fixedConfig, null, default);
        Check(!Directory.Exists(Path.Combine(fixedBatch, "stale")) && !File.Exists(Path.Combine(fixedBatch, "results.csv")), "fixed preparation clears previous output");
        try { runner.Prepare(fixedConfig, root, default); throw new Exception("Missing input overlap rejection"); }
        catch (ArgumentException) { }
        Check(File.Exists(c.BaseParameters), "cleanup preserves overlapping inputs");
        var nestedBaseFile = Path.Combine(root, "nested-base.json");
        File.WriteAllText(nestedBaseFile, """{"entry":{"bias":1},"exit":{"enabled":true},"market":{"timeframe":"hourly","fixed":[null,2]}}""");
        var nestedConfig = c with
        {
            Collection = "Custom", BaseParameters = nestedBaseFile, Samples = 7,
            Ranges = new() { ["entry.bias"] = [1, 2], ["exit.enabled"] = [true, false], ["market.timeframe"] = ["hourly", "daily"] }
        };
        var nestedBatch = runner.Prepare(nestedConfig, Path.Combine(root, "nested-batch"), default);
        var nestedManifest = OptimizationRunner.Load(nestedBatch);
        Check(nestedManifest.Candidates.Count == 8 && nestedManifest.Configuration.Ranges["exit.enabled"].Contains(false), "nested batch manifest and booleans");
        foreach (var candidateFile in Directory.GetFiles(Path.Combine(nestedBatch, "parameters"), "*.json"))
        {
            var candidate = ParameterSet.Load(candidateFile);
            Check(candidate.Get("/market/fixed/0").Json.ValueKind == System.Text.Json.JsonValueKind.Null &&
                candidate.Get("/market/fixed/1") == 2, "nested prepared candidates preserve fixed arrays");
        }
        await runner.ExecuteAsync(nestedBatch, default);
        Check(OptimizationRunner.Load(nestedBatch).Status == "Completed", "nested batch fake execution");
        var pluginFile = Path.Combine(root, "LDAX.dll");
        File.WriteAllText(pluginFile, "fake collection for checksum test");
        File.WriteAllText(Path.Combine(root, "ldax-base.json"), ldaxBase.ToJson());
        var ldaxConfigFile = Path.Combine(root, "ldax-search.json");
        File.WriteAllText(ldaxConfigFile, System.Text.Json.JsonSerializer.Serialize(ldax with
        {
            CollectionDll = "LDAX.dll", BaseParameters = "ldax-base.json", Executable = "native.exe", OutputRoot = "."
        }, SearchConfiguration.Json));
        var loadedLdax = SearchConfiguration.Load(ldaxConfigFile);
        var groupedFile = Path.Combine(root, "grouped.json");
        File.WriteAllText(groupedFile, """
            {
              // Grouped input with relative paths
              "ExecParams": {"Executable":"native.exe", "CollectionDll":"LDAX.dll", "Jobs":4},
              "PriceParams": {"PricesCsv":"prices.csv", "IntervalMinutes":10, "ConnectionStrings":"Data Source=localhost;Integrated Security=True"},
              "Output": {"OutputRoot":"grouped-output", "FixedOutputDirectory":true},
              "SearchParams": {"BaseParameters":"base.json", "Samples":0, "Seed":42, "Ranges":{"StdDev1":[1.0]}},
              "TradeParams": {"Quantity":0, "InitialCapital":1200000},
              "DateParams": {"StartDate":"2016-01-01"}
            }
            """);
        var grouped = SearchConfiguration.Load(groupedFile);
        grouped.Validate();
        Check(grouped.ConnectionStrings == "Data Source=localhost;Integrated Security=True", "grouped connection strings preserved");
        Check(grouped.Jobs == 4 && grouped.PricesCsv == Path.Combine(root, "prices.csv") && grouped.IntervalMinutes == 10 && grouped.FixedOutputDirectory && grouped.Samples == 0 && grouped.Seed == 42 && grouped.Quantity == 0 && grouped.StartDate == "2016-01-01" && grouped.Ranges.ContainsKey("StdDev1"), "grouped configuration values and paths");
        foreach (var invalid in new[] {
            """{"Jobs":2,"ExecParams":{"Jobs":4}}""",
            """{"ExecParams":{"Unknown":1}}""",
            """{"ExecParams":{"PricesCsv":"wrong-group.csv"}}""",
            """{"Output":true}"""
        })
        {
            File.WriteAllText(groupedFile, invalid);
            Reject(() => SearchConfiguration.Load(groupedFile), "invalid or ambiguous grouped configuration");
        }
        File.WriteAllText(ldaxConfigFile, "// Optimizer configuration\n" + File.ReadAllText(ldaxConfigFile).Replace("\"DataSetId\":", "/* Database dataset */ \"DataSetId\":"));
        Check(SearchConfiguration.Load(ldaxConfigFile).DataSetId == loadedLdax.DataSetId, "line and block comments in optimizer configuration");
        Check(loadedLdax.CollectionDll == pluginFile, "DLL resolves relative to configuration");
        var pluginBatch = runner.Prepare(loadedLdax, Path.Combine(root, "plugin"), default);
        Check(OptimizationRunner.Load(pluginBatch).CollectionDllSHA256 == OptimizationRunner.Hash(pluginFile), "DLL checksum saved");
        Check(OptimizationRunner.Load(pluginBatch).Configuration.Signal == ldax.Signal, "selection survives manifest round trip");
        File.AppendAllText(pluginFile, "changed");
        try { await runner.ExecuteAsync(pluginBatch, default); throw new Exception("Missing DLL checksum rejection"); }
        catch (InvalidDataException ex) when (ex.Message.Contains("Collection DLL changed")) { }
        Check(OptimizationRunner.Load(pluginBatch).Attempts == 0, "changed DLL rejected before execution");
        var pluginRun = runner.Prepare(loadedLdax, Path.Combine(root, "plugin-run"), default);
        await runner.ExecuteAsync(pluginRun, default);
        Check(OptimizationRunner.Load(pluginRun).Status == "Completed", "plugin batch execution");
        var defaultDll = Path.Combine(root, "FIGStudyCollection.ADX.dll");
        File.WriteAllText(defaultDll, "default ADX checksum test");
        var defaultDllBatch = runner.Prepare(c, Path.Combine(root, "default-dll"), default);
        Check(OptimizationRunner.Load(defaultDllBatch).CollectionDllSHA256 == OptimizationRunner.Hash(defaultDll), "default adjacent DLL hashed");
        File.AppendAllText(defaultDll, "changed");
        try { await runner.ExecuteAsync(defaultDllBatch, default); throw new Exception("Missing default DLL checksum rejection"); }
        catch (InvalidDataException ex) when (ex.Message.Contains("Collection DLL changed")) { }
        var batch = runner.Prepare(c, Path.Combine(root, "prepared"), default);
        Check(OptimizationRunner.Load(batch).Status == "Prepared", "prepared manifest");
        await runner.ExecuteAsync(batch, default);
        Check(OptimizationRunner.Load(batch).Status == "Completed" && File.Exists(Path.Combine(batch, "reports", "top-candidates.json")), "completed manifest/results");
        var tampered = runner.Prepare(c, Path.Combine(root, "tampered"), default);
        File.AppendAllText(Path.Combine(tampered, "parameters", "run-0001.json"), " ");
        try { await runner.ExecuteAsync(tampered, default); throw new Exception("Missing checksum rejection"); }
        catch (InvalidDataException) { }
        var retry = runner.Prepare(c, Path.Combine(root, "retry"), default);
        fake.ExitCode = 9;
        try { await runner.ExecuteAsync(retry, default); throw new Exception("Missing exit failure"); }
        catch (InvalidOperationException ex) when (ex.Message.Contains("code 9")) { }
        Check(OptimizationRunner.Load(retry).Status == "Failed", "failed manifest");
        fake.ExitCode = 0;
        await runner.ExecuteAsync(retry, default);
        Check(OptimizationRunner.Load(retry).ReportDirectory == "reports-attempt-002", "retry preserves prior output");
        var cancelled = runner.Prepare(c, Path.Combine(root, "cancelled"), default);
        fake.Cancel = true;
        try { await runner.ExecuteAsync(cancelled, cancel.Token); throw new Exception("Missing executor cancellation"); }
        catch (OperationCanceledException) { }
        Check(OptimizationRunner.Load(cancelled).Status == "Cancelled", "cancelled manifest");
        Console.WriteLine($"C# runner self-tests passed. Test artifacts: {root}");
    }

    private sealed class FakeExecutor : IBatchExecutor
    {
        public int ExitCode { get; set; }
        public bool Cancel { get; set; }
        public Task<int> ExecuteAsync(SearchConfiguration c, string parameters, string reports, string logPath, CancellationToken cancellation)
        {
            if (Cancel) throw new OperationCanceledException(cancellation);
            Directory.CreateDirectory(reports);
            if (ExitCode == 0)
            {
                var rows = new List<string> { "RunId,NetProfit,FinalNAV,MaxDrawdownPct,ClosedTrades,TrainNetProfit,ValidationNetProfit,TestNetProfit,ParametersJSON" };
                foreach (var file in Directory.GetFiles(parameters, "*.json").Order(StringComparer.Ordinal))
                    rows.Add($"{Path.GetFileNameWithoutExtension(file)},100,101,-2,5,50,30,20,\"{{\"\"StartBias\"\":1,\"\"AdxLen\"\":50}}\"");
                File.WriteAllLines(Path.Combine(reports, "results.csv"), rows);
            }
            return Task.FromResult(ExitCode);
        }
    }
}
