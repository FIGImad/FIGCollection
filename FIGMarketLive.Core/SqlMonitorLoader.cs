using System.Text.Json;
using FIGCommon.Models;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed record MonitorLoadIssue(int StudyColId,string Message);
public sealed record MonitorLoadResult(List<CollectionMonitor> Monitors,List<MonitorLoadIssue> Issues);

public sealed class SqlMonitorLoader(string connectionString)
{
    public async Task<MonitorLoadResult> LoadAsync(CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);return await LoadAsync(c,null,ct);
    }
    internal static async Task<MonitorLoadResult> LoadAsync(SqlConnection c,SqlTransaction? tx,CancellationToken ct)
    {
        var catalog=await StudyConfigurationStore.ReadAsync(c,tx,ct);
        await using var command=new SqlCommand("SELECT Id,Symbol,LocalSymbol,Name,SecurityType,Currency,ExpiryDate,MinTick,ContractSize,Exchange,PeriodMultiplier FROM dbo.Ticker",c,tx);
        await using var r=await command.ExecuteReaderAsync(ct);var tickers=new List<TickerRS>();
        while(await r.ReadAsync(ct))tickers.Add(new(){Id=r.GetInt32(0),Symbol=r.GetString(1),LocalSymbol=r.GetString(2),Name=r.GetString(3),SecurityType=r.GetString(4),Currency=r.GetString(5),ExpiryDate=r.GetInt32(6),MinTick=r.GetDecimal(7),ContractSize=r.GetInt32(8),Exchange=r.GetString(9),PeriodMultiplier=r.GetInt32(10)});
        return Build(catalog,tickers);
    }
    public static MonitorLoadResult Build(StudyConfigurationCatalog catalog,IReadOnlyList<TickerRS> tickers)
    {
        var result=new List<CollectionMonitor>();var issues=new List<MonitorLoadIssue>();
        foreach(var study in catalog.Studies.Where(s=>s.Enabled))
        {
            var target=StudyIntervalCompatibility.Choices(study.TickerId,catalog.Intervals,catalog.DataSets).SingleOrDefault(i=>i.Interval.Id==study.IntervalId);
            var ticker=tickers.SingleOrDefault(t=>t.Id==study.TickerId);
            var type=catalog.ColTypes.SingleOrDefault(t=>t.Type==study.ColType);
            if(target is null||ticker is null||type is null)
            {issues.Add(new(study.Id,"Enabled StudyCol requires a ticker, a ColType and an exact or compatible source dataset."));continue;}
            result.Add(new(){DataSetId=target.Source.Id,SourceInterval=new(catalog.Intervals.Single(i=>i.Id==target.Source.IntervalId)),Interval=new(target.Interval),
                Ticker=new(ticker),CollectionDll=type.DLLName,StudyCol=new(){Id=study.Id,TickerId=study.TickerId,IntervalId=study.IntervalId,ColType=study.ColType,ColParams=study.ColParams,Enabled=true,OrderSeq=1}});
        }
        return new(result,issues);
    }
    public static string Identity(CollectionMonitor m)=>JsonSerializer.Serialize(new
    {m.DataSetId,m.SourceInterval,m.Interval,m.Ticker,m.CollectionDll,Study=new{m.StudyCol.Id,m.StudyCol.TickerId,m.StudyCol.IntervalId,m.StudyCol.ColType,m.StudyCol.ColParams,m.StudyCol.Enabled}});
}
