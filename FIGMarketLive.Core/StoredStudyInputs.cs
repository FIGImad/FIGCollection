using System.Security.Cryptography;
using FIGCommon.Models;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

/// <summary>Bounded reads of the persisted pre-strategy layer; never feeds an old override to the DLL.</summary>
public sealed class StoredStudyInputs(string connectionString,int studyColId,byte[] definition,long? beforeRawTime=null,int finalTailBars=100)
{
    private readonly Dictionary<long,byte[]> buffered=new();
    public byte[]? InitialTailFingerprint { get; private set; }
    public async Task<byte[]> FingerprintAsync(IReadOnlyList<PriceDataRS> expected,CancellationToken ct,bool tailOnly=false)
    {
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using var tailHash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        int tailStart=Math.Max(0,expected.Count-finalTailBars);
        if(expected.Count==0||(tailOnly&&finalTailBars==0)){var empty=hash.GetHashAndReset();if(!tailOnly)InitialTailFingerprint=empty;return empty;}
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("SELECT RawTime,[Open],High,Low,[Close],Volume,Studies,CASE WHEN StudiesOverride IS NULL THEN 0 ELSE 1 END,StudiesBaseDefinitionHash FROM dbo.StudyHistory WHERE StudyColId=@Id"+(beforeRawTime.HasValue?" AND RawTime<@Before":"")+(tailOnly?" AND RawTime>=@First AND RawTime<=@Last":"")+" ORDER BY RawTime",c);
        cmd.Parameters.AddWithValue("@Id",studyColId);
        if(beforeRawTime.HasValue)cmd.Parameters.AddWithValue("@Before",beforeRawTime.Value);
        if(tailOnly){cmd.Parameters.AddWithValue("@First",expected[tailStart].RawTime);cmd.Parameters.AddWithValue("@Last",expected[^1].RawTime);}
        await using var r=await cmd.ExecuteReaderAsync(ct);int n=tailOnly?tailStart:0;
        while(await r.ReadAsync(ct))
        {
            if(n>=expected.Count)throw new InvalidDataException($"StudyCol {studyColId}: stored history contains bars outside the current price history. Run Full regeneration first.");
            ValidateRow(r,expected[n]);
            byte[] bytes=(byte[])r[6];
            if(bytes.Length<10||!bytes.AsSpan(0,4).SequenceEqual("FIGS"u8))throw new InvalidDataException("Stored base studies are invalid. Run Full regeneration first.");
            var time=BitConverter.GetBytes(expected[n].RawTime);var length=BitConverter.GetBytes(bytes.Length);
            hash.AppendData(time);hash.AppendData(length);hash.AppendData(bytes);
            if(!tailOnly&&n>=tailStart){tailHash.AppendData(time);tailHash.AppendData(length);tailHash.AppendData(bytes);}
            n++;
        }
        if(n!=expected.Count)throw new InvalidDataException("Stored base studies are incomplete. Run Full regeneration first.");
        if(!tailOnly)InitialTailFingerprint=tailHash.GetHashAndReset();
        return hash.GetHashAndReset();
    }
    public async Task PrepareAsync(IReadOnlyList<PriceDataRS> prices,int start,int count,CancellationToken ct)
    {
        if(buffered.ContainsKey(prices[start].RawTime)&&buffered.ContainsKey(prices[Math.Min(prices.Count-1,start+StudyBatchCalculator.MaximumChunkBars-1)].RawTime))return;
        buffered.Clear();int end=Math.Min(prices.Count,start+count);
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("SELECT RawTime,[Open],High,Low,[Close],Volume,Studies,CASE WHEN StudiesOverride IS NULL THEN 0 ELSE 1 END,StudiesBaseDefinitionHash FROM dbo.StudyHistory WHERE StudyColId=@Id AND RawTime>=@First AND RawTime<=@Last ORDER BY RawTime",c);
        cmd.Parameters.AddWithValue("@Id",studyColId);cmd.Parameters.AddWithValue("@First",prices[start].RawTime);cmd.Parameters.AddWithValue("@Last",prices[end-1].RawTime);
        await using var r=await cmd.ExecuteReaderAsync(ct);int n=start;
        while(await r.ReadAsync(ct))
        {
            if(n>=end)throw new InvalidDataException("Stored base studies changed; restart regeneration.");
            ValidateRow(r,prices[n]);
            buffered.Add(prices[n++].RawTime,(byte[])r[6]);
        }
        if(n!=end)throw new InvalidDataException("Stored base studies are incomplete; run Full regeneration.");
    }
    public Dictionary<string,object?> Read(PriceDataRS price)=>StudyBinaryCodec.Decode(buffered[price.RawTime]);
    public async Task ValidateRefreshedAsync(IReadOnlyList<EncodedStudyBar> expected,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("SELECT RawTime,[Open],High,Low,[Close],Volume,Studies,CASE WHEN StudiesOverride IS NULL THEN 0 ELSE 1 END,StudiesBaseDefinitionHash,StudiesOverride FROM dbo.StudyHistory WHERE StudyColId=@Id AND RawTime>=@First AND RawTime<=@Last ORDER BY RawTime",c);
        cmd.Parameters.AddWithValue("@Id",studyColId);cmd.Parameters.AddWithValue("@First",expected[0].Price.RawTime);cmd.Parameters.AddWithValue("@Last",expected[^1].Price.RawTime);
        await using var r=await cmd.ExecuteReaderAsync(ct);int n=0;
        while(await r.ReadAsync(ct))
        {
            if(n>=expected.Count)throw new InvalidDataException("Refreshed tail changed after publication.");
            var bar=expected[n++];ValidateRow(r,bar.Price);
            if(!bar.Studies.AsSpan().SequenceEqual((byte[])r[6])||bar.StudiesOverride is null||!bar.StudiesOverride.AsSpan().SequenceEqual((byte[])r[9]))
                throw new InvalidDataException("Study values changed in the final 100 study bars after tail publication.");
        }
        if(n!=expected.Count)throw new InvalidDataException("Refreshed tail is incomplete after publication.");
    }
    public byte[] Bytes(long rawTime)=>buffered[rawTime];
    private void ValidateRow(SqlDataReader r,PriceDataRS price)
    {
        if(r.GetInt32(7)==0||r.IsDBNull(8))
            throw new InvalidDataException($"Stored history at RawTime {r[0]} has no separate base/override layer. Clear 'L2 overrides only', select 'Full', and regenerate once with this version. Existing combined Studies cannot be reused as pre-Study_L2 inputs.");
        if(!definition.AsSpan().SequenceEqual((byte[])r[8]))
            throw new InvalidDataException($"Stored base configuration at RawTime {r[0]} does not match the current ticker, interval or collection parameters. Run Full regeneration first.");
        if(!Same(r,price))
            throw new InvalidDataException($"Stored base OHLCV/time at RawTime {r[0]} does not match current prices (expected RawTime {price.RawTime}). Run Full regeneration first.");
    }
    private static bool Same(SqlDataReader r,PriceDataRS p)=>Convert.ToInt64(r[0])==p.RawTime&&r.GetDecimal(1)==p.Open&&r.GetDecimal(2)==p.High&&r.GetDecimal(3)==p.Low&&r.GetDecimal(4)==p.Close&&r.GetInt32(5)==p.Volume;
}
