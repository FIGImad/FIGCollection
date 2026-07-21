namespace FIGCommon.Models.FIGProviderAPI
{
    public class CancelOrderStatusDto
    {
        public string OrderId { get; set; } = string.Empty;
        public string BrokerOrderId { get; set; } = string.Empty;
        public string BrokerRef { get; set; } = string.Empty;
        public string OrderRefId { get; set; } = string.Empty;
        public int StatusCode { get; set; } = OrderStatusCodes.NEW;
        public long StatusTime { get; set; } = 0;
    }
}
