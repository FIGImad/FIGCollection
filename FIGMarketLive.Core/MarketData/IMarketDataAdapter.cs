namespace FIGMarketLive.MarketData;

public sealed class MarketDataAdapterSettings
{
    public string ProviderId { get; set; } = "IBKR";
    public Dictionary<string,string> Parameters { get; set; } = [];
}

public sealed record AdapterParameter(string Key,string Label,string DefaultValue);
public sealed record AdapterDescriptor(string Id,string DisplayName,IReadOnlyList<AdapterParameter> Parameters,string HelpText="");
public sealed record InstrumentQuery(string Symbol,string SecurityType,string Exchange,string Currency,string Expiry);

/// <summary>Published contract metadata, independent of any broker SDK or database identities.</summary>
public sealed record MarketInstrument
{
    public string ProviderInstrumentId { get; init; } = "";
    public string Symbol { get; init; } = "";
    public string LocalSymbol { get; init; } = "";
    public string Name { get; init; } = "";
    public string SecurityType { get; init; } = "";
    public string Exchange { get; init; } = "";
    public string PrimaryExchange { get; init; } = "";
    public string Currency { get; init; } = "";
    public string Expiry { get; init; } = "";
    public string ExpirationDate { get; init; } = "";
    /// <summary>SQL capture boundary after an explicit contract roll; broker adapters need not populate it.</summary>
    public long? HistoryStartRawTime { get; init; }
    public decimal MinimumTick { get; init; }
    public decimal ContractMultiplier { get; init; } = 1;
}

public sealed record MarketBar(long RawTime,decimal Open,decimal High,decimal Low,decimal Close,decimal Volume);
public sealed record HistoryQuery(MarketInstrument Instrument,long EndRawTime,int BarSeconds,int DurationSeconds);

/// <summary>Only market-data operations. Implementations own SDK mapping, request serialization and connection lifetime.</summary>
public interface IMarketDataAdapter : IAsyncDisposable
{
    string ProviderId { get; }
    bool IsConnected { get; }
    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);
    Task<IReadOnlyList<MarketInstrument>> SearchAsync(InstrumentQuery query,CancellationToken ct);
    Task<MarketInstrument> ResolveAsync(MarketInstrument instrument,CancellationToken ct);
    Task<IReadOnlyList<MarketBar>> GetHistoricalBarsAsync(HistoryQuery query,CancellationToken ct);
    Task<decimal?> GetSnapshotAsync(MarketInstrument instrument,CancellationToken ct);
}

public interface IMarketDataAdapterFactory
{
    AdapterDescriptor Descriptor { get; }
    void Validate(MarketDataAdapterSettings settings);
    IMarketDataAdapter Create(MarketDataAdapterSettings settings);
}

public sealed class MarketDataAdapterCatalog(IEnumerable<IMarketDataAdapterFactory> factories)
{
    private readonly Dictionary<string,IMarketDataAdapterFactory> factories=factories.ToDictionary(f=>f.Descriptor.Id,StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<AdapterDescriptor> Descriptors=>factories.Values.Select(f=>f.Descriptor).ToArray();
    public IMarketDataAdapterFactory Get(string id)=>factories.TryGetValue(id,out var factory)?factory:
        throw new InvalidDataException($"Market-data provider '{id}' is not installed.");
    public IMarketDataAdapter Create(MarketDataAdapterSettings settings)
    {var factory=Get(settings.ProviderId);factory.Validate(settings);return factory.Create(settings);}
}
