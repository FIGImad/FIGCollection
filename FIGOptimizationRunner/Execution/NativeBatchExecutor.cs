using System.Diagnostics;
using System.Globalization;
using FIGOptimizationRunner.Configuration;

namespace FIGOptimizationRunner.Execution;

public sealed class NativeBatchExecutor : IBatchExecutor
{
    public static IReadOnlyList<string> Arguments(SearchConfiguration c, string parameters, string reports)
    {
        static string N<T>(T value) where T : IFormattable => value.ToString(null, CultureInfo.InvariantCulture);
        var args = new List<string>
        {
            "--backtest-batch", "--interval-minutes", N(c.IntervalMinutes),
            "--parameters-dir", Path.GetFullPath(parameters), "--output-dir", Path.GetFullPath(reports),
            "--jobs", N(c.Jobs), "--max-bars", N(c.MaxSourceBars),
            "--capital", N(c.InitialCapital), "--quantity", N(c.Quantity), "--multiplier", N(c.Multiplier),
            "--commission", N(c.CommissionPerContractPerSide), "--slippage-points", N(c.SlippagePointsPerSide),
            "--force-close", c.ForceCloseAtEnd ? "1" : "0", "--start", c.StartDate,
            "--train-end", c.TrainEndExclusive, "--validation-end", c.ValidationEndExclusive
        };
        args.AddRange(["--collection", c.Collection, "--signal", c.Signal]);
        if (string.IsNullOrWhiteSpace(c.PricesCsv)) args.AddRange(["--dataset", N(c.DataSetId), "--connection-string", c.ConnectionStrings]);
        else args.AddRange(["--prices-csv", Path.GetFullPath(c.PricesCsv)]);
        if (c.FixedOutputDirectory) args.Add("--fixed-output-dir");
        if (!string.IsNullOrEmpty(c.CollectionDll))
            args.AddRange(["--collection-dll", Path.GetFullPath(c.CollectionDll)]);
        if (c.Quantity == 0)
            args.AddRange(["--quantity-pct", N(c.QuantityPct)]);
        if (c.PriceCacheMode != "off" && string.IsNullOrWhiteSpace(c.PricesCsv))
            args.AddRange(["--price-cache-dir", Path.GetFullPath(c.PriceCacheDirectory), "--price-cache-mode", c.PriceCacheMode]);
        if (!string.IsNullOrEmpty(c.EndDateExclusive)) args.AddRange(["--end", c.EndDateExclusive]);
        return args;
    }

    public async Task<int> ExecuteAsync(SearchConfiguration c, string parameters, string reports, string logPath, CancellationToken cancellation)
    {
        if (!File.Exists(c.Executable)) throw new FileNotFoundException("Build FIGSignalCpp Release or specify --executable.", c.Executable);
        c.Validate();
        if (!string.IsNullOrWhiteSpace(c.PricesCsv) && !File.Exists(c.PricesCsv)) throw new FileNotFoundException("Prices CSV not found.", c.PricesCsv);
        if (!string.IsNullOrEmpty(c.CollectionDll) && !File.Exists(c.CollectionDll))
            throw new FileNotFoundException("Collection DLL not found. Build the matching native configuration.", c.CollectionDll);
        var info = new ProcessStartInfo(c.Executable)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(c.Executable))!
        };
        foreach (var arg in Arguments(c, parameters, reports)) info.ArgumentList.Add(arg);
        await using var log = new StreamWriter(logPath) { AutoFlush = true };
        using var gate = new SemaphoreSlim(1);
        using var process = new Process { StartInfo = info };
        cancellation.ThrowIfCancellationRequested();
        if (!process.Start()) throw new InvalidOperationException("Could not start native backtester.");
        async Task Pump(StreamReader reader, bool error)
        {
            while (await reader.ReadLineAsync() is { } line)
            {
                await gate.WaitAsync();
                try
                {
                    await log.WriteLineAsync((error ? "stderr: " : "") + line);
                    if (error) Console.Error.WriteLine(line); else Console.WriteLine(line);
                }
                finally { gate.Release(); }
            }
        }
        var stdout = Pump(process.StandardOutput, false);
        var stderr = Pump(process.StandardError, true);
        try
        {
            var exit = process.WaitForExitAsync(cancellation);
            var pending = new List<Task> { exit, stdout, stderr };
            // Observe either pipe/log failure immediately, even while the child is still running.
            while (pending.Count != 0)
            {
                var finished = await Task.WhenAny(pending);
                await finished;
                pending.Remove(finished);
            }
            return process.ExitCode;
        }
        catch
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            try { await Task.WhenAll(stdout, stderr); } catch { /* Preserve the original process/cancellation error. */ }
            throw;
        }
    }
}
