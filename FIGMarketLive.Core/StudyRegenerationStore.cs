using System.Data;
using FIGCommon.Models;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed record StudyRegenerationState(byte[] Definition,int Completed,int Total,byte[]? PrefixHash,string Status);

public sealed class StudyRegenerationStore(string connectionString)
{
    public event Action<StudyWriteTiming>? WriteMeasured;
    public event Action<int,TimeSpan>? DeadlockRetrying;
    public async Task EnsureSchemaAsync(CancellationToken ct)
    {
        using var stream=typeof(StudyRegenerationStore).Assembly.GetManifestResourceStream("FIGMarketLive.Core.Database.CreateStudyRegeneration.sql")!;
        using var reader=new StreamReader(stream);await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand(await reader.ReadToEndAsync(ct),c);await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task<StudyRegenerationState?> StateAsync(int id,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);await using var cmd=new SqlCommand("SELECT DefinitionHash,CompletedCount,TotalCount,PrefixHash,Status FROM dbo.StudyRegeneration WHERE StudyColId=@Id",c);cmd.Parameters.AddWithValue("@Id",id);
        await using var r=await cmd.ExecuteReaderAsync(ct);return await r.ReadAsync(ct)?new((byte[])r[0],r.GetInt32(1),r.GetInt32(2),r.IsDBNull(3)?null:(byte[])r[3],r.GetString(4)):null;
    }
    public async Task<List<CheckpointCandidate>> CandidatesAsync(int id,byte[] definition,int max,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);await using var cmd=new SqlCommand("SELECT RawTime,MarketLivePrefixCount,MarketLivePrefixHash FROM dbo.StudyHistory WHERE StudyColId=@Id AND MarketLiveDefinitionHash=@Definition AND CheckpointSavedAt IS NOT NULL AND CheckpointState IS NOT NULL AND MarketLivePrefixCount<=@Max AND MarketLivePrefixCount>0 AND DATALENGTH(MarketLivePrefixHash)=32 ORDER BY RawTime DESC",c);
        cmd.Parameters.AddWithValue("@Id",id);cmd.Parameters.AddWithValue("@Definition",definition);cmd.Parameters.AddWithValue("@Max",max);await using var r=await cmd.ExecuteReaderAsync(ct);var result=new List<CheckpointCandidate>();while(await r.ReadAsync(ct))result.Add(new(Convert.ToInt64(r[0]),r.GetInt32(1),(byte[])r[2]));return result;
    }
    public async Task MarkAsync(int id,string status,string message,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);await using var cmd=new SqlCommand("UPDATE dbo.StudyRegeneration SET Status=@Status,Message=@Message,UpdatedAt=SYSUTCDATETIME() WHERE StudyColId=@Id",c);
        cmd.Parameters.AddWithValue("@Id",id);cmd.Parameters.AddWithValue("@Status",status);cmd.Parameters.AddWithValue("@Message",message.Length>2000?message[..2000]:message);await cmd.ExecuteNonQueryAsync(ct);
    }
    public Task PublishAsync(CollectionMonitor monitor,CheckpointReplayResult batch,IReadOnlyList<PriceDataRS> raw,long from,long to,int total,byte[] prefixHash,bool reset,CancellationToken ct,bool beginRun=true,long? previous=null,bool validatePrices=true,bool direct=false,bool overridesOnly=false)
        =>SqlDeadlockRetry.ExecuteAsync(token=>PublishAttemptAsync(monitor,batch,raw,from,to,total,prefixHash,reset,token,beginRun,previous,validatePrices,direct,overridesOnly),ct,
            (attempt,delay)=>DeadlockRetrying?.Invoke(attempt,delay));

    private async Task PublishAttemptAsync(CollectionMonitor monitor,CheckpointReplayResult batch,IReadOnlyList<PriceDataRS> raw,long from,long to,int total,byte[] prefixHash,bool reset,CancellationToken ct,bool beginRun,long? previous,bool validatePrices,bool direct,bool overridesOnly)
    {
        using var timing=new StudyWriteProfiler(monitor.StudyCol.Id,batch.Bars.Count,batch.Checkpoints.Count,m=>WriteMeasured?.Invoke(m));
        await using var c=new SqlConnection(connectionString);using(timing.Stage("Open connection"))await c.OpenAsync(ct);await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        using(timing.Stage("Metadata validation"))await StudyHistoryStore.ValidateAsync(c,tx,monitor,ct,true);
        if(direct)
            await InsertDirectAsync(c,tx,monitor,batch,raw,from,to,reset,previous,validatePrices,timing,ct);
        else
        {
        using(timing.Stage("Create staging tables"))
        await using(var create=new SqlCommand((validatePrices?"SELECT TOP(0) CONVERT(int,Id) AS Id,RawTime,[Open],High,Low,[Close],Volume INTO #Raw FROM dbo.PriceData; ":"")+"SELECT TOP(0) RawTime,[Open],High,Low,[Close],Volume,Studies,StudiesOverride INTO #Bars FROM dbo.StudyHistory;",c,tx))await create.ExecuteNonQueryAsync(ct);
        DataTable Table(bool source)
        {var t=new DataTable();if(source)t.Columns.Add("Id",typeof(int));t.Columns.Add("RawTime",typeof(long));foreach(var name in new[]{"Open","High","Low","Close"})t.Columns.Add(name,typeof(decimal));t.Columns.Add("Volume",typeof(int));if(!source){t.Columns.Add("Studies",typeof(byte[]));t.Columns.Add("StudiesOverride",typeof(byte[]));}return t;}
        using var rawTable=Table(true);using var bars=Table(false);
        using(timing.Stage("Build staging buffers"))
        {foreach(var p in raw)rawTable.Rows.Add(p.Id,p.RawTime,p.Open,p.High,p.Low,p.Close,p.Volume);foreach(var b in batch.Bars){var p=b.Price;bars.Rows.Add(p.RawTime,p.Open,p.High,p.Low,p.Close,p.Volume,b.Studies,(object?)b.StudiesOverride??DBNull.Value);}}
        foreach(var (table,name) in new[]{(rawTable,"#Raw"),(bars,"#Bars")})
        {if(name=="#Raw"&&!validatePrices)continue;using var stage=timing.Stage(name=="#Raw"?"Bulk source prices":"Bulk study results");using var copy=new SqlBulkCopy(c,SqlBulkCopyOptions.Default,tx){DestinationTableName=name,BulkCopyTimeout=new SqlConnectionStringBuilder(connectionString).CommandTimeout};foreach(DataColumn column in table.Columns)copy.ColumnMappings.Add(column.ColumnName,column.ColumnName);await copy.WriteToServerAsync(table,ct);}
        if(validatePrices)
        {
        using(timing.Stage("Source window validation"))
        await using(var cmd=new SqlCommand("""
            -- Materialize the locked window once. Recompile the range extraction for this batch,
            -- rather than reusing estimates from a distant earlier part of the price history.
            SELECT Id,RawTime,[Open],High,Low,[Close],Volume INTO #CurrentRaw
                FROM dbo.PriceData WITH(HOLDLOCK)
                WHERE DataSetId=@DataSet AND RawTime>=@From AND RawTime<@To OPTION(RECOMPILE,MAXDOP 1);
            IF EXISTS(SELECT Id,RawTime,[Open],High,Low,[Close],Volume FROM #CurrentRaw
                EXCEPT SELECT Id,RawTime,[Open],High,Low,[Close],Volume FROM #Raw)
                OR EXISTS(SELECT Id,RawTime,[Open],High,Low,[Close],Volume FROM #Raw EXCEPT
                SELECT Id,RawTime,[Open],High,Low,[Close],Volume FROM #CurrentRaw)
                THROW 51140,'Source prices changed during regeneration. Committed batches remain; restart to reconcile.',1;
            """,c,tx))
        {cmd.Parameters.AddWithValue("@DataSet",monitor.DataSetId);cmd.Parameters.AddWithValue("@From",from);cmd.Parameters.AddWithValue("@To",to);await cmd.ExecuteNonQueryAsync(ct);}
        }
        else {using var skipped=timing.Stage("Source validation skipped (historical regeneration)");}
        if(reset)
        using(timing.Stage("Checkpoint reset"))
        await using(var cmd=new SqlCommand("UPDATE dbo.StudyHistory SET CheckpointState=NULL,CheckpointSavedAt=NULL,MarketLivePrefixCount=NULL,MarketLivePrefixHash=NULL WHERE StudyColId=@Id AND CheckpointState IS NOT NULL",c,tx))
        {cmd.Parameters.AddWithValue("@Id",monitor.StudyCol.Id);await cmd.ExecuteNonQueryAsync(ct);}
        if(overridesOnly)
        {
            using var stage=timing.Stage("StudyHistory overrides update");
            await using var update=new SqlCommand("""
                IF EXISTS(SELECT 1 FROM #Bars b LEFT JOIN dbo.StudyHistory h WITH(UPDLOCK,HOLDLOCK) ON h.StudyColId=@Id AND h.RawTime=b.RawTime
                    WHERE h.Id IS NULL OR h.StudiesOverride IS NULL OR h.StudiesBaseDefinitionHash IS NULL OR h.StudiesBaseDefinitionHash<>@BaseDefinition OR h.Studies<>b.Studies OR h.[Open]<>b.[Open] OR h.High<>b.High OR h.Low<>b.Low OR h.[Close]<>b.[Close] OR h.Volume<>b.Volume)
                    THROW 51142,'Base studies changed during override regeneration. Restart to reconcile.',1;
                UPDATE h SET StudiesOverride=b.StudiesOverride,CheckpointState=NULL,CheckpointSavedAt=NULL,MarketLivePrefixCount=NULL,MarketLivePrefixHash=NULL,MarketLiveDefinitionHash=@Definition
                    FROM dbo.StudyHistory h JOIN #Bars b ON b.RawTime=h.RawTime WHERE h.StudyColId=@Id;
                """,c,tx);
            update.Parameters.AddWithValue("@Id",monitor.StudyCol.Id);update.Parameters.AddWithValue("@Definition",batch.DefinitionHash);update.Parameters.AddWithValue("@BaseDefinition",CheckpointReplay.DefinitionHash(SqlMonitorLoader.Identity(monitor)));await update.ExecuteNonQueryAsync(ct);
        }
        else
        {
        using(timing.Stage("StudyHistory outputs upsert"))
        await using(var cmd=new SqlCommand("""
            UPDATE b SET [Open]=s.[Open],High=s.High,Low=s.Low,[Close]=s.[Close],Volume=s.Volume,Studies=s.Studies,StudiesOverride=s.StudiesOverride,StudiesBaseDefinitionHash=@BaseDefinition,
                CheckpointState=NULL,CheckpointSavedAt=NULL,MarketLivePrefixCount=NULL,MarketLivePrefixHash=NULL,MarketLiveDefinitionHash=@Definition
                FROM dbo.StudyHistory b JOIN #Bars s ON s.RawTime=b.RawTime WHERE b.StudyColId=@Id AND
                    (b.CheckpointState IS NOT NULL OR b.CheckpointSavedAt IS NOT NULL OR EXISTS(
                    SELECT b.[Open],b.High,b.Low,b.[Close],b.Volume,b.Studies,b.StudiesOverride,b.MarketLiveDefinitionHash,b.StudiesBaseDefinitionHash EXCEPT SELECT s.[Open],s.High,s.Low,s.[Close],s.Volume,s.Studies,s.StudiesOverride,@Definition,@BaseDefinition));
            INSERT dbo.StudyHistory(StudyColId,RawTime,[Open],High,Low,[Close],Volume,Studies,StudiesOverride,MarketLiveDefinitionHash,StudiesBaseDefinitionHash)
                SELECT @Id,s.RawTime,s.[Open],s.High,s.Low,s.[Close],s.Volume,s.Studies,s.StudiesOverride,@Definition,@BaseDefinition FROM #Bars s
                WHERE NOT EXISTS(SELECT 1 FROM dbo.StudyHistory b WHERE b.StudyColId=@Id AND b.RawTime=s.RawTime);
            """,c,tx))
        {cmd.Parameters.AddWithValue("@Id",monitor.StudyCol.Id);cmd.Parameters.AddWithValue("@Definition",batch.DefinitionHash);cmd.Parameters.AddWithValue("@BaseDefinition",CheckpointReplay.DefinitionHash(SqlMonitorLoader.Identity(monitor)));await cmd.ExecuteNonQueryAsync(ct);}
        }
        await MarketLiveCheckpointStore.SaveAsync(c,tx,monitor.StudyCol.Id,batch,batch.Bars[0].Price.RawTime,ct,timing,true,beginRun&&!reset,previous);
        }
        using(timing.Stage("Progress upsert"))
        await using(var cmd=new SqlCommand("""
            UPDATE dbo.StudyRegeneration SET DefinitionHash=@Definition,CompletedCount=@Count,TotalCount=@Total,PrefixHash=@Hash,Status='Running',UpdatedAt=SYSUTCDATETIME(),Message='' WHERE StudyColId=@Id;
            IF @@ROWCOUNT=0 INSERT dbo.StudyRegeneration(StudyColId,DefinitionHash,CompletedCount,TotalCount,PrefixHash,Status,UpdatedAt) VALUES(@Id,@Definition,@Count,@Total,@Hash,'Running',SYSUTCDATETIME());
            """,c,tx))
        {cmd.Parameters.AddWithValue("@Id",monitor.StudyCol.Id);cmd.Parameters.AddWithValue("@Definition",batch.DefinitionHash);cmd.Parameters.AddWithValue("@Count",batch.PrefixCount+batch.Bars.Count);cmd.Parameters.AddWithValue("@Total",total);cmd.Parameters.AddWithValue("@Hash",prefixHash);await cmd.ExecuteNonQueryAsync(ct);}
        using(timing.Stage("Commit"))await tx.CommitAsync(ct);timing.Committed=true;
    }

    private static async Task InsertDirectAsync(SqlConnection c,SqlTransaction tx,CollectionMonitor monitor,CheckpointReplayResult batch,
        IReadOnlyList<PriceDataRS> raw,long from,long to,bool reset,long? previous,bool validatePrices,StudyWriteProfiler timing,CancellationToken ct)
    {
        if(validatePrices)
        {
            using var stage=timing.Stage("Source window validation (direct)");
            await using var cmd=new SqlCommand("SELECT Id,RawTime,[Open],High,Low,[Close],Volume FROM dbo.PriceData WITH(HOLDLOCK) WHERE DataSetId=@Id AND RawTime>=@From AND RawTime<@To ORDER BY RawTime OPTION(RECOMPILE,MAXDOP 1)",c,tx);
            cmd.Parameters.AddWithValue("@Id",monitor.DataSetId);cmd.Parameters.AddWithValue("@From",from);cmd.Parameters.AddWithValue("@To",to);
            await using var reader=await cmd.ExecuteReaderAsync(ct);int n=0;
            while(await reader.ReadAsync(ct))
            {
                if(n>=raw.Count)throw new InvalidDataException("Source prices changed during full regeneration.");
                var p=raw[n++];
                if(Convert.ToInt32(reader[0])!=p.Id||Convert.ToInt64(reader[1])!=p.RawTime||reader.GetDecimal(2)!=p.Open||reader.GetDecimal(3)!=p.High||reader.GetDecimal(4)!=p.Low||reader.GetDecimal(5)!=p.Close||reader.GetInt32(6)!=p.Volume)
                    throw new InvalidDataException("Source prices changed during full regeneration.");
            }
            if(n!=raw.Count)throw new InvalidDataException("Source prices changed during full regeneration.");
        }
        else {using var stage=timing.Stage("Source validation skipped (historical regeneration)");}
        if(reset)
        {
            using var stage=timing.Stage("Full history delete");
            await using var delete=new SqlCommand("DELETE FROM dbo.StudyHistory WHERE StudyColId=@Id; DELETE FROM dbo.StudyRegeneration WHERE StudyColId=@Id;",c,tx);
            delete.Parameters.AddWithValue("@Id",monitor.StudyCol.Id);await delete.ExecuteNonQueryAsync(ct);
        }
        else if(previous.HasValue)
        {
            using var stage=timing.Stage("Boundary checkpoint cleanup");
            await using var cleanup=new SqlCommand("UPDATE dbo.StudyHistory SET CheckpointState=NULL,CheckpointSavedAt=NULL,MarketLivePrefixCount=NULL,MarketLivePrefixHash=NULL WHERE StudyColId=@Id AND RawTime=@Previous AND MarketLivePrefixCount%@Every<>0 AND (@Last IS NULL OR RawTime<>@Last)",c,tx);
            cleanup.Parameters.AddWithValue("@Id",monitor.StudyCol.Id);cleanup.Parameters.AddWithValue("@Previous",previous.Value);cleanup.Parameters.AddWithValue("@Every",batch.EveryBars);cleanup.Parameters.Add("@Last",SqlDbType.BigInt).Value=(object?)batch.LastCompletedRawTime??DBNull.Value;await cleanup.ExecuteNonQueryAsync(ct);
        }
        using var table=new DataTable();table.Columns.Add("StudyColId",typeof(int));table.Columns.Add("RawTime",typeof(long));
        foreach(string name in new[]{"Open","High","Low","Close"})table.Columns.Add(name,typeof(decimal));
        table.Columns.Add("Volume",typeof(int));table.Columns.Add("Studies",typeof(byte[])); table.Columns.Add("StudiesOverride",typeof(byte[]));table.Columns.Add("MarketLiveDefinitionHash",typeof(byte[]));table.Columns.Add("StudiesBaseDefinitionHash",typeof(byte[]));
        table.Columns.Add("CheckpointState",typeof(string));table.Columns.Add("CheckpointSavedAt",typeof(DateTime));table.Columns.Add("MarketLivePrefixCount",typeof(int));table.Columns.Add("MarketLivePrefixHash",typeof(byte[]));
        using(timing.Stage("Build direct buffers"))
        {
            var checkpoints=batch.Checkpoints.ToDictionary(p=>p.Payload.RawTime);var saved=DateTime.UtcNow;
            var baseDefinition=CheckpointReplay.DefinitionHash(SqlMonitorLoader.Identity(monitor));
            foreach(var b in batch.Bars)
            {
                var p=b.Price;var row=table.Rows.Add(monitor.StudyCol.Id,p.RawTime,p.Open,p.High,p.Low,p.Close,p.Volume,b.Studies,(object?)b.StudiesOverride??DBNull.Value,batch.DefinitionHash,baseDefinition);
                if(checkpoints.Remove(p.RawTime,out var checkpoint))
                {row["CheckpointState"]=checkpoint.Payload.Json;row["CheckpointSavedAt"]=saved;row["MarketLivePrefixCount"]=checkpoint.Count;row["MarketLivePrefixHash"]=checkpoint.PrefixHash;}
            }
            if(checkpoints.Count!=0)throw new InvalidDataException("Checkpoint requires a matching direct history row.");
        }
        using(timing.Stage("StudyHistory direct bulk insert"))
        using(var copy=new SqlBulkCopy(c,SqlBulkCopyOptions.CheckConstraints,tx){DestinationTableName="dbo.StudyHistory",BulkCopyTimeout=new SqlConnectionStringBuilder(c.ConnectionString).CommandTimeout})
        {foreach(DataColumn column in table.Columns)copy.ColumnMappings.Add(column.ColumnName,column.ColumnName);await copy.WriteToServerAsync(table,ct);}
    }
}
