using FIGCommon.Models.FIGBroker;
using FIGCommon.Models.FIGController;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Services;
using Newtonsoft.Json;

namespace FIGAutoTraderAdminSvc.Services
{
    public class BrokerApiService : ApiServiceBase
    {
        private const string ReqGetBrokerAccount = "/brokeraccount/{serviceid}/{accountid}";
        private const string ReqGetBrokerAccountPos = "/brokeraccount/pos/{serviceid}/{accountid}/{tickerid}";
        private const string ReqPostBrokerAccountTicker = "/brokeraccount/ticker";
        private const string ReqDeleteBrokerAccountTicker = "/brokeraccount/ticker/{accountid}/{tickerid}";
        private const string ReqGetBrokerAccountOrder = "/brokeraccountorder/{accountid}/{fromtime}";
        private const string ReqGetTickers = "/ticker/all";
        private const string ReqGetLastTickerPrice = "/order/snapshot/{accountid}/{tickerid}";
        private const string ReqPlaceOrder = "/order/new";
        private const string ReqCancelOrder = "/order/cancel";
        

        public BrokerApiService(IHttpClientFactory httpClientFactory,
                             ILogger<BrokerApiService> logger,
                             IClientSignalRService controllerClient,
                             IServiceProvider services,
                             IHttpContextAccessor httpContextAccessor,
                             IConfiguration config)
            : base(httpClientFactory.CreateClient(nameof(BrokerApiService)), logger, controllerClient, services, httpContextAccessor, config, false)
        {
        }

        public async Task<BrokerAccountRS?> GetBrokerAccount(string accountId, string serviceId, int timeout)
        {
            serviceId = serviceId.Trim().ToUpper();
            var req = new ControllerRouteRequest();
            req.Route = ReqGetBrokerAccount.Replace("{serviceid}", serviceId).Replace("{accountid}", accountId);
            req.Method = HttpMethod.Get.Method;
            req.DestinationServiceId = serviceId;
            return await ProcessRouteRequest<BrokerAccountRS>(req, timeout);
        }

        public async Task<TickerPosDto?> GetBrokerAccountPos(string accountId, string serviceId, int tickerId, int timeout)
        {
            serviceId = serviceId.Trim().ToUpper();
            var req = new ControllerRouteRequest();
            req.Route = ReqGetBrokerAccountPos.Replace("{serviceid}", serviceId).Replace("{accountid}", accountId).Replace("{tickerid}", tickerId.ToString());
            req.Method = HttpMethod.Get.Method;
            req.DestinationServiceId = serviceId;
            return await ProcessRouteRequest<TickerPosDto>(req, timeout);
        }

        public async Task<List<TickerRS>?> GetTickers(string serviceId, int timeout)
        {
            serviceId = serviceId.Trim().ToUpper();
            var req = new ControllerRouteRequest();
            req.Route = ReqGetTickers;
            req.Method = HttpMethod.Get.Method;
            req.DestinationServiceId = serviceId;
            return await ProcessRouteRequest<List<TickerRS>>(req, timeout);
        }

        public async Task<BrokerAccountTickerRS?> UpsertAccountTicker(string serviceId, BrokerAccountTickerRS rec, int timeout)
        {
            serviceId = serviceId.Trim().ToUpper();
            var req = new ControllerRouteRequest();
            req.Route = ReqPostBrokerAccountTicker;
            req.Method = HttpMethod.Post.Method;
            req.DestinationServiceId = serviceId;
            req.Load = JsonConvert.SerializeObject(rec);
            return await ProcessRouteRequest<BrokerAccountTickerRS>(req, timeout);
        }
        public async Task<bool> DeleteAccountTicker(string serviceId, int accountId, int tickerId, int timeout)
        {
            serviceId = serviceId.Trim().ToUpper();
            var req = new ControllerRouteRequest();
            req.Route = ReqDeleteBrokerAccountTicker.Replace("{accountid}", accountId.ToString()).Replace("{tickerid}", tickerId.ToString());
            req.Method = HttpMethod.Delete.Method;
            req.DestinationServiceId = serviceId;
            return await ProcessRouteRequest<bool>(req, timeout);
        }

        public async Task<List<OrderRS>?> GetBrokerAccountOrders(string accountId, string serviceId, int fromTime, int timeout)
        {
            serviceId = serviceId.Trim().ToUpper();
            var req = new ControllerRouteRequest();
            req.Route = ReqGetBrokerAccountOrder.Replace("{serviceid}", serviceId)
                .Replace("{accountid}", accountId)
                .Replace("{fromtime}", fromTime.ToString());
            req.Method = HttpMethod.Get.Method;
            req.DestinationServiceId = serviceId;
            return await ProcessRouteRequest<List<OrderRS>>(req, timeout);
        }

        public async Task<decimal ?> GetPriceSnapshot(string serviceId, string accountId, int tickerId, int timeout)
        {
            serviceId = serviceId.Trim().ToUpper();
            var req = new ControllerRouteRequest();
            req.Route = ReqGetLastTickerPrice
                .Replace("{serviceid}", serviceId)
                .Replace("{accountid}", accountId)
                .Replace("{tickerid}", tickerId.ToString());
            req.Method = HttpMethod.Get.Method;
            req.DestinationServiceId = serviceId;
            return await ProcessRouteRequest<decimal>(req, timeout);
        }

        internal async Task<OrderStatusDto?> PlaceOrder(string serviceId, OrderRequestDto orderInfo, int timeout)
        {
            serviceId = serviceId.Trim().ToUpper();
            var req = new ControllerRouteRequest();
            req.Route = ReqPlaceOrder;
            req.Method = HttpMethod.Post.Method;
            req.DestinationServiceId = serviceId;
            req.Load = JsonConvert.SerializeObject(orderInfo);
            return await ProcessRouteRequest<OrderStatusDto>(req, timeout);
        }

        public async Task<OrderStatusDto?> CancelOrder(string serviceId, string accountId, string brokerRef, int timeout)
        {
            serviceId = serviceId.Trim().ToUpper();
            var req = new ControllerRouteRequest();
            req.Route = ReqCancelOrder;
            req.Method = HttpMethod.Post.Method;
            req.DestinationServiceId = serviceId;
            OrderCancelRequestDto cancelReq = new OrderCancelRequestDto()
            {
                AccountId = accountId,
                BrokerRef = brokerRef
            };
            req.Load = JsonConvert.SerializeObject(cancelReq);
            return await ProcessRouteRequest<OrderStatusDto>(req, timeout);
        }
    }
}
