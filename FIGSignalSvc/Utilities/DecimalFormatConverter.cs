using System.Text.Json;
using System.Text.Json.Serialization;

namespace FIGSignalExSvc.Utilities
{
    public class DecimalFormatConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDecimal(); // No change when deserializing
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            // Format to 4 decimal places when serializing
            writer.WriteNumberValue(Math.Round(value, 4));
        }
    }
}



