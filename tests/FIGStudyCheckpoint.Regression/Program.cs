using System.Text.Json;
using FIG.Studies;
using FIGCommon.Models;
using FIGSignalExSvc.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Data.SqlClient;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
    Console.WriteLine($"PASS {name}");
}
string Json<T>(T value) => JsonSerializer.Serialize(value, StudyStateJson.Options);
CollectionCheckpoint RoundTrip(CollectionCheckpoint state)
    => JsonSerializer.Deserialize<CollectionCheckpoint>(Json(state), StudyStateJson.Options)!;
void Reject(Action action, string name)
{
    try { action(); }
    catch (InvalidDataException) { Check(true, name); return; }
    throw new Exception($"Expected rejection: {name}");
}

var prices = Enumerable.Range(0, 310).Select(i =>
{
    decimal value = 100 + (decimal)Math.Sin(i * 0.39) * 8 + i * 0.03m;
    return new PriceDataRS { RawTime = 1_700_000_040 + i * 60,
        PriceDate = DateTimeOffset.FromUnixTimeSeconds(1_700_000_040 + i * 60).UtcDateTime, Open = value - 1,
        High = value + 2, Low = value - 3, Close = value, Volume = 1000 + i };
}).ToList();

foreach (var split in new[] { 1, 4, 5, 6, 12, 80, 250 })
{
    var uninterrupted = new SharedCollection();
    uninterrupted.Calc(prices.Take(split).ToList(), 0);
    var snapshot = RoundTrip(uninterrupted.CaptureCheckpoint());
    var restarted = new SharedCollection();
    restarted.RestoreCheckpoint(snapshot);
    Check(Json(restarted.CaptureCheckpoint()) == Json(snapshot), $"Complete state round trip at bar {split}");
    var tail = prices.Skip(split).ToList();
    Check(Json(uninterrupted.Calc(tail, 0)) == Json(restarted.Calc(tail, 0)), $"All seven studies resume identically at bar {split}");
    Check(Json(uninterrupted.CaptureCheckpoint()) == Json(restarted.CaptureCheckpoint()), $"Final internal states match at bar {split}");
}

var baseline = new SharedCollection();
baseline.Calc(prices.Take(250).ToList(), 0);
var saved = RoundTrip(baseline.CaptureCheckpoint());
var restored = new SharedCollection();
restored.RestoreCheckpoint(saved);
var revised = prices.Skip(245).Take(10).Select(p => new PriceDataRS(p)).ToList();
foreach (var p in revised.Where(p => p.RawTime >= prices[247].RawTime)) { p.Close += 0.75m; p.High += 0.75m; }
var continued = baseline.Calc(revised, 3);
Check(Json(continued) == Json(restored.Calc(revised, 3)), "Recent-bar corrections match after restart");
var fullReplay = new SharedCollection();
var correctedPrices = prices.Take(245).Concat(revised).ToList();
var expected = fullReplay.Calc(correctedPrices, 0).Where(p => p.Price.RawTime >= prices[247].RawTime).ToList();
Check(Json(continued) == Json(expected), "Corrected outputs match full replay");
// Repeating an in-progress bar must roll back its parameters, not accumulate it twice.
var repeated = restored.Calc(revised, 3);
var repeatRestored = new SharedCollection();
repeatRestored.RestoreCheckpoint(RoundTrip(restored.CaptureCheckpoint()));
Check(Json(restored.Calc(revised, 3)) == Json(repeatRestored.Calc(revised, 3)), "Repeated current bar survives another restart");

Reject(() => new SharedCollection(6).RestoreCheckpoint(saved), "Changed parameters rejected");
Reject(() => new SharedCollection().RestoreCheckpoint(saved with { FormatVersion = 999 }), "Unknown schema rejected");
Reject(() => new SharedCollection().RestoreCheckpoint(saved with { RawTime = 1 }), "Wrong bar timestamp rejected");
var damaged = saved.Studies.ToList();
damaged[0] = damaged[0] with { Identity = "old-build" };
Reject(() => new SharedCollection().RestoreCheckpoint(saved with { Studies = damaged }), "Changed study build rejected");
var incomplete = saved.Studies.ToList();
var missingFields = incomplete[0].Parameters.ToArray();
missingFields[0] = JsonSerializer.SerializeToElement(new { Price = new { RawTime = saved.RawTime } });
incomplete[0] = incomplete[0] with { Parameters = missingFields };
Reject(() => new SharedCollection().RestoreCheckpoint(saved with { Studies = incomplete }), "Missing parameter fields rejected");
Check(!new UnknownMA().SupportsCheckpoint, "Unknown derived study cannot inherit checkpoint support accidentally");
Check(!new UnknownCollection().SupportsCheckpoint, "Unknown collection must opt in explicitly");

var oneBar = new SharedCollection(); oneBar.RestoreCheckpoint(saved);
var oneBarReference = new SharedCollection(); oneBarReference.Calc(prices.Take(250).ToList(), 0);
Check(Json(oneBar.Calc(new() { prices[250] }, 0)) == Json(oneBarReference.Calc(new() { prices[250] }, 0)), "One new bar needs no warm-up replay");
// Discard uncommitted advanced state and resume again from the last committed snapshot.
var failedBatch = new SharedCollection(); failedBatch.RestoreCheckpoint(saved);
failedBatch.Calc(revised, 3);
var afterFailure = new SharedCollection(); afterFailure.RestoreCheckpoint(saved);
var clean = new SharedCollection(); clean.RestoreCheckpoint(saved);
Check(Json(afterFailure.Calc(prices.Skip(250).ToList(), 0)) == Json(clean.Calc(prices.Skip(250).ToList(), 0)), "Discarding a failed batch preserves committed calculation state");

