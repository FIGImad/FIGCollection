using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using FIGOptimizationRunner.Search;

namespace FIGOptimizationRunner.Configuration;

public sealed record SearchConfiguration
{
    public int DataSetId { get; init; } = 3;
    public int IntervalMinutes { get; init; } = 5;
    public string BaseParameters { get; init; } = "adx-base-parameters.json";
    public string Settings { get; init; } = "signal-settings.json";
    public string OutputRoot { get; init; } = "optimizer-output";
    public string Executable { get; init; } = "x64/Release/FIGSignalCpp.exe";
    public string CollectionDll { get; init; } = "";
    public string Collection { get; init; } = "ADXBase";
    public string Signal { get; init; } = "STrend_LBaseADX_Side";
    [JsonIgnore]
    public string EffectiveCollectionDll => string.IsNullOrEmpty(CollectionDll)
        ? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(Executable))!, "FIGStudyCollection.ADX.dll")
        : Path.GetFullPath(CollectionDll);
    public string PriceCacheDirectory { get; init; } = "price-cache";
    public string PriceCacheMode { get; init; } = "off";
    public int Samples { get; init; } = 1000;
    public int Seed { get; init; } = 12345;
    public int Jobs { get; init; } = 2;
    public int MaxSourceBars { get; init; }
    public decimal InitialCapital { get; init; } = 9_255_000;
    public int Quantity { get; init; } = 15;
    public decimal QuantityPct { get; init; } = 1m;
    public decimal Multiplier { get; init; } = 20;
    public decimal CommissionPerContractPerSide { get; init; } = 1.60m;
    public decimal SlippagePointsPerSide { get; init; }
    public bool ForceCloseAtEnd { get; init; }
    public string StartDate { get; init; } = "2010-01-01";
    public string EndDateExclusive { get; init; } = "";
    public string TrainEndExclusive { get; init; } = "2023-01-01";
    public string ValidationEndExclusive { get; init; } = "2025-01-01";
    public Dictionary<string, ParameterValue[]> Ranges { get; init; } = new(StringComparer.Ordinal);
    // Accepted for compatibility with snapshots produced by the PowerShell runner.
    public string? ExecutableSHA256 { get; init; }

    public static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        MaxDepth = 1024,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static SearchConfiguration Load(string path)
    {
        path = Path.GetFullPath(path);
        var text = File.ReadAllText(path);
        using var document = JsonDocument.Parse(text, new() { MaxDepth = Json.MaxDepth });
        ParameterSet.CheckDuplicates(document.RootElement);
        var c = JsonSerializer.Deserialize<SearchConfiguration>(text, Json)
            ?? throw new ArgumentException("Empty search configuration.");
        var root = Path.GetDirectoryName(path)!;
        string Resolve(string value) => Path.GetFullPath(value, root);
        return c with
        {
            BaseParameters = Resolve(c.BaseParameters), Settings = Resolve(c.Settings),
            OutputRoot = Resolve(c.OutputRoot), Executable = Resolve(c.Executable),
            CollectionDll = string.IsNullOrEmpty(c.CollectionDll) ? "" : Resolve(c.CollectionDll),
            PriceCacheDirectory = Resolve(c.PriceCacheDirectory)
        };
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Collection) || string.IsNullOrWhiteSpace(Signal) || CollectionDll is null)
            throw new ArgumentException("Collection and Signal must be nonempty; CollectionDll must be a path or an empty string.");
        if (PriceCacheMode is not ("off" or "use" or "refresh"))
            throw new ArgumentException("PriceCacheMode must be off, use, or refresh.");
        if (string.IsNullOrWhiteSpace(PriceCacheDirectory))
            throw new ArgumentException("PriceCacheDirectory must not be empty.");
        if (DataSetId <= 0 || IntervalMinutes is < 1 or > 35791394 || Jobs is < 1 or > 64 ||
            MaxSourceBars < 0 || Samples is < 0 or > 100000 || Quantity < 0)
            throw new ArgumentException("Invalid dataset, interval, jobs, source limit, samples, or quantity.");
        if (QuantityPct <= 0 || QuantityPct > 1)
            throw new ArgumentException("QuantityPct must be greater than 0 and at most 1.");
        if (InitialCapital <= 0 || Multiplier <= 0 || CommissionPerContractPerSide < 0 || SlippagePointsPerSide < 0)
            throw new ArgumentException("Capital/multiplier must be positive and trading costs nonnegative.");
        static DateOnly Date(string value) => DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var start = Date(StartDate);
        if (start >= Date(TrainEndExclusive) || Date(TrainEndExclusive) >= Date(ValidationEndExclusive) ||
            (!string.IsNullOrEmpty(EndDateExclusive) && Date(EndDateExclusive) <= start))
            throw new ArgumentException("Dates must satisfy start < train-end < validation-end; end must exceed start.");
        if (Ranges is null) throw new ArgumentException("Ranges cannot be null.");
    }
}
