using FIG.Studies;
using FIGSignalExSvc.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FIGMarketLive;

public sealed class MarketLiveEngine
{
    private readonly MarketLiveSettings settings;
    private readonly PluginCollectionSnapshot plugins;
    private readonly ILoggerFactory loggerFactory;
    private readonly StudyHistoryStore store;
    private readonly MarketLiveCheckpointStore checkpoints;
    private readonly string studyConnection;
    private readonly Dictionary<int, (string Definition,byte[] Prices,bool Warning)> published = [];
    private readonly HashSet<int> known=[];
    public event Action<string>? Status;
    public event Action<int,bool>? AttentionChanged;

    public MarketLiveEngine(MarketLiveSettings settings, ILoggerFactory loggerFactory)
    {
        settings.Validate(); this.settings=settings; this.loggerFactory=loggerFactory;
        plugins=MarketLivePlugins.Discover(settings.PluginPath);
        studyConnection=settings.StudyConnectionString();
        store=new(studyConnection);
        store.WriteMeasured+=measurement=>loggerFactory.CreateLogger<MarketLiveEngine>().LogInformation("{StudyWriteTiming}",measurement.ToString());
        checkpoints=new(studyConnection);
    }

    public async Task RunAsync(CancellationToken ct)
    {
        await MarketLivePlugins.SyncAsync(settings.ConnectionString,plugins,ct);
        AttentionChanged?.Invoke(-3,plugins.Rows.Any(r=>r.Status!="Loaded"));
        foreach(var issue in plugins.Rows.Where(r=>r.Status!="Loaded"))Status?.Invoke($"Plugin {issue.Type} ({issue.Manifest}): {issue.Error}");
        await checkpoints.EnsureSchemaAsync(ct);
        await new StudyRegenerationStore(studyConnection).EnsureSchemaAsync(ct);
        await new PriceEventSetup(settings.ConnectionString).EnsureSchemaAsync(ct);
        using var changes=new StudyPriceChanges();
        using var events=new PriceEventMonitor(settings.ConnectionString,loggerFactory.CreateLogger<FIGCommon.Services.EventMonitorService>(),changes);
        using var stopEvents=ct.Register(events.Dispose);
        await events.StartMonitoringAsync();
        Status?.Invoke($"Studies use PriceData events with backup reconciliation every {settings.PollSeconds} seconds. Notification connection errors are recorded in the log.");
        HashSet<int>? dirty=null;
        long lastReconciliation=Environment.TickCount64;
        while (!ct.IsCancellationRequested)
        {
            if(Environment.TickCount64-lastReconciliation>=settings.PollSeconds*1000L)dirty=null;
            if(dirty is null)lastReconciliation=Environment.TickCount64;
            MonitorLoadResult loaded;
            try
            {
                loaded=await new SqlMonitorLoader(studyConnection).LoadAsync(ct);
                var active=loaded.Monitors.Select(m=>m.StudyCol.Id).Concat(loaded.Issues.Select(i=>i.StudyColId)).ToHashSet();
                foreach(int removed in known.Except(active).ToArray()){AttentionChanged?.Invoke(removed,false);published.Remove(removed);}
                known.Clear();known.UnionWith(active);
                AttentionChanged?.Invoke(-2,active.Count==0);
                if(active.Count==0)Status?.Invoke("No enabled StudyCol rows. Waiting for SQL configuration.");
                foreach(var issue in loaded.Issues){AttentionChanged?.Invoke(issue.StudyColId,true);Status?.Invoke($"StudyCol {issue.StudyColId}: {issue.Message}");}
            }
            catch(OperationCanceledException) when(ct.IsCancellationRequested){throw;}
            catch(Exception ex)
            {
                AttentionChanged?.Invoke(-2,true);Status?.Invoke($"Unable to load SQL study configuration: {ex.Message}");
                dirty=await changes.WaitAsync(TimeSpan.FromSeconds(settings.PollSeconds),ct);continue;
            }
            foreach (var monitor in loaded.Monitors)
            {
                if(dirty is not null&&!dirty.Contains(monitor.DataSetId))continue;
                ct.ThrowIfCancellationRequested();
                try
                {
                    var registration=plugins.Registrations.SingleOrDefault(p=>p.ColType.Equals(monitor.StudyCol.ColType,StringComparison.OrdinalIgnoreCase))
                        ??throw new InvalidDataException($"Collection '{monitor.StudyCol.ColType}' is unavailable in '{plugins.Root}'. See Collection Types for plugin load errors.");
                    var snapshot=await store.ReadAsync(monitor,ct);
                    if (snapshot.Prices.Count == 0) throw new InvalidDataException("Source dataset is empty; import prices first.");
                    string definition=SqlMonitorLoader.Identity(monitor)+"|"+registration.Version+"|"+registration.Factory.GetType().Module.ModuleVersionId+"|"+typeof(StudyColBase).Module.ModuleVersionId;
                    if (published.TryGetValue(monitor.StudyCol.Id,out var prior) && prior.Definition==definition && prior.Prices.AsSpan().SequenceEqual(snapshot.Fingerprint))
                    {AttentionChanged?.Invoke(monitor.StudyCol.Id,prior.Warning);continue;}
                    Status?.Invoke($"Calculating {monitor.StudyCol.ColType}: {snapshot.Prices.Count:N0} bars");
                    var context=new StudyCollectionContext(monitor.StudyCol,monitor.Ticker,monitor.Interval,loggerFactory);
                    var prices=monitor.SourceInterval.Id==monitor.Interval.Id?snapshot.Prices:PriceBarAggregation.Build(snapshot.Prices,monitor.SourceInterval,monitor.Interval,ct);
                    if(prices.Count>settings.StudyBatchSize||(settings.CheckpointsEnabled&&prices.Count>settings.CheckpointEveryBars))
                    {
                        await new StudyRegenerationService(settings,loggerFactory).RegenerateMonitorAsync(monitor,registration,
                            p=>Status?.Invoke($"StudyCol {p.StudyColId}: {p.Message}"),ct,snapshot,true,StudyPriceValidation.Full);
                        published[monitor.StudyCol.Id]=(definition,snapshot.Fingerprint,false);AttentionChanged?.Invoke(monitor.StudyCol.Id,false);continue;
                    }
                    byte[] definitionHash=CheckpointReplay.DefinitionHash(definition);
                    var recovery=settings.CheckpointsEnabled?await checkpoints.ReadRecoveryAsync(monitor.StudyCol.Id,definitionHash,ct):new StudyRecoverySnapshot([],[]);
                    var processing=new StudyProcessingTiming();
                    var result=await CheckpointReplay.CalculateAsync(()=>registration.Factory.Create(context),prices,recovery,
                        (candidate,token)=>checkpoints.LoadAsync(monitor.StudyCol.Id,definitionHash,candidate,token),
                        (window,token)=>checkpoints.LoadPricesAsync(monitor.StudyCol.Id,window,token),definitionHash,settings.CheckpointEveryBars,settings.CheckpointsEnabled,ct,processing);
                    loggerFactory.CreateLogger<MarketLiveEngine>().LogInformation("Study processing StudyCol {StudyColId}: {Processing}",monitor.StudyCol.Id,processing.ToString());
                    Status?.Invoke($"StudyCol {monitor.StudyCol.Id}: {result.Message}");
                    if (await store.PublishAsync(monitor,snapshot,result,ct))
                    {
                        published[monitor.StudyCol.Id]=(definition,snapshot.Fingerprint,result.Warning is not null);
                        AttentionChanged?.Invoke(monitor.StudyCol.Id,result.Warning is not null);
                        Status?.Invoke($"Updated StudyCol {monitor.StudyCol.Id}: {result.Bars.Count:N0} bars, {result.Checkpoints.Count} checkpoints at {DateTimeOffset.Now:t}"+(result.Warning is null?"":". "+result.Warning));
                    }
                    else {changes.Notify(monitor.DataSetId);AttentionChanged?.Invoke(monitor.StudyCol.Id,true);Status?.Invoke("Prices changed during calculation; retrying next pass.");}
                }
                catch (OperationCanceledException) when(ct.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    AttentionChanged?.Invoke(monitor.StudyCol.Id,true);
                    loggerFactory.CreateLogger<MarketLiveEngine>().LogError(ex,"StudyCol {Id} update failed",monitor.StudyCol.Id);
                    Status?.Invoke($"StudyCol {monitor.StudyCol.Id}: {ex.Message}");
                }
            }
            long remaining=Math.Max(0,settings.PollSeconds*1000L-(Environment.TickCount64-lastReconciliation));
            dirty=await changes.WaitAsync(TimeSpan.FromMilliseconds(remaining),ct);
        }
    }
}
