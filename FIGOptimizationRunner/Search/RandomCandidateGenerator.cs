using System.Numerics;
using FIGOptimizationRunner.Configuration;

namespace FIGOptimizationRunner.Search;

public sealed class RandomCandidateGenerator : ICandidateGenerator
{
    public string Name => "seeded-random-v1";
    public static readonly IReadOnlySet<string> ActiveParameters = new HashSet<string>(StringComparer.Ordinal)
    {
        "AdxLen", "FastMALen", "GuideMALen", "GuideStdDev", "StartBias", "StopBias", "ReEntryCooldownBars",
        "ShockATRLen", "ShockDropATR", "ShockArmedDropATR", "ShockArmUpATR", "ShockRangeATR",
        "ChopMinGuideWidthATR", "ChopMaxGuideSlopeATR", "ContinuationEntryBars"
    };
    private static readonly HashSet<string> Periods = ["AdxLen", "FastMALen", "GuideMALen", "ShockATRLen"];
    private static readonly HashSet<string> Counts = ["ReEntryCooldownBars", "ContinuationEntryBars"];

    public IEnumerable<ParameterSet> Generate(SearchConfiguration c, ParameterSet baseline, CancellationToken cancellation)
    {
        c.Validate();
        var keys = c.Ranges.Keys.Order(StringComparer.Ordinal).ToArray();
        BigInteger space = 1;
        foreach (var key in keys)
        {
            if (!ActiveParameters.Contains(key) || !baseline.Values.ContainsKey(key))
                throw new ArgumentException($"Unsupported/inactive long-strategy search parameter: {key}");
            var values = c.Ranges[key];
            if (values is null || values.Length == 0 || values.Distinct().Count() != values.Length)
                throw new ArgumentException($"Empty or duplicate values in range: {key}");
            foreach (var value in values)
            {
                if ((Periods.Contains(key) && (value < 1 || decimal.Truncate(value) != value)) ||
                    (Counts.Contains(key) && (value < 0 || decimal.Truncate(value) != value)) ||
                    (key == "GuideStdDev" && value < .01m) || (key == "StartBias" && value < -10) ||
                    (key == "StopBias" && value < -100) ||
                    (key != "StartBias" && key != "StopBias" && value < 0))
                    throw new ArgumentException($"Invalid value {value} for {key}");
            }
            space *= values.Length;
        }
        var inside = keys.All(k => baseline.Values[k] is decimal v && c.Ranges[k].Contains(v));
        var available = space - (inside ? 1 : 0);
        if (c.Samples > available) throw new ArgumentException($"Only {available} distinct samples available besides the baseline.");
        var seen = new HashSet<string>(StringComparer.Ordinal) { baseline.Identity };
        cancellation.ThrowIfCancellationRequested();
        yield return baseline;
        var random = new Random(c.Seed);
        var emitted = 0;
        if (space <= 100000)
        {
            var indices = Enumerable.Range(0, (int)space).ToArray();
            for (var i = indices.Length - 1; i > 0; --i)
            {
                var j = random.Next(i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }
            foreach (var index in indices)
            {
                cancellation.ThrowIfCancellationRequested();
                if (emitted == c.Samples) yield break;
                var remaining = index;
                var changes = new Dictionary<string, decimal>();
                foreach (var key in keys)
                {
                    var options = c.Ranges[key];
                    changes[key] = options[remaining % options.Length];
                    remaining /= options.Length;
                }
                var candidate = baseline.With(changes);
                if (seen.Add(candidate.Identity)) { ++emitted; yield return candidate; }
            }
        }
        else
        {
            while (emitted < c.Samples)
            {
                cancellation.ThrowIfCancellationRequested();
                var candidate = baseline.With(keys.Select(k => KeyValuePair.Create(k, c.Ranges[k][random.Next(c.Ranges[k].Length)])));
                if (seen.Add(candidate.Identity)) { ++emitted; yield return candidate; }
            }
        }
    }
}
