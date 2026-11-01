using FIGCommon.Models;

namespace FIGMarketLive;

public sealed class MarketLiveSettings
{
    public string ConnectionString { get; set; } = "";
    public string PluginPath { get; set; } = "Plugins";
    public int PollSeconds { get; set; } = 60;
    public int StudySqlTimeoutSeconds { get; set; } = 360;
    public int StudyBatchSize { get; set; } = 1000;
    public string StudyConnectionString()=>new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(ConnectionString){CommandTimeout=StudySqlTimeoutSeconds}.ConnectionString;
    public bool CheckpointsEnabled { get; set; } = true;
    public int CheckpointEveryBars { get; set; } = 5000;
    public int PriceHistoryMinutes { get; set; } = 5 * 24 * 60;
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
        if (PollSeconds < 1 || PollSeconds > 86400) throw new InvalidDataException("PollSeconds must be between 1 and 86400 seconds.");
        if(StudySqlTimeoutSeconds<1||StudySqlTimeoutSeconds>86400)throw new InvalidDataException("Study SQL timeout must be 1–86400 seconds.");
        if(StudyBatchSize<1||StudyBatchSize>100000)throw new InvalidDataException("Study batch size must be 1–100000 records.");
        if (CheckpointEveryBars < 1) throw new InvalidDataException("CheckpointEveryBars must be positive.");
        if (PriceHistoryMinutes < 1 || PriceHistoryMinutes > 30 * 24 * 60) throw new InvalidDataException("PriceHistoryMinutes must be between 1 and 43200 (30 days).");
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
