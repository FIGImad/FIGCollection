using Newtonsoft.Json;

namespace FIGCommon.Models.FIGProviderAPI
{
    public class OrderStatusDto
    {
		public string OrderId { get; set; } = string.Empty;   // Order Table Id
        public string BrokerRef { get; set; } = string.Empty;  // PermId or broker trade id
        public string BrokerOrderId { get; set; } = string.Empty;  // Broker Order Id
        public string OrderRefId { get; set; } = string.Empty;  // Client Order Ref Id 
        public TickerInfo? Ticker { get; set; } = null;
        public Dictionary<string, string> BrokerParams { get; set; } = new();  // broker parameters
        public string OrderType { get; set; } = OrderTypes.UNDEFINED;
        public int Qty { get; set; } = 0;
        public decimal Price { get; set; } = 0;
        public int FilledQty { get; set; } = 0;
        public decimal AvgFillPrice { get; set; } = 0;
        public decimal CommissionAndFees { get; set; } = 0;
        public int StatusCode { get; set; } = OrderStatusCodes.NEW;
        public long StatusTime { get; set; } = 0;

        public OrderStatusDto()
        {
            OrderId = string.Empty;
            BrokerRef = string.Empty;
            BrokerOrderId = string.Empty;
            OrderRefId  = string.Empty;
            BrokerParams = new Dictionary<string, string>();
            OrderType = OrderTypes.UNDEFINED;
            Qty = 0;
            Price = 0;
            FilledQty = 0;
            AvgFillPrice = 0;
            CommissionAndFees = 0;
            StatusCode = OrderStatusCodes.NEW;
            StatusTime = 0;
        }

        public OrderStatusDto(OrderStatusDto other)
        {
            OrderId = other.OrderId;
            BrokerRef = other.BrokerRef;
            BrokerOrderId = other.BrokerOrderId;
            OrderRefId = other.OrderRefId;
            BrokerParams = new Dictionary<string, string>(other.BrokerParams);
            OrderType = other.OrderType;
            Qty = other.Qty;
            Price = other.Price;
            FilledQty = other.FilledQty;
            AvgFillPrice = other.AvgFillPrice;
            CommissionAndFees = other.CommissionAndFees;
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
