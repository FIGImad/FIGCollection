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
    public enum ControllerClientRole { Alert = 256 }
    public class ControllerConfig
    {
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
    public interface IClientSignalRService
    {
        Task WaitForConnectionAsync(CancellationToken ct);
        Task<string> SendToHostAsync(Models.FIGController.ControllerRouteRequest request, CancellationToken ct);
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
