using FIG.Studies;
using FIGCommon.Models;

namespace FIGMarketLive;

/// <summary>Amortizes host calls without changing DLLs, price ownership, bar ordering or checkpoint state.</summary>
public sealed class StudyBatchCalculator(StudyColBase collection,CancellationToken cancellation)
{
    public const int MaximumChunkBars=16;
    private readonly List<PriceDataRS> input=new(MaximumChunkBars);
    private readonly Action<BarStudy> checkCancellation=_=>cancellation.ThrowIfCancellationRequested();

    public static int NextEnd(int start,int total,int committedPrefix,int batchSize,int checkpointEvery,bool checkpoints)
    {
        if(start<0||start>=total||committedPrefix<0||committedPrefix>total||batchSize<1||checkpointEvery<1)
            throw new ArgumentOutOfRangeException(nameof(start));
        long end=Math.Min((long)total,(long)start+MaximumChunkBars);
        // A non-checkpoint plugin may replay its committed prefix to reconstruct state.
        if(start<committedPrefix)end=Math.Min(end,committedPrefix);
        else end=Math.Min(end,committedPrefix+((long)(start-committedPrefix)/batchSize+1)*batchSize);
        if(checkpoints)
        {
            end=Math.Min(end,((long)start/checkpointEvery+1)*checkpointEvery);
            if(start<total-1)end=Math.Min(end,total-1); // snapshot the newest completed bar, never the forming bar
        }
        return checked((int)end);
    }

    public List<BarStudy> Calculate(IReadOnlyList<PriceDataRS> prices,int start,int end)
    {
        if(start<0||end<=start||end>prices.Count||end-start>MaximumChunkBars)throw new ArgumentOutOfRangeException(nameof(end));
        input.Clear();
        for(int i=start;i<end;i++)
        {
            cancellation.ThrowIfCancellationRequested();var p=prices[i];
            if(i>0&&p.RawTime<=prices[i-1].RawTime||p.Low>p.High||p.Open<p.Low||p.Open>p.High||p.Close<p.Low||p.Close>p.High||p.Volume<0)
                throw new InvalidDataException("Invalid source OHLCV.");
            input.Add(new(p)); // plugins retain input references; never share the source snapshot with them
        }
        // The legacy API clears retained studies when its previous timestamp is nonpositive.
        // Preserve that unusual pre-epoch behavior rather than batching across those bars.
        List<BarStudy> results;
        if(input.Any(p=>p.RawTime<=0))
        {results=new(input.Count);foreach(var p in input)results.AddRange(collection.Calc([p],0,checkCancellation));}
        else results=collection.Calc(input,0,checkCancellation);
        if(results.Count!=end-start)throw new InvalidDataException("Collection returned an incomplete calculation chunk.");
        for(int n=0;n<results.Count;n++)
            if(!CheckpointReplay.SameInputs(results[n].Price,prices[start+n]))throw new InvalidDataException("Collection returned mismatched price data.");
        return results;
    }
}
