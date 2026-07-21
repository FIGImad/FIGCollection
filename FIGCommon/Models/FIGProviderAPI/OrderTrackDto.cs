using Newtonsoft.Json;

namespace FIGCommon.Models.FIGProviderAPI
{
    public class OrderTrackDto
    {
        public string BrokerOrderId { get; set; } = string.Empty;
        public Dictionary<string, string> BrokerRef { get; set; } = new();
        public string OrderType { get; set; } = OrderTypes.UNDEFINED;
        public int Qty { get; set; } = 0;
        public decimal Price { get; set; } = 0;
        public int FilledQty { get; set; } = 0;
        public decimal AvgFillPrice { get; set; } = 0;
        public decimal CommissionAndFees { get; set; } = 0;
        public int StatusCode { get; set; } = OrderStatusCodes.NEW;
        public long StatusTime { get; set; } = 0;

        public string BrokerRefJson
        {
            get => JsonConvert.SerializeObject(BrokerRef);
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    BrokerRef = new Dictionary<string, string>();
                }
                else
                {
                    BrokerRef = JsonConvert.DeserializeObject<Dictionary<string, string>>(value)
                                ?? new Dictionary<string, string>();
                }
            }
        }
    }
}
