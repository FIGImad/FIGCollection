using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using FIGCommon.Models;
using FIGCommon.Utilities;
using Microsoft.Extensions.Logging;

namespace FIG.Studies;

public abstract class StudyColBase
{
    protected string collectionTag = "";
    protected int maxNumPrices;
    protected int maxNumStudies;
    protected CPriceList priceList;
    protected CircularBuffer<BarStudy> studies;
    protected List<BaseStudy>? studyList;
    protected readonly JsonElement colParams;
    protected readonly ILogger _logger;
    protected readonly IntervalRS studyInterval;
    protected readonly TickerRS studyTicker;

    protected StudyColBase(
        StudyCollectionContext context,
        int numStudiesToKeep = 10,
        int numPricesToKeep = 900000)
    {
        Context = context;
        _logger = context.LoggerFactory.CreateLogger(GetType().FullName ?? GetType().Name);
        colParams = context.Parameters;
        studyTicker = new TickerRS(context.Ticker);
        studyInterval = new IntervalRS(context.Interval);
        maxNumPrices = numPricesToKeep;
        maxNumStudies = numStudiesToKeep;
        studies = new CircularBuffer<BarStudy>(numStudiesToKeep);
        priceList = new CPriceList(maxNumPrices);
    }

    public StudyCollectionContext Context { get; }

    // Override only after auditing collection-specific state and implementing the hooks below.
    protected virtual string? CheckpointSchema => null;
    // Opt out only when the plugin owns all calculation history itself. Defaults
    // preserve full history for studies that read prior outputs or CPriceList.
    protected virtual bool CheckpointNeedsHistoricalOutputs => true;
    protected virtual int CheckpointPriceHistoryLimit => maxNumPrices;
    // Opt in only when OHLCV/time are sufficient to reconstruct input prices;
    // StudyHistory does not retain PriceData IDs or other source metadata.
    protected virtual bool CheckpointPricesFromHistory => false;
    protected virtual JsonElement CaptureCollectionState() => JsonSerializer.SerializeToElement(new { });
    protected virtual void RestoreCollectionState(JsonElement state) { }
    public bool SupportsCheckpoint
    {
        get
        {
            studyList ??= CreateStudies();
            return CheckpointSchema != null && studyList.All(s => s.SupportsCheckpoint);
        }
    }

    private string CheckpointIdentity => JsonSerializer.Serialize(new
    {
        Type = GetType().FullName, Build = GetType().Module.ModuleVersionId,
        CoreBuild = typeof(StudyColBase).Module.ModuleVersionId,
        CheckpointSchema, Context.Configuration.ColType, Context.Configuration.ColParams,
        Context.Configuration.TickerId, Context.Configuration.IntervalId,
        studyInterval.IntervalLen, maxNumPrices, maxNumStudies,
        CheckpointNeedsHistoricalOutputs, CheckpointPriceHistoryLimit, CheckpointPricesFromHistory
    });

    public CollectionCheckpoint CaptureCheckpoint()
    {
        if (!SupportsCheckpoint) throw new NotSupportedException("Collection or study has not opted into checkpoints.");
        if (CheckpointPriceHistoryLimit < 1 || CheckpointPriceHistoryLimit > maxNumPrices)
            throw new InvalidOperationException("Invalid checkpoint price-history limit.");
        var bars = studies.Items.Take(studies.Count).Select((b, index) => new CheckpointBar(new PriceDataRS(b!.Price),
            index == 0 || CheckpointNeedsHistoricalOutputs
                ? b.Studies.ToDictionary(p => p.Key, p => CheckpointValue.Capture(p.Value)) : new())).ToList();
        return new(CheckpointPricesFromHistory ? 2 : 1, CheckpointIdentity, bars.FirstOrDefault()?.Price.RawTime ?? 0,
            CheckpointPricesFromHistory ? [] : priceList.CapturePrices(CheckpointPriceHistoryLimit), bars,
            studyList!.Select(s => s.CaptureCheckpoint(CheckpointPricesFromHistory)).ToList(), CaptureCollectionState())
        {
            PriceWindow = CheckpointPricesFromHistory && priceList.Count > 0
                ? priceList.CapturePriceWindow(CheckpointPriceHistoryLimit) : null
        };
    }

