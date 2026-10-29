using FIG.Studies;
using FIGSignalExSvc.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FIGMarketLive;

public sealed class MarketLiveEngine
{
    private readonly MarketLiveSettings settings;
    private readonly StudyPluginCatalog plugins;
    private readonly ILoggerFactory loggerFactory;
    private readonly BarStudyStore store;
    private readonly Dictionary<int, (string Definition,byte[] Prices)> published = [];
    private readonly HashSet<int> known=[];
    public event Action<string>? Status;
    public event Action<int,bool>? AttentionChanged;

    public MarketLiveEngine(MarketLiveSettings settings, ILoggerFactory loggerFactory)
    {
        settings.Validate(); this.settings=settings; this.loggerFactory=loggerFactory;
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
            { ["StudyPlugins:Path"]=settings.PluginPath }).Build();
        plugins=new(config,loggerFactory.CreateLogger<StudyPluginCatalog>());
        store=new(settings.ConnectionString);
    }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            MonitorLoadResult loaded;
            try
            {
                loaded=await new SqlMonitorLoader(settings.ConnectionString).LoadAsync(ct);
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
                await Task.Delay(TimeSpan.FromSeconds(settings.PollSeconds),ct);continue;
            }
            foreach (var monitor in loaded.Monitors)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var registration=plugins.Registrations.SingleOrDefault(p=>p.ColType.Equals(monitor.StudyCol.ColType,StringComparison.OrdinalIgnoreCase))
                        ??throw new InvalidDataException($"Collection '{monitor.StudyCol.ColType}' was not loaded from PluginPath. Install its plugin package and restart.");
                    bool dllMatches=Path.IsPathRooted(monitor.CollectionDll)?Path.GetFullPath(monitor.CollectionDll).Equals(Path.GetFullPath(registration.AssemblyPath),StringComparison.OrdinalIgnoreCase):
                        Path.GetFileName(monitor.CollectionDll).Equals(Path.GetFileName(registration.AssemblyPath),StringComparison.OrdinalIgnoreCase);
                    if(!dllMatches)throw new InvalidDataException("ColType.DLLName does not match the loaded collection DLL. Check the plugin package and restart.");
                    var snapshot=await store.ReadAsync(monitor,ct);
                    if (snapshot.Prices.Count == 0) throw new InvalidDataException("Source dataset is empty; import prices first.");
                    string definition=SqlMonitorLoader.Identity(monitor);
                    if (published.TryGetValue(monitor.StudyCol.Id,out var prior) && prior.Definition==definition && prior.Prices.AsSpan().SequenceEqual(snapshot.Fingerprint))
                    {AttentionChanged?.Invoke(monitor.StudyCol.Id,false);continue;}
                    Status?.Invoke($"Calculating {monitor.StudyCol.ColType}: {snapshot.Prices.Count:N0} bars");
                    var context=new StudyCollectionContext(monitor.StudyCol,monitor.Ticker,monitor.Interval,loggerFactory);
                    var prices=PriceBarAggregation.Build(snapshot.Prices,monitor.SourceInterval,monitor.Interval,ct);
                    var bars=StudyReplay.Calculate(()=>plugins.Create(monitor.StudyCol.ColType,context),prices,ct);
                    if (await store.PublishAsync(monitor,snapshot,bars,ct))
                    {
                        published[monitor.StudyCol.Id]=(definition,snapshot.Fingerprint);
                        AttentionChanged?.Invoke(monitor.StudyCol.Id,false);
                        Status?.Invoke($"Updated StudyCol {monitor.StudyCol.Id}: {bars.Count:N0} bars at {DateTimeOffset.Now:t}");
                    }
                    else {AttentionChanged?.Invoke(monitor.StudyCol.Id,true);Status?.Invoke("Prices changed during calculation; retrying next pass.");}
                }
                catch (OperationCanceledException) when(ct.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    AttentionChanged?.Invoke(monitor.StudyCol.Id,true);
                    loggerFactory.CreateLogger<MarketLiveEngine>().LogError(ex,"StudyCol {Id} update failed",monitor.StudyCol.Id);
                    Status?.Invoke($"StudyCol {monitor.StudyCol.Id}: {ex.Message}");
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(settings.PollSeconds),ct);
        }
    }
}
