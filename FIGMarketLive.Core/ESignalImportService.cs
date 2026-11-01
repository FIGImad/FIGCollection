using System.Data;
using System.Security.Cryptography;
using FIGPriceCsvConverter;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed record ESignalPrices(IReadOnlyList<PriceRow> Rows,bool HasVolume,int IntervalSeconds);
public sealed record ImportPriceState(long Count,long? First,long? Last,string Fingerprint);
public enum PriceImportAction { Insert, Replace, Upsert, NoChange }
public sealed record PriceImportPlan(int TickerId,string Symbol,string IntervalId,string IntervalName,int? DataSetId,
    ESignalPrices File,ImportPriceState State,PriceImportAction Action);

public sealed class ESignalImportService(string connectionString)
{
    public static ESignalPrices ReadFile(string path,TimeZoneInfo zone,CancellationToken ct=default)
    {
        var parsed=Converter.ReadFile(path,zone,AmbiguousTimePolicy.Reject,true,cancellation:ct);
        foreach(var r in parsed.Rows)
            if(r.Volume>int.MaxValue || r.RawTime<=0)throw new InvalidDataException("Prices require positive UTC timestamps and volume within SQL int capacity.");
        return new(parsed.Rows,parsed.HasVolume,DetectInterval(parsed.Rows));
    }

    public static int DetectInterval(IReadOnlyList<PriceRow> rows)
    {
        if(rows.Count<2)throw new InvalidDataException("At least two distinct price bars are needed to detect an interval.");
        long smallest=long.MaxValue;
        for(int i=1;i<rows.Count;i++)
        {long delta=rows[i].RawTime-rows[i-1].RawTime;if(delta<=0)throw new InvalidDataException("Price bars must be unique and chronological.");smallest=Math.Min(smallest,delta);}
        if(smallest>int.MaxValue || rows.Skip(1).Where((r,i)=>(r.RawTime-rows[i].RawTime)%smallest!=0).Any())
            throw new InvalidDataException("The file does not have a consistent time-bar interval. Tick/range bars and mixed intervals cannot be imported.");
        return (int)smallest;
    }

    public async Task<List<StudyTicker>> TickersAsync(CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("SELECT Id,Symbol FROM dbo.Ticker ORDER BY Symbol,Id",c);
        await using var r=await cmd.ExecuteReaderAsync(ct);var result=new List<StudyTicker>();
        while(await r.ReadAsync(ct))result.Add(new(r.GetInt32(0),r.GetString(1)));return result;
    }

    public static PriceImportAction Decide(ImportPriceState state,long first,long last)
    {
        if(state.Count==0)return PriceImportAction.Insert;
        if(first<state.First!.Value)return PriceImportAction.Replace;
        return last>state.Last!.Value?PriceImportAction.Upsert:PriceImportAction.NoChange;
    }

    public async Task<PriceImportPlan> PreviewAsync(int tickerId,ESignalPrices file,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        string symbol;
        await using(var cmd=new SqlCommand("SELECT Symbol FROM dbo.Ticker WHERE Id=@Id",c,tx))
        {cmd.Parameters.AddWithValue("@Id",tickerId);symbol=await cmd.ExecuteScalarAsync(ct) as string??throw new InvalidDataException("Select an existing ticker.");}
        string intervalId,name;
        await using(var cmd=new SqlCommand("SELECT TOP(1) Id,Name FROM dbo.Interval WHERE IntervalLen=@Seconds ORDER BY Seq,Id",c,tx))
        {
            cmd.Parameters.AddWithValue("@Seconds",file.IntervalSeconds);await using var r=await cmd.ExecuteReaderAsync(ct);
            if(!await r.ReadAsync(ct))throw new InvalidDataException($"Detected {file.IntervalSeconds}-second bars, but no matching interval is defined in SQL.");
            intervalId=r.GetString(0);name=r.GetString(1);
        }
        int? id=await FindDataSetAsync(c,tx,tickerId,intervalId,ct);
        var state=id is null?new ImportPriceState(0,null,null,""):await StateAsync(c,tx,id.Value,ct);
        await tx.CommitAsync(ct);
        return new(tickerId,symbol,intervalId,name,id,file,state,Decide(state,file.Rows[0].RawTime,file.Rows[^1].RawTime));
    }

