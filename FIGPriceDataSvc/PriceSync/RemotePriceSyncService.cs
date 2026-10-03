using FIGCommon.Models;
using FIGCommon.Models.FIGProviderAPI;
using System.Collections.Concurrent;

namespace FIGPriceDataSvc.PriceSync;

public sealed class RemotePriceSyncService(IRemotePriceStore store,IPriceSyncApiService api,
    RemotePriceSyncOptions options,ILogger<RemotePriceSyncService> logger,TimeProvider? timeProvider=null):BackgroundService
{
    // Keep provider < routing < response < retry so each layer has time to report failure.
    public const int ProviderTimeoutMs = 45_000;
    public const int ControllerRouteTimeoutMs = ProviderTimeoutMs + 15_000;
    public const int ResponseTimeoutMs = ControllerRouteTimeoutMs + 5_000;
    public const int CatchUpRetryDelayMs = ResponseTimeoutMs + 10_000;
    private readonly ConcurrentDictionary<int,long> completedThrough=new();
    private readonly ConcurrentDictionary<int,byte> caughtUp=new();
    private readonly ConcurrentDictionary<int,SemaphoreSlim> datasetGates=new();
    private readonly TimeProvider clock=timeProvider??TimeProvider.System;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Remote price worker started. Destination={Destination}; pre-close offset={OffsetSeconds}s plus 500ms",
            string.IsNullOrWhiteSpace(options.DestinationServiceId)?"PriceSync role":options.DestinationServiceId,options.TimeOffsetSec);
        bool initialized=false;
        DateTimeOffset? correction=null;
        try
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                if(!initialized)
                {
                    var now=clock.GetUtcNow();
                    var closed=MarketScheduleHelper.GetMsUntilNextOpen(options.MarketSchedule,now.UtcDateTime);
                    if(closed>0)
                    {
                        logger.LogInformation("Market closed; next price synchronization in {Delay}",TimeSpan.FromMilliseconds(closed));
                        await Task.Delay(TimeSpan.FromMilliseconds(closed),clock,stoppingToken);
                        continue;
                    }
                    if(!await RunPassAsync(false,stoppingToken))
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(CatchUpRetryDelayMs),clock,stoppingToken);
                        continue;
                    }
                    now=clock.GetUtcNow();
                    correction=DateTimeOffset.FromUnixTimeSeconds((now.ToUnixTimeSeconds()/60+1)*60);
                    initialized=true;
                }
                var fetch=PriceSyncTiming.NextFetch(clock.GetUtcNow(),60,options.TimeOffsetSec);
                bool updateOnly=correction.HasValue && correction.Value<=fetch;
                var due=updateOnly?correction!.Value:fetch;
                var delay=due-clock.GetUtcNow();
                logger.LogDebug("Next price {Phase} due at {Due}",updateOnly?"correction":"fetch",due);
                if(delay>TimeSpan.Zero) await Task.Delay(delay,clock,stoppingToken);
                // Like FIGPriceSyncSvc, finish the pending close correction even at a market break.
                if(!updateOnly && !MarketScheduleHelper.IsMarketOpen(options.MarketSchedule,clock.GetUtcNow().UtcDateTime))
                {
                    initialized=false; correction=null; continue;
                }
                var succeeded=await RunPassAsync(updateOnly,stoppingToken);
                if(!succeeded)
                {
                    initialized=false; correction=null;
                    await Task.Delay(TimeSpan.FromMilliseconds(CatchUpRetryDelayMs),clock,stoppingToken);
                    continue;
                }
                correction=updateOnly?null:PriceSyncTiming.CorrectionAfter(due,options.TimeOffsetSec);
            }
        }
        catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested) { }
        finally { logger.LogInformation("Remote price worker stopped"); }
    }
    private async Task<bool> RunPassAsync(bool updateOnly,CancellationToken ct)
    {
        try
        {
            var ids=options.DataSetIds.Length>0?options.DataSetIds:store.GetDataSetIds();
            if(ids.Length==0) logger.LogWarning("No local one-minute datasets configured for price synchronization");
            bool succeeded=ids.Length>0;
            foreach(var id in ids)
            {
                try { await SyncOnceAsync(id,clock.GetUtcNow().ToUnixTimeSeconds(),ct,updateOnly); }
                catch(OperationCanceledException) when(ct.IsCancellationRequested) { throw; }
                catch(Exception ex) { succeeded=false; logger.LogError(ex,"Remote price synchronization failed for local dataset {DataSetId}; catch-up must complete before corrections resume",id); }
            }
            return succeeded;
        }
        catch(OperationCanceledException) when(ct.IsCancellationRequested) { throw; }
        catch(Exception ex) { logger.LogError(ex,"Unable to load local price datasets; retrying catch-up"); return false; }
    }
    public async Task SyncOnceAsync(int id,long now,CancellationToken ct,bool updateOnly=false)
    {
        var gate=datasetGates.GetOrAdd(id,_=>new SemaphoreSlim(1,1));
        await gate.WaitAsync(ct);
        try
        {
            if(updateOnly)
            {
                if(!caughtUp.ContainsKey(id) || !completedThrough.TryGetValue(id,out var through) || now-through>=60)
                {
                    logger.LogDebug("Skipping correction for dataset {DataSetId}: catch-up is incomplete or stale",id);
                    return;
                }
                await FetchWindowAsync(id,now,ct,true);
                return;
            }
            caughtUp.TryRemove(id,out _);
            // A normal pass drains every historical page through one fixed target, oldest first.
            // Each page advances the frontier only after its local transaction commits.
            do
            {
                ct.ThrowIfCancellationRequested();
                await FetchWindowAsync(id,now,ct,false);
            } while(!completedThrough.TryGetValue(id,out var through) || through<now);
            caughtUp[id]=0;
            logger.LogDebug("Dataset {DataSetId} catch-up committed through {RawTime}",id,now);
        }
        catch
        {
            caughtUp.TryRemove(id,out _);
            throw;
        }
        finally { gate.Release(); }
    }
    private async Task FetchWindowAsync(int id,long now,CancellationToken ct,bool updateOnly)
    {
        var dataset=store.GetDataSet(id) ?? throw new InvalidOperationException("Local dataset not found.");
        // Current FIGSignalSvc builds all studies from one-minute prices.
        if(dataset.IntervalId!="1" || dataset.Interval.IntervalLen!=60)
            throw new InvalidOperationException("Remote signal price synchronization requires a local one-minute dataset.");
        var latest=await store.LastRawTimeAsync(id,ct);
        if(updateOnly && !latest.HasValue) return;
        var endNow=now; // Include the current forming bar, like the provider's pre-close local fetch.
        var floor=latest.HasValue ? latest.Value : endNow-options.InitialHistoryDays*86400L;
        if(!completedThrough.TryGetValue(id,out var frontier)) frontier=floor;
        if(updateOnly) frontier=latest!.Value;
        var from=Math.Max(0,frontier-options.OverlapBars*60L)/60*60;
        var to=Math.Min(endNow,from+options.WindowBars*60L);
        if(to<=from) throw new InvalidOperationException("Local price frontier is ahead of the requested catch-up target.");
        // Duration is whole minutes, but the provider's end time remains the actual pre-close second.
        from=to-((to-from+59)/60*60);
        var ticker=dataset.Ticker;
        var request=new HistoricalDataRequest
        {
            Ticker=new TickerInfo {Symbol=ticker.LocalSymbol,SecType=ticker.SecurityType,Currency=ticker.Currency,
                Exchange=ticker.Exchange,ContractMonth=ticker.ExpiryDate},
            IntervalId="1 min",StartRawTime=to,Duration=$"{to-from} S",NumBars=0,
            ProviderTimeoutMs=ProviderTimeoutMs
        };
        HistoricalPrices.Validate(request);
        var response=await api.HistoricalDataRequestAsync(request,ct);
        if(response.FromRawTime!=from || response.ToRawTime!=to || response.Bars==null)
            throw new InvalidOperationException("Historical response does not match the requested window.");
        HistoricalPrices.ValidateBars(response.Bars,from,to,60);
        var bars=updateOnly?response.Bars.Where(b=>b.RawTime<=latest!.Value).ToList():response.Bars;
        await store.SaveAsync(id,bars,ct,updateOnly);
        // Advance only after a successful complete response and local commit, including empty market-closed windows.
        // On restart the durable PriceData maximum is replayed with overlap; no separate schema is needed.
        if(!updateOnly) completedThrough[id]=to;
    }
}
