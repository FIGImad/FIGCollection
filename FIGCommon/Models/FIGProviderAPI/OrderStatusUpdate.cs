using Newtonsoft.Json;

namespace FIGCommon.Models.FIGProviderAPI
{
    public class OrderStatusUpdate
    {
        public string BrokerRef { get; set; } = string.Empty;  // PermId or broker trade id
        public string BrokerOrderId { get; set; } = string.Empty;  // broker orderId
        public Dictionary<string, string> BrokerParams { get; set; } = new();  // broker parameters, has orderId in it
        public int QtyFilled { get; set; } = 0;
        public int QtyRemaining { get; set; } = 0;
        public decimal AvgFillPrice { get; set; } = 0;
        public int StatusCode { get; set; } = OrderStatusCodes.NEW;
        public long StatusTime { get; set; } = 0;

        public OrderStatusUpdate()
        {
            BrokerRef = string.Empty;
            BrokerOrderId = string.Empty;
            BrokerParams = new Dictionary<string, string>();
            QtyFilled = 0;
            QtyRemaining = 0;
            AvgFillPrice = 0;
            StatusCode = OrderStatusCodes.NEW;
            StatusTime = 0;
        }

        public OrderStatusUpdate(OrderStatusUpdate other)
        {
            BrokerRef = other.BrokerRef;
            BrokerOrderId = other.BrokerOrderId;
            BrokerParams = new Dictionary<string, string>(other.BrokerParams);
            QtyFilled = other.QtyFilled;
            QtyRemaining = other.QtyRemaining;
            AvgFillPrice = other.AvgFillPrice;
            StatusCode = other.StatusCode;
            StatusTime = other.StatusTime;
        }
        public string BrokerParamsJson
        {
            get => JsonConvert.SerializeObject(BrokerParams);
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    BrokerParams = new Dictionary<string, string>();
                }
                else
                {
                    BrokerParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(value)
                                ?? new Dictionary<string, string>();
                }
            }
        }

    }
}
