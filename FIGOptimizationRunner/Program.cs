using FIGOptimizationRunner.Configuration;
using FIGOptimizationRunner.Execution;
using FIGOptimizationRunner.Search;

namespace FIGOptimizationRunner;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; cancellation.Cancel(); };
        Console.CancelKeyPress += handler;
        try
        {
            var command = args.FirstOrDefault() ?? "help";
            if (command is "help" or "--help" or "-h")
            {
                Console.WriteLine("""
                    FIGOptimizationRunner run [--config optimizer-search.json] [--samples N] [--max-source-bars N]
                      [--output-dir NEW_DIRECTORY] [--executable PATH] [--jobs N] [--dataset N]
                    FIGOptimizationRunner prepare [same options]     Generate candidates without a database run.
                    FIGOptimizationRunner execute BATCH_DIRECTORY    Execute a previously prepared batch.
                    FIGOptimizationRunner self-test                  Run local tests; no database needed.

                    Config paths are relative to the config file; CLI paths are relative to the working directory.
                    Ctrl+C stops the native process tree and records cancellation; completed output files remain.
                    """);
                return 0;
            }
            if (command == "self-test") { await SelfTests.RunAsync(); return 0; }
            var runner = new OptimizationRunner(new RandomCandidateGenerator(), new NativeBatchExecutor());
            if (command == "execute")
            {
                if (args.Length != 2) throw new ArgumentException("execute requires exactly one batch directory.");
                await runner.ExecuteAsync(args[1], cancellation.Token);
                return 0;
            }
            if (command is not ("run" or "prepare")) throw new ArgumentException("Unknown command; use help.");
            var flags = new Dictionary<string, string>(StringComparer.Ordinal);
            var valid = new HashSet<string> { "--config", "--samples", "--max-source-bars", "--output-dir", "--executable", "--jobs", "--dataset" };
            for (var i = 1; i < args.Length; i += 2)
            {
                if (!valid.Contains(args[i]) || i + 1 == args.Length || !flags.TryAdd(args[i], args[i + 1]))
                    throw new ArgumentException("Unknown, duplicate, or incomplete option; use help.");
            }
            var config = SearchConfiguration.Load(flags.GetValueOrDefault("--config", "optimizer-search.json"));
            int Number(string key, int fallback) => flags.TryGetValue(key, out var s) ? int.Parse(s, System.Globalization.CultureInfo.InvariantCulture) : fallback;
            config = config with
            {
                Samples = Number("--samples", config.Samples), MaxSourceBars = Number("--max-source-bars", config.MaxSourceBars),
                Jobs = Number("--jobs", config.Jobs), DataSetId = Number("--dataset", config.DataSetId),
                Executable = flags.TryGetValue("--executable", out var exe) ? Path.GetFullPath(exe) : config.Executable
            };
            config.Validate();
            if (command == "run" && (!File.Exists(config.Executable) || !File.Exists(config.Settings)))
                throw new FileNotFoundException("Native executable or database settings missing; build Release, specify --executable, or use prepare.");
            var directory = runner.Prepare(config, flags.GetValueOrDefault("--output-dir"), cancellation.Token);
            if (command == "run") await runner.ExecuteAsync(directory, cancellation.Token);
            return 0;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Optimization cancelled. Existing reports were preserved."); return 130; }
        catch (Exception ex) { Console.Error.WriteLine($"Optimization failed: {ex.Message}"); return 1; }
        finally { Console.CancelKeyPress -= handler; }
    }
}
