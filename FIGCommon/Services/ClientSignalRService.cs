using FIGCommon.Models.FIGController;
using FIGCommon.Models.Identity;
using FIGCommon.Utilities;
using FIGCommon.Exceptions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Headers;
using System.Text;

namespace FIGCommon.Services
{
    public interface IClientSignalRService 
    {
        Task<string?> SendToHostAsync(ControllerRouteRequest req, CancellationToken ct = default);
        Task<string?> SendToHostAsync(ControllerRouteRequest req, int timeoutMs);
        Task<T?> SendToHostAsync<T>(ControllerRouteRequest req, CancellationToken ct = default);
        Task<T?> SendToHostAsync<T>(ControllerRouteRequest req, int timeoutMs);

        Task<ControllerRouteResponse> SendToLocalController(ControllerRouteRequest req);

        /// <summary>
        /// Completes when the SignalR connection is established for the first time.
        /// Cancelled if <paramref name="ct"/> is triggered before connection is ready.
        /// </summary>
        Task WaitForConnectionAsync(CancellationToken ct = default);
    }

    public class ClientSignalRService : BackgroundService, IClientSignalRService
    {
        protected readonly ILogger<ClientSignalRService> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private HubConnection? _connection;
        private readonly HttpClient? _localHttpClient = null;
        private readonly IConfiguration _config;
        private readonly ControllerConfig _controllerConfig;

        private string _hubUrl = "";
        private readonly SemaphoreSlim _tokenLock = new(1, 1);
        private string _accessToken = "";
        private DateTime _accessTokenExpiresUtc = DateTime.MinValue;