var longPrices = Enumerable.Range(0, 1200).Select(i =>
{
    var p = new PriceDataRS(prices[i % prices.Count]);
    p.RawTime = prices[0].RawTime + i * 60;
    p.PriceDate = DateTimeOffset.FromUnixTimeSeconds(p.RawTime).UtcDateTime;
    return p;
}).ToList();
var longWindow = new SharedCollection(720); longWindow.Calc(longPrices.Take(1000).ToList(), 0);
var longRestart = new SharedCollection(720); longRestart.RestoreCheckpoint(RoundTrip(longWindow.CaptureCheckpoint()));
Check(Json(longWindow.Calc(longPrices.Skip(1000).ToList(), 0)) == Json(longRestart.Calc(longPrices.Skip(1000).ToList(), 0)), "Long rolling window resumes without a 500-bar approximation");

foreach (var pageSize in new[] { 1, 7, 100 })
{
    var gappedPrices = prices.Where((_, i) => i < 110 || i > 130).ToList();
    var paged = new SharedCollection();
    var replayed = StudyHistoryReplay.Calculate(paged,
        cursor => gappedPrices.Where(p => p.RawTime >= cursor).Take(pageSize).ToList(), 300, 0);
    var whole = new SharedCollection();
    var allAtOnce = whole.Calc(ExecStudySet.CompilePriceEx(gappedPrices, 300), 0);
    Check(Json(replayed) == Json(allAtOnce), $"Aggregate replay preserves page boundaries and market gaps (page {pageSize})");
    Check(Json(paged.CaptureCheckpoint()) == Json(whole.CaptureCheckpoint()), $"Paged replay restores complete state (page {pageSize})");
}
Reject(() => StudyHistoryReplay.Calculate(new SharedCollection(), _ => new() { prices[0] }, 60, 0), "Non-advancing replay rejected");

var barCheckpoints = new List<CollectionCheckpoint>();
var perBar = new SharedCollection();
var perBarOutputs = perBar.Calc(prices, 0, bar =>
{
    var state = RoundTrip(perBar.CaptureCheckpoint());
    if (state.RawTime != bar.Price.RawTime) throw new Exception("Checkpoint captured at the wrong bar");
    barCheckpoints.Add(state);
});
Check(barCheckpoints.Count == prices.Count, "One checkpoint captured at each bar, including bars inside a batch");
foreach (var index in new[] { 0, 4, 100, 209, 309 })
{
    var resumed = new SharedCollection(); resumed.RestoreCheckpoint(barCheckpoints[index]);
    Check(Json(resumed.Calc(prices.Skip(index + 1).ToList(), 0)) == Json(perBarOutputs.Skip(index + 1).ToList()),
        $"Per-bar checkpoint resumes identically after bar {index + 1}");
}
var replayCheckpoints = new List<CollectionCheckpoint>();
var replayCollection = new SharedCollection();
var retainedOutputs = StudyHistoryReplay.Calculate(replayCollection,
    cursor => prices.Where(p => p.RawTime >= cursor).Take(7).ToList(), 300, prices[200].RawTime,
    _ => replayCheckpoints.Add(RoundTrip(replayCollection.CaptureCheckpoint())));
Check(replayCheckpoints.Select(c => c.RawTime).SequenceEqual(retainedOutputs.Select(b => b.Price.RawTime)),
    "Paged replay captures exactly the retained aggregate bars, excluding warmup");

foreach (var limits in new[] { (Bars: 7, Bytes: long.MaxValue), (Bars: 250, Bytes: 200_000L), (Bars: 250, Bytes: 1L) })
{
    var collection = new SharedCollection();
    var committed = new List<BarStudy>();
    var checkpoints = new List<CollectionCheckpoint>();
    int pagesRead = 0, pagesAtFirstCommit = 0;
    bool validLimits = true;
    var stream = StudyHistoryReplay.ReadBars(cursor =>
    {
        pagesRead++;
        return prices.Where(p => p.RawTime >= cursor).Take(20).ToList();
    }, 60);
    StudyCheckpointProcessor.CalculateAndCommit(collection, stream, 0, 0, limits.Bars, limits.Bytes, (bars, batch) =>
    {
        if (pagesAtFirstCommit == 0) pagesAtFirstCommit = pagesRead;
        validLimits &= bars.Count <= limits.Bars && (batch.PayloadBytes <= limits.Bytes || bars.Count == 1);
        committed.AddRange(bars);
        checkpoints.AddRange(batch.Items.Select(p => JsonSerializer.Deserialize<CollectionCheckpoint>(p.Json, StudyStateJson.Options)!));
    });
    Check(validLimits, $"Commit batches obey count/byte limits ({limits.Bars}, {limits.Bytes})");
    Check(pagesAtFirstCommit < pagesRead, "First commit occurs before historical replay finishes");
    Check(Json(committed) == Json(perBarOutputs), "Streaming commits preserve all calculated outputs");
    Check(Json(checkpoints) == Json(barCheckpoints), "Streaming commits save the exact state of each bar");
}

var interrupted = new SharedCollection();
CollectionCheckpoint? lastCommitted = null;
int attempts = 0, failurePages = 0;
try
{
    StudyCheckpointProcessor.CalculateAndCommit(interrupted, StudyHistoryReplay.ReadBars(cursor =>
    {
        failurePages++;
        return prices.Where(p => p.RawTime >= cursor).Take(10).ToList();
    }, 60), 0, 0, 7, long.MaxValue, (_, batch) =>
    {
        if (++attempts == 2) throw new IOException("Simulated failed commit");
        lastCommitted = JsonSerializer.Deserialize<CollectionCheckpoint>(batch.Items[^1].Json, StudyStateJson.Options);
    });
    throw new Exception("Expected interrupted batch");
}
catch (IOException) { }
Check(lastCommitted!.RawTime == prices[6].RawTime && failurePages == 2,
    "Failed batch leaves earlier commits usable and stops reading further pages");
