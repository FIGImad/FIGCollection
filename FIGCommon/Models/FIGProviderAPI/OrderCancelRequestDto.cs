namespace FIGCommon.Models.FIGProviderAPI
{
    public class OrderCancelRequestDto
    {
        public string AccountId { get; set; } = "";          // AccountId
        public string BrokerRef { get; set; } = "";         // Broker Ref (PermId)

        public string OrderRefId { get; set; } = "";            // Order TxnId (from client)
    }
}