        // Completed once the first successful hub connection is established.
        private readonly TaskCompletionSource _connectedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ClientSignalRService(ILogger<ClientSignalRService> logger,
             IConfiguration config,
             IHostApplicationLifetime appLifetime)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime)); 
            _controllerConfig = new ControllerConfig();
            _config.GetSection("ControllerConfig").Bind(_controllerConfig);
            if (_controllerConfig == null) throw new ArgumentNullException(nameof(_controllerConfig));
            
            _controllerConfig.Validate();

            _hubUrl = _controllerConfig.HostAddress ?? "";
            var address = _config["Kestrel:Endpoints:Https:Url"] ?? _config["Kestrel:Endpoints:Http:Url"];
            if (!string.IsNullOrWhiteSpace(address))
            {
                address = address
                    .Replace("://*:", "://localhost:")
                    .Replace("://+:", "://localhost:");

                if (!Uri.TryCreate(address, UriKind.Absolute, out Uri? baseAddress) ||
                    (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps))
                {
                    throw new InvalidOperationException(
                        $"The configured local Kestrel endpoint '{address}' is not a valid HTTP or HTTPS URL.");
                }

                _localHttpClient = new HttpClient
                {
                    BaseAddress = baseAddress
                };
            }
            else
            {
                // Worker services such as FIGServiceMgrSvc override
                // SendToLocalController and do not expose a local HTTP endpoint.
                _logger.LogDebug(
                    "No local Kestrel endpoint is configured. Local HTTP forwarding is disabled.");
            }
        }

        protected bool IsConnected
        {
            get
            {
                return _connection?.State == HubConnectionState.Connected;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // In FIGAutoTraderAdminSvc the authentication API is hosted by this
            // same ASP.NET Core process, so wait until Kestrel is accepting requests.
            var started = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            using var registration = _appLifetime.ApplicationStarted.Register(
                () => started.TrySetResult(true));

            if (!_appLifetime.ApplicationStarted.IsCancellationRequested)
            {
                await started.Task.WaitAsync(stoppingToken);
            }

            string url =
                $"{_hubUrl}?serviceId={Uri.EscapeDataString(_controllerConfig.Id)}" +
                $"&role={_controllerConfig.Role}" +
                $"&name={Uri.EscapeDataString(_controllerConfig.Name)}";
            try
            {
                _logger.LogInformation("Connecting to controller hub at {Url}", url);
                _connection = new HubConnectionBuilder()
                            .WithUrl(url, options =>
                            {
                                options.AccessTokenProvider = async () =>
                                {
                                    return await GetControllerJwtTokenAsync();
                                };
                                // Give SignalR a fresh handler each time it asks for one so it
                                // can safely dispose it without poisoning the shared cached handler
                                // used by the token-fetch HttpClient and other callers.
                                options.HttpMessageHandlerFactory = _ =>
                                    HttpUtils.CreateFreshHttpHandlerWithCert(_controllerConfig.CertThumbPrint);
                            })
                            .WithAutomaticReconnect(new[]
                            {
                                TimeSpan.Zero,
                                TimeSpan.FromSeconds(2),
                                TimeSpan.FromSeconds(5),
                                TimeSpan.FromSeconds(10),
                                TimeSpan.FromSeconds(30),
                                TimeSpan.FromSeconds(60)
                            })
                            .Build();
                _connection.Reconnected += async connectionId =>
                {
                    try
                    {
                        _logger.LogInformation(
                            "SignalR reconnected. ConnectionId={ConnectionId}. Re-registering service {ServiceId}.",
                            connectionId,
                            _controllerConfig.Id);
                        bool registered = await RegisterServiceToHostAsync();

                        _logger.LogInformation(
                            "Service re-registration completed. ServiceId={ServiceId}, Registered={Registered}",
                            _controllerConfig.Id,
                            registered);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Failed to re-register service after SignalR reconnect. ServiceId={ServiceId}",
                            _controllerConfig.Id);
                    }
                };
                _connection.Closed += async error =>
                {
                    _logger.LogWarning(
                        error,
                        "SignalR connection closed. Restarting connection loop. ServiceId={ServiceId}",
                        _controllerConfig.Id);

                    // Invalidate the cached JWT so the reconnect loop acquires a fresh token.
                    // The old token was issued to the previous host session and will be rejected.
                    await _tokenLock.WaitAsync();
                    try { _accessToken = ""; _accessTokenExpiresUtc = DateTime.MinValue; }
                    finally { _tokenLock.Release(); }

                    // Use stoppingToken so the retry loop doesn't outlive the service lifetime.
                    await StartConnectionLoopAsync(stoppingToken);
                };
                _connection.Reconnecting += error =>
                {
                    _logger.LogWarning(
                        error,
                        "SignalR reconnecting. ServiceId={ServiceId}",
                        _controllerConfig.Id);

                    return Task.CompletedTask;
                };

                RegisterHandlers(_connection);

                await StartConnectionLoopAsync(stoppingToken);

                // Keep ExecuteAsync alive for the lifetime of the service.
                // The connection is maintained by WithAutomaticReconnect on the HubConnection.
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error in SignalR connection loop.");
            }
            finally
            {
                if (_connection != null)
                {
                    await _connection.DisposeAsync();
                }
            }
        }

        private void RegisterHandlers(HubConnection connection)
        {
            connection.On<ControllerRouteRequest, ControllerRouteResponse>("RouteRequest", async req =>
            {
                try
                {
                    //_logger.LogInformation(
                    //    "Received routed request. RequestId={RequestId}, Method={Method}, Route={Route}",
                    //    req.RequestId,
                    //    req.Method,
                    //    req.Route);

                    return await SendToLocalController(req);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed local route request. RequestId={RequestId}, Route={Route}",
                        req.RequestId,
                        req.Route);

                    return ControllerRouteResponse.Failure(
                        req.RequestId,
                        HttpStatusCodeFromException(ex),
                        ex.Message);
                }
            });
        }

        private static int HttpStatusCodeFromException(Exception exception)
        {
            return exception is AppErrorException appError
                ? appError.errorCode switch
                {
                    ErrorCodes.AuthError_Authentication => StatusCodes.Status401Unauthorized,
                    ErrorCodes.AuthError_General => StatusCodes.Status401Unauthorized,
                    ErrorCodes.AuthError_Login => StatusCodes.Status401Unauthorized,
                    ErrorCodes.AuthError_InvalidCred => StatusCodes.Status401Unauthorized,
                    ErrorCodes.AuthError_InvalidUser => StatusCodes.Status401Unauthorized,
                    ErrorCodes.AuthError_EmailConfirm => StatusCodes.Status403Forbidden,
                    ErrorCodes.AuthError_Unauthorized => StatusCodes.Status403Forbidden,
                    ErrorCodes.ClientError_NotFound => StatusCodes.Status404NotFound,
                    ErrorCodes.SysError_BadGateway => StatusCodes.Status502BadGateway,
                    ErrorCodes.SysError_ServiceUnavailable => StatusCodes.Status503ServiceUnavailable,
                    ErrorCodes.SysError_GatewayTimeout => StatusCodes.Status504GatewayTimeout,
                    _ => StatusCodes.Status500InternalServerError
                }
                : StatusCodes.Status500InternalServerError;
        }

        private async Task StartConnectionLoopAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_connection == null)
                        throw new InvalidOperationException("SignalR connection is not initialized.");

                    if (_connection.State == HubConnectionState.Connected)
                        return;

                    _logger.LogInformation("Starting SignalR connection...");

                    await _connection.StartAsync(stoppingToken);

                    _connectedTcs.TrySetResult();

                    _logger.LogInformation("SignalR connected. ConnectionId={ConnectionId}", _connection.ConnectionId);

                    bool registered = await RegisterServiceToHostAsync(stoppingToken);

                    _logger.LogInformation(
                        "Service registration with host completed. ServiceId={ServiceId}, Registered={Registered}",
                        _controllerConfig.Id,
                        registered);

                    return;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to connect/register with host. Will retry. ServiceId={ServiceId}",
                        _controllerConfig.Id);

                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

        public async Task<T?> SendToHostAsync<T>(ControllerRouteRequest req, CancellationToken ct = default)
        {
            var json = await SendToHostAsync(req, ct);
            if (json == null) return default;
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }

        public async Task<T?> SendToHostAsync<T>(ControllerRouteRequest req, int timeoutMs)
        {
            var json = await SendToHostAsync(req, timeoutMs);
            if (json == null) return default;
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }

        public async Task<string?> SendToHostAsync(ControllerRouteRequest req, int timeoutMs)
        {
            return await SendToHostAsync(req, CancellationToken.None, timeoutMs);
        }

        public Task WaitForConnectionAsync(CancellationToken ct = default)
            => _connectedTcs.Task.WaitAsync(ct);

        public Task<string?> SendToHostAsync(ControllerRouteRequest req, CancellationToken ct = default)
            => SendToHostAsync(req, ct, null);

        private async Task<string?> SendToHostAsync(ControllerRouteRequest req, CancellationToken ct, int? timeoutMs)
        {
            if (req == null)
                throw new ArgumentNullException(nameof(req));

            if (_connection == null)
                throw new InvalidOperationException("SignalR connection is not initialized.");

            if (_connection.State != HubConnectionState.Connected)
                throw new InvalidOperationException($"SignalR connection is not connected. State={_connection.State}");

            try
            {

                //_logger.LogDebug(
                //    "Sending route request to host. RequestId={RequestId}, Method={Method}, Route={Route}, DestinationServiceId={DestinationServiceId}, Destination={Destination}",
                //    req.RequestId,
                //    req.Method,
                //    req.Route,
                //    req.DestinationServiceId,
                //    req.Destination);

                using var cts = timeoutMs.HasValue ? new CancellationTokenSource(timeoutMs.Value) : null;
                var effectiveCt = cts != null ? cts.Token : ct;

                ControllerRouteResponse? response = await _connection.InvokeAsync<ControllerRouteResponse>(
                    "RouteRequestFromClient",
                    req,
                    effectiveCt);

                EnsureRouteSucceeded(response, req);
                return response.Body;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Exception sending route request to host. RequestId={RequestId}",
                    req?.RequestId);

                throw;
            }
        }

        public async Task<bool> RegisterServiceToHostAsync(CancellationToken ct = default)
        {
            try
            {
                if (_connection == null)
                    throw new InvalidOperationException("SignalR connection is not initialized.");

                if (_connection.State != HubConnectionState.Connected)
                    throw new InvalidOperationException($"SignalR connection is not connected. State={_connection.State}");

                _logger.LogDebug("Sending registration information to host. {ServiceId}, {ServiceName}",
                    _controllerConfig.Id,
                    _controllerConfig.Name);

                return await _connection.InvokeAsync<bool>(
                    "RegisterService",
                    _controllerConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending registration information to host.");

                throw;
            }
        }


        public virtual async Task<ControllerRouteResponse> SendToLocalController(ControllerRouteRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Route))
                throw new ArgumentException("Route cannot be empty.");

            var method = new HttpMethod(req.Method.ToUpperInvariant());

            using var httpReq = new HttpRequestMessage(method, req.Route);

            if (!string.IsNullOrWhiteSpace(req.Token))
            {
                httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", req.Token);
            }

            if (method == HttpMethod.Post || method == HttpMethod.Put || method.Method == "PATCH")
            {
                httpReq.Content = new StringContent(
                    req.Load ?? "",
                    Encoding.UTF8,
                    "application/json");
            }

            if (_localHttpClient != null)
            {
                using var response = await _localHttpClient.SendAsync(httpReq);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Local routed request failed. RequestId={RequestId}, Method={Method}, Route={Route}, StatusCode={StatusCode}, Reason={Reason}",
                        req.RequestId,
                        req.Method,
                        req.Route,
                        (int)response.StatusCode,
                        response.ReasonPhrase);

                    return ControllerRouteResponse.Failure(
                        req.RequestId,
                        (int)response.StatusCode,
                        response.ReasonPhrase,
                        body);
                }

                return ControllerRouteResponse.Success(
                    req.RequestId,
                    body,
                    (int)response.StatusCode);
            }
            return ControllerRouteResponse.Failure(
                req.RequestId,
                StatusCodes.Status503ServiceUnavailable,
                "Local HTTP forwarding is unavailable because no Kestrel HTTP or HTTPS endpoint is configured.");
        }

        private static void EnsureRouteSucceeded(
            ControllerRouteResponse? response,
            ControllerRouteRequest req)
        {
            if (response == null)
            {
                throw new AppErrorException(
                    ErrorCodes.ApiService_NotResponding,
                    string.Empty,
                    $"Routed request {req.RequestId} ({req.Method} {req.Route}) returned no response envelope.");
            }

            if (response.TransportSucceeded)
                return;

            int errorCode = response.HttpStatusCode switch
            {
                401 => ErrorCodes.AuthError_Authentication,
                403 => ErrorCodes.AuthError_Unauthorized,
                404 => ErrorCodes.ClientError_NotFound,
                500 => ErrorCodes.SysError_InternalServerError,
                502 => ErrorCodes.SysError_BadGateway,
                503 => ErrorCodes.SysError_ServiceUnavailable,
                504 => ErrorCodes.SysError_GatewayTimeout,
                _ => ErrorCodes.ApiService_General
            };

            throw new AppErrorException(
                errorCode,
                response.HttpStatusCode.ToString(),
                $"Routed request {req.RequestId} ({req.Method} {req.Route}) failed with HTTP " +
                $"{response.HttpStatusCode}: {response.ErrorMessage ?? "Request failed"}.");
        }

        private async Task<string> GetControllerJwtTokenAsync()
        {
            if (!string.IsNullOrWhiteSpace(_accessToken) &&
                DateTime.UtcNow < _accessTokenExpiresUtc.AddMinutes(-2))
            {
                return _accessToken;
            }

            await _tokenLock.WaitAsync();

            try
            {
                if (!string.IsNullOrWhiteSpace(_accessToken) &&
                    DateTime.UtcNow < _accessTokenExpiresUtc.AddMinutes(-2))
                {
                    return _accessToken;
                }

                if (string.IsNullOrWhiteSpace(_controllerConfig.AuthURL))
                    throw new InvalidOperationException("ControllerConfig.AuthURL is not configured.");

                var http = HttpUtils.GetHttpClientWithCert(_controllerConfig.CertThumbPrint);
                string loginUrl = _controllerConfig.AuthURL.TrimEnd('/') + _controllerConfig.ReqLogin;

                var loginPayload = new
                {
                    username = _controllerConfig.AuthUsername,
                    password = _controllerConfig.AuthPasswordFlat
                };

                
                using var response = await http.PostAsJsonAsync(loginUrl, loginPayload);

                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Controller login failed. HTTP {(int)response.StatusCode}: {body}");
                }

                var result = await response.Content.ReadFromJsonAsync<TokenDescriptorDto>();

                if (string.IsNullOrWhiteSpace(result?.AccessToken))
                {
                    throw new Exception("Controller login succeeded but no token was returned.");
                }

                _accessToken = result.AccessToken;

                // Adjust this depending on your TokenDescriptorDto.
                // If it has Expires or ExpiryTime, use that instead.
                _accessTokenExpiresUtc = DateTime.UtcNow.AddMinutes(50);

                return _accessToken;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to acquire JWT token for controller authentication.");
                throw;
            }
            finally
            {
                _tokenLock.Release();
            }
        }
    }
}

