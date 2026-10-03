using FIGOptimizationRunner.Configuration;

namespace FIGOptimizationRunner.Execution;

public interface IBatchExecutor
{
    Task<int> ExecuteAsync(SearchConfiguration configuration, string parameterDirectory, string reportDirectory,
        string logPath, CancellationToken cancellation);
}
