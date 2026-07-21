using FIGCommon.Models;
using FIGCommon.Models.FIGController;
using FIGCommon.Services;
using Microsoft.AspNetCore.Http;

namespace FIGAlertSvc.Services
{
    public class ControllerApiService : ApiServiceBase
    {
        //private const string ReqAutoTradeExecStatus = "/api/autotrade/status/{id}";
        private const string ReqDeviceList = "/api/device/list";
        private const string ReqAdminPing = "/net/sping";

        public ControllerApiService(IHttpClientFactory httpClientFactory,
                             ILogger<ControllerApiService> logger,
                             IClientSignalRService controllerClient,
                             IServiceProvider services,
                             IHttpContextAccessor httpContextAccessor,
                             IConfiguration config)
            : base(httpClientFactory.CreateClient(nameof(ControllerApiService)), logger, controllerClient, services, httpContextAccessor, config, true)
        {
        }

        public async Task<List<DeviceRS>?> GetDeviceList(int timeout = 10000)
        {
            var req = new ControllerRouteRequest();
            //req.Route = ReqAutoTradeExecStatus.Replace("{id}", autoTradeId.ToString());
            req.Route = ReqDeviceList;
            req.Method = HttpMethod.Get.Method;
            req.DestinationRole = (int)ControllerClientRole.Bus;
            return await ProcessRouteRequest<List<DeviceRS>>(req, timeout);
        }

        public async Task<PongDto?> Ping(string serviceId, int timeout)
        {
            var req = new ControllerRouteRequest();
            req.Route = ReqAdminPing;
            req.Method = HttpMethod.Get.Method;
            req.DestinationServiceId = serviceId;
            return await ProcessRouteRequest<PongDto>(req, timeout);
        }

    }
}
