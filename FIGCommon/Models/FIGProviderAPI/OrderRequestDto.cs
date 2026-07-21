
namespace FIGCommon.Models.FIGProviderAPI
{
    public class OrderRequestDto
    {
        public string AccountId { get; set; } = "";          // AccountId
        public string BrokerRef { get; set; } = "";         // Broker PermId
        public string OrderRefId { get; set; } = "";            // Order TxnId (from client)
        public string OrderTagRef { get; set; } = "";           // Order Tag Reference (Signal Ref, Group Ref, ..., etc.)
        public string OrderTag { get; set; } = OrderTagDef.UNDEFINED;   // Order Tag (OPEN, CLOSE, etc.)
        public TickerInfo Ticker { get; set; } = new();
        public string OrderType { get; set; } = OrderTypes.MARKET;
        public int Qty { get; set; } = 0;
        public decimal Price { get; set; } = 0;
        public decimal StopPrice { get; set; } = 0;
        public string TimeInForce { get; set; } = TimeInForceTypes.GTC;
    }
}