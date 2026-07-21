using FIGCommon.Models.FIGController;
using FIGCommon.Services;

namespace FIGAutoTraderAdminSvc.Services
{
    public class PriceSyncApiService : ApiServiceBase
    {
        private const string reqMarketDataSubscription = "/api/marketdata/{tickerId}";

        public PriceSyncApiService(IHttpClientFactory httpClientFactory,
                             ILogger<PriceSyncApiService> logger,
                             IClientSignalRService controllerClient,
                             IServiceProvider services,
                             IHttpContextAccessor httpContextAccessor,
                             IConfiguration config)
            : base(httpClientFactory.CreateClient(nameof(PriceSyncApiService)), logger, controllerClient, services, httpContextAccessor, config, false)
        {
        }

        public async Task<bool?> MarketDataSubscription(int tickerId)
        {
            var req = new ControllerRouteRequest();
            req.Route = reqMarketDataSubscription.Replace("{tickerId}", tickerId.ToString());
            req.Method = HttpMethod.Get.Method;
            req.DestinationRole = (int)ControllerClientRole.PriceSync;
            return await ProcessRouteRequest<bool>(req);
        }
    }
}
