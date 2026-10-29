using FIGCommon.Models;

namespace FIGMarketLive;

/// <summary>UTC bar-opening timestamps; fixed periods anchored at Unix epoch, weeks on Monday, calendar months/years.</summary>
public static class PriceBarAggregation
{
    public static IReadOnlyList<PriceDataRS> Build(IReadOnlyList<PriceDataRS> input,IntervalRS source,IntervalRS target,CancellationToken ct=default)
    {
        if(!StudyIntervalCompatibility.CanAggregate(source,target))throw new InvalidDataException("Source interval cannot produce the requested study interval.");
        bool direct=source.Id==target.Id;var output=new List<PriceDataRS>();long? previous=null;
        foreach(var price in input)
        {
            ct.ThrowIfCancellationRequested();
            if(previous.HasValue&&price.RawTime<=previous.Value)throw new InvalidDataException("Aggregation requires strictly increasing source timestamps.");
            previous=price.RawTime;
            if(price.High<price.Low||price.Open<price.Low||price.Open>price.High||price.Close<price.Low||price.Close>price.High||price.Volume<0)
                throw new InvalidDataException("Invalid source OHLCV during aggregation.");
            if(direct){output.Add(new(price));continue;}
            var time=DateTimeOffset.FromUnixTimeSeconds(price.RawTime).UtcDateTime;
            var from=Bounds(time,source);var bucket=Bounds(time,target);
            if(from.Start!=time||from.End>bucket.End)throw new InvalidDataException("Source bars do not align with UTC aggregation boundaries; source bars cannot be split.");
            long raw=new DateTimeOffset(bucket.Start).ToUnixTimeSeconds();
            if(output.Count==0||output[^1].RawTime!=raw)
                output.Add(new(){DataSetId=price.DataSetId,RawTime=raw,PriceDate=bucket.Start,Open=price.Open,High=price.High,Low=price.Low,Close=price.Close,Volume=price.Volume});
            else
            {
                var bar=output[^1];bar.High=Math.Max(bar.High,price.High);bar.Low=Math.Min(bar.Low,price.Low);bar.Close=price.Close;bar.Volume=checked(bar.Volume+price.Volume);
            }
        }
        return output;
    }
    private static (DateTime Start,DateTime End) Bounds(DateTime utc,IntervalRS interval)
    {
        var epoch=DateTime.UnixEpoch;
        char unit=interval.Id.Length>0?interval.Id[^1]:'\0';
        int count=interval.Id.Length==1?1:int.TryParse(interval.Id[..^1],out int n)?n:0;
        if(unit is 'D' or 'W' or 'M' or 'Y')
        {
            if(count<1)throw new InvalidDataException("Invalid calendar interval.");
            if(unit is 'D' or 'W')
            {
                int days=checked(count*(unit=='W'?7:1));var anchor=unit=='W'?epoch.AddDays(-3):epoch;
                var start=anchor.AddDays(Math.Floor((utc.Date-anchor).TotalDays/days)*days);return(start,start.AddDays(days));
            }
            if(unit=='M')
            {
                int months=(utc.Year-1970)*12+utc.Month-1;
                var start=epoch.AddMonths((int)Math.Floor((double)months/count)*count);return(start,start.AddMonths(count));
            }
            int year=1970+(int)Math.Floor((double)(utc.Year-1970)/count)*count;
            var first=new DateTime(year,1,1,0,0,0,DateTimeKind.Utc);return(first,first.AddYears(count));
        }
        if(interval.IntervalLen<1)throw new InvalidDataException("Invalid fixed interval.");
        double seconds=(utc-epoch).TotalSeconds;
        var fixedStart=epoch.AddSeconds(Math.Floor(seconds/interval.IntervalLen)*interval.IntervalLen);
        return(fixedStart,fixedStart.AddSeconds(interval.IntervalLen));
    }
}
