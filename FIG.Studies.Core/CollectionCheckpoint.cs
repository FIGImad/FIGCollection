using System.Text.Json;
using System.Text.Json.Serialization;
using FIGCommon.Models;

namespace FIG.Studies;

public sealed record CollectionCheckpoint(int FormatVersion, string Identity, long RawTime,
    List<PriceDataRS> Prices, List<CheckpointBar> Bars, List<StudyCheckpoint> Studies, JsonElement Custom)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CheckpointPriceWindow? PriceWindow { get; init; }
}

public sealed record CheckpointBar(PriceDataRS Price, Dictionary<string, CheckpointValue> Values);

// Preserve scalar CLR types used by study-to-study inputs; plain object JSON would restore JsonElements.
public sealed record CheckpointValue(string Kind, JsonElement Value)
{
    public static CheckpointValue Capture(object? value)
    {
        var kind = value switch
        {
            null => "null", decimal => "decimal", double => "double", float => "float",
            int => "int", long => "long", bool => "bool", string => "string",
            _ => throw new NotSupportedException($"Checkpoint output type {value.GetType().Name} is unsupported.")
        };
        return new(kind, JsonSerializer.SerializeToElement(value));
    }

    public object? Restore() => Kind switch
    {
        "null" => null, "decimal" => Value.GetDecimal(), "double" => Value.GetDouble(),
        "float" => Value.GetSingle(), "int" => Value.GetInt32(), "long" => Value.GetInt64(),
        "bool" => Value.GetBoolean(), "string" => Value.GetString(),
        _ => throw new InvalidDataException("Unknown checkpoint output type.")
    };
}
