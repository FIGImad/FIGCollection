using System.Data;
using System.Text.Json;
using FIG.Studies;
using FIGCommon.Models;
using FIGSignalExSvc.Services;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

/// <summary>Retains the full base runtime independently of override-only checkpoints.</summary>
public sealed class StudyTailCheckpointStore(string connectionString)
{
    public async Task SaveAsync(CollectionMonitor monitor,byte[] definition,int count,byte[] prefixHash,StudyColBase collection,CancellationToken ct)
    {
        if(count==0||!collection.SupportsCheckpoint)return;
        var payload=StudyCheckpointPayload.Capture(collection.CaptureCheckpoint());
        await SaveAsync(monitor,definition,count,prefixHash,payload,ct);
    }
    public async Task SaveAsync(CollectionMonitor monitor,byte[] definition,int count,byte[] prefixHash,StudyCheckpointPayload payload,CancellationToken ct)
    {
        if(payload.Bytes>64L*1024*1024)throw new InvalidDataException("Tail checkpoint exceeds 64 MiB.");
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        await StudyHistoryStore.ValidateAsync(c,tx,monitor,ct,true);
        await using var cmd=new SqlCommand("""
            UPDATE dbo.StudyTailCheckpoint SET DefinitionHash=@Definition,PrefixCount=@Count,PrefixHash=@Hash,CheckpointState=@State,UpdatedAt=SYSUTCDATETIME() WHERE StudyColId=@Id;
            IF @@ROWCOUNT=0 INSERT dbo.StudyTailCheckpoint(StudyColId,DefinitionHash,PrefixCount,PrefixHash,CheckpointState,UpdatedAt) VALUES(@Id,@Definition,@Count,@Hash,@State,SYSUTCDATETIME());
            """,c,tx);
        cmd.Parameters.AddWithValue("@Id",monitor.StudyCol.Id);cmd.Parameters.AddWithValue("@Definition",definition);cmd.Parameters.AddWithValue("@Count",count);cmd.Parameters.AddWithValue("@Hash",prefixHash);cmd.Parameters.AddWithValue("@State",payload.Json);
        await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
    }

    public async Task<StudyColBase> PrepareAsync(CollectionMonitor monitor,Func<StudyColBase> create,IReadOnlyList<PriceDataRS> prices,int boundary,byte[] definition,bool checkpoints,Action<string> report,CancellationToken ct)
    {
        var collection=create();if(boundary==0)return collection;
        var candidates=new List<(CheckpointCandidate Candidate,CollectionCheckpoint? State)>();
        var history=new MarketLiveCheckpointStore(connectionString);
        if(checkpoints&&collection.SupportsCheckpoint)
        {
            await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
            await using var cmd=new SqlCommand("SELECT PrefixCount,PrefixHash,CheckpointState FROM dbo.StudyTailCheckpoint WHERE StudyColId=@Id AND DefinitionHash=@Definition AND PrefixCount>0 AND PrefixCount<=@Max AND DATALENGTH(CheckpointState)<=67108864",c);
            cmd.Parameters.AddWithValue("@Id",monitor.StudyCol.Id);cmd.Parameters.AddWithValue("@Definition",definition);cmd.Parameters.AddWithValue("@Max",boundary);
            await using(var r=await cmd.ExecuteReaderAsync(ct))
                if(await r.ReadAsync(ct))
                {
                    int count=r.GetInt32(0);
                    try{candidates.Add((new(prices[count-1].RawTime,count,(byte[])r[1]),JsonSerializer.Deserialize<CollectionCheckpoint>(r.GetString(2),StudyStateJson.Options)));}
                    catch(JsonException){ /* Rebuild rather than trust damaged cached state. */ }
                }
            candidates.AddRange((await new StudyRegenerationStore(connectionString).CandidatesAsync(monitor.StudyCol.Id,definition,boundary,ct)).Select(c=>(c,(CollectionCheckpoint?)null)));
        }
        var counts=candidates.Select(c=>c.Candidate.Count).Append(boundary).ToHashSet();
        var hashes=CheckpointReplay.PrefixHashes(prices,counts,ct);int start=0;
        foreach(var entry in candidates.OrderByDescending(c=>c.Candidate.Count))
        {
            var candidate=entry.Candidate;
            if(candidate.Count>boundary||candidate.RawTime!=prices[candidate.Count-1].RawTime||!hashes[candidate.Count].AsSpan().SequenceEqual(candidate.PrefixHash))continue;
            try
            {
                var state=entry.State??await history.LoadAsync(monitor.StudyCol.Id,definition,candidate,ct);if(state is null||state.RawTime!=candidate.RawTime)continue;
                var window=collection.GetRequiredCheckpointPrices(state);
                collection.RestoreCheckpoint(state,window is null?null:prices.Where(p=>p.RawTime>=window.FirstRawTime&&p.RawTime<=window.LastRawTime).ToList());
                start=candidate.Count;break;
            }
            catch(Exception ex)when(ex is InvalidDataException or JsonException or InvalidOperationException or ArgumentException or NotSupportedException){collection=create();}
        }
        report(start>0?$"Restored full state at bar {start:N0}; warming {boundary-start:N0} bars for the final 10-bar refresh.":$"No compatible full checkpoint; warming {boundary:N0} bars once. Earlier stored Studies will not be rewritten.");
        var calculator=new StudyBatchCalculator(collection,ct);int nextReport=start+5000;
        for(int i=start;i<boundary;)
        {
            int end=Math.Min(boundary,i+StudyBatchCalculator.MaximumChunkBars);calculator.Calculate(prices,i,end);i=end;
            if(i>=nextReport){report($"Preparing full state: {i:N0}/{boundary:N0} bars.");nextReport=i+5000;}
        }
        if(checkpoints&&collection.SupportsCheckpoint)await SaveAsync(monitor,definition,boundary,hashes[boundary],collection,ct);
        return collection;
    }
}
