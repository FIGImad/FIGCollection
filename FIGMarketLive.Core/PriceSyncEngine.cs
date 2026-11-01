using System.Data;
using FIGMarketLive.MarketData;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

/// <summary>Refreshes configured SQL datasets using authoritative provider OHLCV bars.</summary>
public sealed class PriceSyncEngine(MarketLiveSettings settings, IMarketDataAdapter adapter)
{
    public event Action<string>? Status;
    public event Action<string>? StateChanged;
    public event Action<int,bool>? AttentionChanged;

    public async Task RunAsync(CancellationToken ct)
    {
        settings.Validate();
        await new PriceEventSetup(settings.ConnectionString).EnsureSchemaAsync(ct);
        var incomplete=new HashSet<int>();
        var schedules=new ExchangeScheduleStore(settings.ConnectionString);
        bool startupCatchUp=true;
        long? lastRequestAt=null;
        while(true)
        {
            ct.ThrowIfCancellationRequested();
            var catalog=await new StudyConfigurationStore(settings.ConnectionString).ReadAsync(ct);
            var exchanges=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var dataset in catalog.DataSets.Where(d=>d.Monitor))
            {
                try
                {
                    var interval=catalog.Intervals.Single(i=>i.Id==dataset.IntervalId);
                    if(interval.IntervalLen>=86400 || interval.Id.EndsWith('D') || interval.Id.EndsWith('W') || interval.Id.EndsWith('M') || interval.Id.EndsWith('Y'))
                        throw new InvalidDataException("Price sync currently supports intraday source datasets. Use Live Studies for calendar aggregates.");
                    var instrument=await new TickerProviderStore(settings.ConnectionString).ReadInstrumentAsync(dataset.TickerId,adapter.ProviderId,ct);
                    exchanges.Add(instrument.Exchange);
                    var schedule=await schedules.FindAsync(instrument.Exchange,ct)??throw new InvalidDataException($"No price-sync schedule configured for exchange {instrument.Exchange}.");
                    if(!startupCatchUp&&!schedule.IsOpen(DateTime.UtcNow))continue;
                    StateChanged?.Invoke($"{(startupCatchUp?"Catching up prices":"Syncing prices")} — DataSet {dataset.Id} ({instrument.Exchange})");
                    long now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    await using var c=new SqlConnection(settings.ConnectionString);await c.OpenAsync(ct);
                    long? last;
                    await using(var latest=new SqlCommand("SELECT MAX(RawTime) FROM dbo.PriceData WHERE DataSetId=@Id",c))
                    {latest.Parameters.AddWithValue("@Id",dataset.Id);var value=await latest.ExecuteScalarAsync(ct);last=value is null or DBNull?null:Convert.ToInt64(value);}
                    int limit=checked(settings.PriceHistoryMinutes*60);
                    long requestedStart=Math.Max(last??now-limit,instrument.HistoryStartRawTime??long.MinValue);
                    bool gap=last is null&&instrument.HistoryStartRawTime is null || requestedStart<now-limit;
                    if(gap)incomplete.Add(dataset.Id);
                    // Re-read recent bars so current bars and provider corrections are upserted.
                    int duration=(int)Math.Min(limit,Math.Max(interval.IntervalLen*2L,now-requestedStart+interval.IntervalLen));
                    var retrieved=new SortedDictionary<long,MarketBar>();
                    var queries=HistoryQueries(instrument,now,interval.IntervalLen,duration);
                    for(int q=0;q<queries.Count;q++)
                    {
                        if(lastRequestAt is long previous)
                        {
                            long remaining=11000-(Environment.TickCount64-previous);
                            if(remaining>0)await Task.Delay(TimeSpan.FromMilliseconds(remaining),ct);
                        }
                        if(!startupCatchUp&&!await schedules.IsOpenAsync(instrument.Exchange,ct))throw new ScheduleClosedException();
                        lastRequestAt=Environment.TickCount64;
                        var page=await adapter.GetHistoricalBarsAsync(queries[q],ct);
                        foreach(var b in ValidateBars(page,now))retrieved[b.RawTime]=b;
                    }
                    var bars=CaptureBars(retrieved.Values,instrument.HistoryStartRawTime);
                    if(bars.Length==0&&instrument.HistoryStartRawTime is not null)
                    {Status?.Invoke($"DataSet {dataset.Id}: waiting for bars from the new contract after its rollover boundary.");continue;}
                    if(bars.Length==0)throw new InvalidDataException("Provider returned no price bars; check market hours and market-data permissions.");
                    var validated=ValidateBars(bars,now);
                    var publishSchedule=await schedules.FindAsync(instrument.Exchange,ct)??throw new InvalidDataException("Exchange schedule was removed during retrieval.");
                    if(!startupCatchUp&&!publishSchedule.IsOpen(DateTime.UtcNow))throw new ScheduleClosedException();
                    await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
                    // Lock the dataset so deletion/reconfiguration cannot race publication.
                    await using(var check=new SqlCommand("SELECT Id FROM dbo.DataSet WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Id AND TickerId=@Ticker AND IntervalId=@Interval AND Monitor=1 AND EXISTS(SELECT 1 FROM dbo.Ticker WHERE Id=@Ticker AND Exchange=@Exchange)",c,tx))
                    {
                        check.Parameters.AddWithValue("@Id",dataset.Id);check.Parameters.AddWithValue("@Ticker",dataset.TickerId);check.Parameters.AddWithValue("@Interval",dataset.IntervalId);
                        check.Parameters.AddWithValue("@Exchange",instrument.Exchange);
                        if(await check.ExecuteScalarAsync(ct) is null)throw new InvalidDataException("Dataset changed during retrieval; retrying next pass.");
                    }
                    if(await TickerProviderStore.ReadInstrumentAsync(c,tx,dataset.TickerId,adapter.ProviderId,ct)!=instrument)
                        throw new InvalidDataException("Ticker contract changed during retrieval. Discarding prices and retrying next pass.");
                    foreach(var bar in validated)
                    {
                        if(!startupCatchUp&&!publishSchedule.IsOpen(DateTime.UtcNow))throw new ScheduleClosedException();
                        await using var cmd=new SqlCommand("""
                            UPDATE dbo.PriceData SET [Open]=@Open,High=@High,Low=@Low,[Close]=@Close,Volume=@Volume
                            WHERE DataSetId=@Id AND RawTime=@Time
                                AND ([Open]<>@Open OR High<>@High OR Low<>@Low OR [Close]<>@Close OR Volume<>@Volume);
                            IF NOT EXISTS(SELECT 1 FROM dbo.PriceData WHERE DataSetId=@Id AND RawTime=@Time)
                                INSERT dbo.PriceData(DataSetId,RawTime,[Open],High,Low,[Close],Volume)
                                VALUES(@Id,@Time,@Open,@High,@Low,@Close,@Volume);
                            """,c,tx);
                        cmd.Parameters.AddWithValue("@Id",dataset.Id);cmd.Parameters.AddWithValue("@Time",bar.RawTime);

                        foreach(var (name,value) in new[]{("@Open",bar.Open),("@High",bar.High),("@Low",bar.Low),("@Close",bar.Close)})
                        {var p=cmd.Parameters.Add(name,SqlDbType.Decimal);p.Precision=28;p.Scale=10;p.Value=value;}
                        cmd.Parameters.AddWithValue("@Volume",checked((int)bar.Volume));
                        await cmd.ExecuteNonQueryAsync(ct);
                    }
                    if(!startupCatchUp&&!await schedules.IsOpenAsync(instrument.Exchange,ct))throw new ScheduleClosedException();
                    await tx.CommitAsync(ct);
                    AttentionChanged?.Invoke(dataset.Id,incomplete.Contains(dataset.Id));
                    Status?.Invoke($"DataSet {dataset.Id}: synchronized {validated.Count} bars."+(incomplete.Contains(dataset.Id)?$" History before the {settings.PriceHistoryMinutes}-minute catch-up window needs verification/import. Restart price sync after importing history.":""));
                }
                catch(OperationCanceledException) when(ct.IsCancellationRequested){throw;}
                catch(ScheduleClosedException){Status?.Invoke($"DataSet {dataset.Id}: exchange schedule closed during sync; no prices published. Waiting for the next open.");}
                catch(Exception ex){AttentionChanged?.Invoke(dataset.Id,true);Status?.Invoke($"Price sync DataSet {dataset.Id}: {ex.Message}");}
            }
            startupCatchUp=false;
            var states=new List<string>();
            foreach(var exchange in exchanges.Order())
            {
                var schedule=await schedules.FindAsync(exchange,ct);
                states.Add(schedule is null?$"{exchange}: exchange schedule is missing.":schedule.IsOpen(DateTime.UtcNow)?$"{exchange}: Price Sync running.":schedule.WaitingStatus(DateTime.UtcNow));
            }
            if(!catalog.DataSets.Any(d=>d.Monitor))states.Add("No datasets enabled for price sync. Enable Monitor in Configuration → Ticker and DataSet.");
            StateChanged?.Invoke(string.Join(Environment.NewLine,states));
            await Task.Delay(TimeSpan.FromSeconds(5),ct);
        }
    }

    public static IReadOnlyList<MarketBar> ValidateBars(IReadOnlyList<MarketBar> bars,long now)
    {
        var result=bars.OrderBy(b=>b.RawTime).ToArray();
        long previous=-1;
        foreach(var b in result)
        {
            if(b.RawTime<=previous || b.RawTime>now || b.RawTime<=0 || b.Low>b.High || b.Open<b.Low || b.Open>b.High || b.Close<b.Low || b.Close>b.High
                || b.Volume<0 || b.Volume>int.MaxValue || decimal.Truncate(b.Volume)!=b.Volume)
                throw new InvalidDataException("Provider returned invalid, duplicate or unsupported OHLCV bars; dataset was not changed.");
            previous=b.RawTime;
        }
        return result;
    }
    private sealed class ScheduleClosedException : Exception { }
    public static MarketBar[] CaptureBars(IEnumerable<MarketBar> bars,long? historyStart)
        =>bars.Where(b=>historyStart is null||b.RawTime>=historyStart.Value).ToArray();

    public static IReadOnlyList<HistoryQuery> HistoryQueries(MarketInstrument instrument,long end,int barSeconds,int duration)
    {
        if(end<=0||barSeconds<=0||duration<=0)throw new InvalidDataException("History range must be positive.");
        int pageSeconds=barSeconds<60?3600:86400;
        var result=new List<HistoryQuery>();long start=checked(end-duration);
        while(start<end)
        {int length=(int)Math.Min(pageSeconds,end-start);result.Add(new(instrument,start+length,barSeconds,length));start+=length;}
        return result;
    }
}
