//using FIGCommon.Models.FIGProviderAPI;
//using Microsoft.AspNetCore.SignalR;

//namespace FIGPriceSyncService.Services
//{
//    public class MarketDataMonitorHub : Hub
//    {
//        public MarketDataMonitorHub()
//        {
//        }

//        public override async Task OnConnectedAsync()
//        {
//            // send this after a delay to ensure the client has time to set up the listener for this event
//            await base.OnConnectedAsync();
//        }

//        public async Task SendMarketData(MarketDataDto data)
//        {
//            await Clients.Caller.SendAsync("marketdata", data);
//        }
//    }
//}
