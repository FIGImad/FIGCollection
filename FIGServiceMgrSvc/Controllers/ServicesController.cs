using FIGCommon.Models;
using FIGServiceMgrSvc.Services;
using System.Runtime.Versioning;

namespace FIGServiceMgrSvc.Controllers
{
    /// <summary>
    /// Handles all routes under /services:
    ///   GET  /services                    – status of all managed services
    ///   GET  /services/{name}             – status of one service
    ///   GET  /services/{name}/status      – status of one service (alias)
    ///   POST /services/{name}/start       – start a service
    ///   POST /services/{name}/stop        – stop a service
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal sealed class ServicesController : ISignalRController
    {
        private readonly ManagedServiceController _svcController;

        public ServicesController(ManagedServiceController svcController)
        {
            _svcController = svcController ?? throw new ArgumentNullException(nameof(svcController));
        }

        public bool CanHandle(string method, string[] segments)
            => segments.Length >= 1 && Is("services", segments[0]);

        public Task<string?> HandleAsync(string method, string[] segments)
        {
            // GET /services
            if (segments.Length == 1 && method == HttpMethod.Get.Method)
            {
                var statuses = _svcController.GetAllStatuses();
                return SignalRResult.Ok(new { statuses });
            }

            if (segments.Length >= 2)
            {
                string serviceName = segments[1];

                // POST /services/{name}/start
                if (segments.Length == 3 && Is("start", segments[2]) && method == HttpMethod.Post.Method)
                {
                    _svcController.Start(serviceName);
                    return SignalRResult.Ok(new ServiceStatusDto
                    {
                        ServiceName = serviceName,
                        Status = _svcController.GetStatusString(serviceName)
                    });
                }

                // POST /services/{name}/stop
                if (segments.Length == 3 && Is("stop", segments[2]) && method == HttpMethod.Post.Method)
                {
                    _svcController.Stop(serviceName);
                    return SignalRResult.Ok(new ServiceStatusDto
                    {
                        ServiceName = serviceName,
                        Status = _svcController.GetStatusString(serviceName)
                    });
                }

                // GET /services/{name}  or  GET /services/{name}/status
                if (method == HttpMethod.Get.Method &&
                    (segments.Length == 2 || (segments.Length == 3 && Is("status", segments[2]))))
                {
                    return SignalRResult.Ok(new ServiceStatusDto
                    {
                        ServiceName = serviceName,
                        Status = _svcController.GetStatusString(serviceName)
                    });
                }
            }

            return SignalRResult.Fail(new { Error = $"Unhandled services route: {string.Join("/", segments)}" });
        }

        private static bool Is(string expected, string segment)
            => segment.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }
}
