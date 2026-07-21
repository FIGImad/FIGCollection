using System.Text.Json.Serialization;

namespace FIGCommon.Models.FIGBroker
{
	public class ConfigParamIBKR : ConfigParamBase
	{

        [JsonPropertyName("client_id")]
        public int ClientId { get; set; } = 1;

        [JsonPropertyName("tracking_client_id")]
        public int TrackingClientId { get; set; } = 1;

        [JsonPropertyName("order_exec_timeout")]
        public int OrderExecTimeout { get; set; } = 30;

        [JsonPropertyName("exec_timeout")]
        public int ExecTimeout { get; set; } = 10;

        public override int BrokerClientId => ClientId;
        public override int BrokerTrackingClientId => TrackingClientId;
		public override int OrderExecTimeoutSeconds => OrderExecTimeout;
        public override int ExecTimeoutSeconds => ExecTimeout;
    }
}


