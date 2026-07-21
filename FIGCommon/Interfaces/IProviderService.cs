using FIGCommon.Models.FIGProviderAPI;

namespace FIGCommon.Interfaces
{
    public interface IProviderService
    {
        string Id { get; }
        string ClientId { get; }

        bool IsConnected { get; }
        public ProviderOptions Options { get; }

        void CreateConnection(bool forceWait = false);
        void Disconnect();

        List<BarData>? HistoricalDataRequest(HistoricalDataRequest request, int timeoutInMsec = 60000);
        ContractInfo? ContractInfo(InstrumentLookupRequest contr, int timeout = 30000);
        OrderStatusDto PlaceOrder(BrokerOrderRequestDto orderReq, int timeout = 30000);
        List<ContractInfo>? SymbolLookup(string symbol, int timeout = 30000);
        OrderStatusDto? TrackOrder(string orderId, int timeoutInMsec = int.MaxValue);
        OrderStatusDto? TrackBrokerRef(string brokerRef, int timeoutInMsec = int.MaxValue);

        void QueryOpenOrders();
        void QueryAllCompletedOrders();
        List<OrderStatusDto>? QueryAllOpenOrders(int timeoutInMsec = 5000);

        CancelStatusDto? CancelOrder(string orderId, string brokerRef, int timeoutInMsec = 30000);

        public int SubscribeMarketData(TickerInfo ticker);

        public void SubscribePositions();

        public decimal? TickerSnapshot(TickerInfo ticker, int timeoutInMsec = 5000);

    }
}