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
            "--backtest-batch", "--dataset", N(c.DataSetId), "--interval-minutes", N(c.IntervalMinutes),
            "--parameters-dir", Path.GetFullPath(parameters), "--output-dir", Path.GetFullPath(reports),
            "--settings", c.Settings, "--jobs", N(c.Jobs), "--max-bars", N(c.MaxSourceBars),
            "--capital", N(c.InitialCapital), "--quantity", N(c.Quantity), "--multiplier", N(c.Multiplier),
            "--commission", N(c.CommissionPerContractPerSide), "--slippage-points", N(c.SlippagePointsPerSide),
            "--force-close", c.ForceCloseAtEnd ? "1" : "0", "--start", c.StartDate,
            "--train-end", c.TrainEndExclusive, "--validation-end", c.ValidationEndExclusive
        };
        if (c.Quantity == 0)
            args.AddRange(["--quantity-pct", N(c.QuantityPct)]);
        if (c.PriceCacheMode != "off")
            args.AddRange(["--price-cache-dir", Path.GetFullPath(c.PriceCacheDirectory), "--price-cache-mode", c.PriceCacheMode]);
        if (!string.IsNullOrEmpty(c.EndDateExclusive)) args.AddRange(["--end", c.EndDateExclusive]);
        return args;
    }

    public async Task<int> ExecuteAsync(SearchConfiguration c, string parameters, string reports, string logPath, CancellationToken cancellation)
    {
        if (!File.Exists(c.Executable)) throw new FileNotFoundException("Build FIGSignalCpp Release or specify --executable.", c.Executable);
        if (!File.Exists(c.Settings)) throw new FileNotFoundException("Database settings not found.", c.Settings);
        var info = new ProcessStartInfo(c.Executable)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(c.Settings)!
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
