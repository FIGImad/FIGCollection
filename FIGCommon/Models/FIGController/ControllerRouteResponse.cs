namespace FIGCommon.Models.FIGController
{
    /// <summary>
    /// Explicit transport envelope for a request routed through FIGControllerSvc.
    /// The response body is never inspected to determine whether routing succeeded.
    /// </summary>
    public sealed class ControllerRouteResponse
    {
        public string RequestId { get; set; } = string.Empty;
        public bool TransportSucceeded { get; set; }
        public int HttpStatusCode { get; set; }
        public string Body { get; set; } = string.Empty;
        // Avoid the name "Error": it is reserved by the SignalR JSON protocol
        // for invocation completion messages.
        public string? ErrorMessage { get; set; }

        public static ControllerRouteResponse Success(
            string requestId,
            string? body,
            int httpStatusCode = StatusCodes.Status200OK)
        {
            return new ControllerRouteResponse
            {
                RequestId = requestId,
                TransportSucceeded = true,
                HttpStatusCode = httpStatusCode,
                Body = body ?? string.Empty
            };
        }

        public static ControllerRouteResponse Failure(
            string requestId,
            int httpStatusCode,
            string? error,
            string? body = null)
        {
            return new ControllerRouteResponse
            {
                RequestId = requestId,
                TransportSucceeded = false,
                HttpStatusCode = httpStatusCode,
                Body = body ?? string.Empty,
                ErrorMessage = error
            };
        }
    }
}
