using System.Data;
using System.Text.Json;
using FIG.Studies;
using FIGCommon.Models;
using FIGSignalExSvc.Services;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed class MarketLiveCheckpointStore(string connectionString)
{
    public async Task EnsureSchemaAsync(CancellationToken ct)
    {
        using var stream=typeof(MarketLiveCheckpointStore).Assembly.GetManifestResourceStream("FIGMarketLive.Core.Database.CreateStudyHistoryCheckpoints.sql")??throw new InvalidDataException("Missing checkpoint schema resource.");
        using var reader=new StreamReader(stream);string sql=await reader.ReadToEndAsync(ct);
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);await using var cmd=new SqlCommand(sql,c){CommandTimeout=new SqlConnectionStringBuilder(connectionString).CommandTimeout};await cmd.ExecuteNonQueryAsync(ct);
        await StudyHistoryBinaryMigration.MigrateAsync(connectionString,ct);
    }
    public async Task<StudyRecoverySnapshot> ReadRecoveryAsync(int studyColId,byte[] definition,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("""
            SELECT RawTime,[Open],High,Low,[Close],Volume,
                CONVERT(bit,CASE WHEN DATALENGTH(Studies)>=10 AND SUBSTRING(Studies,1,4)=0x46494753 THEN 1 ELSE 0 END)
                FROM dbo.StudyHistory WHERE StudyColId=@Id ORDER BY RawTime;
            SELECT RawTime,MarketLivePrefixCount,MarketLivePrefixHash FROM dbo.StudyHistory
                WHERE StudyColId=@Id AND MarketLiveDefinitionHash=@Definition AND CheckpointSavedAt IS NOT NULL
                AND MarketLivePrefixCount>0 AND DATALENGTH(MarketLivePrefixHash)=32 ORDER BY RawTime DESC;
            """,c){CommandTimeout=new SqlConnectionStringBuilder(connectionString).CommandTimeout};
        cmd.Parameters.AddWithValue("@Id",studyColId);cmd.Parameters.AddWithValue("@Definition",definition);
        await using var r=await cmd.ExecuteReaderAsync(ct);var published=new List<PublishedStudyInput>();var checkpoints=new List<CheckpointCandidate>();
        while(await r.ReadAsync(ct))published.Add(new(Price(r),r.GetBoolean(6)));
        await r.NextResultAsync(ct);while(await r.ReadAsync(ct))checkpoints.Add(new(Convert.ToInt64(r.GetValue(0)),r.GetInt32(1),(byte[])r.GetValue(2)));
        return new(checkpoints,published);
    }
    private static PriceDataRS Price(SqlDataReader r)
    {long time=Convert.ToInt64(r.GetValue(0));return new(){RawTime=time,PriceDate=DateTimeOffset.FromUnixTimeSeconds(time).UtcDateTime,Open=r.GetDecimal(1),High=r.GetDecimal(2),Low=r.GetDecimal(3),Close=r.GetDecimal(4),Volume=r.GetInt32(5)};}
    public async Task<CollectionCheckpoint?> LoadAsync(int studyColId,byte[] definition,CheckpointCandidate candidate,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("""
            SELECT CASE WHEN DATALENGTH(CheckpointState)<=67108864 THEN CheckpointState ELSE NULL END FROM dbo.StudyHistory
                WHERE StudyColId=@Id AND RawTime=@Time AND MarketLiveDefinitionHash=@Definition AND MarketLivePrefixCount=@Count AND MarketLivePrefixHash=@Hash;
            """,c);
        cmd.Parameters.AddWithValue("@Id",studyColId);cmd.Parameters.AddWithValue("@Time",candidate.RawTime);cmd.Parameters.AddWithValue("@Definition",definition);cmd.Parameters.AddWithValue("@Count",candidate.Count);cmd.Parameters.AddWithValue("@Hash",candidate.PrefixHash);
        return await cmd.ExecuteScalarAsync(ct) is string json?JsonSerializer.Deserialize<CollectionCheckpoint>(json,StudyStateJson.Options):null;
    }
    public async Task<List<PriceDataRS>> LoadPricesAsync(int studyColId,CheckpointPriceWindow window,CancellationToken ct)
    {
        window.Validate(int.MaxValue-1,window.LastRawTime);
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("SELECT TOP(@Count) RawTime,[Open],High,Low,[Close],Volume FROM dbo.StudyHistory WHERE StudyColId=@Id AND RawTime>=@First AND RawTime<=@Last ORDER BY RawTime",c){CommandTimeout=new SqlConnectionStringBuilder(connectionString).CommandTimeout};
        cmd.Parameters.AddWithValue("@Count",checked(window.Count+1));cmd.Parameters.AddWithValue("@Id",studyColId);cmd.Parameters.AddWithValue("@First",window.FirstRawTime);cmd.Parameters.AddWithValue("@Last",window.LastRawTime);
        await using var r=await cmd.ExecuteReaderAsync(ct);var prices=new List<PriceDataRS>();while(await r.ReadAsync(ct))prices.Add(Price(r));return prices;
    }
    internal static async Task SaveAsync(SqlConnection c,SqlTransaction tx,int id,CheckpointReplayResult run,long first,CancellationToken ct,
        StudyWriteProfiler? timing=null,bool incremental=false,bool cleanForward=false,long? previous=null)
    {
        if(!run.PersistHistory)return;
        using(timing?.Stage(incremental?cleanForward?"Forward checkpoint cleanup":"Boundary checkpoint cleanup":"Full checkpoint cleanup"))
        await using(var cleanup=new SqlCommand(!incremental?"""
            UPDATE dbo.StudyHistory SET CheckpointState=NULL,CheckpointSavedAt=NULL,MarketLivePrefixCount=NULL,MarketLivePrefixHash=NULL
            WHERE StudyColId=@Id AND CheckpointSavedAt IS NOT NULL AND CheckpointState IS NOT NULL AND MarketLiveDefinitionHash IS NOT NULL AND
                (MarketLiveDefinitionHash<>@Definition OR RawTime>=@First OR
                (MarketLivePrefixCount%@Every<>0 AND (@LastCompleted IS NULL OR RawTime<>@LastCompleted)));
            """:(cleanForward?"""
            UPDATE dbo.StudyHistory SET CheckpointState=NULL,CheckpointSavedAt=NULL,MarketLivePrefixCount=NULL,MarketLivePrefixHash=NULL
            WHERE StudyColId=@Id AND CheckpointSavedAt IS NOT NULL AND CheckpointState IS NOT NULL AND
                (MarketLiveDefinitionHash<>@Definition OR RawTime>=@First OR
                (MarketLivePrefixCount%@Every<>0 AND (@Previous IS NULL OR RawTime<>@Previous)));
            """:"")+"""
            UPDATE dbo.StudyHistory SET CheckpointState=NULL,CheckpointSavedAt=NULL,MarketLivePrefixCount=NULL,MarketLivePrefixHash=NULL
            WHERE StudyColId=@Id AND RawTime=@Previous AND CheckpointState IS NOT NULL AND MarketLivePrefixCount%@Every<>0
                AND (@LastCompleted IS NULL OR RawTime<>@LastCompleted);
            """,c,tx))
        {
            cleanup.Parameters.AddWithValue("@Id",id);cleanup.Parameters.AddWithValue("@Definition",run.DefinitionHash);cleanup.Parameters.AddWithValue("@First",first);cleanup.Parameters.AddWithValue("@Every",run.EveryBars);
            cleanup.Parameters.Add("@Previous",SqlDbType.BigInt).Value=(object?)previous??DBNull.Value;
            cleanup.Parameters.Add("@LastCompleted",SqlDbType.BigInt).Value=(object?)run.LastCompletedRawTime??DBNull.Value;
            await cleanup.ExecuteNonQueryAsync(ct);
        }
        if(run.Checkpoints.Count==0)return;
        using(timing?.Stage("Checkpoint staging"))
        {
            ct.ThrowIfCancellationRequested();
            await using(var create=new SqlCommand("CREATE TABLE #MarketLiveCheckpoints(RawTime bigint NOT NULL PRIMARY KEY,CheckpointState nvarchar(max) NOT NULL,PrefixCount int NOT NULL,PrefixHash varbinary(32) NOT NULL)",c,tx))await create.ExecuteNonQueryAsync(ct);
            using var data=new DataTable();data.Columns.Add("RawTime",typeof(long));data.Columns.Add("CheckpointState",typeof(string));data.Columns.Add("PrefixCount",typeof(int));data.Columns.Add("PrefixHash",typeof(byte[]));
            foreach(var checkpoint in run.Checkpoints)data.Rows.Add(checkpoint.Payload.RawTime,checkpoint.Payload.Json,checkpoint.Count,checkpoint.PrefixHash);
            using var copy=new SqlBulkCopy(c,SqlBulkCopyOptions.Default,tx){DestinationTableName="#MarketLiveCheckpoints",BulkCopyTimeout=new SqlConnectionStringBuilder(c.ConnectionString).CommandTimeout};
            foreach(DataColumn column in data.Columns)copy.ColumnMappings.Add(column.ColumnName,column.ColumnName);await copy.WriteToServerAsync(data,ct);
        }
        using(timing?.Stage("Checkpoint payload + metadata upsert"))
        await using(var save=new SqlCommand("""
            IF EXISTS(SELECT b.RawTime FROM #MarketLiveCheckpoints b LEFT JOIN dbo.StudyHistory h ON h.StudyColId=@Id AND h.RawTime=b.RawTime GROUP BY b.RawTime HAVING COUNT_BIG(h.Id)<>1)
                THROW 51141,'Checkpoint requires exactly one matching history row.',1;
            UPDATE h SET CheckpointState=b.CheckpointState,CheckpointSavedAt=SYSUTCDATETIME(),MarketLiveDefinitionHash=@Definition,MarketLivePrefixCount=b.PrefixCount,MarketLivePrefixHash=b.PrefixHash
            FROM dbo.StudyHistory h JOIN #MarketLiveCheckpoints b ON h.RawTime=b.RawTime WHERE h.StudyColId=@Id;
            DROP TABLE #MarketLiveCheckpoints;
            """,c,tx))
        {save.Parameters.AddWithValue("@Id",id);save.Parameters.AddWithValue("@Definition",run.DefinitionHash);await save.ExecuteNonQueryAsync(ct);}
    }
}
