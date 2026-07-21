using FIGCommon.Models.FIGController;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Services;
using Newtonsoft.Json;

namespace FIGBrokerSvc.Services
{
    public class ATSApi : ApiServiceBase
    {
        private const string ReqOrderStatusUpdate = "/api/autotrade/orderstatus";
        private readonly ControllerConfig _controllerConfig;

        public ATSApi(IHttpClientFactory httpClientFactory,
                             ILogger<ATSApi> logger,
                             IServiceProvider services,
                             IHttpContextAccessor httpContextAccessor,
                             IConfiguration config,
                             IClientSignalRService brokerAgent)
            : base(httpClientFactory.CreateClient(nameof(ATSApi)), logger, brokerAgent, services, httpContextAccessor, config, true)
        {
            // change configuration for ATS API if needed
            _controllerConfig = new ControllerConfig();
            _config.GetSection("ControllerConfig").Bind(_controllerConfig);
            if (_controllerConfig == null) throw new ArgumentNullException(nameof(_controllerConfig));

            _controllerConfig.Validate();
        }

        public async Task<bool?> SendStatusUpdate(OrderStatusDto orderStatus)
        {
            var req = new ControllerRouteRequest();
            req.Route = ReqOrderStatusUpdate;
            req.Method = HttpMethod.Post.Method;
            req.DestinationRole = (int)ControllerClientRole.ATS;
            req.Load = JsonConvert.SerializeObject(orderStatus);
            return await ProcessRouteRequest<bool>(req, 5000);
        }
    }
}
