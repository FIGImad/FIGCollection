namespace FIGCommon.Models.FIGProviderAPI
{
    public class OrderStatusRequestDto
    {
        public string AccountId { get; set; } = "";          // AccountId
        public string BrokerRef { get; set; } = "";         // Broker ClientId/OrderId information (if any) 

        public string OrderRef { get; set; } = "";         // Order reference
    }
}
