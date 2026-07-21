using Newtonsoft.Json.Linq;

namespace FIGCommon.Models.FIGBroker
{
    public enum BrokerCallbackMessageType
    {
        Unknown = 0,
        OpenOrder = 1,
        OrderStatus = 2,
        CompletedOrder = 3,
        Execution = 4,
        MarketData = 5,
        Error = 6,
        PositionUpdate = 7,
        PositionUpdateError = 8
    }

    public class BrokerCallbackMessage
    {
        public BrokerCallbackMessageType Type { get; set; } = BrokerCallbackMessageType.Unknown;
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// How long this message is valid after <see cref="TimestampUtc"/>.
        /// Null means the message never expires.
        /// </summary>
        public TimeSpan? Ttl { get; set; } = null;

        public bool IsExpired =>
            Ttl.HasValue && DateTime.UtcNow - TimestampUtc > Ttl.Value;

        // Optional payload fields
        public JToken? DataJson { get; set; } = null;
    }
}
