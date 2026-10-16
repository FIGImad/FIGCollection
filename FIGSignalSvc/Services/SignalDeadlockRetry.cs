using Microsoft.Data.SqlClient;

namespace FIGSignalExSvc.Services;

internal static class SignalDeadlockRetry
{
    // Only 1205 guarantees that SQL Server aborted the victim transaction.
    // Timeouts and connection failures can have an uncertain commit outcome.
    internal static bool IsDeadlock(Exception error) =>
        error is SqlException sql && sql.Errors.Cast<SqlError>().Any(e => e.Number == 1205);

    internal static void Execute(Action transaction, Action<int, int, Exception> onRetry,
        Action<int>? delay = null)
    {
        delay ??= Thread.Sleep;
        const int maxRetries = 3;
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                transaction();
                return;
            }
            catch (Exception ex) when (attempt < maxRetries && IsDeadlock(ex))
            {
                // The attempt must dispose its transaction/connection before returning here.
                int milliseconds = (100 << attempt) + Random.Shared.Next(0, 101);
                onRetry(attempt + 1, milliseconds, ex);
                delay(milliseconds);
            }
        }
    }
}
