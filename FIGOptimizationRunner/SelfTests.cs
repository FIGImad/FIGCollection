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
        var generator = new RandomCandidateGenerator();
        var baseline = new ParameterSet(new Dictionary<string, decimal?> { ["StartBias"] = 1m, ["AdxLen"] = 50 });
        var c = new SearchConfiguration { Samples = 3, Ranges = new() { ["StartBias"] = [0, 1, 2, 3] } };
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
        var large = c with { Samples = 10, Ranges = new() { ["AdxLen"] = Enumerable.Range(1, 100001).Select(n => (decimal)n).ToArray() } };
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
        c = c with { BaseParameters = Path.Combine(root, "base.json"), Executable = Path.Combine(root, "native.exe"), Settings = Path.Combine(root, "settings.json"), OutputRoot = root };
        var fake = new FakeExecutor();
        var runner = new OptimizationRunner(generator, fake);
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
