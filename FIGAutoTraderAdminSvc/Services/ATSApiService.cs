using FIGCommon.Models;
using FIGCommon.Models.FIGController;
using FIGCommon.Services;
using Newtonsoft.Json;

namespace FIGAutoTraderAdminSvc.Services
{
    public class ATSApiService : ApiServiceBase
    {
        private const string ReqAutoTradeExecStatus = "/api/autotrade/status/{id}";
        private const string ReqAutoTradeExecStatusList = "/api/autotrade/liststatus";
        private const string ReqAutoTradeStart = "/api/autotrade/start/{id}";
        private const string ReqAutoTradeStop = "/api/autotrade/stop/{id}";

        public ATSApiService(IHttpClientFactory httpClientFactory,
                             ILogger<ATSApiService> logger,
                             IClientSignalRService controllerClient,
                             IServiceProvider services,
                             IHttpContextAccessor httpContextAccessor,
                             IConfiguration config)
            : base(httpClientFactory.CreateClient(nameof(ATSApiService)), logger, controllerClient, services, httpContextAccessor, config, false)
        {
        }

        public async Task<AutoTradeExecStatus?> GetExecStatus(int autoTradeId, int timeout)
        {
            var req = new ControllerRouteRequest();
            req.Route = ReqAutoTradeExecStatus.Replace("{id}", autoTradeId.ToString());
            req.Method = HttpMethod.Get.Method;
            req.DestinationRole = (int) ControllerClientRole.ATS;
            return await ProcessRouteRequest<AutoTradeExecStatus>(req, timeout);
        }

        public async Task<EAutoTradeStatus> StartAutoTradeExec(int autoTradeId, int timeout)
        {
            var req = new ControllerRouteRequest();
            req.Route = ReqAutoTradeStart.Replace("{id}", autoTradeId.ToString());
            req.Method = HttpMethod.Get.Method;
            req.DestinationRole = (int)ControllerClientRole.ATS;
            return (EAutoTradeStatus)(await ProcessRouteRequest<int?>(req, timeout) ?? (int?)EAutoTradeStatus.NotAvailable);
        }
        
        public async Task<EAutoTradeStatus> StopAutoTradeExec(int autoTradeId, int timeout)
        {
            var req = new ControllerRouteRequest();
            req.Route = ReqAutoTradeStop.Replace("{id}", autoTradeId.ToString());
            req.Method = HttpMethod.Get.Method;
            req.DestinationRole = (int)ControllerClientRole.ATS;
            return (EAutoTradeStatus)(await ProcessRouteRequest<int?>(req, timeout) ?? (int?)EAutoTradeStatus.NotAvailable);
        }

        public async Task<List<AutoTradeExecStatus>?> GetExecStatusFromList(List<int> autoTradeIds, int timeout)
        {
            var req = new ControllerRouteRequest();
            req.Route = ReqAutoTradeExecStatusList;
            req.Method = HttpMethod.Post.Method;
            req.DestinationRole = (int)ControllerClientRole.ATS;
            req.Load = JsonConvert.SerializeObject(autoTradeIds);
            return await ProcessRouteRequest<List<AutoTradeExecStatus>>(req, timeout);
        }
    }
}
