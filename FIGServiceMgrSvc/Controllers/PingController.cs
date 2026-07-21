using FIGCommon.Models;

namespace FIGServiceMgrSvc.Controllers
{
    /// <summary>
    /// Handles: GET /net/sping
    /// </summary>
    internal sealed class PingController : ISignalRController
    {
        public bool CanHandle(string method, string[] segments)
            => method == HttpMethod.Get.Method
            && segments.Length == 2
            && Is("net", segments[0])
            && Is("sping", segments[1]);

        public Task<string?> HandleAsync(string method, string[] segments)
            => SignalRResult.Ok(new PongDto());

        private static bool Is(string expected, string segment)
            => segment.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }
}
