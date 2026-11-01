using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FIG.Studies;
using FIGCommon.Models;
using FIGSignalExSvc.Services;

namespace FIGMarketLive;

public sealed record CheckpointCandidate(long RawTime,int Count,byte[] PrefixHash);
public sealed record PublishedStudyInput(PriceDataRS Price,bool ValidStudies);
public sealed record StudyRecoverySnapshot(IReadOnlyList<CheckpointCandidate> Candidates,IReadOnlyList<PublishedStudyInput> Published);
public sealed record LiveCheckpoint(StudyCheckpointPayload Payload,int Count,byte[] PrefixHash);
public sealed record CheckpointReplayResult(IReadOnlyList<EncodedStudyBar> Bars,IReadOnlyList<LiveCheckpoint> Checkpoints,int PrefixCount,
    byte[] DefinitionHash,int EveryBars,bool PersistHistory,long? LastCompletedRawTime,string Message,string? Warning);

public static class CheckpointReplay
{
    public static byte[] DefinitionHash(string definition)=>SHA256.HashData(Encoding.UTF8.GetBytes(definition));
    public static bool SameInputs(PriceDataRS a,PriceDataRS b)=>a.RawTime==b.RawTime&&a.Open==b.Open&&a.High==b.High&&a.Low==b.Low&&a.Close==b.Close&&a.Volume==b.Volume;
    public static Dictionary<int,byte[]> PrefixHashes(IReadOnlyList<PriceDataRS> prices,ISet<int> counts,CancellationToken ct=default)
    {
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);var result=new Dictionary<int,byte[]>();
        for(int i=0;i<prices.Count;i++)
        {
            ct.ThrowIfCancellationRequested();var p=prices[i];
            hash.AppendData(Encoding.UTF8.GetBytes(string.Create(CultureInfo.InvariantCulture,$"{p.Id}|{p.DataSetId}|{p.RawTime}|{p.Open:G29}|{p.High:G29}|{p.Low:G29}|{p.Close:G29}|{p.Volume}\n")));
            if(counts.Contains(i+1))result[i+1]=hash.GetCurrentHash();
        }
        return result;
    }
    public static async Task<CheckpointReplayResult> CalculateAsync(Func<StudyColBase> create,IReadOnlyList<PriceDataRS> prices,
        StudyRecoverySnapshot recovery,Func<CheckpointCandidate,CancellationToken,Task<CollectionCheckpoint?>> load,
        Func<CheckpointPriceWindow,CancellationToken,Task<List<PriceDataRS>>> hydrate,byte[] definition,int everyBars,bool enabled,CancellationToken ct=default,StudyProcessingTiming? processing=null)
    {
        processing??=new StudyProcessingTiming();
        if(everyBars<1)throw new ArgumentOutOfRangeException(nameof(everyBars));
        var collection=StudyLayers.Configure(create());bool supported=enabled&&collection.SupportsCheckpoint;
        int covered=0;
        while(covered<prices.Count&&covered<recovery.Published.Count&&recovery.Published[covered].ValidStudies&&SameInputs(prices[covered],recovery.Published[covered].Price))covered++;
        var counts=recovery.Candidates.Select(c=>c.Count).Where(c=>c>0&&c<prices.Count).ToHashSet();
        if(supported)for(int n=everyBars;n<prices.Count;n+=everyBars)counts.Add(n);
        if(supported&&prices.Count>everyBars)counts.Add(prices.Count-1);
        var hashes=supported?PrefixHashes(prices,counts,ct):[];
        int prefix=0;string message=supported?"No compatible checkpoint; full replay.":"Checkpointing disabled or unsupported by this collection; full replay.";
        if(supported)
        foreach(var candidate in recovery.Candidates.OrderByDescending(c=>c.Count))
        {
            ct.ThrowIfCancellationRequested();
            if(candidate.Count<1||candidate.Count>covered||candidate.Count>=prices.Count||prices[candidate.Count-1].RawTime!=candidate.RawTime||
                !hashes.TryGetValue(candidate.Count,out var hash)||!hash.AsSpan().SequenceEqual(candidate.PrefixHash))continue;
            try
            {
                var state=await load(candidate,ct)??throw new InvalidDataException("Missing checkpoint state.");
                if(state.RawTime!=candidate.RawTime)throw new InvalidDataException("Checkpoint row and payload times differ.");
                var window=collection.GetRequiredCheckpointPrices(state);
                collection.RestoreCheckpoint(state,window is null?null:await hydrate(window,ct));
                prefix=candidate.Count;message=$"Restored checkpoint at {candidate.RawTime}; replaying {prices.Count-prefix:N0} of {prices.Count:N0} bars.";break;
            }
            catch(Exception ex) when(ex is System.Text.Json.JsonException or InvalidDataException or ArgumentException or InvalidOperationException or NotSupportedException)
            {collection=StudyLayers.Configure(create());message=$"Checkpoint rejected ({ex.Message}); rebuilding from a compatible state or full history.";}
        }
        var bars=new List<EncodedStudyBar>(prices.Count-prefix);var saved=new List<LiveCheckpoint>();long payloadBytes=0;string? warning=null;
        using var encoder=new StudyLayers.Encoder();
        for(int i=prefix;i<prices.Count;i++)
        {
            ct.ThrowIfCancellationRequested();var p=prices[i];
            if(i>0&&p.RawTime<=prices[i-1].RawTime||p.High<p.Low||p.Open<p.Low||p.Open>p.High||p.Close<p.Low||p.Close>p.High||p.Volume<0)throw new InvalidDataException("Invalid replay prices.");
            long started=Stopwatch.GetTimestamp();var output=collection.Calc([new PriceDataRS(p)],0).Single();processing.Calculation(started);
            started=Stopwatch.GetTimestamp();bars.Add(encoder.Encode(output));processing.Encoding(started);
            int count=i+1;
            if(supported&&warning is null&&i<prices.Count-1&&count>=everyBars&&(count%everyBars==0||i==prices.Count-2))
            {
                try
                {
                    started=Stopwatch.GetTimestamp();var native=collection.CaptureCheckpoint();processing.Capture(started);
                    started=Stopwatch.GetTimestamp();var payload=StudyCheckpointPayload.Capture(native);processing.Serialization(started);
                    if(payload.RawTime!=p.RawTime||payload.Bytes>64L*1024*1024)throw new InvalidDataException("Invalid or oversized checkpoint payload.");
                    while(saved.Count>0&&payloadBytes+payload.Bytes>64L*1024*1024){payloadBytes-=saved[0].Payload.Bytes;saved.RemoveAt(0);}
                    saved.Add(new(payload,count,hashes[count]));payloadBytes+=payload.Bytes;
                }
                catch(Exception ex) when(ex is System.Text.Json.JsonException or InvalidDataException or InvalidOperationException or NotSupportedException or ArgumentException)
                {warning=$"Checkpoint capture failed: {ex.Message}. Studies were calculated; checkpoint recovery may require replay.";}
            }
        }
        return new(bars,saved,prefix,definition,everyBars,supported,prices.Count>1?prices[^2].RawTime:null,message,warning);
    }
}