var retry = new SharedCollection(); retry.RestoreCheckpoint(lastCommitted!);
var retryOutputs = new List<BarStudy>();
StudyCheckpointProcessor.CalculateAndCommit(retry, prices.Skip(4), 0, 3, 7, long.MaxValue,
    (bars, _) => retryOutputs.AddRange(bars));
Check(Json(retryOutputs) == Json(perBarOutputs.Skip(4).ToList()), "Interrupted replay resumes with corrections from the last committed batch");

var warmup = new SharedCollection();
var warmupOutputs = new List<BarStudy>();
StudyCheckpointProcessor.CalculateAndCommit(warmup, prices, prices[200].RawTime, 0, 7, 200_000,
    (bars, _) => warmupOutputs.AddRange(bars));
Check(Json(warmupOutputs) == Json(perBarOutputs.Skip(200).ToList()), "Warmup reconstructs full state without writing older rows");

SparseCheckpointChecks.Run(() => new SharedCollection(), prices, Check);

var counted = new CountingCollection();
StudyCheckpointProcessor.CalculateAndCommit(counted, prices, 0, 0, 7, long.MaxValue,
    (_, _) => { }, cadence: new(30));
Check(counted.Captures == (prices.Count - 1) / 30, "Capture/serialization work runs only at scheduled checkpoints");
foreach (int interval in new[] { 1, 7, 500 })
{
    var times = new List<long>();
    StudyCheckpointProcessor.CalculateAndCommit(new SharedCollection(), prices, 0, 0, 7, long.MaxValue,
        (_, batch) => times.AddRange(batch.Items.Select(p => p.RawTime)), cadence: new(interval));
    Check(times.SequenceEqual(prices.Where((_, i) => (i + 1) % interval == 0 && i + 1 < prices.Count).Select(p => p.RawTime)),
        $"Configurable checkpoint interval {interval} counts study bars independently of batch size");
}
var gappedInput = prices.Where((_, i) => i < 50 || i > 70).ToList();
var gappedTimes = new List<long>();
StudyCheckpointProcessor.CalculateAndCommit(new SharedCollection(), gappedInput, 0, 0, 7, long.MaxValue,
    (_, batch) => gappedTimes.AddRange(batch.Items.Select(p => p.RawTime)), cadence: new(30));
Check(gappedTimes.SequenceEqual(gappedInput.Where((_, i) => (i + 1) % 30 == 0 && i + 1 < gappedInput.Count).Select(p => p.RawTime)),
    "Market gaps do not count nonexistent bars toward the checkpoint interval");

AdaptiveCheckpointChecks.Run(() => new SharedCollection(), longPrices, Check);

var validationStore = new StudyCheckpointStore(() => throw new Exception("Validation must not access SQL"));
var validationRows = new[] { new StudyHistoryRS { StudyColId = 1, RawTime = saved.RawTime } };
var validationPayload = StudyCheckpointPayload.Capture(saved);
Reject(() => validationStore.SaveHistoryBatch(1, validationRows, [validationPayload, validationPayload], saved.RawTime, null!),
    "Sparse writer rejects duplicate checkpoint timestamps before SQL");
Reject(() => validationStore.SaveHistoryBatch(1, validationRows, [validationPayload with { RawTime = saved.RawTime + 60 }], saved.RawTime, null!),
    "Sparse writer rejects checkpoints without a matching history row before SQL");
Reject(() => validationStore.SaveHistoryBatch(2, validationRows, [], saved.RawTime, null!),
    "Sparse writer rejects history for a different collection before SQL");
var validationPlan = StudyCheckpointReplayPlan.FromCheckpoint(saved, prices[^1].RawTime, 3);
Reject(() => validationPlan.RequireStart(prices.Where(p => p.RawTime > validationPlan.Start)).ToList(),
    "Restart refuses missing checkpoint correction inputs before calculating");
Reject(() => validationPlan.RequireStart([]).ToList(), "Restart refuses an empty source-price history");
Check(validationPlan.RequireStart(prices.Where(p => p.RawTime >= validationPlan.Start)).First().RawTime == validationPlan.Start,
    "Restart includes all checkpoint correction inputs");

