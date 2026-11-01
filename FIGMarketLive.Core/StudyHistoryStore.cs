using System.Data;
using System.Security.Cryptography;
using FIGCommon.Models;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed record PriceSnapshot(IReadOnlyList<PriceDataRS> Prices, byte[] Fingerprint);

public sealed class StudyHistoryStore(string connectionString)
{
    public event Action<StudyWriteTiming>? WriteMeasured;
    public async Task<PriceSnapshot> ReadAsync(CollectionMonitor monitor, CancellationToken ct,bool includeDisabled=false)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await ValidateAsync(connection, null, monitor, ct,includeDisabled);
        return await ReadAsync(connection, null, monitor.DataSetId, ct);
    }

    internal static async Task ValidateAsync(SqlConnection connection, SqlTransaction? tx, CollectionMonitor m, CancellationToken ct,bool includeDisabled=false)
    {
        var loaded=await SqlMonitorLoader.LoadAsync(connection,tx,ct,includeDisabled);
        var current=loaded.Monitors.SingleOrDefault(row=>row.StudyCol.Id==m.StudyCol.Id);
        if(current is null||SqlMonitorLoader.Identity(current)!=SqlMonitorLoader.Identity(m))
            throw new InvalidDataException("SQL StudyCol metadata or its selected source dataset changed; configuration will reload next pass.");
        await using var command = new SqlCommand("""
            IF NOT EXISTS(SELECT 1 FROM dbo.StudyCol WHERE Id=@StudyColId AND TickerId=@TickerId AND IntervalId=@IntervalId
                AND ColType=@ColType AND ColParams=@ColParams AND Enabled=@Enabled)
                THROW 51021, 'StudyCol does not match monitor configuration.', 1;
            IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.StudyHistory') AND name='Studies'
                AND system_type_id=165 AND max_length=-1)
                THROW 51022, 'StudyHistory.Studies must be varbinary(max). Run database setup to migrate legacy storage.', 1;
            """, connection, tx);
        command.Parameters.AddWithValue("@DataSetId", m.DataSetId);
        command.Parameters.AddWithValue("@TickerId", m.Ticker.Id);
        command.Parameters.AddWithValue("@IntervalId", m.Interval.Id);
        command.Parameters.AddWithValue("@StudyColId", m.StudyCol.Id);
        command.Parameters.AddWithValue("@ColType", m.StudyCol.ColType);
        command.Parameters.AddWithValue("@Enabled",m.StudyCol.Enabled);
        command.Parameters.AddWithValue("@ColParams", m.StudyCol.ColParams);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<PriceSnapshot> ReadAsync(SqlConnection connection, SqlTransaction? tx, int dataSetId, CancellationToken ct)
    {
        string hint = tx is null ? "" : " WITH (UPDLOCK,HOLDLOCK)";
        await using var command = new SqlCommand($"SELECT Id,RawTime,[Open],High,Low,[Close],Volume FROM dbo.PriceData{hint} WHERE DataSetId=@Id ORDER BY RawTime", connection, tx);
        command.Parameters.AddWithValue("@Id", dataSetId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var prices = new List<PriceDataRS>();
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using var bytes = new MemoryStream();
        using var writer = new BinaryWriter(bytes);
        while (await reader.ReadAsync(ct))
        {
            var p = new PriceDataRS
            {
                Id=reader.GetInt32(0), DataSetId=dataSetId, RawTime=Convert.ToInt64(reader.GetValue(1)),
                PriceDate=DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(reader.GetValue(1))).UtcDateTime, Open=reader.GetDecimal(2),
                High=reader.GetDecimal(3), Low=reader.GetDecimal(4), Close=reader.GetDecimal(5), Volume=reader.GetInt32(6)
            };
            prices.Add(p);
            bytes.SetLength(0); bytes.Position=0;
            writer.Write(p.Id); writer.Write(p.RawTime);
            writer.Write(p.Open); writer.Write(p.High); writer.Write(p.Low); writer.Write(p.Close); writer.Write(p.Volume);
            writer.Flush(); hash.AppendData(bytes.GetBuffer().AsSpan(0,(int)bytes.Length));
        }
        return new(prices, hash.GetHashAndReset());
    }

    public Task<bool> PublishAsync(CollectionMonitor monitor,PriceSnapshot source,CheckpointReplayResult run,CancellationToken ct)
        =>PublishCoreAsync(monitor,source,run.Bars,run,ct);
    public Task<bool> PublishAsync(CollectionMonitor monitor,PriceSnapshot source,IReadOnlyList<EncodedStudyBar> bars,CancellationToken ct)
        =>PublishCoreAsync(monitor,source,bars,null,ct);
    private async Task<bool> PublishCoreAsync(CollectionMonitor monitor, PriceSnapshot source,
        IReadOnlyList<EncodedStudyBar> bars,CheckpointReplayResult? run,CancellationToken ct)
    {
        if (source.Prices.Count == 0) throw new InvalidDataException("Source dataset is empty; no study rows were changed.");
        var expected=PriceBarAggregation.Build(source.Prices,monitor.SourceInterval,monitor.Interval,ct);
        int prefix=run?.PrefixCount??0;
        if(prefix<0||prefix>=expected.Count||bars.Count!=expected.Count-prefix||bars.Where((b,i)=>!CheckpointReplay.SameInputs(b.Price,expected[i+prefix])).Any())
            throw new InvalidDataException("Study results do not cover the source snapshot.");
        if(run is not null&&(run.DefinitionHash.Length!=32||run.EveryBars<1||run.Checkpoints.Any(p=>p.Count<=prefix||p.Count>=expected.Count||p.PrefixHash.Length!=32||p.Payload.RawTime!=expected[p.Count-1].RawTime)))
            throw new InvalidDataException("Invalid checkpoint publication batch.");
        using var timing=new StudyWriteProfiler(monitor.StudyCol.Id,bars.Count,run?.Checkpoints.Count??0,m=>WriteMeasured?.Invoke(m));
        await using var connection = new SqlConnection(connectionString);
        using(timing.Stage("Open connection"))await connection.OpenAsync(ct);
        await using var tx = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        using(timing.Stage("Metadata + source validation"))
        {
        await using (var claim = new SqlCommand("SELECT Id FROM dbo.StudyCol WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Id",connection,tx))
        { claim.Parameters.AddWithValue("@Id",monitor.StudyCol.Id); await claim.ExecuteScalarAsync(ct); }
        await ValidateAsync(connection,tx,monitor,ct);
        var current = await ReadAsync(connection,tx,monitor.DataSetId,ct);
        if (!current.Fingerprint.AsSpan().SequenceEqual(source.Fingerprint))
        { await tx.RollbackAsync(ct); return false; }
        }
        using(timing.Stage("Create expected staging"))
        await using(var create=new SqlCommand("SELECT TOP(0) RawTime,[Open],High,Low,[Close],Volume INTO #Expected FROM dbo.StudyHistory",connection,tx))await create.ExecuteNonQueryAsync(ct);
        var all=new DataTable();all.Columns.Add("RawTime",typeof(long));foreach(var name in new[]{"Open","High","Low","Close"})all.Columns.Add(name,typeof(decimal));all.Columns.Add("Volume",typeof(int));
        foreach(var price in expected)all.Rows.Add(price.RawTime,price.Open,price.High,price.Low,price.Close,price.Volume);
        using(timing.Stage("Bulk expected input"))
        using(var bulk=new SqlBulkCopy(connection,SqlBulkCopyOptions.Default,tx){DestinationTableName="#Expected",BulkCopyTimeout=new SqlConnectionStringBuilder(connectionString).CommandTimeout})
        {foreach(DataColumn column in all.Columns)bulk.ColumnMappings.Add(column.ColumnName,column.ColumnName);await bulk.WriteToServerAsync(all,ct);}
        using(timing.Stage("Create results staging"))
        await using (var create = new SqlCommand("SELECT TOP (0) RawTime,[Open],High,Low,[Close],Volume,Studies,StudiesOverride INTO #Bars FROM dbo.StudyHistory",connection,tx))
            await create.ExecuteNonQueryAsync(ct);
        var table = new DataTable();
        table.Columns.Add("RawTime",typeof(long));
        foreach (var name in new[]{"Open","High","Low","Close"}) table.Columns.Add(name,typeof(decimal));
        table.Columns.Add("Volume",typeof(int)); table.Columns.Add("Studies",typeof(byte[])); table.Columns.Add("StudiesOverride",typeof(byte[]));
        foreach (var b in bars) table.Rows.Add(b.Price.RawTime,b.Price.Open,b.Price.High,b.Price.Low,b.Price.Close,b.Price.Volume,b.Studies,(object?)b.StudiesOverride??DBNull.Value);
        using(timing.Stage("Bulk study results"))
        using (var bulk = new SqlBulkCopy(connection,SqlBulkCopyOptions.Default,tx))
        {
            bulk.DestinationTableName="#Bars";bulk.BulkCopyTimeout=new SqlConnectionStringBuilder(connectionString).CommandTimeout;
            foreach (DataColumn c in table.Columns) bulk.ColumnMappings.Add(c.ColumnName,c.ColumnName);
            await bulk.WriteToServerAsync(table,ct);
        }
        await using var publish = new SqlCommand("""
            IF EXISTS(SELECT 1 FROM dbo.StudyHistory s WHERE s.StudyColId=@Id
                AND NOT EXISTS(SELECT 1 FROM #Expected b WHERE b.RawTime=s.RawTime))
                THROW 51023, 'Stored studies contain bars missing from source prices. Restore source coverage before publishing.', 1;
            IF @Prefix IS NOT NULL AND EXISTS(SELECT 1 FROM #Expected b WHERE b.RawTime<=@Prefix AND NOT EXISTS(
                SELECT 1 FROM dbo.StudyHistory s WHERE s.StudyColId=@Id AND s.RawTime=b.RawTime AND s.[Open]=b.[Open] AND s.High=b.High AND s.Low=b.Low AND s.[Close]=b.[Close] AND s.Volume=b.Volume
                AND DATALENGTH(s.Studies)>=10 AND SUBSTRING(s.Studies,1,4)=0x46494753))
                THROW 51024,'Published checkpoint prefix changed during calculation. Retry recovery.',1;
            UPDATE s SET [Open]=b.[Open],High=b.High,Low=b.Low,[Close]=b.[Close],Volume=b.Volume,Studies=b.Studies,StudiesOverride=b.StudiesOverride,StudiesBaseDefinitionHash=@BaseDefinition,
                CheckpointState=NULL,CheckpointSavedAt=NULL,MarketLivePrefixCount=NULL,MarketLivePrefixHash=NULL,MarketLiveDefinitionHash=@Definition
            FROM dbo.StudyHistory s JOIN #Bars b ON s.RawTime=b.RawTime WHERE s.StudyColId=@Id AND
                (s.CheckpointState IS NOT NULL OR s.CheckpointSavedAt IS NOT NULL OR EXISTS(
                SELECT s.[Open],s.High,s.Low,s.[Close],s.Volume,s.Studies,s.StudiesOverride,s.MarketLiveDefinitionHash,s.StudiesBaseDefinitionHash EXCEPT
                SELECT b.[Open],b.High,b.Low,b.[Close],b.Volume,b.Studies,b.StudiesOverride,@Definition,@BaseDefinition));
            INSERT dbo.StudyHistory(StudyColId,RawTime,[Open],High,Low,[Close],Volume,Studies,StudiesOverride,MarketLiveDefinitionHash,StudiesBaseDefinitionHash)
                SELECT @Id,b.RawTime,b.[Open],b.High,b.Low,b.[Close],b.Volume,b.Studies,b.StudiesOverride,@Definition,@BaseDefinition FROM #Bars b
                WHERE NOT EXISTS(SELECT 1 FROM dbo.StudyHistory s WHERE s.StudyColId=@Id AND s.RawTime=b.RawTime);
            """, connection,tx);
        publish.Parameters.AddWithValue("@Id",monitor.StudyCol.Id);
        publish.Parameters.AddWithValue("@BaseDefinition",CheckpointReplay.DefinitionHash(SqlMonitorLoader.Identity(monitor)));
        publish.Parameters.Add("@Definition",SqlDbType.VarBinary,32).Value=(object?)run?.DefinitionHash??DBNull.Value;
        publish.Parameters.Add("@Prefix",SqlDbType.BigInt).Value=prefix>0?expected[prefix-1].RawTime:DBNull.Value;
        using(timing.Stage("StudyHistory validation + outputs upsert"))await publish.ExecuteNonQueryAsync(ct);
        if(run is not null)await MarketLiveCheckpointStore.SaveAsync(connection,tx,monitor.StudyCol.Id,run,bars[0].Price.RawTime,ct,timing);
        using(timing.Stage("Commit"))await tx.CommitAsync(ct);timing.Committed=true;
        return true;
    }
}
