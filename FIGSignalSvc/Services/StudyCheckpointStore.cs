using System.Data;
using System.Text.Json;
using FIG.Studies;
using FIGCommon.DataAccess;
using FIGCommon.Models;
using Microsoft.Data.SqlClient;

namespace FIGSignalExSvc.Services;

public sealed class StudyCheckpointStore
{
    private readonly Func<string> connectionString;
    public StudyCheckpointStore() : this(() => MainRepo.ConnectionString) { }
    public StudyCheckpointStore(Func<string> connectionString) => this.connectionString = connectionString;

    public CollectionCheckpoint? Load(int studyColId)
    {
        using var connection = new SqlConnection(connectionString());
        connection.Open();
        using var command = new SqlCommand("""
            IF EXISTS (SELECT 1 FROM sys.indexes
                WHERE object_id=OBJECT_ID(N'dbo.StudyHistory') AND name=N'UX_StudyHistory_Checkpoint')
                THROW 51003, 'Run study_history_checkpoints.sql to replace the single-checkpoint index before processing.', 1;
            SELECT TOP (1) RawTime,CheckpointState FROM dbo.StudyHistory
            WHERE StudyColId=@Id AND CheckpointSavedAt IS NOT NULL
            ORDER BY RawTime DESC
            """, connection);
        command.Parameters.AddWithValue("@Id", studyColId);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        var checkpoint = JsonSerializer.Deserialize<CollectionCheckpoint>(reader.GetString(1), StudyStateJson.Options)
            ?? throw new InvalidDataException("Empty study checkpoint.");
        if (checkpoint.RawTime != Convert.ToInt64(reader.GetValue(0)))
            throw new InvalidDataException("Checkpoint payload does not match its study-history row.");
        return checkpoint;
    }

    public void Save(int studyColId, CollectionCheckpoint checkpoint, SqlTransaction transaction)
        => SaveBatch(studyColId, [StudyCheckpointPayload.Capture(checkpoint)], transaction);

    // Restore-only read: processing keeps the hydrated window in memory.
    // Do not select Studies or CheckpointState, which can be large on every row.
    public List<PriceDataRS> LoadPrices(int studyColId, CheckpointPriceWindow window)
    {
        window.Validate(int.MaxValue - 1, window.LastRawTime);
        using var connection = new SqlConnection(connectionString());
        connection.Open();
        using var command = new SqlCommand("""
            SELECT TOP (@Count) RawTime,[Open],High,Low,[Close],Volume
            FROM dbo.StudyHistory
            WHERE StudyColId=@Id AND RawTime>=@First AND RawTime<=@Last
            ORDER BY RawTime;
            """, connection) { CommandTimeout = 600 };
        command.Parameters.AddWithValue("@Id", studyColId);
        command.Parameters.AddWithValue("@Count", window.Count + 1); // detect unexpected or duplicate rows too
        command.Parameters.AddWithValue("@First", window.FirstRawTime);
        command.Parameters.AddWithValue("@Last", window.LastRawTime);
        using var reader = command.ExecuteReader();
        var prices = new List<PriceDataRS>();
        while (reader.Read())
        {
            var time = Convert.ToInt64(reader.GetValue(0));
            prices.Add(new PriceDataRS
            {
                RawTime = time, PriceDate = DateTimeOffset.FromUnixTimeSeconds(time).UtcDateTime,
                Open = reader.GetDecimal(1), High = reader.GetDecimal(2), Low = reader.GetDecimal(3),
                Close = reader.GetDecimal(4), Volume = reader.GetInt32(5)
            });
        }
        return prices;
    }

