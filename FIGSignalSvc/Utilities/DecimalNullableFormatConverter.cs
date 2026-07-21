using System.Text.Json;
using System.Text.Json.Serialization;

namespace FIGSignalExSvc.Utilities
{
    public class DecimalNullableFormatConverter : JsonConverter<decimal?>
    {
        public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType == JsonTokenType.Null ? null : reader.GetDecimal();
        }

        public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteNumberValue(Math.Round(value.Value, 4));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}



