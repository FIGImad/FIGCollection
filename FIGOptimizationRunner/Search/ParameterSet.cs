using System.Globalization;
using System.Text.Json;

namespace FIGOptimizationRunner.Search;

public sealed class ParameterSet
{
    private readonly SortedDictionary<string, decimal?> values;
    public IReadOnlyDictionary<string, decimal?> Values => values;
    public ParameterSet(IEnumerable<KeyValuePair<string, decimal?>> source) => values = new(source.ToDictionary(), StringComparer.Ordinal);
    public ParameterSet With(IEnumerable<KeyValuePair<string, decimal>> replacements)
    {
        var copy = new SortedDictionary<string, decimal?>(values, StringComparer.Ordinal);
        foreach (var (key, value) in replacements) copy[key] = value;
        return new(copy);
    }
    // Ignore formatting differences such as 1 versus 1.0 when detecting identical candidates.
    public string Identity => string.Join(";", values.Select(p => p.Key + "=" + p.Value?.ToString("G29", CultureInfo.InvariantCulture)));
    public string ToJson() => JsonSerializer.Serialize(values, Configuration.SearchConfiguration.Json);
    public static ParameterSet Load(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("Parameters must be a JSON object.");
        var parsed = new Dictionary<string, decimal?>(StringComparer.Ordinal);
        foreach (var p in document.RootElement.EnumerateObject())
        {
            decimal? value = p.Value.ValueKind == JsonValueKind.Null ? null : p.Value.GetDecimal();
            if (!parsed.TryAdd(p.Name, value)) throw new ArgumentException($"Duplicate parameter: {p.Name}");
        }
        return new(parsed);
    }
}
