using System.Data;
using FIGCommon.Models;
using Microsoft.Data.SqlClient;

namespace FIGSignalExSvc.Services;

public sealed class StudySignalStore
{
    public long ReadProgressForUpdate(int studyColId, SqlTransaction transaction)
    {
        using var command = Command("""
            IF OBJECT_ID(N'dbo.StudySignalProgress',N'U') IS NULL
                THROW 51020, 'Run study_signal_progress.sql before starting FIGSignalSvc; signal replay protection is required.', 1;
            -- Serialize signal decisions before reading signal state or writing any history.
            -- Use the same parent-row lock as the checkpoint writer, in the same order.
            IF NOT EXISTS (SELECT 1 FROM dbo.StudyCol WITH (UPDLOCK,HOLDLOCK) WHERE Id=@Id)
                THROW 51021, 'Signal collection does not exist.', 1;
            EXEC sys.sp_executesql N'
                IF NOT EXISTS (SELECT 1 FROM dbo.StudySignalProgress WHERE StudyColId=@Id)
                    INSERT dbo.StudySignalProgress(StudyColId,ProcessedThrough)
                    SELECT @Id,COALESCE(MAX(t.RawTime),-1) FROM (
                        SELECT CONVERT(bigint,RawTime) RawTime FROM dbo.StudyHistory WHERE StudyColId=@Id
                        UNION ALL
                        SELECT CONVERT(bigint,s.StartTime) FROM dbo.Signal s
                            JOIN dbo.StrategyConfig c ON c.Strategy=s.Strategy WHERE c.StudyColId=@Id
                        UNION ALL
                        SELECT CONVERT(bigint,s.StopTime) FROM dbo.Signal s
                            JOIN dbo.StrategyConfig c ON c.Strategy=s.Strategy WHERE c.StudyColId=@Id
                    ) t;
                SELECT ProcessedThrough FROM dbo.StudySignalProgress WITH (UPDLOCK,HOLDLOCK) WHERE StudyColId=@Id;',
                N'@Id int',@Id=@Id;
            """, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = studyColId;
        var value = command.ExecuteScalar();
        if (value is null or DBNull) throw new InvalidDataException("Missing durable signal progress.");
        return Convert.ToInt64(value);
    }

    public SignalRS? ReadLatestSignal(string strategy, SqlTransaction transaction)
    {
        using var command = Command("""
            SELECT TOP (1) Id,Strategy,Tag,Status,Side,StartTime,StopTime,StartPrice,StopPrice,LastUpdated
            FROM dbo.Signal WHERE Strategy=@Strategy ORDER BY StartTime DESC,Id DESC;
            """, transaction);
        command.Parameters.Add("@Strategy", SqlDbType.VarChar, 100).Value = strategy;
        using var reader = command.ExecuteReader();
        return reader.Read() ? new SignalRS().CreateFromSqlDataReader(reader) : null;
    }

    public void SaveProgress(int studyColId, long expected, long processedThrough, SqlTransaction transaction)
    {
        if (processedThrough < expected) throw new InvalidDataException("Signal progress cannot move backwards.");
        if (processedThrough == expected) return;
        using var command = Command("""
            UPDATE dbo.StudySignalProgress SET ProcessedThrough=@Time,UpdatedAt=SYSUTCDATETIME()
                WHERE StudyColId=@Id AND ProcessedThrough=@Expected;
            IF @@ROWCOUNT<>1 THROW 51022, 'Signal progress changed during processing.', 1;
            """, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = studyColId;
        command.Parameters.Add("@Expected", SqlDbType.BigInt).Value = expected;
        command.Parameters.Add("@Time", SqlDbType.BigInt).Value = processedThrough;
        command.ExecuteNonQuery();
    }

    private static SqlCommand Command(string sql, SqlTransaction transaction)
        => new(sql, transaction.Connection, transaction) { CommandTimeout = 600 };
}