if (args.Contains("--sql"))
{
    var name = "FIGCheckpointTest_" + Guid.NewGuid().ToString("N");
    const string master = @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;TrustServerCertificate=true";
    void Sql(string cs, string sql)
    {
        using var connection = new SqlConnection(cs); connection.Open();
        using var command = new SqlCommand(sql, connection); command.ExecuteNonQuery();
    }
    Sql(master, $"CREATE DATABASE [{name}]");
    var cs = new SqlConnectionStringBuilder(master) { InitialCatalog = name }.ConnectionString;
    try
    {
        Sql(cs, """
            CREATE TABLE dbo.StudyCol(Id int PRIMARY KEY); INSERT dbo.StudyCol VALUES(1),(2),(3),(4),(5),(6),(7),(8);
            CREATE TABLE dbo.StudyHistory(
                Id int IDENTITY PRIMARY KEY, StudyColId int NOT NULL, RawTime int NOT NULL,
                [Open] decimal(18,4) NOT NULL, High decimal(18,4) NOT NULL, Low decimal(18,4) NOT NULL,
                [Close] decimal(18,4) NOT NULL, Volume int NOT NULL, Studies varchar(max) NOT NULL);
            """);
        var migration = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "FIGCommon/DataSchema/study_history_checkpoints.sql"));
        var legacyMigration = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "FIGCommon/DataSchema/study_checkpoints.sql"));
        Sql(cs, migration); Sql(cs, migration);
        var store = new StudyCheckpointStore(() => cs);
        Check(store.Load(1) == null, "No checkpoint before first commit");
        using var connection = new SqlConnection(cs); connection.Open();
        void InsertHistory(int col, long time, SqlTransaction tx)
            => FIGCommon.DataAccess.MainRepo.BulkInsertStudyHistory(new()
            {
                new StudyHistoryRS { StudyColId = col, RawTime = time, Studies = "{}" }
            }, tx);
        int Scalar(string query)
        {
            using var command = new SqlCommand(query, connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }
        SqlException ExpectSqlError(Action action, int number, string description)
        {
            try { action(); }
            catch (SqlException ex) when (ex.Number == number) { Check(true, description); return ex; }
            throw new Exception("Expected SQL rejection: " + description);
        }
        void InsertLegacy(int col, CollectionCheckpoint checkpoint)
        {
            using var command = new SqlCommand("INSERT dbo.StudyCheckpoint(StudyColId,RawTime,Payload) VALUES(@Id,@Time,@Payload)", connection);
            command.Parameters.AddWithValue("@Id", col);
            command.Parameters.AddWithValue("@Time", checkpoint.RawTime);
            command.Parameters.AddWithValue("@Payload", Json(checkpoint));
            command.ExecuteNonQuery();
        }
        using (var tx = connection.BeginTransaction()) { InsertHistory(1, saved.RawTime, tx); tx.Commit(); }
        Check(Scalar("SELECT COUNT(*) FROM dbo.StudyHistory WHERE CheckpointState IS NULL") == 1, "Existing bulk insert maps columns after migration");
        using (var tx = connection.BeginTransaction()) { store.Save(1, saved, tx); tx.Rollback(); }
        Check(store.Load(1) == null, "Rollback leaves no checkpoint");
        using (var tx = connection.BeginTransaction()) { store.Save(1, saved, tx); tx.Commit(); }
        Check(Json(store.Load(1)) == Json(saved), "Committed SQL checkpoint round trip");
        using (var corrupt = new SqlCommand("UPDATE dbo.StudyHistory SET CheckpointState=@Payload WHERE StudyColId=1", connection))
        {
            corrupt.Parameters.AddWithValue("@Payload", Json(saved with { RawTime = saved.RawTime - 60 }));
            corrupt.ExecuteNonQuery();
        }
        Reject(() => store.Load(1), "SQL rejects a payload whose timestamp differs from its checkpoint row");
        using (var tx = connection.BeginTransaction()) { store.Save(1, saved, tx); tx.Commit(); }
        Sql(cs, "CREATE UNIQUE INDEX UX_StudyHistory_Checkpoint ON dbo.StudyHistory(StudyColId) WHERE CheckpointSavedAt IS NOT NULL");
        ExpectSqlError(() => store.Load(1), 51003, "Obsolete single-checkpoint index fails before calculation");
        Sql(cs, migration); Sql(cs, migration);
        Check(Json(store.Load(1)) == Json(saved) && Scalar("SELECT COUNT(*) FROM sys.indexes WHERE name='UX_StudyHistory_Checkpoint'") == 0,
            "Upgrade removes single-checkpoint constraint and preserves saved payload");
        var advanced = baseline.CaptureCheckpoint();
        using (var tx = connection.BeginTransaction())
        {
            InsertHistory(1, advanced.RawTime, tx); store.Save(1, advanced, tx); tx.Rollback();
        }
        Check(Json(store.Load(1)) == Json(saved) && Scalar("SELECT COUNT(*) FROM dbo.StudyHistory") == 1,
            "Rollback restores previous checkpoint and removes new history row");
        var fromDb = new SharedCollection(); fromDb.RestoreCheckpoint(store.Load(1)!);
        var fromMemory = new SharedCollection(); fromMemory.RestoreCheckpoint(saved);
        Check(Json(fromDb.Calc(prices.Skip(250).ToList(), 0)) == Json(fromMemory.Calc(prices.Skip(250).ToList(), 0)), "Resume from database matches memory");
        using (var tx = connection.BeginTransaction())
        {
            InsertHistory(1, advanced.RawTime, tx); store.Save(1, advanced, tx); tx.Commit();
        }
        Check(Json(store.Load(1)) == Json(advanced)
            && Scalar("SELECT COUNT(*) FROM dbo.StudyHistory WHERE CheckpointState IS NOT NULL") == 2,
            "Latest checkpoint advances while each earlier bar retains its own state");
        using (var tx = connection.BeginTransaction())
        {
            ExpectSqlError(() => store.Save(1, advanced with { RawTime = advanced.RawTime + 600 }, tx), 51002, "Missing history row rejected");
            tx.Rollback();
        }
        using (var tx = connection.BeginTransaction())
        {
            InsertHistory(1, advanced.RawTime, tx);
            ExpectSqlError(() => store.Save(1, advanced, tx), 51002, "Duplicate history timestamps rejected");
            tx.Rollback();
        }
        Check(Json(store.Load(1)) == Json(advanced), "Rejected saves preserve committed checkpoint");
        ExpectSqlError(() => Sql(cs, """
            INSERT dbo.StudyHistory(StudyColId,RawTime,[Open],High,Low,[Close],Volume,Studies,CheckpointState,CheckpointSavedAt)
            SELECT TOP (1) StudyColId,RawTime,[Open],High,Low,[Close],Volume,Studies,CheckpointState,CheckpointSavedAt
            FROM dbo.StudyHistory WHERE StudyColId=1 AND CheckpointState IS NOT NULL
            """), 2601, "Unique index rejects duplicate checkpoint timestamps, not distinct bars");

        using (var batch = new StudyCheckpointBatch())
        {
            foreach (var state in barCheckpoints) batch.Add(state);
            var times = barCheckpoints.Select(c => c.RawTime).ToHashSet();
            using (var tx = connection.BeginTransaction())
            {
                foreach (var time in times) InsertHistory(4, time, tx);
                batch.Save(store, 4, times, tx);
                tx.Rollback();
            }
            Check(store.Load(4) == null && Scalar("SELECT COUNT(*) FROM dbo.StudyHistory WHERE StudyColId=4") == 0,
                "Rollback removes all per-bar history and checkpoints together");
            using (var tx = connection.BeginTransaction())
            {
                foreach (var time in times) InsertHistory(4, time, tx);
                batch.Save(store, 4, times, tx);
                tx.Commit();
            }
        }
        Check(Scalar("SELECT COUNT(*) FROM dbo.StudyHistory WHERE StudyColId=4 AND CheckpointState IS NOT NULL") == prices.Count,
            "Batch persists a checkpoint on every processed bar");
        Sql(cs, "DELETE FROM dbo.StudyHistory WHERE Id IN (SELECT TOP (100) Id FROM dbo.StudyHistory WHERE StudyColId=4 ORDER BY RawTime DESC)");
        var retained = store.Load(4)!;
        Check(Json(retained) == Json(barCheckpoints[^101]), "Deleting 100 bars loads the checkpoint on the latest surviving bar");
        var recovered = new SharedCollection(); recovered.RestoreCheckpoint(retained);
        Check(Json(recovered.Calc(prices.Skip(prices.Count - 100).ToList(), 0)) == Json(perBarOutputs.TakeLast(100).ToList()),
            "Rebuilding the deleted 100 bars matches uninterrupted outputs");
        var correctedCollection = new SharedCollection(); correctedCollection.RestoreCheckpoint(retained);
        var correctedTail = prices.Skip(207).Take(3).Select(p => new PriceDataRS(p) { Close = p.Close + 1, High = p.High + 1 }).ToList();
        using (var batch = new StudyCheckpointBatch())
        {
            correctedCollection.Calc(correctedTail, 3, _ => batch.Add(correctedCollection.CaptureCheckpoint()));
            using var tx = connection.BeginTransaction();
            batch.Save(store, 4, correctedTail.Select(p => p.RawTime).ToHashSet(), tx);
            tx.Commit();
        }
        Check(Json(store.Load(4)) == Json(correctedCollection.CaptureCheckpoint())
            && Scalar("SELECT COUNT(*) FROM dbo.StudyHistory WHERE StudyColId=4 AND CheckpointState IS NOT NULL") == 210,
            "Corrections replace their per-bar checkpoints while retaining earlier snapshots");
        using (var batch = new StudyCheckpointBatch())
        using (var tx = connection.BeginTransaction())
        {
            batch.Add(saved);
            Reject(() => batch.Save(store, 4, new HashSet<long> { retained.RawTime }, tx), "Checkpoint for an unwritten bar aborts the write");
            tx.Rollback();
        }

        Sql(cs, "CREATE TABLE dbo.CheckpointWriteAudit(RowsWritten int NOT NULL, MissingState int NOT NULL)");
        Sql(cs, """
            CREATE TRIGGER dbo.TestCheckpointWrite ON dbo.StudyHistory AFTER INSERT,UPDATE AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (SELECT 1 FROM inserted WHERE StudyColId=5)
                    INSERT dbo.CheckpointWriteAudit
                    SELECT COUNT(*),SUM(CASE WHEN CheckpointState IS NULL THEN 1 ELSE 0 END)
                    FROM inserted WHERE StudyColId=5;
            END
            """);
        StudyHistoryRS HistoryRow(PriceDataRS p) => new()
        {
            StudyColId = 5, RawTime = p.RawTime, Open = p.Open, High = p.High, Low = p.Low,
            Close = p.Close, Volume = p.Volume, Studies = "{}"
        };
        var newRows = prices.Take(25).Select(HistoryRow).ToList();
        var newPayloads = barCheckpoints.Take(25).Select(StudyCheckpointPayload.Capture).ToList();
        using (var tx = connection.BeginTransaction())
        {
            store.SaveHistoryBatch(5, newRows, newPayloads, null, tx); tx.Rollback();
        }
        Check(store.Load(5) == null && Scalar("SELECT COUNT(*) FROM dbo.CheckpointWriteAudit") == 0,
            "Bulk history/checkpoint rollback includes trigger effects");
        using (var tx = connection.BeginTransaction())
        {
            store.SaveHistoryBatch(5, newRows, newPayloads, null, tx); tx.Commit();
        }
        Check(Scalar("SELECT COUNT(*) FROM dbo.CheckpointWriteAudit") == 1
            && Scalar("SELECT SUM(MissingState) FROM dbo.CheckpointWriteAudit") == 0
            && Scalar("SELECT COUNT(*) FROM dbo.StudyHistory WHERE StudyColId=5 AND CheckpointState IS NOT NULL") == 25,
            "Bulk insert supplies checkpoints with history and fires its trigger once");
        Check(Json(store.Load(5)) == Json(barCheckpoints[24]), "Bulk-inserted payload restores exactly");
        var mixedInsert = prices.Skip(25).Take(5).Select(HistoryRow).ToList();
        var mixedUpdate = newRows.TakeLast(3).ToList();
        using (var tx = connection.BeginTransaction())
        {
            store.SaveHistoryBatch(5, mixedInsert.Concat(mixedUpdate).ToList(),
                barCheckpoints.Skip(22).Take(8).Select(StudyCheckpointPayload.Capture).ToList(), prices[24].RawTime, tx);
            tx.Commit();
        }
        Check(Scalar("SELECT COUNT(*) FROM dbo.CheckpointWriteAudit") == 3
            && Scalar("SELECT SUM(MissingState) FROM dbo.CheckpointWriteAudit") == 0
            && Scalar("SELECT COUNT(*) FROM dbo.StudyHistory WHERE StudyColId=5") == 30,
            "Mixed correction/append uses one update and one insert, retaining earlier bars");
        using (var tx = connection.BeginTransaction())
        {
            var error = ExpectSqlError(() => store.SaveHistoryBatch(5, newRows, newPayloads, prices[24].RawTime, tx), 51002,
                "Bulk writer detects a changed database tail");
            Check(error.Message.Contains("StudyColId=5") && error.Message.Contains($"Expected RawTime={prices[24].RawTime}")
                && error.Message.Contains($"actual RawTime={prices[29].RawTime}"), "Tail conflict reports collection and both timestamps");
            tx.Rollback();
        }
        using (var tx = connection.BeginTransaction())
        {
            Reject(() => store.SaveHistoryBatch(5, newRows, newPayloads.Append(newPayloads[0]).ToList(), prices[29].RawTime, tx),
                "Bulk writer rejects duplicate sparse checkpoint payloads");
            tx.Rollback();
        }

        using (var tx = connection.BeginTransaction())
        {
            store.SaveHistoryBatch(5, newRows, newPayloads, prices[29].RawTime, tx); tx.Commit();
        }
        Check(Scalar("SELECT COUNT(*) FROM dbo.StudyHistory WHERE StudyColId=5") == 30
            && Json(store.Load(5)) == Json(barCheckpoints[29]), "Replaying existing rows updates them without inserting duplicates or moving the tail");

        // A missing earlier timestamp is not evidence of a concurrent writer.
        Sql(cs, $"DELETE FROM dbo.StudyHistory WHERE StudyColId=5 AND RawTime={prices[27].RawTime}");
        var gapRows = prices.Skip(27).Take(4).Select(HistoryRow).ToList();
        var gapPayloads = barCheckpoints.Skip(27).Take(4).Select(StudyCheckpointPayload.Capture).ToList();
        using (var tx = connection.BeginTransaction())
        {
            store.SaveHistoryBatch(5, gapRows, gapPayloads, prices[29].RawTime, tx); tx.Rollback();
        }
        Check(Scalar("SELECT COUNT(*) FROM dbo.StudyHistory WHERE StudyColId=5") == 29
            && Scalar($"SELECT COUNT(*) FROM dbo.StudyHistory WHERE StudyColId=5 AND RawTime={prices[27].RawTime}") == 0
            && Json(store.Load(5)) == Json(barCheckpoints[29]), "Gap repair and append roll back together");
        using (var tx = connection.BeginTransaction())
        {
            store.SaveHistoryBatch(5, gapRows, gapPayloads, prices[29].RawTime, tx); tx.Commit();
        }
        Check(Scalar("SELECT COUNT(*) FROM dbo.StudyHistory WHERE StudyColId=5 AND CheckpointState IS NOT NULL") == 31
            && Scalar($"SELECT COUNT(*) FROM dbo.StudyHistory WHERE StudyColId=5 AND RawTime={prices[27].RawTime} AND CheckpointState IS NOT NULL") == 1
            && Json(store.Load(5)) == Json(barCheckpoints[30]), "Bulk writer fills a gap, corrects existing bars, and appends with per-bar checkpoints");

        using (var tx = connection.BeginTransaction())
        {
            using var delete = new SqlCommand($"DELETE FROM dbo.StudyHistory WHERE StudyColId=5 AND RawTime={prices[30].RawTime}", connection, tx);
            delete.ExecuteNonQuery();
            ExpectSqlError(() => store.SaveHistoryBatch(5, gapRows, gapPayloads, prices[30].RawTime, tx), 51002,
                "A tail deletion during calculation is rejected instead of resurrecting the deleted tail");
            tx.Rollback();
        }
        using (var tx = connection.BeginTransaction())
        {
            InsertHistory(5, newRows[0].RawTime, tx);
            var error = ExpectSqlError(() => store.SaveHistoryBatch(5, newRows, newPayloads, prices[30].RawTime, tx), 51002,
                "Bulk writer rejects ambiguous duplicate history rows");
            Check(error.Message.Contains("Duplicate study history") && error.Message.Contains($"RawTime={newRows[0].RawTime}"),
                "Duplicate error identifies the affected bar");
            tx.Rollback();
        }
        using (var tx = connection.BeginTransaction())
        {
            ExpectSqlError(() => store.SaveHistoryBatch(5, newRows, newPayloads, null, tx), 51002,
                "An expected empty tail cannot overwrite existing history");
            tx.Rollback();
        }
        using (var tx = connection.BeginTransaction())
        {
            var otherRows = newRows.Select(r => new StudyHistoryRS(r) { StudyColId = 6 }).ToList();
            ExpectSqlError(() => store.SaveHistoryBatch(6, otherRows, newPayloads, prices[24].RawTime, tx), 51002,
                "A deleted entire history is detected when a nonempty tail was expected");
            tx.Rollback();
        }
        using (var tx = connection.BeginTransaction())
        {
            foreach (var p in prices.Take(8).Reverse()) InsertHistory(6, p.RawTime, tx);
            var tail = FIGCommon.DataAccess.MainRepo.GetTopStudyHistory(6, 5, tx);
            Check(tail.Select(r => r.RawTime).SequenceEqual(prices.Skip(3).Take(5).Select(p => p.RawTime)),
                "History tail selects the newest five timestamps even when identity order is reversed");
            Check(FIGCommon.DataAccess.MainRepo.GetTopStudyHistory(6, 1, tx).Single().RawTime == prices[7].RawTime,
                "Latest study bar is selected by time rather than identity");
            var existingRows = prices.Take(8).Select(p => new StudyHistoryRS(HistoryRow(p)) { StudyColId = 6 }).ToList();
            store.SaveHistoryBatch(6, existingRows, barCheckpoints.Take(8).Select(StudyCheckpointPayload.Capture).ToList(), tail[^1].RawTime, tx);
            tx.Commit();
        }
        Check(Json(store.Load(6)) == Json(barCheckpoints[7]), "Out-of-order legacy rows accept checkpoint replay without false conflicts");

        // Referenced-price restoration reads the actual SQL OHLCV types, excluding
        // large Studies/CheckpointState columns and rows after the saved bar.
        var sqlPrices = prices.Take(50).Select(p => new PriceDataRS(p)
        {
            Open = decimal.Round(p.Open, 4), High = decimal.Round(p.High, 4),
            Low = decimal.Round(p.Low, 4), Close = decimal.Round(p.Close, 4)
        }).ToList();
        var referenced = new HistoryPriceCollection();
        long? committedTime = null;
        StudyCheckpointProcessor.CalculateAndCommit(referenced, sqlPrices.Take(40), 0, 0, 7, 16 * 1024 * 1024,
            (bars, batch) =>
            {
                var rows = bars.Select(b => new StudyHistoryRS
                {
                    StudyColId = 7, RawTime = b.Price.RawTime, Open = b.Price.Open, High = b.Price.High,
                    Low = b.Price.Low, Close = b.Price.Close, Volume = b.Price.Volume, Studies = "{}"
                }).ToList();
                using var tx = connection.BeginTransaction();
                store.SaveHistoryBatch(7, rows, batch.Items, committedTime, tx);
                tx.Commit(); committedTime = bars[^1].Price.RawTime;
            });
        var referencedSaved = store.Load(7)!;
        var sqlRestored = new HistoryPriceCollection();
        var window = sqlRestored.GetRequiredCheckpointPrices(referencedSaved)!;
        var loadedPrices = store.LoadPrices(7, window);
        sqlRestored.RestoreCheckpoint(referencedSaved, loadedPrices);
        Check(referencedSaved.Prices.Count == 0 && loadedPrices.Count == 24
            && Json(sqlRestored.CaptureCheckpoint()) == Json(referencedSaved), "SQL restores the referenced price window and exact study state");
        Check(Json(referenced.Calc(sqlPrices.Skip(40).ToList(), 0)) == Json(sqlRestored.Calc(sqlPrices.Skip(40).ToList(), 0)),
            "Continuation from SQL price history matches uninterrupted calculation");
        Sql(cs, $"UPDATE dbo.StudyHistory SET [Close]=[Close]+1 WHERE StudyColId=7 AND RawTime={sqlPrices[20].RawTime}");
        Reject(() => new HistoryPriceCollection().RestoreCheckpoint(referencedSaved, store.LoadPrices(7, window)),
            "SQL price edits invalidate a checkpoint before restored state is used");
        Sql(cs, $"UPDATE dbo.StudyHistory SET [Close]=[Close]-1 WHERE StudyColId=7 AND RawTime={sqlPrices[20].RawTime}");
        Sql(cs, $"DELETE FROM dbo.StudyHistory WHERE StudyColId=7 AND RawTime>{sqlPrices[34].RawTime}");
        var afterTailDeletion = store.Load(7)!;
        var recoveredFromHistory = new HistoryPriceCollection();
        recoveredFromHistory.RestoreCheckpoint(afterTailDeletion, store.LoadPrices(7,
            recoveredFromHistory.GetRequiredCheckpointPrices(afterTailDeletion)!));
        var historyReplay = new HistoryPriceCollection();
        var expectedTail = historyReplay.Calc(sqlPrices, 0).Skip(35).ToList();
        Check(Json(recoveredFromHistory.Calc(sqlPrices.Skip(35).ToList(), 0)) == Json(expectedTail),
            "Deleting a SQL history tail leaves earlier referenced checkpoints usable");
        Sql(cs, $"DELETE FROM dbo.StudyHistory WHERE StudyColId=7 AND RawTime={sqlPrices[20].RawTime}");
        Reject(() => new HistoryPriceCollection().RestoreCheckpoint(afterTailDeletion, store.LoadPrices(7, afterTailDeletion.PriceWindow!)),
            "Missing SQL price rows cause restoration to fail safely for replay");

        // Sparse production writes: commits between snapshots must retain every history row,
        // and NULL state must always be paired with NULL CheckpointSavedAt.
        var sparseCollection = new HistoryPriceCollection();
        var sparseCadence = new StudyCheckpointCadence(30);
        long? sparseTail = null;
        void SaveSparse(IReadOnlyList<BarStudy> bars, StudyCheckpointBatch batch, bool rollback = false)
        {
            var rows = bars.Select(b => new StudyHistoryRS
            {
                StudyColId = 8, RawTime = b.Price.RawTime, Open = b.Price.Open, High = b.Price.High,
                Low = b.Price.Low, Close = b.Price.Close, Volume = b.Price.Volume, Studies = Json(b.Studies)
            }).ToList();
            using var tx = connection.BeginTransaction();
            store.SaveHistoryBatch(8, rows, batch.Items, sparseTail, tx);
            if (rollback) tx.Rollback();
            else { tx.Commit(); sparseTail = Math.Max(sparseTail ?? 0, rows.Max(r => r.RawTime)); }
        }
        StudyCheckpointProcessor.CalculateAndCommit(sparseCollection, sqlPrices.Take(45), 0, 0, 7, long.MaxValue,
            (bars, batch) => SaveSparse(bars, batch), cadence: sparseCadence);
        Check(Scalar("SELECT COUNT(*) FROM dbo.StudyHistory WHERE StudyColId=8") == 45
            && Scalar("SELECT COUNT(*) FROM dbo.StudyHistory WHERE StudyColId=8 AND CheckpointSavedAt IS NOT NULL") == 1,
            "Sparse SQL batches persist all 45 rows with only checkpoint 30");
        var sparseSaved = store.Load(8)!;
        Check(sparseSaved.RawTime == sqlPrices[29].RawTime, "SQL loads a checkpoint older than the history tail");
        var sparseResume = new HistoryPriceCollection();
        sparseResume.RestoreCheckpoint(sparseSaved, store.LoadPrices(8, sparseSaved.PriceWindow!));
        var sparsePlan = StudyCheckpointReplayPlan.FromCheckpoint(sparseSaved, sparseTail!.Value, 3);
        sparseCadence = new(30); sparseCadence.Restore(sparseSaved);
        var expectedSparse = new HistoryPriceCollection().Calc(sqlPrices, 0);
        var regenerated = new List<BarStudy>();
        StudyCheckpointProcessor.CalculateAndCommit(sparseResume, sqlPrices.Where(p => p.RawTime >= sparsePlan.Start),
            sparsePlan.Start, sparsePlan.Rewind, 7, long.MaxValue, (bars, batch) =>
            {
                SaveSparse(bars, batch); regenerated.AddRange(bars);
            }, cadence: sparseCadence);
        Check(Json(regenerated) == Json(expectedSparse.Where(b => b.Price.RawTime >= sparsePlan.Start))
            && Scalar("SELECT COUNT(*) FROM dbo.StudyHistory WHERE StudyColId=8") == 50,
            "SQL restart regenerates committed post-checkpoint rows and appends new rows without duplicates");
        var durableSparse = Json(store.Load(8));
        using (var tx = connection.BeginTransaction())
        {
            store.SaveHistoryBatch(8, [new StudyHistoryRS { StudyColId = 8, RawTime = sparseSaved.RawTime, Studies = "{}" }],
                [], sparseTail, tx);
            using var probe = new SqlCommand("SELECT COUNT(*) FROM dbo.StudyHistory WHERE StudyColId=8 AND CheckpointState IS NULL AND CheckpointSavedAt IS NULL", connection, tx);
            Check(Convert.ToInt32(probe.ExecuteScalar()) == 50, "Recalculation clears both fields on a bar without a new checkpoint");
            tx.Rollback();
        }
        Check(Json(store.Load(8)) == durableSparse,
            "Rolling back a sparse correction preserves the durable checkpoint");

        Sql(cs, legacyMigration);
        using (var tx = connection.BeginTransaction()) { InsertHistory(2, saved.RawTime, tx); tx.Commit(); }
        InsertLegacy(2, saved);
        Sql(cs, migration); Sql(cs, migration);
        Check(Json(store.Load(2)) == Json(saved) && Scalar("SELECT COUNT(*) FROM sys.tables WHERE name='StudyCheckpoint'") == 0,
            "Legacy checkpoint transferred and old table removed; rerun is safe");
        using (var tx = connection.BeginTransaction()) { InsertHistory(2, advanced.RawTime, tx); store.Save(2, advanced, tx); tx.Commit(); }
        Sql(cs, legacyMigration); InsertLegacy(2, saved); Sql(cs, migration);
        Check(Json(store.Load(2)) == Json(advanced), "Legacy migration permits checkpoints on other bars of the same collection");
        Sql(cs, legacyMigration);
        InsertLegacy(2, saved with { Identity = "conflicting snapshot" });
        ExpectSqlError(() => Sql(cs, migration), 51012, "Conflicting migration rejected");
        Check(Json(store.Load(2)) == Json(advanced) && Scalar("SELECT COUNT(*) FROM dbo.StudyCheckpoint") == 1,
            "Conflict rollback preserves both payloads");
        Sql(cs, "DELETE FROM dbo.StudyCheckpoint");
        InsertLegacy(3, saved);
        ExpectSqlError(() => Sql(cs, migration), 51011, "Unmatched legacy checkpoint rejected");
        Check(Scalar("SELECT COUNT(*) FROM dbo.StudyCheckpoint") == 1, "Unmatched legacy checkpoint retained");
        using (var tx = connection.BeginTransaction()) { InsertHistory(3, saved.RawTime, tx); tx.Commit(); }
        Sql(cs, migration);
        Check(Json(store.Load(3)) == Json(saved), "Migration succeeds after missing history is repaired");
    }
    finally { SqlConnection.ClearAllPools(); Sql(master, $"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]"); }
}
Console.WriteLine($"{checks} checks passed.");