    // Validate compatibility and the bounded request before the host reads any price rows.
    public CheckpointPriceWindow? GetRequiredCheckpointPrices(CollectionCheckpoint checkpoint)
    {
        if (!SupportsCheckpoint || checkpoint.FormatVersion != (CheckpointPricesFromHistory ? 2 : 1)
            || checkpoint.Identity != CheckpointIdentity || checkpoint.Studies == null || checkpoint.Bars == null || checkpoint.Prices == null
            || checkpoint.Studies.Count != studyList!.Count || checkpoint.Bars.Count > maxNumStudies || checkpoint.Bars.Count == 0
            || checkpoint.RawTime != checkpoint.Bars[0].Price.RawTime)
            throw new InvalidDataException("Incompatible or incomplete collection checkpoint.");
        if (CheckpointPricesFromHistory)
        {
            if (checkpoint.Prices.Count != 0 || checkpoint.PriceWindow == null)
                throw new InvalidDataException("Missing checkpoint price-window reference.");
            checkpoint.PriceWindow.Validate(CheckpointPriceHistoryLimit, checkpoint.RawTime);
            if (checkpoint.PriceWindow.Count < checkpoint.Bars.Count)
                throw new InvalidDataException("Checkpoint price window cannot cover retained study bars.");
        }
        else if (checkpoint.PriceWindow != null || checkpoint.Prices.Count == 0 || checkpoint.Prices.Count > maxNumPrices
            || checkpoint.RawTime != checkpoint.Prices[^1].RawTime)
            throw new InvalidDataException("Incompatible or incomplete checkpoint prices.");
        return checkpoint.PriceWindow;
    }

    /// <summary>Restore into a newly constructed collection. Discard it if restoration fails.</summary>
    public void RestoreCheckpoint(CollectionCheckpoint checkpoint) => RestoreCheckpoint(checkpoint, null);

    public void RestoreCheckpoint(CollectionCheckpoint checkpoint, IReadOnlyList<PriceDataRS>? historyPrices)
    {
        var window = GetRequiredCheckpointPrices(checkpoint);
        var restoredPrices = window == null ? checkpoint.Prices : historyPrices
            ?? throw new InvalidDataException("Checkpoint requires prices from StudyHistory.");
        window?.ValidatePrices(restoredPrices);
        for (int i = 1; i < restoredPrices.Count; i++)
            if (restoredPrices[i].RawTime <= restoredPrices[i - 1].RawTime)
                throw new InvalidDataException("Unordered checkpoint prices.");
        for (int i = 1; i < checkpoint.Bars.Count; i++)
            if (checkpoint.Bars[i].Price.RawTime >= checkpoint.Bars[i - 1].Price.RawTime)
                throw new InvalidDataException("Unordered checkpoint bars.");
        Dictionary<long, PriceDataRS>? barPrices = null;
        if (window != null)
        {
            var byTime = restoredPrices.ToDictionary(p => p.RawTime);
            foreach (var bar in checkpoint.Bars)
                if (!byTime.TryGetValue(bar.Price.RawTime, out var price) || !SamePriceInputs(price, bar.Price))
                    throw new InvalidDataException("Checkpoint study bars do not match price history.");
            barPrices = checkpoint.Bars.ToDictionary(b => b.Price.RawTime, b => b.Price);
        }
        for (int i = 0; i < studyList!.Count; i++)
        {
            var parameters = checkpoint.Studies[i].Parameters;
            if (parameters == null || parameters.Length == 0 || !parameters[0].TryGetProperty("Price", out var price)
                || !price.TryGetProperty("RawTime", out var time) || !time.TryGetInt64(out var rawTime)
                || rawTime != checkpoint.RawTime)
                throw new InvalidDataException("Study state does not match checkpoint bar.");
            var state = checkpoint.Studies[i];
            if (barPrices != null)
                state = state with { Parameters = parameters.Select(p => ResolveParameterPrice(p, barPrices)).ToArray() };
            studyList[i].RestoreCheckpoint(state);
        }
        RestoreCollectionState(checkpoint.Custom);
        priceList.Clear();
        foreach (var price in restoredPrices) priceList.Add(new PriceDataRS(price));
        studies.Clear();
        foreach (var bar in checkpoint.Bars.AsEnumerable().Reverse())
            studies.AddToFront(new BarStudy(bar.Price) { Studies = bar.Values.ToDictionary(p => p.Key, p => p.Value.Restore()) });
    }

