namespace FIGCommon.Models.FIGProviderAPI
{
    public class BrokerOrderRequestDto
    {
        public string AccountId { get; set; } = "";          // AccountId
        public string TxnId { get; set; } = "";            // Reference transaction Id (i.e. Order.Id)
        public string RefClientId { get; set; } = "";            // Reference client Id (i.e. OrderRefId)
        public TickerInfo Ticker { get; set; } = new();
        public string OrderType { get; set; } = OrderTypes.MARKET;
        public int Qty { get; set; } = 0;
        public decimal Price { get; set; } = 0;
        public decimal StopPrice { get; set; } = 0;
        public string TimeInForce { get; set; } = TimeInForceTypes.GTC;
    }
}