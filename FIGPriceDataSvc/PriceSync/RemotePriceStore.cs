using System.Data;
using System.Text.Json;
using FIGCommon.DataAccess;
using FIGCommon.Models;
using FIGCommon.Models.FIGProviderAPI;
using Microsoft.Data.SqlClient;

namespace FIGPriceDataSvc.PriceSync;

public interface IRemotePriceStore
{
    int[] GetDataSetIds();
    DataSetRS? GetDataSet(int id);
    Task<long?> LastRawTimeAsync(int id,CancellationToken ct);
    Task SaveAsync(int id,IReadOnlyList<HistoricalPriceBar> bars,CancellationToken ct,bool updateOnly=false);
}
public sealed class RemotePriceStore : IRemotePriceStore
{
    private readonly Func<string> connectionString;
    private readonly Func<int,DataSetRS?> dataset;
    public RemotePriceStore():this(()=>MainRepo.ConnectionString,MainRepo.GetDataSet) { }
    public RemotePriceStore(Func<string> connectionString,Func<int,DataSetRS?> dataset)
    { this.connectionString=connectionString; this.dataset=dataset; }
    public DataSetRS? GetDataSet(int id)=>dataset(id);
    public int[] GetDataSetIds()=>MainRepo.GetDataSets().Where(d=>d.IntervalId=="1").Select(d=>d.Id).ToArray();
    public async Task<long?> LastRawTimeAsync(int id,CancellationToken ct)
    {
        await using var connection=new SqlConnection(connectionString()); await connection.OpenAsync(ct);
        await using var command=new SqlCommand("SELECT MAX(RawTime) FROM dbo.PriceData WHERE DataSetId=@Id",connection);
        command.Parameters.AddWithValue("@Id",id);
        var result=await command.ExecuteScalarAsync(ct);
        return result is null or DBNull ? null : Convert.ToInt64(result);
    }
    public async Task SaveAsync(int id,IReadOnlyList<HistoricalPriceBar> bars,CancellationToken ct,bool updateOnly=false)
    {
        if(bars.Count==0) return;
        await using var connection=new SqlConnection(connectionString()); await connection.OpenAsync(ct);
        await using var tx=(SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        await using var command=new SqlCommand("""
            SET XACT_ABORT ON;
            -- Serialize writers for this dataset; the existing unique (DataSetId,RawTime) index remains authoritative.
            IF NOT EXISTS(SELECT 1 FROM dbo.DataSet WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Id)
                THROW 51010, 'Local price dataset no longer exists.', 1;
            DECLARE @Bars TABLE(RawTime int PRIMARY KEY,[Open] decimal(18,4),High decimal(18,4),Low decimal(18,4),[Close] decimal(18,4),Volume int);
            INSERT @Bars SELECT RawTime,[Open],High,Low,[Close],Volume FROM OPENJSON(@Json) WITH (
                RawTime int '$.RawTime',[Open] decimal(18,4) '$.Open',High decimal(18,4) '$.High',
                Low decimal(18,4) '$.Low',[Close] decimal(18,4) '$.Close',Volume int '$.Volume');
            UPDATE p SET [Open]=b.[Open],High=b.High,Low=b.Low,[Close]=b.[Close],Volume=b.Volume,
                PriceDate=DATEADD(second,b.RawTime,CONVERT(datetime,'19700101',112))
            FROM dbo.PriceData p JOIN @Bars b ON p.RawTime=b.RawTime WHERE p.DataSetId=@Id AND EXISTS(
                SELECT p.[Open],p.High,p.Low,p.[Close],p.Volume
                EXCEPT SELECT b.[Open],b.High,b.Low,b.[Close],b.Volume);
            INSERT dbo.PriceData(DataSetId,RawTime,PriceDate,[Open],High,Low,[Close],Volume)
                SELECT @Id,b.RawTime,DATEADD(second,b.RawTime,CONVERT(datetime,'19700101',112)),b.[Open],b.High,b.Low,b.[Close],b.Volume
                FROM @Bars b WHERE @UpdateOnly=0 AND NOT EXISTS(SELECT 1 FROM dbo.PriceData p WHERE p.DataSetId=@Id AND p.RawTime=b.RawTime);
            """,connection,tx);
        command.Parameters.AddWithValue("@Id",id);
        command.Parameters.AddWithValue("@UpdateOnly",updateOnly);
        command.Parameters.Add("@Json",SqlDbType.NVarChar,-1).Value=JsonSerializer.Serialize(bars);
        await command.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
    }
}