    private static bool SamePriceInputs(PriceDataRS left, PriceDataRS right)
        => left.RawTime == right.RawTime && left.Open == right.Open && left.High == right.High
            && left.Low == right.Low && left.Close == right.Close && left.Volume == right.Volume;

    private static JsonElement ResolveParameterPrice(JsonElement parameter, Dictionary<long, PriceDataRS> prices)
    {
        if (!parameter.TryGetProperty("Price", out var reference) || reference.ValueKind != JsonValueKind.Object
            || reference.EnumerateObject().Count() != 1 || !reference.TryGetProperty("RawTime", out var time)
            || !time.TryGetInt64(out var rawTime) || !prices.TryGetValue(rawTime, out var price))
            throw new InvalidDataException("Invalid study price reference.");
        var hydrated = JsonNode.Parse(parameter.GetRawText())!.AsObject();
        hydrated["Price"] = JsonSerializer.SerializeToNode(price, StudyStateJson.Options);
        return JsonSerializer.SerializeToElement(hydrated);
    }

    protected abstract void InitializeStudies();

    public List<BarStudy> Calc(List<PriceDataRS> input, int numDel)
        => Calc(input, numDel, null);

    // Invoke after all studies finish each bar, before advancing to the next bar.
    // Keep the two-argument overload for existing compiled plugins/callers.
    public List<BarStudy> Calc(List<PriceDataRS> input, int numDel, Action<BarStudy>? onBarProcessed)
    {
        var lastNdx = 0;
        long lastRawTime = 0;

        numDel = Math.Max(0, numDel);
        numDel = Math.Min(studies.Count, numDel);
        if (studies.Count > numDel && studies.Count > 0)
        {
            lastRawTime = studies.Items[numDel]?.Price.RawTime ?? 0;
        }

        if (lastRawTime > 0)
        {
            for (var i = input.Count - 1; i >= 0; i--)
            {
                if (input[i].RawTime > lastRawTime)
                {
                    lastNdx = i;
                    continue;
                }
                break;
            }

            while (studies.Count > 0)
            {
                var studyRawTime = studies.Items[0]?.Price.RawTime ?? 0;
                if (studyRawTime <= lastRawTime)
                {
                    break;
                }
                studies.RemoveFromFront();
            }
        }
        else
        {
            studies.Clear();
        }

        studyList ??= CreateStudies();

        var barStudyList = new List<BarStudy>();
        for (var i = lastNdx; i < input.Count; i++)
        {
            priceList.Add(input[i]);
            var newStudy = new BarStudy(input[i]);
            studies.AddToFront(newStudy);

            foreach (var study in studyList)
            {
                study.Process(input[i], studies, priceList);
            }

            barStudyList.Add(new BarStudy(newStudy));
            onBarProcessed?.Invoke(newStudy);
        }

        return barStudyList;
    }

    public virtual int GetIntervalLen() => studyInterval.IntervalLen;

    public abstract int GetMaxLen();

    protected T GetParameters<T>() where T : class => Context.DeserializeParameters<T>();

    protected int? GetIntParam(string key)
        => TryGetParameter(key, out var value) && value.TryGetInt32(out var result) ? result : null;

    protected double? GetDoubleParam(string key)
        => TryGetParameter(key, out var value) && value.TryGetDouble(out var result) ? result : null;

    protected List<T> GetArrayParam<T>(string key)
    {
        if (!TryGetParameter(key, out var value))
        {
            return [];
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            return value.EnumerateArray()
                .Select(item => ConvertValue<T>(item.ToString()))
                .ToList();
        }

        return value.ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ConvertValue<T>)
            .ToList();
    }

    protected static void AddStudy(List<BaseStudy> list, BaseStudy study) => list.Add(study);

    private List<BaseStudy> CreateStudies()
    {
        InitializeStudies();
        return studyList is { Count: > 0 }
            ? studyList
            : throw new InvalidOperationException(
                $"Study collection '{collectionTag}' did not configure any studies.");
    }

    private bool TryGetParameter(string key, out JsonElement value)
    {
        foreach (var property in colParams.EnumerateObject())
        {
            if (string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static T ConvertValue<T>(string value)
        => (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
}
