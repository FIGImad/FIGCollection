using FIGMarketLive.Adapters.IBKR;
using FIGMarketLive.MarketData;
using FIGCommon.Interfaces;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Utilities.FIGProviderAPI;
using Microsoft.Extensions.Logging.Abstractions;

internal static class AdapterChecks
{
    public static async Task<int> RunAsync()
    {
        int checks=0;
        void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
        var factory=new IbkrAdapterFactory(NullLoggerFactory.Instance);
        factory.Validate(new());Check(true,"Default embedded IBKR settings validate");
        foreach(var (key,value) in new[]{("Port","0"),("ClientId","0"),("RequestTimeoutSeconds","-1"),("Unknown","yes")})
        {
            bool rejected=false;try{factory.Validate(new(){Parameters=new(){[key]=value}});}catch(InvalidDataException){rejected=true;}
            Check(rejected,$"Invalid adapter parameter {key} rejected");
        }
        var signal=new ProviderConnectionSignal();var sdk=new FakeProvider(signal);int releases=0;
        using(var wrapper=new IBKRProvider.Utilities.EWrapperImpl(new IBKRProvider.Models.ReaderSignal(()=>{}),NullLogger.Instance,new CallbackQueue(),signal))
        {
            wrapper.error(new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.ConnectionRefused));
            Check(wrapper.LastErrorMessage?.StartsWith("SocketException:",StringComparison.Ordinal)==true,"SDK socket error is retained for connection diagnostics");
            wrapper.error(-1,0,502,"Connection rejected by endpoint","");
            Check(wrapper.LastErrorMessage=="IBKR error 502: Connection rejected by endpoint","SDK error code and explanation are retained together");
        }
        var adapter=new IbkrMarketDataAdapter(sdk,signal,1,1,()=>releases++);
        Check(!adapter.IsConnected,"Embedded adapter starts disconnected");
        await adapter.ConnectAsync(default);Check(adapter.IsConnected,"Connect completes after provider API handshake");
        var stocks=await adapter.SearchAsync(new("ABC","STK","SMART","USD",""),default);
        Check(stocks.Single().ProviderInstrumentId=="15" && stocks.Single().SecurityType=="STK","IBKR metadata maps to provider-neutral instruments");
        bool invalidExpiry=false;try{await adapter.SearchAsync(new("NQ","FUT","CME","USD",""),default);}catch(InvalidDataException){invalidExpiry=true;}
        Check(invalidExpiry,"Embedded futures search requires explicit expiry");
        var futures=await adapter.SearchAsync(new("NQ","FUT","CME","USD","202612"),default);
        Check(sdk.LastLookup?.LastTradeDateOrContractMonth=="202612" && futures.Single().MinimumTick==0.25m,"Future contract request preserves expiry and tick metadata");
        var resolved=await adapter.ResolveAsync(futures.Single(),default);
        Check(resolved.ContractMultiplier==20m,"Resolved multiplier is preserved");
        bool wrongIdentity=false;try{await adapter.ResolveAsync(resolved with{ProviderInstrumentId="999"},default);}catch(InvalidDataException){wrongIdentity=true;}
        Check(wrongIdentity,"Resolution cannot substitute a different selected contract");
        var bars=await adapter.GetHistoricalBarsAsync(new(resolved,1700000100,60,600),default);
        Check(sdk.LastHistory?.StartRawTime==1700000100 && sdk.LastHistory.IntervalId=="1 min" && sdk.LastHistory.Duration=="600 S" && sdk.LastHistory.NumBars==0,
            "Neutral history range maps to the existing provider's end-time semantics");
        Check(bars.Select(b=>b.RawTime).SequenceEqual(new long[]{100,160}) && bars[0].Volume==12.5m,"History is sorted and fractional volume is preserved at adapter boundary");
        using var entered=new ManualResetEventSlim();using var unblock=new ManualResetEventSlim();
        sdk.SnapshotAction=()=>{entered.Set();unblock.Wait();return 42m;};
        using var cancel=new CancellationTokenSource();
        var first=adapter.GetSnapshotAsync(resolved,cancel.Token);
        Check(entered.Wait(TimeSpan.FromSeconds(2)),"SDK snapshot started on worker thread");
        cancel.Cancel();var second=adapter.GetSnapshotAsync(resolved,default);
        await Task.Delay(30);Check(sdk.SnapshotCalls==1,"Cancellation does not let a second SDK operation overlap an active call");
        unblock.Set();bool cancelled=false;try{await first;}catch(OperationCanceledException){cancelled=true;}
        Check(cancelled && await second==42m,"Cancellation is reported after SDK work drains, then queued request succeeds");
        await adapter.DisposeAsync();await adapter.DisposeAsync();
        Check(sdk.DisconnectCalls==1 && releases==1 && !adapter.IsConnected,"Adapter shutdown disconnects and releases owned resources once");
        bool disposed=false;try{await adapter.ConnectAsync(default);}catch(ObjectDisposedException){disposed=true;}
        Check(disposed,"Disposed adapter cannot reconnect");
        var generic=new MarketDataAdapterCatalog([new AlternateFactory()]);
        await using(var alternate=generic.Create(new(){ProviderId="ALTERNATE",Parameters=new(){["Endpoint"]="example"}}))
        {
            await alternate.ConnectAsync(default);
            Check(alternate.ProviderId=="ALTERNATE" && alternate.IsConnected && generic.Descriptors.Single().Parameters.Single().Key=="Endpoint",
                "An alternative provider uses the same catalog and interface with its own settings");
        }
        return checks;
    }

    private sealed class FakeProvider(ProviderConnectionSignal signal) : IProviderService
    {
        public string Id=>"test";public string ClientId=>"31";public bool IsConnected{get;private set;}
        public ProviderOptions Options{get;}=new();public int DisconnectCalls;public int SnapshotCalls;
        public InstrumentLookupRequest? LastLookup;public HistoricalDataRequest? LastHistory;public Func<decimal?> SnapshotAction=()=>42m;
        public void CreateConnection(bool forceWait=false){IsConnected=true;signal.NotifyConnected();}
        public void Disconnect(){DisconnectCalls++;IsConnected=false;signal.NotifyDisconnected();}
        public List<ContractInfo>? SymbolLookup(string symbol,int timeout=30000)=>[new(){ConId=15,Symbol="ABC",SecType="STK",Exchange="SMART",Currency="USD"}];
        public ContractInfo? ContractInfo(InstrumentLookupRequest request,int timeout=30000)
        {LastLookup=request;return new(){ConId=16,Symbol=request.Symbol,LocalSymbol="NQZ6",SecType=request.SecType,Exchange=request.Exchange,Currency=request.Currency,LastTradeDateOrContractMonth=request.LastTradeDateOrContractMonth,Multiplier="20",MinTick=0.25m};}
        public List<BarData>? HistoricalDataRequest(HistoricalDataRequest request,int timeoutInMsec=60000)
        {LastHistory=request;return [new(160,10,12,9,11,13),new(100,10,12,9,11,12.5m)];}
        public decimal? TickerSnapshot(TickerInfo ticker,int timeoutInMsec=5000){Interlocked.Increment(ref SnapshotCalls);return SnapshotAction();}
        public int SubscribeMarketData(TickerInfo ticker)=>throw new NotSupportedException();
        public void SubscribePositions()=>throw new NotSupportedException();
        public OrderStatusDto PlaceOrder(BrokerOrderRequestDto request,int timeout=30000)=>throw new NotSupportedException();
        public OrderStatusDto? TrackOrder(string id,int timeoutInMsec=int.MaxValue)=>throw new NotSupportedException();
        public OrderStatusDto? TrackBrokerRef(string id,int timeoutInMsec=int.MaxValue)=>throw new NotSupportedException();
        public void QueryOpenOrders()=>throw new NotSupportedException();public void QueryAllCompletedOrders()=>throw new NotSupportedException();
        public List<OrderStatusDto>? QueryAllOpenOrders(int timeoutInMsec=5000)=>throw new NotSupportedException();
        public CancelStatusDto? CancelOrder(string id,string brokerRef,int timeoutInMsec=30000)=>throw new NotSupportedException();
    }

    private sealed class AlternateFactory : IMarketDataAdapterFactory
    {
        public AdapterDescriptor Descriptor{get;}=new("ALTERNATE","Another service",[new("Endpoint","Endpoint","")]);
        public void Validate(MarketDataAdapterSettings settings) {if(!settings.Parameters.ContainsKey("Endpoint"))throw new InvalidDataException();}
        public IMarketDataAdapter Create(MarketDataAdapterSettings settings)=>new AlternateAdapter();
    }
    private sealed class AlternateAdapter : IMarketDataAdapter
    {
        public string ProviderId=>"ALTERNATE";public bool IsConnected{get;private set;}
        public Task ConnectAsync(CancellationToken ct){IsConnected=true;return Task.CompletedTask;}
        public Task DisconnectAsync(CancellationToken ct){IsConnected=false;return Task.CompletedTask;}
        public Task<IReadOnlyList<MarketInstrument>> SearchAsync(InstrumentQuery query,CancellationToken ct)=>Task.FromResult<IReadOnlyList<MarketInstrument>>([]);
        public Task<MarketInstrument> ResolveAsync(MarketInstrument i,CancellationToken ct)=>Task.FromResult(i);
        public Task<IReadOnlyList<MarketBar>> GetHistoricalBarsAsync(HistoryQuery query,CancellationToken ct)=>Task.FromResult<IReadOnlyList<MarketBar>>([]);
        public Task<decimal?> GetSnapshotAsync(MarketInstrument i,CancellationToken ct)=>Task.FromResult<decimal?>(null);
        public ValueTask DisposeAsync(){IsConnected=false;return ValueTask.CompletedTask;}
    }
}
