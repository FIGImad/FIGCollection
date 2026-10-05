using System.Text.Json;
using System.Text.Json.Serialization;
using FIGCommon.Models;

namespace FIG.Studies;

/// <summary>Versioned data only: deserialization never resolves CLR types from the database.</summary>
public sealed record StudyCheckpoint(string Identity, JsonElement[] Parameters, JsonElement Runtime);

public static class StudyStateJson
{
    // Study parameter classes use fields. Default JSON options silently omit them.
    public static readonly JsonSerializerOptions Options = new() { IncludeFields = true };
    internal static readonly JsonSerializerOptions PriceReferences = new(Options)
    {
        Converters = { new PriceReferenceConverter() }
    };

    private sealed class PriceReferenceConverter : JsonConverter<PriceDataRS>
    {
        public override void Write(Utf8JsonWriter writer, PriceDataRS value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("RawTime", value.RawTime);
            writer.WriteEndObject();
        }

        public override PriceDataRS Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException("Price references must be resolved before restoring study state.");
    }
}