    public void SaveBatch(int studyColId, IReadOnlyList<StudyCheckpointPayload> checkpoints, SqlTransaction transaction)
    {
        if (checkpoints.Count == 0) return;
        using var data = new DataTable();
        data.Columns.Add("RawTime", typeof(long)); data.Columns.Add("CheckpointState", typeof(string));
        foreach (var item in checkpoints) data.Rows.Add(item.RawTime, item.Json);
        Execute("""
            CREATE TABLE #FIGCheckpointBatch(RawTime bigint NOT NULL PRIMARY KEY, CheckpointState nvarchar(max) NOT NULL);
            """, studyColId, transaction);
        BulkStage(data, transaction);
        Execute("""
            IF NOT EXISTS (SELECT 1 FROM dbo.StudyCol WITH (UPDLOCK,HOLDLOCK) WHERE Id=@Id)
                THROW 51001, 'Checkpoint collection does not exist.', 1;
            IF EXISTS (SELECT b.RawTime FROM #FIGCheckpointBatch b
                LEFT JOIN dbo.StudyHistory h WITH (UPDLOCK,HOLDLOCK) ON h.StudyColId=@Id AND h.RawTime=b.RawTime
                GROUP BY b.RawTime HAVING COUNT_BIG(h.Id)<>1)
                THROW 51002, 'Checkpoint requires exactly one matching StudyHistory row.', 1;
            UPDATE h SET CheckpointState=b.CheckpointState, CheckpointSavedAt=SYSUTCDATETIME()
            FROM dbo.StudyHistory h JOIN #FIGCheckpointBatch b ON h.RawTime=b.RawTime WHERE h.StudyColId=@Id;
            DROP TABLE #FIGCheckpointBatch;
            """, studyColId, transaction);
    }

