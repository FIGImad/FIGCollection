using FIGCommon.Models.FIGController;
using FIGCommon.Services;
using FIGServiceMgrSvc.Controllers;
using System.Runtime.Versioning;

namespace FIGServiceMgrSvc.Services
{
    /// <summary>
    /// Derives from <see cref="ClientSignalRService"/> and overrides
    /// <see cref="SendToLocalController"/> to dispatch inbound requests through
    /// <see cref="SignalRDispatcher"/> to the appropriate controller.
    ///
    /// All connection resilience, JWT auth, re-registration logic
    /// is inherited unchanged from the base class.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ServiceManagerSignalRService : ClientSignalRService
    {
        private readonly SignalRDispatcher _dispatcher;

        public ServiceManagerSignalRService(
            ILogger<ServiceManagerSignalRService> logger,
            IConfiguration config,
            SignalRDispatcher dispatcher,
            IHostApplicationLifetime appLifetime)
            : base(logger, config, appLifetime)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        /// <summary>
        /// Forwards the inbound <see cref="ControllerRouteRequest"/> to the
        /// <see cref="SignalRDispatcher"/> which routes it to the correct controller.
        /// </summary>
        public override Task<ControllerRouteResponse> SendToLocalController(ControllerRouteRequest req)
            => _dispatcher.DispatchAsync(req);
    }
}