class SharedCollection : StudyColBase
{
    readonly int period;
    public SharedCollection(int period = 5) : base(new StudyCollectionContext(
        new StudyColRS { Id = 1, ColType = "test", ColParams = $"{{\"period\":{period}}}" },
        new TickerRS(), new IntervalRS { IntervalLen = 60 }, NullLoggerFactory.Instance)) => this.period = period;
    protected override string? CheckpointSchema => "shared-test-v1";
    protected override void InitializeStudies() => studyList = new()
    {
        new StudyMA(period, "close", "test"), new StudyADX(5, 5, "test"),
        new StudyBB(period, 2, "close", "test", "B"), new StudySEM(period, "close", "test"),
        new StudyWMA(period, "close", "test"), new StudyExtended(period, "close", 1, 2, "extended"),
        new StudyROC(3, "MAtest")
    };
    public override int GetMaxLen() => period;
}
sealed class UnknownMA : StudyMA { public UnknownMA() : base(5, "close", "unknown") { } }
sealed class UnknownCollection : SharedCollection { protected override string? CheckpointSchema => null; }
sealed class HistoryPriceCollection : SharedCollection
{
    protected override bool CheckpointPricesFromHistory => true;
    protected override int CheckpointPriceHistoryLimit => 24;
}
sealed class CountingCollection : SharedCollection
{
    public int Captures { get; private set; }
    protected override JsonElement CaptureCollectionState()
    {
        Captures++;
        return base.CaptureCollectionState();
    }
}
