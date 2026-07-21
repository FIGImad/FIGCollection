using System.Text.Json;

namespace FIGCommon.Models.FIGBroker
{
    public static class JsonConfigExtensions
    {
        // Centralized options (add whatever you need here)
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true, // helpful if JSON changes casing
                                                // You can add converters, number handling, etc.
                                                // NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public static TConfig ToConfig<TConfig>(this string json)
            where TConfig : new()
        {
            if (string.IsNullOrWhiteSpace(json))
                return new TConfig();

            var obj = JsonSerializer.Deserialize<TConfig>(json, Options);
            return obj ?? new TConfig();
        }

        public static string FromConfig<TConfig>(this TConfig config)
            => JsonSerializer.Serialize(config, Options);
    }
}
