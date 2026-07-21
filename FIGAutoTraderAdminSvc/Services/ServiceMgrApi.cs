using FIGCommon.Models;
using FIGCommon.Models.FIGController;
using FIGCommon.Services;

namespace FIGAutoTraderAdminSvc.Services
{

    public class ServiceMgrApi : ApiServiceBase
    {
        private const string ReqServiceStart = "/services/{servicename}/start";
        private const string ReqServiceStop = "/services/{servicename}/stop";
        private const string ReqServiceStatus = "/services/{servicename}/status";

        public ServiceMgrApi(IHttpClientFactory httpClientFactory,
                             ILogger<ServiceMgrApi> logger,
                             IClientSignalRService controllerClient,
                             IServiceProvider services,
                             IHttpContextAccessor httpContextAccessor,
                             IConfiguration config)
            : base(httpClientFactory.CreateClient(nameof(ServiceMgrApi)), logger, controllerClient, services, httpContextAccessor, config, true)
        {
        }


        public async Task<ServiceStatusDto?> GetStatusAsync(string serviceId, string serviceName, int timeout)
        {
            var req = new ControllerRouteRequest();
            req.Route = ReqServiceStatus.Replace("{servicename}", serviceName);
            req.Method = HttpMethod.Get.Method;
            req.DestinationRole = (int)ControllerClientRole.None;
            req.DestinationServiceId = serviceId;
            return await ProcessRouteRequest<ServiceStatusDto?>(req, timeout);
        }

        public async Task<ServiceStatusDto?> StartAsync(string serviceId, string serviceName, int timeout)
        {
            var req = new ControllerRouteRequest();
            req.Route = ReqServiceStart.Replace("{servicename}", serviceName);
            req.Method = HttpMethod.Post.Method;
            req.DestinationRole = (int)ControllerClientRole.None;
            req.DestinationServiceId = serviceId;
            return await ProcessRouteRequest<ServiceStatusDto?>(req, timeout);
        }
        public async Task<ServiceStatusDto?> StopAsync(string serviceId, string serviceName, int timeout)
        {
            var req = new ControllerRouteRequest();
            req.Route = ReqServiceStop.Replace("{servicename}", serviceName);
            req.Method = HttpMethod.Post.Method;
            req.DestinationRole = (int)ControllerClientRole.None;
            req.DestinationServiceId = serviceId;
            return await ProcessRouteRequest<ServiceStatusDto?>(req, timeout);
        }

    }
}
