using System.Globalization;

namespace FIGCommon.Models.FIGProviderAPI;

public sealed record HistoricalPriceBar(long RawTime, decimal Open, decimal High, decimal Low, decimal Close, int Volume);
public sealed record HistoricalPricesResponse(long FromRawTime, long ToRawTime, List<HistoricalPriceBar> Bars);

// Bounded intraday requests; StartRawTime is the provider's END time (existing provider convention).
public static class HistoricalPrices
{
    public const string Route = "/api/historicaldata";
    public const int MaxBars = 1000;
    private static readonly Dictionary<string, int> Intervals = new(StringComparer.Ordinal)
    {
        ["1 secs"]=1,["5 secs"]=5,["10 secs"]=10,["15 secs"]=15,["30 secs"]=30,
        ["1 min"]=60,["2 mins"]=120,["3 mins"]=180,["5 mins"]=300,["10 mins"]=600,
        ["15 mins"]=900,["20 mins"]=1200,["30 mins"]=1800,["1 hrs"]=3600,["2 hrs"]=7200,["4 hrs"]=14400
    };
    public static (long From, long To, int Interval) Validate(HistoricalDataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if(request.ProviderTimeoutMs is < 1_000 or > 300_000)
            throw new ArgumentException("ProviderTimeoutMs must be between 1000 and 300000 milliseconds.");
        var ticker=request.Ticker;
        if(ticker==null || new[]{ticker.Symbol,ticker.Currency,ticker.Exchange}.Any(s=>string.IsNullOrWhiteSpace(s)||s.Length>100)
            || (ticker.SecType!="FUT" && ticker.SecType!="STK") || (ticker.SecType=="FUT" && ticker.ContractMonth<=0))
            throw new ArgumentException("A complete FUT or STK contract is required. Continuous futures cannot be paged by end time.");
        if(!Intervals.TryGetValue(request.IntervalId ?? "",out var interval) || request.NumBars!=0)
            throw new ArgumentException("Use a supported intraday provider interval and NumBars=0; results must not be truncated.");
        var parts=(request.Duration??"").Split(' ',StringSplitOptions.RemoveEmptyEntries);
        if(parts.Length!=2 || parts[1]!="S" || !int.TryParse(parts[0],NumberStyles.None,CultureInfo.InvariantCulture,out var seconds)
            || seconds<interval || seconds>86400 || seconds%interval!=0 || seconds/interval>MaxBars)
            throw new ArgumentException("Duration must be whole bars expressed as '<seconds> S', at most one day and 1000 bars.");
        if(request.StartRawTime<seconds || request.StartRawTime>int.MaxValue)
            throw new ArgumentException("StartRawTime must be an explicit UTC Unix end time that fits the local schema.");
        return(request.StartRawTime-seconds,request.StartRawTime,interval);
    }

    public static void ValidateBars(IEnumerable<HistoricalPriceBar> bars,long from,long to,int interval)
    {
        var times=new HashSet<long>();
        foreach(var bar in bars)
        {
            if(!times.Add(bar.RawTime) || times.Count>MaxBars || bar.RawTime<from || bar.RawTime>=to
                || bar.RawTime%interval!=0 || bar.Volume<0 || bar.High<bar.Low
                || bar.Open<bar.Low || bar.Open>bar.High || bar.Close<bar.Low || bar.Close>bar.High)
                throw new InvalidOperationException("Invalid or duplicate historical bar; the window has not been accepted.");
            foreach(var price in new[]{bar.Open,bar.High,bar.Low,bar.Close})
                if(price>99999999999999.9999m || price< -99999999999999.9999m || decimal.Round(price,4)!=price)
                    throw new InvalidOperationException("Historical price does not fit decimal(18,4).");
        }
    }
}
