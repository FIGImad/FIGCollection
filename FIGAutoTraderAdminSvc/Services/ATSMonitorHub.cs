using Microsoft.AspNetCore.SignalR;

namespace FIGAutoTraderAdminSvc.Services
{
    public class ATSMonitorHub : Hub
    {
        private readonly IConnectionStatusStore _store;

        public ATSMonitorHub(IConnectionStatusStore store)
        {
            _store = store;
        }

        public override async Task OnConnectedAsync()
        {
            // send this after a delay to ensure the client has time to set up the listener for this event
            await base.OnConnectedAsync();
            var status = _store.GetAll();
            await Clients.Caller.SendAsync("ConnectionStatusChanged", status);
        }

    }
}
