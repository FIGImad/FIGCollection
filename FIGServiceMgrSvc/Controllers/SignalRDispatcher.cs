using FIGCommon.Models;
using FIGCommon.Models.FIGController;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace FIGServiceMgrSvc.Controllers
{
    /// <summary>
    /// Routes an inbound <see cref="ControllerRouteRequest"/> to the appropriate
    /// <see cref="ISignalRController"/> and returns its JSON response.
    /// </summary>
    public sealed class SignalRDispatcher
    {
        private readonly IReadOnlyList<ISignalRController> _controllers;
        private readonly ILogger<SignalRDispatcher> _logger;

        public SignalRDispatcher(
            ILogger<SignalRDispatcher> logger,
            IEnumerable<ISignalRController> controllers)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _controllers = [.. controllers ?? throw new ArgumentNullException(nameof(controllers))];
        }

        public async Task<ControllerRouteResponse> DispatchAsync(ControllerRouteRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Route))
            {
                string? body = await SignalRResult.Fail("Route cannot be empty.");
                return ControllerRouteResponse.Failure(
                    req.RequestId,
                    StatusCodes.Status400BadRequest,
                    "Route cannot be empty.",
                    body);
            }

            var segments = req.Route
                .Trim('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            var method = req.Method.ToUpperInvariant();

            try
            {
                foreach (var controller in _controllers)
                {
                    if (controller.CanHandle(method, segments))
                    {
                        string? handlerBody = await controller.HandleAsync(method, segments);
                        return ControllerRouteResponse.Success(req.RequestId, handlerBody);
                    }
                }

                _logger.LogWarning(
                    "Unrecognised route. RequestId={RequestId}, Method={Method}, Route={Route}",
                    req.RequestId, req.Method, req.Route);

                string error = $"Unknown route: [{req.Method}] {req.Route}";
                string? errorBody = await SignalRResult.Fail(new { Error = error });
                return ControllerRouteResponse.Failure(
                    req.RequestId,
                    StatusCodes.Status404NotFound,
                    error,
                    errorBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Route handler failed. RequestId={RequestId}, Method={Method}, Route={Route}",
                    req.RequestId, req.Method, req.Route);

                string? exceptionBody = await SignalRResult.Fail(new { Error = ex.Message });
                return ControllerRouteResponse.Failure(
                    req.RequestId,
                    StatusCodes.Status500InternalServerError,
                    ex.Message,
                    exceptionBody);
            }
        }
    }
}
