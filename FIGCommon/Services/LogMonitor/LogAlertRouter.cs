using FIGCommon.Models;
using FIGCommon.Models.FIGController;
using FIGCommon.Models.LogMonitor;
using FIGCommon.Utilities;
using Newtonsoft.Json;
using System.Text.Json;

namespace FIGCommon.Services.LogMonitor
{
    /// <summary>
    /// Drains <see cref="LogAnalyzerSink.Queue"/> in the background and forwards
    /// each alert to the host controller via <see cref="IClientSignalRService"/>.
    ///
    /// The SignalR call uses a fire-and-log pattern: failures are logged locally
    /// and the alert is discarded rather than retried, keeping the router non-blocking.
    /// </summary>
    public sealed class LogAlertRouter : BackgroundService
    {
        private readonly ILogger<LogAlertRouter> _logger;
        private readonly LogAnalyzerSink _sink;
        private readonly IClientSignalRService? _signalR;
        private readonly ControllerConfig _controllerConfig;

        private readonly SemaphoreSlim _loginGate = new(1, 1);
        private readonly object _tokenLock = new();
        private string? _token = null;
        private volatile bool _loginConfigInvalid = false;

        private const string reqReportLogRec = "/api/alert/logrec";

        public LogAlertRouter(
            ILogger<LogAlertRouter> logger,
            LogAnalyzerSink sink,
            IConfiguration config,
            IClientSignalRService? signalR = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _signalR = signalR;

            _controllerConfig = new ControllerConfig();
            config.GetSection("ControllerConfig").Bind(_controllerConfig);
            _controllerConfig.Validate();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_signalR == null)
            {
                _logger.LogInformation(
                    "LogAlertRouter is disabled because no SignalR client is registered.");
                return;
            }

            _logger.LogInformation("LogAlertRouter started. Listening for in-process log alerts.");

            // BlockingCollection.GetConsumingEnumerable blocks the thread, so we
            // offload the iteration to a background thread and await its completion.
            await Task.Run(() => DrainLoop(stoppingToken), stoppingToken);
        }

        private void DrainLoop(CancellationToken ct)
        {
            try
            {
                foreach (var alert in _sink.Queue.GetConsumingEnumerable(ct))
                {
                    // Fire-and-forget with a synchronous wait so we don't pile up threads.
                    ReportAlertAsync(alert, ct).GetAwaiter().GetResult();
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown – swallow.
            }
        }

        private async Task ReportAlertAsync(LogAlertMessage alert, CancellationToken ct)
        {
            // Any error raised by authentication or SignalR forwarding must not
            // become another alert, otherwise one outage creates a feedback loop.
            using var suppression = LogAnalyzerSink.SuppressForwarding();

            try
            {
                if (_signalR == null)
                    return;

                await _signalR.WaitForConnectionAsync(ct);

                string token = await GetTokenAsync(ct);
                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning(
                        "Log alert was not forwarded because an authentication token is unavailable.");
                    return;
                }

                var req = new ControllerRouteRequest
                {
                    Method = HttpMethod.Post.Method,
                    Route = reqReportLogRec,
                    DestinationRole = (int)ControllerClientRole.Alert,
                    Load = JsonConvert.SerializeObject(alert),
                    Token = token
                };
                await _signalR.SendToHostAsync(req, ct);

                _logger.LogDebug("Log alert reported to host. LogTime={LogTime}, Level={Level}",
                    DateTimeOffset.UnixEpoch.AddSeconds(alert.RawTime).ToLocalTime(), alert.Level);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Normal service shutdown.
            }
            catch (Exception ex)
            {
                // Don't re-queue — just log locally so the router stays healthy.
                _logger.LogWarning(ex,
                    "Failed to report log alert to host. LogTime={LogTime}, Level={Level}",
                    DateTimeOffset.UnixEpoch.AddSeconds(alert.RawTime).ToLocalTime(), alert.Level);
            }
        }

        private string Token
        {
            get { lock (_tokenLock) { return _token ?? string.Empty; } }
            set { lock (_tokenLock) { _token = value; } }
        }

        private async Task<string> GetTokenAsync(CancellationToken ct)
        {
            if (!string.IsNullOrEmpty(Token))
                return Token;

            if (_loginConfigInvalid)
                return string.Empty;

            await _loginGate.WaitAsync(ct);
            try
            {
                if (!string.IsNullOrEmpty(Token))
                    return Token;

                if (_loginConfigInvalid)
                    return string.Empty;

                string usr = _controllerConfig.AuthUsername;
                string pass = _controllerConfig.AuthPasswordFlat;
                string authBase = _controllerConfig.AuthURL;
                string loginRoute = _controllerConfig.ReqLogin;
                string thumbPrint = _controllerConfig.CertThumbPrint;

                if (string.IsNullOrEmpty(usr) || string.IsNullOrEmpty(pass) ||
                    string.IsNullOrEmpty(authBase) || string.IsNullOrEmpty(loginRoute))
                {
                    _loginConfigInvalid = true;
                    _logger.LogError("LogAlertRouter: invalid login configuration; token will not be acquired.");
                    return string.Empty;
                }

                string url = $"{authBase}{loginRoute}";
                LoginRequestDto loginReq = new() { Username = usr, Password = pass };
                string resp = await HttpUtils.PostJsonAsync<LoginRequestDto>(url, loginReq, string.Empty, thumbPrint, cancellationToken: ct);
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                LoginResponseDto? loginResp = System.Text.Json.JsonSerializer.Deserialize<LoginResponseDto>(resp, options);
                Token = loginResp?.AccessToken ?? string.Empty;
                return Token;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LogAlertRouter: failed to acquire token.");
                return string.Empty;
            }
            finally
            {
                _loginGate.Release();
            }
        }
    }
}