    // Production path: stage every history row with an optional scheduled checkpoint.
    // Triggers fire once per insert/update statement instead of once per checkpoint.
    public void SaveHistoryBatch(int studyColId, IReadOnlyList<StudyHistoryRS> rows,
        IReadOnlyList<StudyCheckpointPayload> checkpoints, long? expectedHistoryRawTime, SqlTransaction transaction)
    {
        var times = rows.Select(r => r.RawTime).ToHashSet();
        if (times.Count != rows.Count || rows.Any(r => r.StudyColId != studyColId)
            || checkpoints.Select(p => p.RawTime).Distinct().Count() != checkpoints.Count
            || checkpoints.Any(p => !times.Contains(p.RawTime)))
            throw new InvalidDataException("Checkpoints must be a unique subset of the history batch for this collection.");
        var payloads = checkpoints.ToDictionary(p => p.RawTime, p => p.Json);
        if (rows.Count == 0) return;
        using var data = new DataTable();
        data.Columns.Add("RawTime", typeof(long));
        foreach (var name in new[] { "Open", "High", "Low", "Close" }) data.Columns.Add(name, typeof(decimal));
        data.Columns.Add("Volume", typeof(int)); data.Columns.Add("Studies", typeof(string));
        data.Columns.Add("CheckpointState", typeof(string));
        foreach (var row in rows)
            data.Rows.Add(row.RawTime, row.Open, row.High, row.Low, row.Close, row.Volume,
                row.Studies, payloads.TryGetValue(row.RawTime, out var payload) ? payload : DBNull.Value);
        Execute("""
            -- Inherit the database's actual price precision and text types.
            SELECT TOP (0) RawTime,[Open],High,Low,[Close],Volume,Studies,CheckpointState
            INTO #FIGCheckpointBatch FROM dbo.StudyHistory;
            CREATE UNIQUE CLUSTERED INDEX IX_FIGCheckpointBatch ON #FIGCheckpointBatch(RawTime);
            """, studyColId, transaction);
        BulkStage(data, transaction);
        Execute("""
            IF NOT EXISTS (SELECT 1 FROM dbo.StudyCol WITH (UPDLOCK,HOLDLOCK) WHERE Id=@Id)
                THROW 51001, 'Checkpoint collection does not exist.', 1;

            -- A timestamp below the tail can be a gap, not necessarily an update.
            -- Check the tail separately to detect a concurrent append/deletion, then
            -- choose inserts/updates by actual row existence under the same locks.
            DECLARE @ActualHistoryRawTime bigint = (
                SELECT MAX(RawTime) FROM dbo.StudyHistory WITH (UPDLOCK,HOLDLOCK) WHERE StudyColId=@Id);
            DECLARE @Message nvarchar(2048);
            IF @ActualHistoryRawTime<>@ExpectedHistoryRawTime
                OR (@ActualHistoryRawTime IS NULL AND @ExpectedHistoryRawTime IS NOT NULL)
                OR (@ActualHistoryRawTime IS NOT NULL AND @ExpectedHistoryRawTime IS NULL)
            BEGIN
                SET @Message=CONCAT('Study history tail changed for StudyColId=',@Id,
                    '. Expected RawTime=',COALESCE(CONVERT(varchar(20),@ExpectedHistoryRawTime),'empty'),
                    ', actual RawTime=',COALESCE(CONVERT(varchar(20),@ActualHistoryRawTime),'empty'),'.');
                THROW 51002, @Message, 1;
            END;
            DECLARE @DuplicateRawTime bigint;
            SELECT TOP (1) @DuplicateRawTime=b.RawTime FROM #FIGCheckpointBatch b
                LEFT JOIN dbo.StudyHistory h WITH (UPDLOCK,HOLDLOCK) ON h.StudyColId=@Id AND h.RawTime=b.RawTime
                GROUP BY b.RawTime HAVING COUNT_BIG(h.Id)>1 ORDER BY b.RawTime;
            IF @DuplicateRawTime IS NOT NULL
            BEGIN
                SET @Message=CONCAT('Duplicate study history rows for StudyColId=',@Id,
                    ', RawTime=',@DuplicateRawTime,'. Expected at most one row per bar.');
                THROW 51002, @Message, 1;
            END;

            IF EXISTS (SELECT 1 FROM #FIGCheckpointBatch b JOIN dbo.StudyHistory h
                ON h.StudyColId=@Id AND h.RawTime=b.RawTime)
                UPDATE h SET [Open]=b.[Open],High=b.High,Low=b.Low,[Close]=b.[Close],Volume=b.Volume,
                    Studies=b.Studies,CheckpointState=b.CheckpointState,
                    CheckpointSavedAt=CASE WHEN b.CheckpointState IS NULL THEN NULL ELSE SYSUTCDATETIME() END
                FROM dbo.StudyHistory h JOIN #FIGCheckpointBatch b ON h.RawTime=b.RawTime
                WHERE h.StudyColId=@Id;
            IF EXISTS (SELECT 1 FROM #FIGCheckpointBatch b WHERE NOT EXISTS (
                SELECT 1 FROM dbo.StudyHistory h WHERE h.StudyColId=@Id AND h.RawTime=b.RawTime))
                INSERT dbo.StudyHistory(StudyColId,RawTime,[Open],High,Low,[Close],Volume,Studies,CheckpointState,CheckpointSavedAt)
                SELECT @Id,RawTime,[Open],High,Low,[Close],Volume,Studies,CheckpointState,
                    CASE WHEN CheckpointState IS NULL THEN NULL ELSE SYSUTCDATETIME() END
                FROM #FIGCheckpointBatch b WHERE NOT EXISTS (
                    SELECT 1 FROM dbo.StudyHistory h WHERE h.StudyColId=@Id AND h.RawTime=b.RawTime)
                ORDER BY b.RawTime;
            DROP TABLE #FIGCheckpointBatch;
            """, studyColId, transaction, expectedHistoryRawTime);
    }

    private static void BulkStage(DataTable data, SqlTransaction transaction)
    {
        using var copy = new SqlBulkCopy(transaction.Connection!, SqlBulkCopyOptions.Default, transaction)
        { DestinationTableName = "#FIGCheckpointBatch", BulkCopyTimeout = 600 };
        foreach (DataColumn column in data.Columns) copy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        copy.WriteToServer(data);
    }

    private static void Execute(string sql, int studyColId, SqlTransaction transaction, long? expectedHistoryRawTime = null)
    {
        using var command = new SqlCommand(sql, transaction.Connection, transaction) { CommandTimeout = 600 };
        // Temp-table creation must be an unparameterized batch: a #table created
        // inside sp_executesql would disappear before SqlBulkCopy can use it.
        if (sql.Contains("@Id", StringComparison.Ordinal)) command.Parameters.AddWithValue("@Id", studyColId);
        if (sql.Contains("@ExpectedHistoryRawTime", StringComparison.Ordinal))
            command.Parameters.Add("@ExpectedHistoryRawTime", SqlDbType.BigInt).Value = (object?)expectedHistoryRawTime ?? DBNull.Value;
        command.ExecuteNonQuery();
    }
}
