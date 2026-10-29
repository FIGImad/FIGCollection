using FIGMarketLive.MarketData;
using FIGCommon.Interfaces;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Utilities.FIGProviderAPI;
using IBKRProvider.Service;
using IBKRProvider.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace FIGMarketLive.Adapters.IBKR;

public sealed class IbkrAdapterFactory(ILoggerFactory logs) : IMarketDataAdapterFactory
{
    public AdapterDescriptor Descriptor { get; } = new("IBKR","IBKR — TWS / IB Gateway",
    [new("Host","Host","127.0.0.1"),new("Port","API socket port","7497"),new("ClientId","Dedicated API client ID","31"),
     new("ConnectionTimeoutSeconds","API handshake timeout (seconds)","15"),new("RequestTimeoutSeconds","Request timeout (seconds)","30")],
     "Connects directly to an already logged-in TWS / IB Gateway with socket API access enabled. Use its configured API port and a dedicated client ID. No FIG login or bearer token is needed.");

    public void Validate(MarketDataAdapterSettings settings)
    {
        if(!settings.ProviderId.Equals(Descriptor.Id,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Wrong adapter factory.");
        if(settings.Parameters.Keys.Any(k=>!Descriptor.Parameters.Any(p=>p.Key==k)))throw new InvalidDataException("Unknown IBKR adapter parameter.");
        if(string.IsNullOrWhiteSpace(Value(settings,"Host")))throw new InvalidDataException("IBKR host is required.");
        _=Number(settings,"Port",1,65535);_=Number(settings,"ClientId",1,int.MaxValue);
        _=Number(settings,"ConnectionTimeoutSeconds",1,120);_=Number(settings,"RequestTimeoutSeconds",1,120);
    }
    private string Value(MarketDataAdapterSettings s,string key)=>s.Parameters.TryGetValue(key,out var value)?value:Descriptor.Parameters.Single(p=>p.Key==key).DefaultValue;
    private int Number(MarketDataAdapterSettings s,string key,int min,int max)=>int.TryParse(Value(s,key),NumberStyles.None,CultureInfo.InvariantCulture,out int n)&&n>=min&&n<=max?n:
        throw new InvalidDataException($"{key} must be an integer between {min} and {max}.");

    public IMarketDataAdapter Create(MarketDataAdapterSettings settings)
    {
        Validate(settings);
        var lifetime=new AdapterLifetime();var signal=new ProviderConnectionSignal();
        var services=new ServiceCollection().AddSingleton<IHostApplicationLifetime>(lifetime)
            .AddSingleton(logs).AddSingleton(typeof(ILogger<>),typeof(Logger<>)).AddSingleton<ProviderTask>().BuildServiceProvider();
        var provider=new Provider(logs.CreateLogger<Provider>(),services,lifetime,new ProviderOptions
        {Id="FIGMarketLive",Host=Value(settings,"Host").Trim(),Port=Number(settings,"Port",1,65535),ClientId=Number(settings,"ClientId",1,int.MaxValue)},new CallbackQueue(),signal);
        return new IbkrMarketDataAdapter(provider,signal,Number(settings,"ConnectionTimeoutSeconds",1,120),Number(settings,"RequestTimeoutSeconds",1,120),
            ()=> {lifetime.StopApplication();services.Dispose();lifetime.Dispose();});
    }

    private sealed class AdapterLifetime : IHostApplicationLifetime,IDisposable
    {
        private readonly CancellationTokenSource stopping=new();
        public CancellationToken ApplicationStarted=>CancellationToken.None;
        public CancellationToken ApplicationStopping=>stopping.Token;
        public CancellationToken ApplicationStopped=>stopping.Token;
        public void StopApplication()=>stopping.Cancel();
        public void Dispose()=>stopping.Dispose();
    }
}
