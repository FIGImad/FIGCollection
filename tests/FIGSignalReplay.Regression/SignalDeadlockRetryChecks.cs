using System.Reflection;
using FIGSignalExSvc.Services;
using Microsoft.Data.SqlClient;

static class SignalDeadlockRetryChecks
{
    public static void Run(Action<bool, string> check)
    {
        var deadlock = SqlError(1205);
        check(SignalDeadlockRetry.IsDeadlock(deadlock), "SQL 1205 is recognized as a deadlock victim");
        foreach (var number in new[] { -2, 1222, 2601 })
        {
            int calls = 0;
            var failure = SqlError(number);
            try
            {
                SignalDeadlockRetry.Execute(() => { calls++; throw failure; },
                    (_, _, _) => throw new Exception("Unexpected retry"), _ => { });
            }
            catch (SqlException ex) when (ReferenceEquals(ex, failure)) { }
            check(calls == 1, $"SQL {number} propagates without unsafe transaction replay");
        }

        int attempts = 0, disposed = 0, commits = 0;
        var waits = new List<int>();
        var batches = new List<StudySignalBatch>();
        SignalDeadlockRetry.Execute(() =>
        {
            attempts++;
            try
            {
                // Each attempt loads durable progress afresh. Simulate a row identity
                // assigned before the transaction is deadlocked and rolled back.
                var batch = new StudySignalBatch(["test"], -1, _ => null);
                batch.Process([new FIGCommon.Models.StudyHistoryRS
                {
                    RawTime = 1, Close = 10, rawStudies = new() { ["test_SIDE"] = 1m }
                }], 2);
                batches.Add(batch);
                check(batch.Changes.Single().Id == -1, "Retried batch discards rolled-back signal identity");
                batch.Changes.Single().Id = 99;
                if (attempts < 3) throw deadlock;
                commits++;
            }
            finally { disposed++; }
        }, (_, _, _) => check(disposed == attempts, "Transaction cleanup precedes retry"), waits.Add);
        check(attempts == 3 && commits == 1, "Two deadlock victims recover with one successful commit");
        check(batches.Select(b => b.Changes.Single().Tag).Distinct().Count() == 3,
            "Rolled-back signal state is rebuilt for every attempt");
        check(waits.Count == 2 && waits[0] is >= 100 and <= 200 && waits[1] is >= 200 and <= 300,
            "Retries use bounded increasing delay with jitter");

        attempts = 0;
        waits.Clear();
        bool exhausted = false;
        try
        {
            SignalDeadlockRetry.Execute(() => { attempts++; throw deadlock; }, (_, _, _) => { }, waits.Add);
        }
        catch (SqlException ex) when (ReferenceEquals(ex, deadlock)) { exhausted = true; }
        check(exhausted && attempts == 4 && waits.Count == 3,
            "Persistent deadlock propagates after initial attempt plus three retries");
    }

    // SqlClient exposes no public constructor for server errors. Fabricate only test
    // exception objects; production uses actual SqlException.Errors from the server.
    private static SqlException SqlError(int number)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var ctor = typeof(SqlError).GetConstructors(flags).First();
        var args = ctor.GetParameters().Select(p =>
            p.Name == "infoNumber" ? (object)number :
            p.ParameterType == typeof(string) ? "test" :
            p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null).ToArray();
        var error = (SqlError)ctor.Invoke(args);
        var errors = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), true)!;
        typeof(SqlErrorCollection).GetMethod("Add", flags)!.Invoke(errors, [error]);
        var factory = typeof(SqlException).GetMethod("CreateException", BindingFlags.Static | BindingFlags.NonPublic,
            null, [typeof(SqlErrorCollection), typeof(string)], null)!;
        return (SqlException)factory.Invoke(null, [errors, "16.0"])!;
    }
}
