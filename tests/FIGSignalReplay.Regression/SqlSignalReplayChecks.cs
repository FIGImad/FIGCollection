using System.Text.Json;
using System.Text.RegularExpressions;
using FIGCommon.DataAccess;
using FIGCommon.Models;
using FIGSignalExSvc.Publication;
using FIGSignalExSvc.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

static class SqlSignalReplayChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        const string master = @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;TrustServerCertificate=true";
        var name = "FIGSignalReplayTest_" + Guid.NewGuid().ToString("N");
        var cs = new SqlConnectionStringBuilder(master) { InitialCatalog = name }.ConnectionString;
        async Task<object?> Sql(string connectionString, string sql)
        {
            await using var connection = new SqlConnection(connectionString); await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 60 };
            return await command.ExecuteScalarAsync();
        }
        Task<object?> Query(string sql) => Sql(cs, sql);
        async Task Migration(string file) => await Query(await File.ReadAllTextAsync(Path.Combine("FIGCommon/DataSchema", file)));
        await Sql(master, $"CREATE DATABASE [{name}]");
        try
        {
            var schema = await File.ReadAllTextAsync("FIGCommon/DataSchema/FIGAutoTrader_Schema.sql");
            schema = Regex.Replace(schema, @"^USE FIGAutoTrader\s*\r?\nGO\s*\r?\n", "", RegexOptions.IgnoreCase);
            if (Regex.IsMatch(schema, @"^\s*(USE|CREATE DATABASE|ALTER DATABASE)\b", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                throw new Exception("Unexpected database management in schema fixture");
            foreach (var batch in Regex.Split(schema, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                if (!string.IsNullOrWhiteSpace(batch)) await Query(batch);
            int id = Convert.ToInt32(await Query("""
                INSERT dbo.StudyCol(TickerId,IntervalId,ColType,ColParams,Enabled) VALUES(1,'1','test','{}',1);
                DECLARE @Id int=CONVERT(int,SCOPE_IDENTITY());
                INSERT dbo.StrategyConfig(Strategy,StudyColId) VALUES('replay-test',@Id);
                SELECT @Id;
                """));
            await Migration("study_history_checkpoints.sql");
            await Migration("signal_publication_source.sql");
            var publication = new SignalPublicationStore(() => cs);
            await publication.InitializeAsync("isolated-replay-test", default);
            var clock = new TestClock();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = cs, ["SingalProcessing:TimeOffset"] = "0"
            }).Build();
            using var provider = new ServiceCollection().AddLogging().AddSingleton<IConfiguration>(config)
                .AddSingleton<TimeProvider>(clock).BuildServiceProvider();
            MainRepo.Initialize(provider);
            var collection = MainRepo.GetStudyCol(id)!;
            long Time(int bar) => 1_700_000_040L + bar * 60L;
            StudyHistoryRS Row(int bar, decimal side) => new()
            {
                StudyColId = id, RawTime = Time(bar), Open = 100, High = 102, Low = 98, Close = 101,
                Volume = 1000, rawStudies = new() { ["replay-test_SIDE"] = side },
                Studies = JsonSerializer.Serialize(new Dictionary<string, decimal> { ["replay-test_SIDE"] = side })
            };
            clock.Now = DateTimeOffset.FromUnixTimeSeconds(Time(151));
            var historyStore = new StudyCheckpointStore(() => cs);
            void Process(List<StudyHistoryRS> rows, bool fail = false, Barrier? barrier = null)
            {
                var tail = MainRepo.GetTopStudyHistory(id, 1).FirstOrDefault()?.RawTime;
                var signals = new StudySignal(collection, provider); // each call simulates a fresh process
                if (barrier != null && !barrier.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
                signals.ProcessSignals(rows, false,
                    saveHistory: tx => historyStore.SaveHistoryBatch(id, rows, [], tail, tx),
                    saveCheckpoint: fail ? _ => throw new IOException("Simulated failure before commit") : null);
            }
            try { Process([Row(1, 1)]); throw new Exception("Expected missing migration rejection"); }
            catch (SqlException ex) when (ex.Number == 51020) { check(true, "Missing replay-protection migration prevents signal/history writes"); }
            await Migration("study_signal_progress.sql"); await Migration("study_signal_progress.sql");
            check(Convert.ToInt64(await Query($"SELECT ProcessedThrough FROM dbo.StudySignalProgress WHERE StudyColId={id}")) == -1,
                "Signal progress migration is repeatable and initializes an empty collection");
            var rows = Enumerable.Range(1, 150).Select(i => Row(i, i is >= 35 and < 40 || i >= 50 ? 1 : 0)).ToList();
            Process(rows);
            int signalCount = Convert.ToInt32(await Query("SELECT COUNT(*) FROM dbo.Signal WHERE Strategy='replay-test'"));
            int outboxCount = Convert.ToInt32(await Query("SELECT COUNT(*) FROM dbo.SignalPublicationOutbox"));
            string before = Convert.ToString(await Query("SELECT Id,Tag,Status,Side,StartTime,StopTime,LastUpdated FROM dbo.Signal WHERE Strategy='replay-test' ORDER BY Id FOR JSON PATH"))!;
            Process(rows.Skip(87).Select(r => Row((int)((r.RawTime - Time(0)) / 60), -1)).ToList());
            check(Convert.ToString(await Query("SELECT Id,Tag,Status,Side,StartTime,StopTime,LastUpdated FROM dbo.Signal WHERE Strategy='replay-test' ORDER BY Id FOR JSON PATH")) == before,
                "Actual StudySignal restart regenerates history without changing any existing signal");
            check(Convert.ToInt32(await Query("SELECT COUNT(*) FROM dbo.SignalPublicationOutbox")) == outboxCount,
                "Replay creates no extra trade-publication messages");
            check(Convert.ToInt32(await Query($"SELECT COUNT(*) FROM dbo.StudyHistory WHERE StudyColId={id}")) == 150,
                "Signal suppression does not prevent history regeneration");

            // Deleting calculation history cannot erase the independently persisted signal frontier.
            await Query($"DELETE dbo.StudyHistory WHERE StudyColId={id} AND RawTime>{Time(50)}");
            Process(rows.Skip(27).ToList());
            check(Convert.ToInt32(await Query("SELECT COUNT(*) FROM dbo.Signal WHERE Strategy='replay-test'")) == signalCount
                && Convert.ToInt32(await Query("SELECT COUNT(*) FROM dbo.SignalPublicationOutbox")) == outboxCount,
                "Rebuilding 100 deleted SQL history bars produces no additional signals or publications");

            clock.Now = DateTimeOffset.FromUnixTimeSeconds(Time(152));
            Process([Row(151, -1)]);
            int afterNew = Convert.ToInt32(await Query("SELECT COUNT(*) FROM dbo.SignalPublicationOutbox"));
            Process([Row(151, 1)]);
            check(Convert.ToInt32(await Query("SELECT COUNT(*) FROM dbo.Signal WHERE Strategy='replay-test'")) == signalCount + 1
                && Convert.ToInt32(await Query("SELECT COUNT(*) FROM dbo.SignalPublicationOutbox")) == afterNew,
                "One genuinely new bar creates one new signal; a repeated commit cannot create another");

            clock.Now = DateTimeOffset.FromUnixTimeSeconds(Time(153));
            try { Process([Row(152, 0)], fail: true); throw new Exception("Expected rollback"); }
            catch (IOException) { }
            check(Convert.ToInt64(await Query($"SELECT ProcessedThrough FROM dbo.StudySignalProgress WHERE StudyColId={id}")) == Time(151)
                && Convert.ToInt32(await Query("SELECT COUNT(*) FROM dbo.SignalPublicationOutbox")) == afterNew
                && Convert.ToInt64(await Query($"SELECT MAX(RawTime) FROM dbo.StudyHistory WHERE StudyColId={id}")) == Time(151),
                "Rollback removes signal changes, publication messages, history and progress atomically");
            Process([Row(152, 0)]);
            int afterRetry = Convert.ToInt32(await Query("SELECT COUNT(*) FROM dbo.SignalPublicationOutbox"));
            Process([Row(152, 1)]);
            check(Convert.ToInt32(await Query("SELECT COUNT(*) FROM dbo.SignalPublicationOutbox")) == afterRetry,
                "Restart cannot reopen a signal already closed on the last processed bar");

            // Both workers calculate against the same history tail before either transaction begins.
            clock.Now = DateTimeOffset.FromUnixTimeSeconds(Time(154));
            using (var barrier = new Barrier(2))
            {
                async Task Worker() => await Task.Run(() =>
                {
                    try { Process([Row(153, 1)], barrier: barrier); }
                    catch (SqlException ex) when (ex.Number == 51002) { Process([Row(153, 1)]); }
                });
                await Task.WhenAll(Worker(), Worker());
            }
            check(Convert.ToInt32(await Query($"SELECT COUNT(*) FROM dbo.Signal WHERE Strategy='replay-test' AND StartTime={Time(153)}")) == 1
                && Convert.ToInt32(await Query("SELECT COUNT(*) FROM dbo.SignalPublicationOutbox")) == afterRetry + 1,
                "Two concurrent workers create one signal and one publication for the new bar");

            // A pending forming bar is not lost when the service restarts or the migration is rerun.
            clock.Now = DateTimeOffset.FromUnixTimeSeconds(Time(154));
            Process([Row(154, -1)]);
            await Migration("study_signal_progress.sql");
            check(Convert.ToInt64(await Query($"SELECT ProcessedThrough FROM dbo.StudySignalProgress WHERE StudyColId={id}")) == Time(153),
                "A forming bar and migration rerun cannot advance durable signal progress");
            clock.Now = DateTimeOffset.FromUnixTimeSeconds(Time(155));
            Process([Row(154, -1)]); Process([Row(154, 1)]);
            check(Convert.ToInt32(await Query($"SELECT COUNT(*) FROM dbo.Signal WHERE Strategy='replay-test' AND StartTime={Time(154)}")) == 1,
                "A previously forming bar closes across restart and creates its signal exactly once");

            // Separate legacy fixture exercises conservative initialization from retained signals.
            int legacyId = Convert.ToInt32(await Query("""
                INSERT dbo.StudyCol(TickerId,IntervalId,ColType,ColParams,Enabled) VALUES(1,'1','test','{}',1);
                DECLARE @Id int=CONVERT(int,SCOPE_IDENTITY());
                INSERT dbo.StrategyConfig VALUES('legacy-replay-test',@Id);
                INSERT dbo.StudyHistory(StudyColId,RawTime,[Open],High,Low,[Close],Volume,Studies) VALUES(@Id,100,1,1,1,1,1,'{}');
                INSERT dbo.Signal(Strategy,Tag,Status,Side,StartTime,StopTime,StartPrice,StopPrice,LastUpdated)
                    VALUES('legacy-replay-test',CONVERT(varchar(50),NEWID()),'STOP',1,110,120,1,1,120);
                SELECT @Id;
                """));
            await Migration("study_signal_progress.sql");
            check(Convert.ToInt64(await Query($"SELECT ProcessedThrough FROM dbo.StudySignalProgress WHERE StudyColId={legacyId}")) == 120,
                "Upgrade initialization preserves signals issued beyond a deleted history tail");
            check(Convert.ToInt32(await Query("SELECT COUNT(*) FROM (SELECT Strategy,StartTime FROM dbo.Signal GROUP BY Strategy,StartTime HAVING COUNT(*)>1) d")) == 0,
                "No strategy/bar signal identities were duplicated by replay, retries or concurrent workers");
        }
        finally
        {
            SqlConnection.ClearAllPools();
            await Sql(master, $"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]");
        }
    }

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; }
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
