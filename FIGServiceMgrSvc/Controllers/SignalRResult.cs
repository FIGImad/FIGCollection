using System.Text.Json;

namespace FIGServiceMgrSvc.Controllers
{
    internal static class SignalRResult
    {
        public static Task<string?> Ok(object payload)
            => Task.FromResult<string?>(JsonSerializer.Serialize(payload));

        public static Task<string?> Fail(object error)
            => Task.FromResult<string?>(JsonSerializer.Serialize(error));
    }
}
