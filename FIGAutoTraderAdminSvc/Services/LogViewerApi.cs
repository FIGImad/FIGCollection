using FIGCommon.Models;
using FIGCommon.Models.FIGController;
using FIGCommon.Models.LogMonitor;
using FIGCommon.Services;

namespace FIGAutoTraderAdminSvc.Services
{

    public class LogViewerApi : ApiServiceBase
    {
        private const string ReqCurrentLog = "/log/current/{startpos}/{numbytes}";
        private const string ReqArchivedLog = "/log/archive/{filename}/{startpos}/{numbytes}";
        private const string ReqListLogs = "/log/list";


        public LogViewerApi(IHttpClientFactory httpClientFactory,
                             ILogger<LogViewerApi> logger,
                             IClientSignalRService controllerClient,
                             IServiceProvider services,
                             IHttpContextAccessor httpContextAccessor,
                             IConfiguration config)
            : base(httpClientFactory.CreateClient(nameof(LogViewerApi)), logger, controllerClient, services, httpContextAccessor, config, true)
        {
        }


        public async Task<LogContentDto?> GetCurrentLogContentAsync(string serviceId, long startPos, long numBytes, int timeout)
        {
            var req = new ControllerRouteRequest();
            req.Route = ReqCurrentLog.Replace("{startpos}", startPos.ToString()).Replace("{numbytes}", numBytes.ToString());
            req.Method = HttpMethod.Get.Method;
            req.DestinationRole = (int)ControllerClientRole.None;
            req.DestinationServiceId = serviceId;
            return await ProcessRouteRequest<LogContentDto>(req, timeout);
        }

        public async Task<LogContentDto?> GetArchivedLogContentAsync(string serviceId, string filename, long startPos, long numBytes, int timeout)
        {
            var req = new ControllerRouteRequest();
            req.Route = ReqArchivedLog.Replace("{filename}", filename).Replace("{startpos}", startPos.ToString()).Replace("{numbytes}", numBytes.ToString());
            req.Method = HttpMethod.Get.Method;
            req.DestinationRole = (int)ControllerClientRole.None;
            req.DestinationServiceId = serviceId;
            return await ProcessRouteRequest<LogContentDto>(req, timeout);
        }

        public async Task<List<string>?> GetListOfLogFilesAsync(string serviceId, int timeout)
        {
            var req = new ControllerRouteRequest();
            req.Route = ReqListLogs;
            req.Method = HttpMethod.Get.Method;
            req.DestinationRole = (int)ControllerClientRole.None;
            req.DestinationServiceId = serviceId;
            return await ProcessRouteRequest<List<string>>(req, timeout);
        }
    }
}
