using System.Numerics;
using FIGOptimizationRunner.Configuration;

namespace FIGOptimizationRunner.Search;

public sealed class RandomCandidateGenerator : ICandidateGenerator
{
    public string Name => "seeded-random-json-v2";
    public static readonly IReadOnlySet<string> ActiveParameters = new HashSet<string>(StringComparer.Ordinal)
    {
        "AdxLen", "FastMALen", "GuideMALen", "GuideStdDev", "StartBias", "StopBias", "ReEntryCooldownBars",
        "ShockATRLen", "ShockDropATR", "ShockArmedDropATR", "ShockArmUpATR", "ShockRangeATR",
        "ChopMinGuideWidthATR", "ChopMaxGuideSlopeATR", "ContinuationEntryBars"
    };
    // LDAX preserves the EFS cooldown behavior and ignores ContinuationMaxShockATR.
    // Neither is a useful search dimension. TrendReEntryShockMax actively gates LDAX entries.
    private static readonly IReadOnlySet<string> LdaxActiveParameters = ActiveParameters
        .Except(["ReEntryCooldownBars", "ContinuationEntryBars"])
        .Append("TrendReEntryShockMax").ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> Periods = ["AdxLen", "FastMALen", "GuideMALen", "ShockATRLen"];
    private static readonly HashSet<string> Counts = ["ReEntryCooldownBars", "ContinuationEntryBars"];

    public IEnumerable<ParameterSet> Generate(SearchConfiguration c, ParameterSet baseline, CancellationToken cancellation)
    {
        c.Validate();
        var active = c.Collection.Equals("LDAX", StringComparison.OrdinalIgnoreCase) ? LdaxActiveParameters
            : c.Collection.Equals("ADXBase", StringComparison.OrdinalIgnoreCase) ||
              c.Collection.Equals("ADXTrend", StringComparison.OrdinalIgnoreCase) ? ActiveParameters : null;
        var keys = c.Ranges.Keys.Order(StringComparer.Ordinal).ToArray();
        var paths = keys.ToDictionary(k => k, k => baseline.ResolvePath(k), StringComparer.Ordinal);
        for (var i = 0; i < keys.Length; ++i)
            for (var j = i + 1; j < keys.Length; ++j)
            {
                var left = paths[keys[i]];
                var right = paths[keys[j]];
                var common = Math.Min(left.Length, right.Length);
                if (left.Take(common).SequenceEqual(right.Take(common)))
                    throw new ArgumentException($"Search paths overlap or select the same value: {keys[i]}, {keys[j]}");
            }
        BigInteger space = 1;
        foreach (var key in keys)
        {
            var path = paths[key];
            var legacyKey = path.Length == 1 ? path[0] : key;
            if (active is not null && !active.Contains(legacyKey))
                throw new ArgumentException($"Unsupported/inactive {c.Collection} search parameter: {key}");
            var values = c.Ranges[key];
            if (values is null || values.Length == 0 || values.Distinct().Count() != values.Length)
                throw new ArgumentException($"Empty or duplicate values in range: {key}");
            foreach (var value in values)
            {
                // Other plugins own their parameter schema; require baseline keys and let the DLL validate values.
                if (active is not null)
                {
                    if (!value.TryDecimal(out var number) ||
                        (Periods.Contains(legacyKey) && (number < 1 || decimal.Truncate(number) != number)) ||
                        (Counts.Contains(legacyKey) && (number < 0 || decimal.Truncate(number) != number)) ||
                        (legacyKey == "GuideStdDev" && number < .01m) || (legacyKey == "StartBias" && number < -10) ||
                        (legacyKey == "StopBias" && number < -100) ||
                        (legacyKey != "StartBias" && legacyKey != "StopBias" && number < 0))
                        throw new ArgumentException($"Invalid value {value.Json.GetRawText()} for {key}");
                }
            }
            space *= values.Length;
        }
        bool ValidThresholds(ParameterSet parameters)
        {
            if (!c.Collection.Equals("LADX", StringComparison.OrdinalIgnoreCase)) return true;
            var values = parameters.Values;
            if (!values.TryGetValue("marketTrendSettings", out var settings) &&
                !values.TryGetValue("MarketTrendSettings", out settings)) return true;
            var obj = settings.Json;
            if (obj.ValueKind != System.Text.Json.JsonValueKind.Object) return false;
            bool Read(string camel, string pascal, decimal fallback, out decimal value)
            {
                value = fallback;
                if (!obj.TryGetProperty(camel, out var element) && !obj.TryGetProperty(pascal, out element)) return true;
                return element.ValueKind == System.Text.Json.JsonValueKind.Number && element.TryGetDecimal(out value);
            }
            return Read("adxStrongThreshold", "AdxStrongThreshold", 25, out var strong) &&
                Read("adxWeakThreshold", "AdxWeakThreshold", 20, out var weak) &&
                weak >= 0 && weak <= strong && strong <= 100;
        }
        if (!ValidThresholds(baseline)) throw new ArgumentException("LADX baseline requires 0 <= adxWeakThreshold <= adxStrongThreshold <= 100.");
        ParameterSet AtIndex(int index)
        {
            var changes = new Dictionary<string, ParameterValue>();
            foreach (var key in keys)
            {
                var options = c.Ranges[key];
                changes[key] = options[index % options.Length];
                index /= options.Length;
            }
            return baseline.With(changes);
        }
        int[]? validIndices = null;
        if (c.Collection.Equals("LADX", StringComparison.OrdinalIgnoreCase) && space <= 100000)
            validIndices = Enumerable.Range(0, (int)space).Where(i => { cancellation.ThrowIfCancellationRequested(); return ValidThresholds(AtIndex(i)); }).ToArray();
        var inside = keys.All(k => c.Ranges[k].Contains(baseline.Get(k)));
        var available = (validIndices is null ? space : validIndices.Length) - (inside ? 1 : 0);
        if (c.Samples > available) throw new ArgumentException($"Only {available} distinct samples available besides the baseline.");
        var seen = new HashSet<string>(StringComparer.Ordinal) { baseline.Identity };
        cancellation.ThrowIfCancellationRequested();
        yield return baseline;
        var random = new Random(c.Seed);
        var emitted = 0;
        if (space <= 100000)
        {
            var indices = validIndices ?? Enumerable.Range(0, (int)space).ToArray();
            for (var i = indices.Length - 1; i > 0; --i)
            {
                var j = random.Next(i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }
            foreach (var index in indices)
            {
                cancellation.ThrowIfCancellationRequested();
                if (emitted == c.Samples) yield break;
                var candidate = AtIndex(index);
                if (ValidThresholds(candidate) && seen.Add(candidate.Identity)) { ++emitted; yield return candidate; }
            }
        }
        else
        {
            var attempts = 0L;
            while (emitted < c.Samples)
            {
                cancellation.ThrowIfCancellationRequested();
                if (++attempts > Math.Max(1000000L, (long)c.Samples * 1000))
                    throw new ArgumentException("Unable to sample enough valid distinct candidates; narrow the parameter ranges.");
                var candidate = baseline.With(keys.Select(k => KeyValuePair.Create(k, c.Ranges[k][random.Next(c.Ranges[k].Length)])));
                if (ValidThresholds(candidate) && seen.Add(candidate.Identity)) { ++emitted; yield return candidate; }
            }
        }
    }
}
