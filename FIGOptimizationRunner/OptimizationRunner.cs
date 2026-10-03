using System.Security.Cryptography;
using System.Text.Json;
using FIGOptimizationRunner.Configuration;
using FIGOptimizationRunner.Execution;
using FIGOptimizationRunner.Results;
using FIGOptimizationRunner.Search;

namespace FIGOptimizationRunner;

public sealed class OptimizationRunner(ICandidateGenerator generator, IBatchExecutor executor)
{
    public static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
    public static void Save(string directory, BatchManifest manifest)
    {
        var path = Path.Combine(directory, "batch.json");
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(manifest, SearchConfiguration.Json));
        File.Move(temp, path, overwrite: true);
    }
    public static BatchManifest Load(string directory) =>
        JsonSerializer.Deserialize<BatchManifest>(File.ReadAllText(Path.Combine(directory, "batch.json")), SearchConfiguration.Json)
        ?? throw new InvalidDataException("Empty batch manifest.");

    public string Prepare(SearchConfiguration c, string? outputDirectory, CancellationToken cancellation)
    {
        c.Validate();
        var baseline = ParameterSet.Load(c.BaseParameters);
        var directory = Path.GetFullPath(outputDirectory ?? Path.Combine(c.OutputRoot,
            DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-cs-" + Guid.NewGuid().ToString("N")[..8]));
        if (Directory.Exists(directory) || File.Exists(directory)) throw new IOException("Choose a new output directory for every batch.");
        // Materialize only the first candidate to validate ranges before creating output files.
        using var candidates = generator.Generate(c, baseline, cancellation).GetEnumerator();
        if (!candidates.MoveNext()) throw new InvalidDataException("Generator omitted the baseline.");
        if (candidates.Current.Identity != baseline.Identity) throw new InvalidDataException("First candidate must be the baseline.");
        Directory.CreateDirectory(directory);
        var manifest = new BatchManifest
        {
            Configuration = c, Generator = generator.Name,
            ExecutableSHA256 = File.Exists(c.Executable) ? Hash(c.Executable) : null
        };
        Save(directory, manifest);
        try
        {
            var parameterDirectory = Path.Combine(directory, "parameters");
            Directory.CreateDirectory(parameterDirectory);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var index = 0;
            do
            {
                cancellation.ThrowIfCancellationRequested();
                if (index >= c.Samples + 1) throw new InvalidDataException("Generator exceeded the candidate budget.");
                var p = candidates.Current;
                if (!seen.Add(p.Identity)) throw new InvalidDataException("Generator returned duplicate candidates.");
                var name = $"run-{++index:D4}.json";
                var file = Path.Combine(parameterDirectory, name);
                File.WriteAllText(file, p.ToJson());
                manifest.Candidates.Add(new(name, Hash(file)));
            } while (candidates.MoveNext());
            if (index != c.Samples + 1) throw new InvalidDataException("Generator did not satisfy the candidate budget.");
            File.WriteAllText(Path.Combine(directory, "search-settings.json"), JsonSerializer.Serialize(c, SearchConfiguration.Json));
            manifest.Status = "Prepared";
            Save(directory, manifest);
            Console.WriteLine($"Prepared {index} candidates: {directory}");
            return directory;
        }
        catch (Exception ex)
        {
            manifest.Status = ex is OperationCanceledException ? "PreparationCancelled" : "PreparationFailed";
            manifest.Error = ex.Message;
            Save(directory, manifest);
            throw;
        }
    }

    public async Task ExecuteAsync(string directory, CancellationToken cancellation)
    {
        directory = Path.GetFullPath(directory);
        // OS file lock prevents two controllers launching this same prepared batch concurrently.
        using var batchLock = new FileStream(Path.Combine(directory, "execution.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var m = Load(directory);
        if (m.Version != 1 || m.Status is not ("Prepared" or "Failed" or "Cancelled"))
            throw new InvalidOperationException($"Cannot execute batch with version {m.Version}, status {m.Status}.");
        m.Configuration.Validate();
        var parameterDirectory = Path.Combine(directory, "parameters");
        var actualFiles = Directory.GetFiles(parameterDirectory, "*.json");
        if (actualFiles.Length != m.Candidates.Count || m.Candidates.Count != m.Configuration.Samples + 1)
            throw new InvalidDataException("Prepared candidate inventory changed.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in m.Candidates)
        {
            if (Path.GetFileName(p.FileName) != p.FileName || !names.Add(p.FileName) ||
                Hash(Path.Combine(parameterDirectory, p.FileName)) != p.SHA256)
                throw new InvalidDataException("Prepared parameter files changed. Prepare a new batch.");
        }
        var executableHash = Hash(m.Configuration.Executable);
        if (m.ExecutableSHA256 is not null && m.ExecutableSHA256 != executableHash)
            throw new InvalidDataException("Native executable changed since preparation. Prepare a new batch with the new build.");
        m.ExecutableSHA256 = executableHash;
        ++m.Attempts;
        m.ReportDirectory = m.Attempts == 1 ? "reports" : $"reports-attempt-{m.Attempts:D3}";
        var reports = Path.Combine(directory, m.ReportDirectory);
        if (Directory.Exists(reports)) throw new IOException("Report directory already exists; refusing to overwrite.");
        m.StartedUTC = DateTimeOffset.UtcNow; m.FinishedUTC = null; m.Error = null; m.Status = "Running";
        Save(directory, m);
        try
        {
            var exit = await executor.ExecuteAsync(m.Configuration, parameterDirectory, reports,
                Path.Combine(directory, $"native-attempt-{m.Attempts:D3}.log"), cancellation);
            if (exit != 0) throw new InvalidOperationException($"Native backtester exited with code {exit}; see the native log.");
            var results = ResultReader.Read(Path.Combine(reports, "results.csv"));
            if (results.Count != m.Candidates.Count) throw new InvalidDataException("Native results omitted candidates.");
            var expectedIds = Enumerable.Range(1, m.Candidates.Count).Select(i => $"run-{i:D4}").ToHashSet(StringComparer.Ordinal);
            if (!expectedIds.SetEquals(results.Select(r => r.RunId))) throw new InvalidDataException("Unexpected native result IDs.");
            // Native output is already ranked by training profit. Preserve that ordering, never rank by test data.
            File.WriteAllText(Path.Combine(reports, "top-candidates.json"),
                JsonSerializer.Serialize(results.Take(10), SearchConfiguration.Json));
            m.Status = "Completed";
            Console.WriteLine($"Results: {Path.Combine(reports, "results.csv")}");
        }
        catch (Exception ex)
        {
            m.Status = ex is OperationCanceledException ? "Cancelled" : "Failed";
            m.Error = ex.Message;
            throw;
        }
        finally
        {
            m.FinishedUTC = DateTimeOffset.UtcNow;
            Save(directory, m);
        }
    }
}
