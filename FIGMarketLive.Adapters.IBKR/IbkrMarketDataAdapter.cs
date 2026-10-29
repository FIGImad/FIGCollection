using System.Globalization;
using FIGCommon.Interfaces;
using FIGCommon.Models.FIGProviderAPI;
using FIGMarketLive.MarketData;

namespace FIGMarketLive.Adapters.IBKR;

/// <summary>All calls share a gate. Cancellation never releases it while SDK work is still running.</summary>
public sealed class IbkrMarketDataAdapter(IProviderService provider,IProviderConnectionSignal signal,int connectionTimeoutSeconds,int requestTimeoutSeconds,Action? release=null) : IMarketDataAdapter
{
    private readonly SemaphoreSlim gate=new(1,1);
    private bool disposed;
    private volatile bool ready;
    public string ProviderId=>"IBKR";
    public bool IsConnected=>!disposed && ready && provider.IsConnected;

    private async Task<T> ExecuteAsync<T>(Func<T> action,CancellationToken ct,bool requireConnection=true)
    {
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(disposed,this);ct.ThrowIfCancellationRequested();
            if(requireConnection && !IsConnected)throw new InvalidOperationException("Connect the market-data adapter first.");
            var result=await Task.Run(action,CancellationToken.None).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();return result;
        }
        finally{gate.Release();}
    }

    public Task ConnectAsync(CancellationToken ct)=>ExecuteAsync(()=>
    {
        if(IsConnected)return true;
        ready=false;
        signal.NotifyDisconnected();
        try
        {
            provider.CreateConnection();
            // IsConnected alone tests only the socket; wait for the API handshake callback too.
            signal.WaitForConnectionAsync().WaitAsync(TimeSpan.FromSeconds(connectionTimeoutSeconds),ct).GetAwaiter().GetResult();
            if(!provider.IsConnected)throw new IOException("Provider disconnected during handshake.");ready=true;return true;
        }
        catch{provider.Disconnect();signal.NotifyDisconnected();throw;}
    },ct,false);

    public Task DisconnectAsync(CancellationToken ct)=>ExecuteAsync(()=>{ready=false;provider.Disconnect();signal.NotifyDisconnected();return true;},ct,false);

    public Task<IReadOnlyList<MarketInstrument>> SearchAsync(InstrumentQuery query,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(query.Symbol) || query.SecurityType is not ("FUT" or "STK"))throw new InvalidDataException("Enter a symbol and FUT/STK security type.");
        return ExecuteAsync<IReadOnlyList<MarketInstrument>>(()=>
        {
            if(query.SecurityType=="FUT")
            {
                ValidateExpiry(query.Expiry);
                var contract=provider.ContractInfo(new(){Symbol=query.Symbol.Trim(),SecType=query.SecurityType,Exchange=query.Exchange.Trim(),Currency=query.Currency.Trim(),LastTradeDateOrContractMonth=query.Expiry},requestTimeoutSeconds*1000);
                return contract is null?[]:[Map(contract)];
            }
            var result=provider.SymbolLookup(query.Symbol.Trim(),requestTimeoutSeconds*1000)??throw new IOException("IBKR symbol lookup failed.");
            return result.Where(c=>c.SecType=="STK" && (string.IsNullOrWhiteSpace(query.Currency)||c.Currency.Equals(query.Currency,StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(query.Exchange)||c.Exchange.Equals(query.Exchange,StringComparison.OrdinalIgnoreCase)||c.PrimaryExch.Equals(query.Exchange,StringComparison.OrdinalIgnoreCase))).Select(Map).ToArray();
        },ct);
    }

    public Task<MarketInstrument> ResolveAsync(MarketInstrument instrument,CancellationToken ct)=>ExecuteAsync(()=>
    {
        if(instrument.SecurityType=="FUT")ValidateExpiry(instrument.Expiry);
        var result=provider.ContractInfo(new(){Symbol=instrument.Symbol,SecType=instrument.SecurityType,
            Exchange=string.IsNullOrWhiteSpace(instrument.Exchange)?instrument.PrimaryExchange:instrument.Exchange,Currency=instrument.Currency,LastTradeDateOrContractMonth=instrument.Expiry},requestTimeoutSeconds*1000)
            ??throw new InvalidDataException("No unambiguous contract was resolved.");
        var mapped=Map(result);
        if(!string.IsNullOrEmpty(instrument.ProviderInstrumentId) && instrument.ProviderInstrumentId!=mapped.ProviderInstrumentId)throw new InvalidDataException("Resolved instrument does not match the selection.");
        return mapped;
    },ct);

    public Task<IReadOnlyList<MarketBar>> GetHistoricalBarsAsync(HistoryQuery query,CancellationToken ct)=>ExecuteAsync<IReadOnlyList<MarketBar>>(()=>
    {
        if(query.BarSeconds<1 || query.DurationSeconds<1 || query.EndRawTime<=0)throw new InvalidDataException("History requires a positive UTC end, bar interval and duration.");
        if(query.Instrument.SecurityType is not ("STK" or "FUT"))throw new InvalidDataException("History supports stocks and individual futures contracts.");
        var bars=provider.HistoricalDataRequest(new(){Ticker=Ticker(query.Instrument),StartRawTime=query.EndRawTime,NumBars=0,
            IntervalId=BarSize(query.BarSeconds),Duration=$"{query.DurationSeconds} S"},requestTimeoutSeconds*1000)??throw new IOException("IBKR historical request failed.");
        return bars.OrderBy(b=>b.RawTime).Select(b=>new MarketBar(b.RawTime,(decimal)b.Open,(decimal)b.High,(decimal)b.Low,(decimal)b.Close,b.Volume)).ToArray();
    },ct);
    public Task<decimal?> GetSnapshotAsync(MarketInstrument instrument,CancellationToken ct)=>ExecuteAsync(()=>provider.TickerSnapshot(Ticker(instrument),requestTimeoutSeconds*1000),ct);
    public static string BarSize(int seconds)=>seconds switch
    {1=>"1 secs",5=>"5 secs",10=>"10 secs",15=>"15 secs",30=>"30 secs",60=>"1 min",120=>"2 mins",180=>"3 mins",300=>"5 mins",600=>"10 mins",900=>"15 mins",1200=>"20 mins",1800=>"30 mins",3600=>"1 hour",7200=>"2 hours",10800=>"3 hours",14400=>"4 hours",28800=>"8 hours",86400=>"1 day",_=>throw new InvalidDataException("Unsupported IBKR historical bar interval.")};
    private static TickerInfo Ticker(MarketInstrument i)=>new(){Symbol=i.Symbol,SecType=i.SecurityType,Exchange=i.Exchange,Currency=i.Currency,ContractMonth=int.TryParse(i.Expiry,out int month)?month:0};
    private static void ValidateExpiry(string expiry)
    {if(expiry.Length is not (6 or 8)||!expiry.All(char.IsAsciiDigit))throw new InvalidDataException("Futures require YYYYMM or YYYYMMDD expiry.");}
    private static MarketInstrument Map(ContractInfo c)=>new(){ProviderInstrumentId=c.ConId>0?c.ConId.ToString(CultureInfo.InvariantCulture):"",Symbol=c.Symbol,LocalSymbol=c.LocalSymbol,Name=c.Name,
        SecurityType=c.SecType,Exchange=c.Exchange,PrimaryExchange=c.PrimaryExch,Currency=c.Currency,Expiry=c.LastTradeDateOrContractMonth,MinimumTick=c.MinTick,
        ContractMultiplier=string.IsNullOrWhiteSpace(c.Multiplier)?1:decimal.Parse(c.Multiplier,CultureInfo.InvariantCulture)};

    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try{if(disposed)return;disposed=true;ready=false;await Task.Run(provider.Disconnect).ConfigureAwait(false);}
        finally{if(disposed){release?.Invoke();release=null;}gate.Release();}
    }
}
