using FIGCommon.Models.FIGController;
using FIGCommon.Services;

namespace FIGSignalExSvc.Publication;

// Remote publication does not grant the main environment access to private APIs or logs.
public sealed class PrivateSignalControllerClient(ILogger<ClientSignalRService> logger,
    IConfiguration config, IHostApplicationLifetime lifetime) : ClientSignalRService(logger, config, lifetime)
{
    public static bool IsHealthRequest(string method, string route) => method == "GET"
        && (route.Equals("/net/ping", StringComparison.OrdinalIgnoreCase)
            || route.Equals("/net/sping", StringComparison.OrdinalIgnoreCase));
    public override Task<ControllerRouteResponse> SendToLocalController(ControllerRouteRequest req)
        => IsHealthRequest(req.Method, req.Route) ? base.SendToLocalController(req)
            : Task.FromResult(ControllerRouteResponse.Failure(req.RequestId, 403,
                "Remote signal installation accepts health requests only."));
}
