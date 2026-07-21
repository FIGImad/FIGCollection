using FIGCommon.Models;
using FIGCommon.Models.FIGController;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Services;
using Newtonsoft.Json;

namespace FIGAutoTraderSvc.Services
{
    public class FigBrokerAPIService : ApiServiceBase
    {
        private const string ReqPlaceOrder = "/order/new";
        private const string ReqCancelOrder = "/order/cancelasync";
        private const string ReqOrderStatus = "/order/status";

        public FigBrokerAPIService(IHttpClientFactory httpClientFactory, 
                             ILogger<FigBrokerAPIService> logger,
                             IClientSignalRService controllerClient,
                             IServiceProvider services,
                             IHttpContextAccessor httpContextAccessor,
                             IConfiguration config)
           : base(httpClientFactory.CreateClient(nameof(FigBrokerAPIService)), logger, controllerClient, services, httpContextAccessor, config, true)
        {
        }

        public async Task<OrderStatusDto?> PlaceOrderAsync(OrderRequestDto orderReq, string destinationServiceId, int millisecondsTimeout, CancellationToken cancellationToken = default)
        {
            var req = new ControllerRouteRequest();
            req.Route = ReqPlaceOrder;
            req.Method = HttpMethod.Post.Method;
            req.DestinationRole = (int)ControllerClientRole.Broker;
            req.Load = JsonConvert.SerializeObject(orderReq);
            req.DestinationServiceId = destinationServiceId;
            return await ProcessRouteRequest<OrderStatusDto?>(req, millisecondsTimeout);
        }
        public async Task<OrderStatusDto?> CancelOrder(OrderCancelRequestDto cancelReq, string destinationServiceId, int millisecondsTimeout, CancellationToken cancellationToken = default)
        {
            var req = new ControllerRouteRequest();
            req.Route = ReqCancelOrder;
            req.Method = HttpMethod.Post.Method;
            req.DestinationRole = (int)ControllerClientRole.Broker;
            req.Load = JsonConvert.SerializeObject(cancelReq);
            req.DestinationServiceId = destinationServiceId;
            return await ProcessRouteRequest<OrderStatusDto?>(req, millisecondsTimeout);
        }

        public async Task<OrderStatusDto?> GetOrderStatus(OrderStatusRequestDto statusReq, string destinationServiceId)
        {
            var req = new ControllerRouteRequest();
            req.Route = ReqOrderStatus;
            req.Method = HttpMethod.Post.Method;
            req.DestinationRole = (int)ControllerClientRole.Broker;
            req.Load = JsonConvert.SerializeObject(statusReq);
            req.DestinationServiceId = destinationServiceId;
            return await ProcessRouteRequest<OrderStatusDto>(req);
        }

        public async Task<PongDto?> PingBroker(string destinationServiceId)
        {
            var req = new ControllerRouteRequest();
            req.Route = ReqOrderStatus;
            req.Method = HttpMethod.Get.Method;
            req.DestinationRole = (int)ControllerClientRole.Broker;
            req.DestinationServiceId = destinationServiceId;
            return await ProcessRouteRequest<PongDto>(req);
        }
    }
}