    public async Task<int> ImportConfirmedAsync(PriceImportPlan plan,bool approveCreate,CancellationToken ct)
    {
        if(plan.Action==PriceImportAction.NoChange)throw new InvalidOperationException("File does not extend the existing price range; no import is needed.");
        if(plan.DataSetId is null&&!approveCreate)throw new InvalidOperationException("Dataset creation was not approved. Import cancelled.");
        await new PriceEventSetup(connectionString).EnsureSchemaAsync(ct);
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        await using(var metadata=new SqlCommand("SELECT t.Symbol FROM dbo.Ticker t WITH(UPDLOCK,HOLDLOCK) WHERE t.Id=@Ticker AND EXISTS(SELECT 1 FROM dbo.Interval WITH(HOLDLOCK) WHERE Id=@Interval AND IntervalLen=@Seconds)",c,tx))
        {
            metadata.Parameters.AddWithValue("@Ticker",plan.TickerId);metadata.Parameters.AddWithValue("@Interval",plan.IntervalId);metadata.Parameters.AddWithValue("@Seconds",plan.File.IntervalSeconds);
            if(await metadata.ExecuteScalarAsync(ct) as string!=plan.Symbol)throw new InvalidDataException("Ticker or interval changed. Preview the import again.");
        }
        int? id=await FindDataSetAsync(c,tx,plan.TickerId,plan.IntervalId,ct);
        if(id!=plan.DataSetId)throw new InvalidDataException("Dataset changed after preview. Preview the import again.");
        if(id is null)
        {
            await using var create=new SqlCommand("INSERT dbo.DataSet(TickerId,IntervalId) VALUES(@Ticker,@Interval); SELECT CONVERT(int,SCOPE_IDENTITY());",c,tx);
            create.Parameters.AddWithValue("@Ticker",plan.TickerId);create.Parameters.AddWithValue("@Interval",plan.IntervalId);id=(int)(await create.ExecuteScalarAsync(ct))!;
        }
        else if(await StateAsync(c,tx,id.Value,ct)!=plan.State)throw new InvalidDataException("Prices changed after preview. Preview the import again.");
        await using(var stage=new SqlCommand("SELECT TOP(0) RawTime,[Open],High,Low,[Close],Volume INTO #Import FROM dbo.PriceData",c,tx))await stage.ExecuteNonQueryAsync(ct);
        var table=new DataTable();table.Columns.Add("RawTime",typeof(long));
        foreach(var name in new[]{"Open","High","Low","Close"})table.Columns.Add(name,typeof(decimal));table.Columns.Add("Volume",typeof(int));
        foreach(var row in plan.File.Rows)
        {ct.ThrowIfCancellationRequested();table.Rows.Add(row.RawTime,row.Open,row.High,row.Low,row.Close,checked((int)row.Volume));}
        using(var bulk=new SqlBulkCopy(c,SqlBulkCopyOptions.CheckConstraints,tx){DestinationTableName="#Import",BulkCopyTimeout=0})
        {foreach(DataColumn col in table.Columns)bulk.ColumnMappings.Add(col.ColumnName,col.ColumnName);await bulk.WriteToServerAsync(table,ct);}
        await using var write=new SqlCommand("""
            IF @Replace=1 DELETE dbo.PriceData WHERE DataSetId=@Id;
            UPDATE p SET [Open]=s.[Open],High=s.High,Low=s.Low,[Close]=s.[Close],Volume=s.Volume
                FROM dbo.PriceData p JOIN #Import s ON p.RawTime=s.RawTime WHERE p.DataSetId=@Id;
            INSERT dbo.PriceData(DataSetId,RawTime,[Open],High,Low,[Close],Volume)
                SELECT @Id,s.RawTime,s.[Open],s.High,s.Low,s.[Close],s.Volume FROM #Import s
                WHERE NOT EXISTS(SELECT 1 FROM dbo.PriceData p WHERE p.DataSetId=@Id AND p.RawTime=s.RawTime);
            """,c,tx){CommandTimeout=0};
        write.Parameters.AddWithValue("@Id",id.Value);write.Parameters.AddWithValue("@Replace",plan.Action==PriceImportAction.Replace);
        await write.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);return id.Value;
    }

    private static async Task<int?> FindDataSetAsync(SqlConnection c,SqlTransaction tx,int tickerId,string interval,CancellationToken ct)
    {
        await using var cmd=new SqlCommand("SELECT Id FROM dbo.DataSet WITH(UPDLOCK,HOLDLOCK) WHERE TickerId=@Ticker AND IntervalId=@Interval",c,tx);
        cmd.Parameters.AddWithValue("@Ticker",tickerId);cmd.Parameters.AddWithValue("@Interval",interval);var value=await cmd.ExecuteScalarAsync(ct);return value is null?null:(int)value;
    }

    private static async Task<ImportPriceState> StateAsync(SqlConnection c,SqlTransaction tx,int id,CancellationToken ct)
    {
        await using var cmd=new SqlCommand("SELECT Id,RawTime,[Open],High,Low,[Close],Volume FROM dbo.PriceData WITH(UPDLOCK,HOLDLOCK) WHERE DataSetId=@Id ORDER BY RawTime",c,tx){CommandTimeout=0};cmd.Parameters.AddWithValue("@Id",id);
        await using var r=await cmd.ExecuteReaderAsync(ct);long count=0;long? first=null,last=null;
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);using var bytes=new MemoryStream();using var writer=new BinaryWriter(bytes);
        while(await r.ReadAsync(ct))
        {
            long time=Convert.ToInt64(r.GetValue(1));first??=time;last=time;count++;
            bytes.SetLength(0);bytes.Position=0;writer.Write(r.GetInt32(0));writer.Write(time);
            for(int i=2;i<6;i++)writer.Write(r.GetDecimal(i));writer.Write(r.GetInt32(6));writer.Flush();hash.AppendData(bytes.GetBuffer().AsSpan(0,(int)bytes.Length));
        }
        return new(count,first,last,Convert.ToHexString(hash.GetHashAndReset()));
    }
}
