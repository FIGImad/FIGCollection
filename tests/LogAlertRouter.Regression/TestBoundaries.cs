// Offline boundaries for exercising the production router without credentials or services.
namespace Newtonsoft.Json
{
    public static class JsonConvert
    {
        public static string SerializeObject(object value) => System.Text.Json.JsonSerializer.Serialize(value);
    }
}
namespace FIGCommon.Models
{
    public class LoginRequestDto { public string Username { get; set; } = ""; public string Password { get; set; } = ""; }
    public class LoginResponseDto { public string AccessToken { get; set; } = ""; }
}
namespace FIGCommon.Models.FIGController
{
    public enum ControllerClientRole { Alert = 256, SystemAlert = 512 }
    public class ControllerConfig
    {
        public string Id { get; set; } = "monitor-test";
        public string Name { get; set; } = "Monitor test";
        public string AuthUsername { get; set; } = "test";
        public string AuthPasswordFlat => "test";
        public string AuthURL => "https://offline.invalid";
        public string ReqLogin => "/login";
        public string CertThumbPrint => "";
        public void Validate() { }
    }
    public class ControllerRouteRequest
    {
        public string Method { get; set; } = "";
        public string Route { get; set; } = "";
        public int DestinationRole { get; set; }
        public string Load { get; set; } = "";
        public string Token { get; set; } = "";
    }
}
namespace FIGCommon.Utilities
{
    public static class ErrorCodes
    {
        public const int AuthError_Authentication = 401;
        public static string ToText(int code) => code.ToString();
    }
    public static class HttpUtils
    {
        public static int Logins;
        public static bool FailLogin;
        public static Task<string> PostJsonAsync<T>(string url, T request, string token, string thumbPrint, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Logins++;
            if (FailLogin) throw new HttpRequestException("Auth service unavailable");
            return Task.FromResult($"{{\"accessToken\":\"token-{Logins}\"}}");
        }
    }
}
namespace FIGCommon.Services
{
    public class EventMonitorService
    {
        public Action<Models.EventRS?>? Callback;
        public void Subscribe(string name, Action<Models.EventRS?> callback) => Callback = callback;
        public void Unsubscribe(string name, Action<Models.EventRS?> callback) => Callback = null;
    }
    public interface IClientSignalRService
    {
        Task WaitForConnectionAsync(CancellationToken ct);
        Task<string> SendToHostAsync(Models.FIGController.ControllerRouteRequest request, CancellationToken ct);
    }
}

namespace FIGCommon.Models
{
    public class EventRS { public string EventType { get; set; } = "SYSTEMALERT"; }
    public class SystemAlertRS
    {
        public int Id { get; set; }
        public long RawTime { get; set; }
        public int MSec { get; set; }
        public string Source { get; set; } = "test";
        public string Message { get; set; } = "test";
    }
}
namespace FIGCommon.DataAccess
{
    public static class MainRepo
    {
        public static readonly System.Collections.Concurrent.ConcurrentDictionary<int, Models.SystemAlertRS> Pending = new();
        public static int Reads;
        public static bool FailAcknowledgement;
        public static List<Models.SystemAlertRS> GetPendingSystemAlerts(int maxRec)
        {
            Interlocked.Increment(ref Reads);
            return Pending.Values.OrderBy(r => r.Id).Take(maxRec).ToList();
        }
        public static void MarkSystemAlertDelivered(int id)
        {
            if (FailAcknowledgement) throw new Exception("Source database unavailable");
            Pending.TryRemove(id, out _);
        }
    }
}
namespace FIGCommon.Services.LogMonitor
{
    public class LogAnalyzerSink
    {
        public System.Collections.Concurrent.BlockingCollection<Models.LogMonitor.LogAlertMessage> Queue { get; } = new();
        public static int SuppressionDepth;
        public static IDisposable SuppressForwarding() { SuppressionDepth++; return new Scope(); }
        private sealed class Scope : IDisposable { public void Dispose() => SuppressionDepth--; }
    }
}
