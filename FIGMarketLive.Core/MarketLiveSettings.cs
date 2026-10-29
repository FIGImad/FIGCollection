using FIGCommon.Models;

namespace FIGMarketLive;

public sealed class MarketLiveSettings
{
    public string ConnectionString { get; set; } = "";
    public string PluginPath { get; set; } = "Plugins";
    public int PollSeconds { get; set; } = 5;
    public MarketData.MarketDataAdapterSettings Adapter { get; set; } = new();
    // Read legacy files without requiring a manual migration; the active adapter never uses these credentials.
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public BrokerAdapterSettings? Broker { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("DataSelections")]
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public List<MarketDataSelection>? LegacyDataSelections { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("Monitors")]
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public List<CollectionMonitor>? LegacyMonitors { get; set; }
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString)) throw new InvalidDataException("ConnectionString is required.");
        if (PollSeconds < 1) throw new InvalidDataException("PollSeconds must be positive.");
    }
}

public sealed class BrokerAdapterSettings
{
    public string BaseUrl { get; set; } = "";
    public string AccountId { get; set; } = "";
    public string ProtectedAccessToken { get; set; } = "";
}

public sealed class MarketDataSelection
{
    public string ProviderId { get; set; } = "";
    public MarketData.MarketInstrument? Instrument { get; set; }
    public int DataSetId { get; set; }
    public int TickerId { get; set; }
    public string IntervalId { get; set; } = "";
    public FIGCommon.Models.FIGProviderAPI.ContractInfo Contract { get; set; } = new();
}

public sealed class CollectionMonitor
{
    public int DataSetId { get; set; }
    public IntervalRS SourceInterval { get; set; } = new();
    public string CollectionDll { get; set; } = "";
    public StudyColRS StudyCol { get; set; } = new();
    public TickerRS Ticker { get; set; } = new();
    public IntervalRS Interval { get; set; } = new();
}
