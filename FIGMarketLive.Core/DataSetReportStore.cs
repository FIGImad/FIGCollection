using System.Data;
using FIGCommon.Models;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed record DataSetReportChoice(int Id,string Symbol,string LocalSymbol,string Interval)
{public override string ToString()=>$"DataSet {Id} — {Symbol} / {LocalSymbol} — {Interval}";}
public sealed record DataSetReportBar(long RawTime,decimal Open,decimal High,decimal Low,decimal Close,long Volume);
public sealed record DataSetReport(int DataSetId,TickerRS Ticker,string IntervalId,string IntervalName,int IntervalSeconds,
    bool Monitor,long BarCount,int StudyCollections,IReadOnlyList<DataSetReportBar> FirstBars,IReadOnlyList<DataSetReportBar> LastBars);

/// <summary>Read-only, consistent metadata/count/boundary-bar inspection.</summary>
public sealed class DataSetReportStore(string connectionString)
{
    public async Task<List<DataSetReportChoice>> ListAsync(CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("SELECT d.Id,t.Symbol,t.LocalSymbol,i.Name FROM dbo.DataSet d JOIN dbo.Ticker t ON t.Id=d.TickerId JOIN dbo.Interval i ON i.Id=d.IntervalId ORDER BY t.Symbol,i.Seq,d.Id",c);
        await using var r=await cmd.ExecuteReaderAsync(ct);var result=new List<DataSetReportChoice>();
        while(await r.ReadAsync(ct))result.Add(new(r.GetInt32(0),r.GetString(1),r.GetString(2),r.GetString(3)));return result;
    }
    public async Task<DataSetReport> AnalyzeAsync(int id,CancellationToken ct)
    {
        if(id<1)throw new InvalidDataException("Select a dataset to analyze.");
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        await using var cmd=new SqlCommand("""
            SELECT t.Id,t.Symbol,t.LocalSymbol,t.Name,t.SecurityType,t.Currency,t.ExpiryDate,t.MinTick,t.ContractSize,t.Exchange,t.PeriodMultiplier,
                d.IntervalId,i.Name,i.IntervalLen,d.Monitor
            FROM dbo.DataSet d JOIN dbo.Ticker t ON t.Id=d.TickerId JOIN dbo.Interval i ON i.Id=d.IntervalId WHERE d.Id=@Id;
            SELECT COUNT_BIG(*) FROM dbo.PriceData WHERE DataSetId=@Id;
            SELECT TOP(3) RawTime,[Open],High,Low,[Close],Volume FROM dbo.PriceData WHERE DataSetId=@Id ORDER BY RawTime,Id;
            SELECT TOP(3) RawTime,[Open],High,Low,[Close],Volume FROM dbo.PriceData WHERE DataSetId=@Id ORDER BY RawTime DESC,Id DESC;
            """,c,tx){CommandTimeout=120};
        cmd.Parameters.AddWithValue("@Id",id);
        await using var r=await cmd.ExecuteReaderAsync(ct);
        if(!await r.ReadAsync(ct))throw new InvalidDataException("The dataset no longer exists. Refresh datasets and select again.");
        var ticker=new TickerRS{Id=r.GetInt32(0),Symbol=r.GetString(1),LocalSymbol=r.GetString(2),Name=r.GetString(3),SecurityType=r.GetString(4),Currency=r.GetString(5),ExpiryDate=r.GetInt32(6),MinTick=r.GetDecimal(7),ContractSize=r.GetInt32(8),Exchange=r.GetString(9),PeriodMultiplier=r.GetInt32(10)};
        string intervalId=r.GetString(11),intervalName=r.GetString(12);int seconds=r.GetInt32(13);bool monitor=r.GetBoolean(14);
        await r.NextResultAsync(ct);await r.ReadAsync(ct);long count=r.GetInt64(0);
        async Task<List<DataSetReportBar>> Bars()
        {
            await r.NextResultAsync(ct);var bars=new List<DataSetReportBar>();
            while(await r.ReadAsync(ct))bars.Add(new(Convert.ToInt64(r.GetValue(0)),r.GetDecimal(1),r.GetDecimal(2),r.GetDecimal(3),r.GetDecimal(4),Convert.ToInt64(r.GetValue(5))));return bars;
        }
        var first=await Bars();var last=await Bars();last.Reverse();await r.DisposeAsync();
        var catalog=await StudyConfigurationStore.ReadAsync(c,tx,ct);
        int studies=StudyIntervalCompatibility.DependentStudyIds(id,catalog).Length;
        await tx.CommitAsync(ct);
        return new(id,ticker,intervalId,intervalName,seconds,monitor,count,studies,first,last);
    }
}
