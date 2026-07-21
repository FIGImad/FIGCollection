using System.Globalization;
using System.Text.Json;
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

    protected abstract void InitializeStudies();

    public List<BarStudy> Calc(List<PriceDataRS> input, int numDel)
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
