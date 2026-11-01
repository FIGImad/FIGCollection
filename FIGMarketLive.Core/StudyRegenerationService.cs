using System.Diagnostics;
using FIG.Studies;
using FIGCommon.Models;
using FIGSignalExSvc.Plugins;
using FIGSignalExSvc.Services;
using Microsoft.Extensions.Logging;

namespace FIGMarketLive;

public enum StudyPriceValidation { Full, RegenerationTail }
public sealed record RegenerationChoice(string ColType,int StudyCols,int Enabled,bool Available,string Problem);
public sealed record RegenerationProgress(string ColType,int StudyColId,int Completed,int Total,string Status,string Message,int CollectionIndex=1,int CollectionCount=1);

public sealed class StudyRegenerationService(MarketLiveSettings settings,ILoggerFactory logs)
{
    public event Action<StudyWriteTiming>? WriteMeasured;
    public event Action<StudyProcessingTiming>? ProcessingMeasured;
    public async Task<List<RegenerationChoice>> ListAsync(CancellationToken ct)
    {
        var plugins=MarketLivePlugins.Discover(settings.PluginPath);await MarketLivePlugins.SyncAsync(settings.ConnectionString,plugins,ct);
        var catalog=await new StudyConfigurationStore(settings.ConnectionString).ReadAsync(ct);
        return catalog.ColTypes.Select(t=>new RegenerationChoice(t.Type,catalog.Studies.Count(s=>s.ColType==t.Type),catalog.Studies.Count(s=>s.ColType==t.Type&&s.Enabled),plugins.Registrations.Any(p=>p.ColType.Equals(t.Type,StringComparison.OrdinalIgnoreCase)),string.Join("; ",plugins.Rows.Where(p=>p.Type==t.Type&&p.Status!="Loaded").Select(p=>p.Error)))).OrderBy(t=>t.ColType).ToList();
    }
    public async Task RunAsync(IReadOnlyList<string> types,IProgress<RegenerationProgress> progress,CancellationToken ct,bool full=false,bool overridesOnly=false)
    {
        settings.Validate();string cs=settings.StudyConnectionString();var plugins=MarketLivePlugins.Discover(settings.PluginPath);
        await MarketLivePlugins.SyncAsync(cs,plugins,ct);await new MarketLiveCheckpointStore(cs).EnsureSchemaAsync(ct);await new StudyRegenerationStore(cs).EnsureSchemaAsync(ct);
        var all=await new SqlMonitorLoader(cs).LoadAsync(ct,true);var catalog=await new StudyConfigurationStore(cs).ReadAsync(ct);
        var selected=types.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var configured=catalog.Studies.Where(s=>s.Enabled&&selected.Contains(s.ColType)).OrderBy(s=>s.OrderSeq).ThenBy(s=>s.Id).ToList();
        var totals=selected.ToDictionary(t=>t,t=>configured.Count(s=>s.ColType.Equals(t,StringComparison.OrdinalIgnoreCase)),StringComparer.OrdinalIgnoreCase);
        var indices=selected.ToDictionary(t=>t,_=>0,StringComparer.OrdinalIgnoreCase);
        var failed=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var failures=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase);
        void Fail(string type,int id,string message,Exception? exception=null)
        {
            failed.Add(type);
            if(!failures.TryGetValue(type,out var details))failures[type]=details=[];
            details.Add($"StudyCol {id}: {message}");
            logs.CreateLogger<StudyRegenerationService>().LogError(exception,"Regeneration failed for {ColType}, StudyCol {StudyColId}: {Reason}",type,id,message);
        }
        foreach(var study in configured)
        {
            ct.ThrowIfCancellationRequested();string type=study.ColType;int index=++indices[type],total=totals[type];
            var registration=plugins.Registrations.SingleOrDefault(p=>p.ColType.Equals(type,StringComparison.OrdinalIgnoreCase));
            var monitor=all.Monitors.SingleOrDefault(m=>m.StudyCol.Id==study.Id);
            if(registration is null||monitor is null)
            {
                string reason=registration is null?"Plugin is not available. Check Collection Types.":all.Issues.FirstOrDefault(i=>i.StudyColId==study.Id)?.Message??"No compatible source dataset.";
                Fail(type,study.Id,reason);progress.Report(new(type,study.Id,0,0,"Failed",reason,index,total));continue;
            }
            try{await RegenerateMonitorAsync(monitor,registration,p=>progress.Report(p with{CollectionIndex=index,CollectionCount=total}),ct,validation:StudyPriceValidation.RegenerationTail,full:full,overridesOnly:overridesOnly);}
            catch(OperationCanceledException)when(ct.IsCancellationRequested){throw;}
            catch(Exception ex){Fail(type,study.Id,ex.Message,ex);progress.Report(new(type,study.Id,0,0,"Failed",ex.Message,index,total));}
        }
        foreach(string type in selected)
            progress.Report(new(type,0,0,0,failed.Contains(type)?"Failed":"Completed",totals[type]==0?"No enabled StudyCol rows configured.":failed.Contains(type)?string.Join(Environment.NewLine,failures[type]):$"Regenerated {totals[type]} enabled StudyCol(s).",totals[type],totals[type]));
    }
    public async Task RegenerateMonitorAsync(CollectionMonitor monitor,StudyPluginRegistration registration,Action<RegenerationProgress> progress,CancellationToken ct,PriceSnapshot? supplied=null,bool resumeExisting=false,StudyPriceValidation validation=StudyPriceValidation.Full,bool full=false,bool overridesOnly=false)
    {
        if(overridesOnly&&(full||!StudyLayers.Applies(monitor.StudyCol.ColType)))throw new InvalidDataException("L2 overrides only cannot be combined with Full and requires the L2 collection.");
        if(overridesOnly)validation=StudyPriceValidation.RegenerationTail;
        if(validation is not (StudyPriceValidation.Full or StudyPriceValidation.RegenerationTail))throw new ArgumentOutOfRangeException(nameof(validation));
        string cs=settings.StudyConnectionString();var store=new StudyHistoryStore(cs);var history=new MarketLiveCheckpointStore(cs);var regeneration=new StudyRegenerationStore(cs);await regeneration.EnsureSchemaAsync(ct);
        StudyWriteTiming? lastWrite=null;
        regeneration.WriteMeasured+=measurement=>{lastWrite=measurement;logs.CreateLogger<StudyRegenerationService>().LogInformation("{StudyWriteTiming}",measurement.ToString());WriteMeasured?.Invoke(measurement);};
        string type=monitor.StudyCol.ColType;int id=monitor.StudyCol.Id;int lastReported=0,totalReported=0;
        regeneration.DeadlockRetrying+=(attempt,delay)=>
        {
            string message=$"StudyCol {id}: SQL deadlock; retrying the same batch ({attempt}/{SqlDeadlockRetry.MaximumRetries}) after {delay.TotalMilliseconds:N0} ms. Committed batches are retained.";
            logs.CreateLogger<StudyRegenerationService>().LogWarning("{DeadlockRetry}",message);
            progress(new(type,id,lastReported,totalReported,"Retrying",message));
        };
        bool ownsProgress=false;
        void Report(int n,int total,string status,string message){lastReported=n;totalReported=total;progress(new(type,id,n,total,status,message));}
        try
        {
            Report(0,0,"Reading","Reading source prices…");var source=supplied??await store.ReadAsync(monitor,ct,true);
            var prices=monitor.SourceInterval.Id==monitor.Interval.Id?source.Prices:PriceBarAggregation.Build(source.Prices,monitor.SourceInterval,monitor.Interval,ct);
            if(prices.Count==0)throw new InvalidDataException("The source dataset has no prices.");
            int tailStart=Math.Max(0,prices.Count-10);
            int calculationEnd=overridesOnly?tailStart:prices.Count;
            IReadOnlyList<FIGCommon.Models.PriceDataRS> storedPrices=overridesOnly?prices.Take(tailStart).ToArray():prices;
            int validationStart=validation==StudyPriceValidation.Full?0:Math.Max(0,prices.Count-100);
            string validationMessage=validation==StudyPriceValidation.Full?"Price validation: full source validation for live updates.":$"Price validation: last {prices.Count-validationStart:N0} study bars only; historical comparisons and final full-history validation are skipped.";
            logs.CreateLogger<StudyRegenerationService>().LogInformation("StudyCol {StudyColId}: {Validation}",id,validationMessage);Report(0,prices.Count,"Reading",validationMessage);
            var context=new StudyCollectionContext(monitor.StudyCol,monitor.Ticker,monitor.Interval,logs);
            StoredStudyInputs? stored=overridesOnly?new(cs,id,CheckpointReplay.DefinitionHash(SqlMonitorLoader.Identity(monitor)),prices[tailStart].RawTime,Math.Max(0,Math.Min(100,prices.Count)-(prices.Count-tailStart))):null;
            byte[]? baseFingerprint=stored is null?null:await stored.FingerprintAsync(storedPrices,ct);
            StudyColBase Create()=>StudyLayers.Configure(registration.Factory.Create(context),stored is null?null:stored.Read);
            var collection=Create();bool supports=settings.CheckpointsEnabled&&collection.SupportsCheckpoint;
            byte[] definition=CheckpointReplay.DefinitionHash(SqlMonitorLoader.Identity(monitor)+"|"+registration.Version+"|"+registration.Factory.GetType().Module.ModuleVersionId+"|"+typeof(StudyColBase).Module.ModuleVersionId);
            byte[] fullDefinition=definition;
            var tailStore=new StudyTailCheckpointStore(cs);
            StudyColBase? tailCollection=overridesOnly?await tailStore.PrepareAsync(monitor,()=>StudyLayers.Configure(registration.Factory.Create(context)),prices,tailStart,fullDefinition,settings.CheckpointsEnabled,message=>Report(0,prices.Count,"Preparing tail",message),ct):null;
            if(baseFingerprint is not null)definition=CheckpointReplay.DefinitionHash(Convert.ToHexString(definition)+"|Study_L2-hybrid-v2|"+Convert.ToHexString(baseFingerprint));
            var state=full?null:await regeneration.StateAsync(id,ct);bool resume=state is not null&&(state.Status!="Completed"||resumeExisting)&&state.Definition.AsSpan().SequenceEqual(definition);
            ownsProgress=resume&&state!.Status!="Completed";
            var candidates=!full&&(resume||resumeExisting)&&supports?await regeneration.CandidatesAsync(id,definition,resume?Math.Min(state!.Completed,overridesOnly?tailStart:prices.Count-1):overridesOnly?tailStart:prices.Count-1,ct):[];
            var counts=candidates.Select(c=>c.Count).ToHashSet();
            for(int n=settings.CheckpointEveryBars;n<prices.Count;n+=settings.CheckpointEveryBars)counts.Add(n);
            for(int n=settings.StudyBatchSize;n<prices.Count;n+=settings.StudyBatchSize)counts.Add(n);
            counts.Add(prices.Count);if(tailStart>0)counts.Add(tailStart);if(prices.Count>1)counts.Add(prices.Count-1);if(resume&&state!.Completed<=prices.Count)counts.Add(state.Completed);
            var hashes=CheckpointReplay.PrefixHashes(prices,counts,ct);
            int start=0,skip=0;
            foreach(var candidate in candidates)
            {
                if(candidate.Count>=prices.Count||candidate.RawTime!=prices[candidate.Count-1].RawTime||!hashes.TryGetValue(candidate.Count,out var hash)||!hash.AsSpan().SequenceEqual(candidate.PrefixHash))continue;
                try
                {
                    var checkpoint=await history.LoadAsync(id,definition,candidate,ct);if(checkpoint is null)continue;
                    var window=collection.GetRequiredCheckpointPrices(checkpoint);collection.RestoreCheckpoint(checkpoint,window is null?null:await history.LoadPricesAsync(id,window,ct));start=skip=candidate.Count;break;
                }
                catch(Exception ex)when(ex is System.Text.Json.JsonException or InvalidDataException or InvalidOperationException or ArgumentException or NotSupportedException){collection=Create();}
            }
            if(resume&&!supports&&state!.Completed<=calculationEnd&&hashes.TryGetValue(state.Completed,out var prior)&&state.PrefixHash is not null&&prior.AsSpan().SequenceEqual(state.PrefixHash))skip=state.Completed;
            bool additional=false;for(int n=skip+settings.StudyBatchSize;n<prices.Count;n+=settings.StudyBatchSize)additional|=counts.Add(n);
            if(additional)hashes=CheckpointReplay.PrefixHashes(prices,counts,ct);
            Report(skip,prices.Count,start>0?"Resuming":"Calculating",start>0?$"Restored checkpoint after {start:N0} committed bars.":skip>0?"Replaying the committed prefix to rebuild DLL state; existing batches are retained.":!supports?"Full regeneration; checkpointing is disabled or unsupported, so interrupted state must be replayed.":"Full regeneration from the first source bar.");
            StudyCheckpointPayload? pendingTailState=null;
            var processing=new StudyProcessingTiming();
            var batch=new List<EncodedStudyBar>();var checkpoints=new List<LiveCheckpoint>();long checkpointBytes=0;bool reset=skip==0,beginRun=true;int rawIndex=0;
            using var encoder=new StudyLayers.Encoder();
            using var overrideEncoder=new StudyBinaryCodec.Encoder();
            var calculator=new StudyBatchCalculator(collection,ct);
            int chunkStart=start;
            while(chunkStart<calculationEnd)
            {
                int end=StudyBatchCalculator.NextEnd(chunkStart,prices.Count,skip,settings.StudyBatchSize,settings.CheckpointEveryBars,supports);
                if(chunkStart<tailStart)end=Math.Min(end,tailStart);
                if(stored is not null)await stored.PrepareAsync(storedPrices,chunkStart,Math.Max(settings.StudyBatchSize,StudyBatchCalculator.MaximumChunkBars),ct);
                long started=Stopwatch.GetTimestamp();var outputs=calculator.Calculate(prices,chunkStart,end);processing.Calculation(started,end-chunkStart);
                if(end<=skip)
                {if(end==skip||end%settings.StudyBatchSize==0)Report(skip,prices.Count,"Rebuilding state",$"Replayed {end:N0} of {skip:N0} committed bars.");chunkStart=end;continue;}
                started=Stopwatch.GetTimestamp();
                foreach(var output in outputs){ct.ThrowIfCancellationRequested();batch.Add(stored is null?encoder.Encode(output):new(output.Price,stored.Bytes(output.Price.RawTime),overrideEncoder.Encode(output.OverrideStudies??throw new InvalidDataException("Missing override outputs."))));}
                processing.Encoding(started);
                int i=end-1;var price=prices[i];chunkStart=end;
                int count=i+1;bool flush=(count-skip)%settings.StudyBatchSize==0||count==prices.Count||(overridesOnly&&count==tailStart);
                if(supports&&i<prices.Count-1&&(count%settings.CheckpointEveryBars==0||flush||i==prices.Count-2||count==tailStart))
                {
                    started=Stopwatch.GetTimestamp();var native=collection.CaptureCheckpoint();processing.Capture(started);
                    started=Stopwatch.GetTimestamp();var payload=StudyCheckpointPayload.Capture(native);processing.Serialization(started);
                    if(payload.RawTime!=price.RawTime)throw new InvalidDataException("Checkpoint timestamp does not match the calculated bar.");
                    if(payload.Bytes>64L*1024*1024)throw new InvalidDataException("Checkpoint exceeds 64 MiB; reduce plugin state retention.");
                    // Keep every captured snapshot. A memory-triggered flush occurs at this known checkpoint boundary.
                    checkpoints.Add(new(payload,count,hashes[count]));checkpointBytes+=payload.Bytes;
                    if(!overridesOnly&&count==tailStart)pendingTailState=payload;
                    if(checkpointBytes>=64L*1024*1024)flush=true;
                }
                if(!flush)continue;
                int prefix=count-batch.Count;long from=prices[prefix].RawTime,to=count<prices.Count?prices[count].RawTime:long.MaxValue;
                bool validatePrices=count>validationStart;
                var raw=new List<PriceDataRS>();
                if(validatePrices)
                {
                    from=Math.Max(from,prices[validationStart].RawTime);
                    while(rawIndex<source.Prices.Count&&source.Prices[rawIndex].RawTime<from)rawIndex++;
                    int rawStart=rawIndex;while(rawIndex<source.Prices.Count&&source.Prices[rawIndex].RawTime<to)rawIndex++;
                    raw.Capacity=rawIndex-rawStart;for(int n=rawStart;n<rawIndex;n++)raw.Add(source.Prices[n]);
                }
                var prefixHash=hashes[count];
                var run=new CheckpointReplayResult(batch,checkpoints,prefix,definition,settings.CheckpointEveryBars,true,count<prices.Count?prices[count-1].RawTime:prices.Count>1?prices[^2].RawTime:null,"Regeneration batch",null);
                await regeneration.PublishAsync(monitor,run,raw,from,to,prices.Count,prefixHash,reset,ct,beginRun,prefix>0?prices[prefix-1].RawTime:null,validatePrices,full,overridesOnly);reset=false;beginRun=false;ownsProgress=true;
                if(pendingTailState is not null){await tailStore.SaveAsync(monitor,fullDefinition,tailStart,hashes[tailStart],pendingTailState,ct);pendingTailState=null;}
                logs.CreateLogger<StudyRegenerationService>().LogInformation("Study processing StudyCol {StudyColId}: {Processing}",id,processing.ToString());
                try{ProcessingMeasured?.Invoke(processing);}catch{ /* Diagnostics cannot fail a committed batch. */ }
                string timingText=lastWrite is null?"":$" DB {lastWrite.TotalMilliseconds:N0} ms; slowest: {lastWrite.Stages.MaxBy(p=>p.Value).Key} {lastWrite.Stages.Max(p=>p.Value):N0} ms.";
                Report(count,prices.Count,count==prices.Count?"Finalizing":"Calculating",$"Committed {count:N0}/{prices.Count:N0} bars (batch {batch.Count:N0})."+timingText);batch.Clear();checkpoints.Clear();checkpointBytes=0;processing=new StudyProcessingTiming();
            }
            IReadOnlyList<EncodedStudyBar>? refreshedTail=null;
            if(tailCollection is not null)
            {
                Report(tailStart,prices.Count,"Refreshing tail",$"Recalculating all studies for the final {prices.Count-tailStart} bars...");
                if(tailStart>0)tailCollection.AdoptOverrideState(collection);
                var tailCalculator=new StudyBatchCalculator(tailCollection,ct);
                var tailBars=new List<EncodedStudyBar>();var tailCheckpoints=new List<LiveCheckpoint>();
                var tailTiming=new StudyProcessingTiming();
                for(int i=tailStart;i<prices.Count;i++)
                {
                    long t=Stopwatch.GetTimestamp();var output=tailCalculator.Calculate(prices,i,i+1).Single();tailTiming.Calculation(t);
                    t=Stopwatch.GetTimestamp();tailBars.Add(encoder.Encode(output));tailTiming.Encoding(t);
                    if(settings.CheckpointsEnabled&&tailCollection.SupportsCheckpoint&&i==prices.Count-2)
                    {
                        t=Stopwatch.GetTimestamp();var native=tailCollection.CaptureCheckpoint();tailTiming.Capture(t);
                        t=Stopwatch.GetTimestamp();var payload=StudyCheckpointPayload.Capture(native);tailTiming.Serialization(t);
                        if(payload.Bytes>64L*1024*1024)throw new InvalidDataException("Tail checkpoint exceeds 64 MiB.");
                        tailCheckpoints.Add(new(payload,i+1,hashes[i+1]));
                    }
                }
                long from=prices[tailStart].RawTime;
                var tailRaw=source.Prices.Where(p=>p.RawTime>=from).ToList();
                refreshedTail=tailBars;
                var tailRun=new CheckpointReplayResult(tailBars,tailCheckpoints,tailStart,fullDefinition,settings.CheckpointEveryBars,true,prices.Count>1?prices[^2].RawTime:null,"Full final 10 bars",null);
                await regeneration.PublishAsync(monitor,tailRun,tailRaw,from,long.MaxValue,prices.Count,hashes[prices.Count],reset,ct,beginRun,tailStart>0?prices[tailStart-1].RawTime:null,validatePrices:true);
                ownsProgress=true;
                logs.CreateLogger<StudyRegenerationService>().LogInformation("Study processing StudyCol {StudyColId} full tail: {Processing}",id,tailTiming.ToString());
                Report(prices.Count,prices.Count,"Finalizing",$"Committed {prices.Count:N0}/{prices.Count:N0} bars; both study layers refreshed for the final {tailBars.Count} bars.");
            }
            if(stored is not null)
            {
                Report(prices.Count,prices.Count,"Validating",$"Validating the last 100 study bars, including the refreshed tail...");
                var currentTail=await stored.FingerprintAsync(storedPrices,ct,tailOnly:true);
                if(!stored.InitialTailFingerprint!.AsSpan().SequenceEqual(currentTail))throw new InvalidDataException("Base studies changed in the final 100 study bars during override regeneration. Restart to reconcile.");
                await stored.ValidateRefreshedAsync(refreshedTail!,ct);
            }
            if(validation==StudyPriceValidation.Full)
            {
                var current=await store.ReadAsync(monitor,ct,true);if(!current.Fingerprint.AsSpan().SequenceEqual(source.Fingerprint))throw new InvalidDataException("Source prices changed during regeneration. Committed batches remain; run again to reconcile.");
            }
            Report(prices.Count,prices.Count,"Finalizing","Checking stored row count and saving completion status...");
            await using(var c=new Microsoft.Data.SqlClient.SqlConnection(cs))
            {await c.OpenAsync(ct);await using var count=new Microsoft.Data.SqlClient.SqlCommand("SELECT COUNT_BIG(*) FROM dbo.StudyHistory WHERE StudyColId=@Id",c);count.Parameters.AddWithValue("@Id",id);if(Convert.ToInt64(await count.ExecuteScalarAsync(ct))!=prices.Count)throw new InvalidDataException("Stored studies contain timestamps outside source coverage. Committed batches remain; restore source coverage before completing regeneration.");}
            await regeneration.MarkAsync(id,"Completed","All batches committed.",ct);Report(prices.Count,prices.Count,"Completed","All price bars regenerated and committed.");
        }
        catch(OperationCanceledException)when(ct.IsCancellationRequested){if(ownsProgress)await regeneration.MarkAsync(id,"Cancelled","Cancelled; committed batches retained.",CancellationToken.None);Report(0,0,"Cancelled","Committed batches retained; rerun to resume.");throw;}
        catch(Exception ex){if(ownsProgress)await regeneration.MarkAsync(id,"Failed",ex.Message,CancellationToken.None);throw;}
    }
}
