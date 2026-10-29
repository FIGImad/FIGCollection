using System.Data;
using System.Security.Cryptography;
using FIGCommon.Models;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed record PriceSnapshot(IReadOnlyList<PriceDataRS> Prices, byte[] Fingerprint);

public sealed class BarStudyStore(string connectionString)
{
    public async Task<PriceSnapshot> ReadAsync(CollectionMonitor monitor, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await ValidateAsync(connection, null, monitor, ct);
        return await ReadAsync(connection, null, monitor.DataSetId, ct);
    }

    private static async Task ValidateAsync(SqlConnection connection, SqlTransaction? tx, CollectionMonitor m, CancellationToken ct)
    {
        var loaded=await SqlMonitorLoader.LoadAsync(connection,tx,ct);
        var current=loaded.Monitors.SingleOrDefault(row=>row.StudyCol.Id==m.StudyCol.Id);
        if(current is null||SqlMonitorLoader.Identity(current)!=SqlMonitorLoader.Identity(m))
            throw new InvalidDataException("SQL StudyCol metadata or its selected source dataset changed; configuration will reload next pass.");
        await using var command = new SqlCommand("""
            IF NOT EXISTS(SELECT 1 FROM dbo.StudyCol WHERE Id=@StudyColId AND TickerId=@TickerId AND IntervalId=@IntervalId
                AND ColType=@ColType AND ColParams=@ColParams AND Enabled=1)
                THROW 51021, 'StudyCol does not match monitor configuration.', 1;
            IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.BarStudy') AND name='Studies'
                AND system_type_id=165 AND max_length=-1)
                THROW 51022, 'BarStudy.Studies must be varbinary(max). Existing JSON databases require a deliberate migration.', 1;
            """, connection, tx);
        command.Parameters.AddWithValue("@DataSetId", m.DataSetId);
        command.Parameters.AddWithValue("@TickerId", m.Ticker.Id);
        command.Parameters.AddWithValue("@IntervalId", m.Interval.Id);
        command.Parameters.AddWithValue("@StudyColId", m.StudyCol.Id);
        command.Parameters.AddWithValue("@ColType", m.StudyCol.ColType);
        command.Parameters.AddWithValue("@ColParams", m.StudyCol.ColParams);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<PriceSnapshot> ReadAsync(SqlConnection connection, SqlTransaction? tx, int dataSetId, CancellationToken ct)
    {
        string hint = tx is null ? "" : " WITH (UPDLOCK,HOLDLOCK)";
        await using var command = new SqlCommand($"SELECT Id,RawTime,PriceDate,[Open],High,Low,[Close],Volume FROM dbo.PriceData{hint} WHERE DataSetId=@Id ORDER BY RawTime", connection, tx);
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
                PriceDate=DateTime.SpecifyKind(reader.GetDateTime(2),DateTimeKind.Utc), Open=reader.GetDecimal(3),
                High=reader.GetDecimal(4), Low=reader.GetDecimal(5), Close=reader.GetDecimal(6), Volume=reader.GetInt32(7)
            };
            prices.Add(p);
            bytes.SetLength(0); bytes.Position=0;
            writer.Write(p.Id); writer.Write(p.RawTime); writer.Write(p.PriceDate.Ticks);
            writer.Write(p.Open); writer.Write(p.High); writer.Write(p.Low); writer.Write(p.Close); writer.Write(p.Volume);
            writer.Flush(); hash.AppendData(bytes.GetBuffer().AsSpan(0,(int)bytes.Length));
        }
        return new(prices, hash.GetHashAndReset());
    }

    public async Task<bool> PublishAsync(CollectionMonitor monitor, PriceSnapshot source,
        IReadOnlyList<EncodedStudyBar> bars, CancellationToken ct)
    {
        if (source.Prices.Count == 0) throw new InvalidDataException("Source dataset is empty; no study rows were changed.");
        var expected=PriceBarAggregation.Build(source.Prices,monitor.SourceInterval,monitor.Interval,ct);
        if (bars.Count != expected.Count || bars.Where((b,i) => b.Price.RawTime != expected[i].RawTime || b.Price.Open!=expected[i].Open ||
            b.Price.High!=expected[i].High || b.Price.Low!=expected[i].Low || b.Price.Close!=expected[i].Close || b.Price.Volume!=expected[i].Volume).Any())
            throw new InvalidDataException("Study results do not cover the source snapshot.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var tx = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await using (var claim = new SqlCommand("SELECT Id FROM dbo.StudyCol WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Id",connection,tx))
        { claim.Parameters.AddWithValue("@Id",monitor.StudyCol.Id); await claim.ExecuteScalarAsync(ct); }
        await ValidateAsync(connection,tx,monitor,ct);
        var current = await ReadAsync(connection,tx,monitor.DataSetId,ct);
        if (!current.Fingerprint.AsSpan().SequenceEqual(source.Fingerprint))
        { await tx.RollbackAsync(ct); return false; }
        await using (var create = new SqlCommand("SELECT TOP (0) RawTime,[Open],High,Low,[Close],Volume,Studies INTO #Bars FROM dbo.BarStudy",connection,tx))
            await create.ExecuteNonQueryAsync(ct);
        var table = new DataTable();
        table.Columns.Add("RawTime",typeof(long));
        foreach (var name in new[]{"Open","High","Low","Close"}) table.Columns.Add(name,typeof(decimal));
        table.Columns.Add("Volume",typeof(int)); table.Columns.Add("Studies",typeof(byte[]));
        foreach (var b in bars) table.Rows.Add(b.Price.RawTime,b.Price.Open,b.Price.High,b.Price.Low,b.Price.Close,b.Price.Volume,b.Studies);
        using (var bulk = new SqlBulkCopy(connection,SqlBulkCopyOptions.Default,tx))
        {
            bulk.DestinationTableName="#Bars";
            foreach (DataColumn c in table.Columns) bulk.ColumnMappings.Add(c.ColumnName,c.ColumnName);
            await bulk.WriteToServerAsync(table,ct);
        }
        await using var publish = new SqlCommand("""
            IF EXISTS(SELECT 1 FROM dbo.BarStudy s WHERE s.StudyColId=@Id
                AND NOT EXISTS(SELECT 1 FROM #Bars b WHERE b.RawTime=s.RawTime))
                THROW 51023, 'Stored studies contain bars missing from source prices. Restore source coverage before publishing.', 1;
            UPDATE s SET [Open]=b.[Open],High=b.High,Low=b.Low,[Close]=b.[Close],Volume=b.Volume,Studies=b.Studies
            FROM dbo.BarStudy s JOIN #Bars b ON s.RawTime=b.RawTime WHERE s.StudyColId=@Id AND EXISTS(
                SELECT s.[Open],s.High,s.Low,s.[Close],s.Volume,s.Studies EXCEPT
                SELECT b.[Open],b.High,b.Low,b.[Close],b.Volume,b.Studies);
            INSERT dbo.BarStudy(StudyColId,RawTime,[Open],High,Low,[Close],Volume,Studies)
                SELECT @Id,b.RawTime,b.[Open],b.High,b.Low,b.[Close],b.Volume,b.Studies FROM #Bars b
                WHERE NOT EXISTS(SELECT 1 FROM dbo.BarStudy s WHERE s.StudyColId=@Id AND s.RawTime=b.RawTime);
            """, connection,tx);
        publish.Parameters.AddWithValue("@Id",monitor.StudyCol.Id);
        await publish.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }
}
