using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FIGOptimizationRunner.Search;

public sealed class ParameterSet
{
    private readonly JsonObject root;
    public ParameterSet(IEnumerable<KeyValuePair<string, decimal?>> source)
    {
        root = new JsonObject();
        foreach (var (key, value) in source) root.Add(key, value is null ? null : JsonValue.Create(value.Value));
    }
    private ParameterSet(JsonObject root) => this.root = (JsonObject)root.DeepClone();

    // Legacy flat callers retain convenient key lookup; nested values use Get(path).
    public IReadOnlyDictionary<string, ParameterValue> Values => root.ToDictionary(p => p.Key, p => Value(p.Value), StringComparer.Ordinal);
    public string Identity => Value(root).Identity;
    public string ToJson() => root.ToJsonString(Configuration.SearchConfiguration.Json);
    private static ParameterValue Value(JsonNode? node) => new(JsonSerializer.SerializeToElement(node, Configuration.SearchConfiguration.Json));

    // Flat literal names win, for compatibility with keys containing dots or slashes.
    // Otherwise dot paths select object properties; JSON Pointer selects arbitrary keys/array items.
    public string[] ResolvePath(string path)
    {
        if (root.ContainsKey(path)) return [path];
        string[] segments;
        if (path.StartsWith('/')) segments = path[1..].Split('/').Select(Unescape).ToArray();
        else segments = path.Split('.');
        if (segments.Length == 0 || (!path.StartsWith('/') && segments.Any(s => s.Length == 0)))
            throw new ArgumentException($"Invalid parameter path: {path}");
        JsonNode? node = root;
        foreach (var segment in segments) node = Child(node, segment, path);
        return segments;
    }
    private static string Unescape(string segment)
    {
        for (var n = 0; n < segment.Length; ++n)
            if (segment[n] == '~' && (++n == segment.Length || (segment[n] != '0' && segment[n] != '1')))
                throw new ArgumentException("Invalid JSON Pointer escape; use ~0 for ~ and ~1 for /");
        return segment.Replace("~1", "/").Replace("~0", "~");
    }
    private static int Index(string segment, JsonArray array, string path)
    {
        if (segment.Length == 0 || (segment.Length > 1 && segment[0] == '0') ||
            !int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index >= array.Count)
            throw new ArgumentException($"Invalid array index in parameter path: {path}");
        return index;
    }
    private static JsonNode? Child(JsonNode? node, string segment, string path) => node switch
    {
        JsonObject obj when obj.TryGetPropertyValue(segment, out var child) => child,
        JsonArray array => array[Index(segment, array, path)],
        _ => throw new ArgumentException($"Parameter path does not exist: {path}")
    };
    public ParameterValue Get(string path)
    {
        var segments = ResolvePath(path);
        JsonNode? node = root;
        foreach (var segment in segments) node = Child(node, segment, path);
        return Value(node);
    }
    public ParameterSet With(IEnumerable<KeyValuePair<string, decimal>> replacements) =>
        With(replacements.Select(p => KeyValuePair.Create(p.Key, (ParameterValue)p.Value)));
    public ParameterSet With(IEnumerable<KeyValuePair<string, ParameterValue>> replacements)
    {
        var copy = (JsonObject)root.DeepClone();
        foreach (var (path, value) in replacements)
        {
            var segments = ResolvePath(path);
            JsonNode? parent = copy;
            foreach (var segment in segments[..^1]) parent = Child(parent, segment, path);
            var node = JsonNode.Parse(value.Json.GetRawText(), documentOptions: new() { MaxDepth = 1024 });
            if (parent is JsonObject obj) obj[segments[^1]] = node;
            else if (parent is JsonArray array) array[Index(segments[^1], array, path)] = node;
            else throw new ArgumentException($"Parameter parent is not a container: {path}");
        }
        return new(copy);
    }
    public static ParameterSet Parse(string json)
    {
        using var document = JsonDocument.Parse(json, new() { MaxDepth = 1024, CommentHandling = JsonCommentHandling.Skip });
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("Parameters must be a JSON object.");
        CheckDuplicates(document.RootElement);
        return new((JsonObject)JsonNode.Parse(json, documentOptions: new() { MaxDepth = 1024, CommentHandling = JsonCommentHandling.Skip })!);
    }
    public static ParameterSet Load(string path) => Parse(File.ReadAllText(path));
    internal static void CheckDuplicates(JsonElement node)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in node.EnumerateObject())
            {
                if (!seen.Add(property.Name)) throw new ArgumentException($"Duplicate parameter: {property.Name}");
                CheckDuplicates(property.Value);
            }
        }
        else if (node.ValueKind == JsonValueKind.Array)
            foreach (var item in node.EnumerateArray()) CheckDuplicates(item);
    }
}
