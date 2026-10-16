using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FIGOptimizationRunner.Search;

// Immutable, detached JSON: values survive disposal of the source document.
[JsonConverter(typeof(ParameterValueConverter))]
public readonly struct ParameterValue : IEquatable<ParameterValue>
{
    private readonly JsonElement value;
    public JsonElement Json => value.ValueKind == JsonValueKind.Undefined
        ? JsonSerializer.SerializeToElement<object?>(null) : value;
    public ParameterValue(JsonElement value)
    {
        ParameterSet.CheckDuplicates(value);
        this.value = value.Clone();
    }
    public static implicit operator ParameterValue(int value) => new(JsonSerializer.SerializeToElement(value));
    public static implicit operator ParameterValue(decimal value) => new(JsonSerializer.SerializeToElement(value));
    public static implicit operator ParameterValue(bool value) => new(JsonSerializer.SerializeToElement(value));
    public static implicit operator ParameterValue(string value) => new(JsonSerializer.SerializeToElement(value));
    public bool TryDecimal(out decimal result)
    {
        result = 0;
        return Json.ValueKind == JsonValueKind.Number && Json.TryGetDecimal(out result);
    }
    public string Identity => Canonical(Json);
    public bool Equals(ParameterValue other) => Identity == other.Identity;
    public override bool Equals(object? other) => other is ParameterValue v && Equals(v);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Identity);
    public static bool operator ==(ParameterValue left, ParameterValue right) => left.Equals(right);
    public static bool operator !=(ParameterValue left, ParameterValue right) => !left.Equals(right);

    private static string Canonical(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => "{" + string.Join(",", element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => JsonSerializer.Serialize(p.Name) + ":" + Canonical(p.Value))) + "}",
        JsonValueKind.Array => "[" + string.Join(",", element.EnumerateArray().Select(Canonical)) + "]",
        JsonValueKind.Number => NormalizeNumber(element.GetRawText()),
        JsonValueKind.String => JsonSerializer.Serialize(element.GetString()),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => "null"
    };
    // Exact normalization without floating-point rounding or decimal range limits.
    private static string NormalizeNumber(string text)
    {
        var negative = text.StartsWith('-');
        if (negative) text = text[1..];
        var exponentIndex = text.IndexOfAny(['e', 'E']);
        var exponent = exponentIndex < 0 ? BigInteger.Zero : BigInteger.Parse(text[(exponentIndex + 1)..], CultureInfo.InvariantCulture);
        var mantissa = exponentIndex < 0 ? text : text[..exponentIndex];
        var dot = mantissa.IndexOf('.');
        if (dot >= 0) exponent -= mantissa.Length - dot - 1;
        var digits = mantissa.Replace(".", "").TrimStart('0');
        if (digits.Length == 0) return "0";
        var significant = digits.TrimEnd('0');
        exponent += digits.Length - significant.Length;
        return (negative ? "-" : "") + significant + "e" + exponent.ToString(CultureInfo.InvariantCulture);
    }
}

public sealed class ParameterValueConverter : JsonConverter<ParameterValue>
{
    public override ParameterValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        return new(document.RootElement);
    }
    public override void Write(Utf8JsonWriter writer, ParameterValue value, JsonSerializerOptions options) => value.Json.WriteTo(writer);
}
