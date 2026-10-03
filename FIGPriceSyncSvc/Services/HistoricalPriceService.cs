using FIGCommon.Interfaces;
using FIGCommon.Exceptions;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Utilities.FIGProviderAPI;

namespace FIGPriceSyncSvc.Services;

public interface IHistoricalPriceService
{
    Task<HistoricalPricesResponse> RequestAsync(HistoricalDataRequest request,CancellationToken ct);
}
public sealed class HistoricalPriceBusyException : Exception;

public sealed class HistoricalPriceService(Func<IProviderService?> provider) : IHistoricalPriceService
{
    private readonly SemaphoreSlim gate=new(1,1);
    public async Task<HistoricalPricesResponse> RequestAsync(HistoricalDataRequest request,CancellationToken ct)
    {
        var range=HistoricalPrices.Validate(request);
        if(!await gate.WaitAsync(0,ct)) throw new HistoricalPriceBusyException();
        try
        {
            var active=provider() ?? throw new InvalidOperationException("No historical-data provider configured.");
            List<BarData>? data;
            // The provider is synchronous. Keep the gate until it actually finishes, even if HTTP disconnects.
            try { data=await Task.Run(()=>active.HistoricalDataRequest(request,request.ProviderTimeoutMs ?? 45_000),CancellationToken.None); }
            catch(ProviderException ex) when(ex.ProviderErrorCode==(int)ProviderErrorCodes.NO_DATA) { data=[]; }
            ct.ThrowIfCancellationRequested();
            if(data==null) throw new InvalidOperationException("Provider returned no response.");
            if(data.Any(b=>b.Volume!=decimal.Truncate(b.Volume)))
                throw new InvalidOperationException("Provider volume cannot be represented in the integer PriceData schema.");
            var bars=data.Where(b=>b.RawTime>=range.From && b.RawTime<range.To).Select(b=>
                new HistoricalPriceBar(b.RawTime,checked((decimal)b.Open),checked((decimal)b.High),
                    checked((decimal)b.Low),checked((decimal)b.Close),checked((int)b.Volume))).OrderBy(b=>b.RawTime).ToList();
            HistoricalPrices.ValidateBars(bars,range.From,range.To,range.Interval);
            if(data.Count>0 && bars.Count==0 && data.Any(b=>b.RawTime!=range.To))
                throw new InvalidOperationException("Provider returned bars outside the requested window.");
            return new(range.From,range.To,bars);
        }
        finally { gate.Release(); }
    }
}
