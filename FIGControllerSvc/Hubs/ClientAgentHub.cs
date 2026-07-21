using FIGCommon.DataAccess;
using FIGCommon.Exceptions;
using FIGCommon.Models;
using FIGCommon.Models.FIGController;
using FIGCommon.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace FIGControllerSvc.Hubs
{
    //[AllowAnonymous]
    [Authorize(Roles = "SVC,Admin")]
    public class ClientAgentHub : Hub
    {
        private readonly ILogger<ClientAgentHub> _logger;
        private readonly ControllerConfig _controllerConfig;
        private readonly IConfiguration _config;
        private readonly HttpClient? _localHttpClient = null;

        private static string? _cachedLocalIp;
        private readonly object _lockAccess = new();

        public ClientAgentHub(ILogger<ClientAgentHub> logger,
                              IConfiguration config)
        {
            _logger = logger;
            _config = config;

            // Controller Config
            _controllerConfig = new ControllerConfig();
            config.GetSection("ControllerConfig").Bind(_controllerConfig);
            if (_controllerConfig == null) throw new ArgumentNullException(nameof(_controllerConfig));
            _controllerConfig.Validate();

            // _localHttpClient setup
            var address = _config["Kestrel:Endpoints:Https:Url"]?.Replace("://*:", "://localhost:");
            _localHttpClient = new HttpClient
            {
                BaseAddress = new Uri(address!)
            };
        }

        public override Task OnConnectedAsync()
        {
            var http = Context.GetHttpContext();

            string serviceId = http?.Request.Query["serviceId"].ToString() ?? "";
            string name = http?.Request.Query["name"].ToString() ?? "";
            string roleRaw = http?.Request.Query["role"].ToString() ?? "0";

            int.TryParse(roleRaw, out int role);

            if (string.IsNullOrWhiteSpace(serviceId))
            {
                _logger.LogWarning("ClientAgent rejected. Missing serviceId. ConnectionId={ConnectionId}", Context.ConnectionId);
                Context.Abort();
                return Task.CompletedTask;
            }

            _logger.LogInformation(
                "ClientAgent connected. ServiceId={ServiceId}, Role={Role}, ConnectionId={ConnectionId}",
                serviceId,
                role,
                Context.ConnectionId);

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation(
                exception,
                "ClientAgent disconnected. ConnectionId={ConnectionId}",
                Context.ConnectionId);

            try
            {
                InvalidateHubConnection(Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to invalidate hub connection on disconnect. ConnectionId={ConnectionId}", Context.ConnectionId);
            }

            return base.OnDisconnectedAsync(exception);
        }

        public Task<bool> RegisterService(ControllerConfig regInfo)
        {
            try
            {
                if (regInfo == null) return Task.FromResult(false);

                _logger.LogInformation($"Received registration from seviceId: {regInfo.Id}, name: {regInfo.Name}");

                // Cache the local IP — it never changes at runtime
                _cachedLocalIp ??= Dns.GetHostEntry(Dns.GetHostName()).AddressList
                    .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    ?.ToString() ?? "127.0.0.1";

                var remoteIp = Context.GetHttpContext()?.Connection.RemoteIpAddress;
                bool isLoopback = remoteIp != null && System.Net.IPAddress.IsLoopback(remoteIp);
                string? resolvedIp = isLoopback ? _cachedLocalIp : remoteIp?.MapToIPv4().ToString();

                regInfo.HostAddress = Context.GetHttpContext()?.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                    ?? resolvedIp
                    ?? regInfo.HostAddress;

                bool registered = RegisterServiceInternal(regInfo, Context.ConnectionId);
                if (!registered)
                {
                    Context.Abort();
                }

                return Task.FromResult(registered);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "RegisterService failed. ServiceId={ServiceId}, Name={Name}",
                    regInfo?.Id,
                    regInfo?.Name);

                return Task.FromResult(false);
            }
        }

        public async Task<ControllerRouteResponse> RouteRequestFromClient(ControllerRouteRequest req)
        {
            try
            {
                _logger.LogInformation(
                    "Received route request from ClientAgent. RequestId={RequestId}, Method={Method}, Route={Route}, DestinationServiceId={DestinationServiceId}, Destination={Destination}",
                    req.RequestId,
                    req.Method,
                    req.Route,
                    req.DestinationServiceId,
                    req.DestinationRole);

                ControllerRouteResponse response = await RouteAsync(req);
                if (!response.TransportSucceeded)
                {
                    _logger.LogWarning(
                        "Route request failed. RequestId={RequestId}, DestinationServiceId={DestinationServiceId}, DestinationRole={DestinationRole}, StatusCode={StatusCode}, Error={Error}",
                        req.RequestId,
                        req.DestinationServiceId,
                        req.DestinationRole,
                        response.HttpStatusCode,
                        response.ErrorMessage);
                }
                else
                {
                    _logger.LogDebug(
                        "Route request completed. RequestId={RequestId}, StatusCode={StatusCode}",
                        req.RequestId,
                        response.HttpStatusCode);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed handling route request from ClientAgent. RequestId={RequestId}",
                    req.RequestId);

                return CreateFailureResponse(req, ex);
            }
        }


        public void InvalidateHubConnection(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId)) return;
            var device = MainRepo.GetDeviceByConnection(connectionId);
            if (device == null) return;
            _logger.LogInformation("Clearing stale hub connection for DeviceId={DeviceId}, ConnectionId={ConnectionId}", device.Id, connectionId);
            device.ConnectionId= "";
            MainRepo.InvalidateDevice(device.Id, connectionId);
        }

        public bool RegisterServiceInternal(ControllerConfig regInfo, string connectionId)
        {
            if (regInfo == null)
            {
                throw new ArgumentNullException(nameof(regInfo));
            }
            if (string.IsNullOrEmpty(regInfo.Id))
            {
                throw new ArgumentException("Service Id cannot be null or empty.", nameof(regInfo.Id));
            }
            if (string.IsNullOrEmpty(regInfo.Name))
            {
                throw new ArgumentException("Service Name cannot be null or empty.", nameof(regInfo.Name));
            }

            var registeredService = MainRepo.GetDevice(regInfo.Id);
            if (registeredService != null)
            {
                var wasDisabled = !registeredService.Enabled;

                // Update existing service
                registeredService.Name = regInfo.Name;
                registeredService.ServiceName = regInfo.ServiceName;
                registeredService.ServiceType = regInfo.ServiceType;
                registeredService.MachineName = regInfo.DeviceName;
                registeredService.Role = regInfo.Role;
                registeredService.Version = regInfo.VersionNumber;
                registeredService.ConnectionId = wasDisabled ? string.Empty : connectionId;
                registeredService.IPAddress = regInfo.HostAddress;
                registeredService.LastUpdatedTime = -1; // set to -1 to indicate update from hub, actual timestamp will be set in repo

                MainRepo.UpsertDevice(registeredService);

                if (wasDisabled)
                {
                    _logger.LogWarning(
                        "Ignoring registration for disabled device. ServiceId={ServiceId}, ConnectionId={ConnectionId}",
                        regInfo.Id,
                        connectionId);

                    return false;
                }
            }
            else
            {
                // Insert new service
                var newDevice = new DeviceRS
                {
                    Id = regInfo.Id,
                    Name = regInfo.Name,
                    ServiceName = regInfo.ServiceName,
                    ServiceType = regInfo.ServiceType,
                    MachineName = regInfo.DeviceName,
                    Role = regInfo.Role,
                    Version = regInfo.VersionNumber,
                    ConnectionId = connectionId,
                    IPAddress = regInfo.HostAddress,
                    Enabled = true // re-enable the service in case it was marked disabled due to stale connection
                };
                MainRepo.UpsertDevice(newDevice);
            }
            return true;
        }

        public async Task<ControllerRouteResponse> RouteAsync(ControllerRouteRequest req, CancellationToken ct = default)
        {
            if (req == null)
            {
                throw new ArgumentException("Service Id cannot be null or empty.", nameof(req));
            }
            DeviceRS? activeService = null;
            string url = string.Empty;
            string token = req.Token;
            string thumbPrint = string.Empty;

            lock (_lockAccess)
            {

                DeviceRS? regSvc = null;
                if (req.DestinationRole == (int)ControllerClientRole.None && string.IsNullOrEmpty(req.DestinationServiceId))
                {
                    // there is no target to route the request to
                    _logger?.LogError("Routing request missing destination information. Either Destination role or DestinationServiceId must be provided.");
                    return ControllerRouteResponse.Failure(
                        req.RequestId,
                        StatusCodes.Status400BadRequest,
                        "Either DestinationRole or DestinationServiceId must be provided.");
                }
                else if (req.DestinationRole == (int)ControllerClientRole.None)
                {
                    // route by service id
                    regSvc = MainRepo.GetDevice(req.DestinationServiceId);
                    if (regSvc == null)
                    {
                        _logger?.LogError("No registered service found with Id {0} for routing request.", req.DestinationServiceId);
                        return ControllerRouteResponse.Failure(
                            req.RequestId,
                            StatusCodes.Status404NotFound,
                            $"No registered service was found with Id {req.DestinationServiceId}.");
                    }
                }
                else if (string.IsNullOrEmpty(req.DestinationServiceId))
                {
                    // route by role, select the first one for now
                    var registeredServices = MainRepo.GetDevicesByRole(req.DestinationRole);
                    if (registeredServices == null || registeredServices.Count == 0 || registeredServices[0] == null)
                    {
                        return ControllerRouteResponse.Failure(
                            req.RequestId,
                            StatusCodes.Status404NotFound,
                            $"No registered service was found with role {req.DestinationRole}.");
                    }
                    regSvc = registeredServices[0];
                }
                else // route by service id and role, validate the service has the correct role
                {
                    regSvc = MainRepo.GetDevice(req.DestinationServiceId);
                    if (regSvc == null)
                    {
                        _logger?.LogError("No registered service found with Id {0} for routing request.", req.DestinationServiceId);
                        return ControllerRouteResponse.Failure(
                            req.RequestId,
                            StatusCodes.Status404NotFound,
                            $"No registered service was found with Id {req.DestinationServiceId}.");
                    }
                    if (regSvc.Role != req.DestinationRole)
                    {
                        _logger?.LogError("Registered service with Id {0} has role {1} which does not match the requested destination role {2}.", req.DestinationServiceId, regSvc.Role, req.DestinationRole);
                        return ControllerRouteResponse.Failure(
                            req.RequestId,
                            StatusCodes.Status400BadRequest,
                            $"Service {req.DestinationServiceId} does not have role {req.DestinationRole}.");
                    }
                }
                if (regSvc != null)
                {
                    if (!regSvc.Enabled)
                    {
                        _logger?.LogWarning(
                            "Registered service is disabled and will not be used for routing. ServiceId={ServiceId}, Role={Role}",
                            regSvc.Id,
                            regSvc.Role);

                        return ControllerRouteResponse.Failure(
                            req.RequestId,
                            StatusCodes.Status404NotFound,
                            $"No enabled service was found with Id {regSvc.Id}.");
                    }

                    activeService = new(regSvc);
                }
            }

            // process routing here
            if (activeService != null)
            {
                if (activeService.Id != _controllerConfig.Id && string.IsNullOrEmpty(activeService.ConnectionId))
                {
                    _logger.LogError("Registered service found for routing request but has no active connection. ServiceId={ServiceId}, Role={Role}", activeService.Id, activeService.Role);
                    throw new AppErrorException(ErrorCodes.SysError_BadGateway, "Invalid Connection");
                }

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(60));

                try
                {
                    if (activeService.Id == _controllerConfig.Id)
                    {
                        // route locally
                        return await SendToLocalController(req, timeoutCts.Token);
                    }
                    else
                    {
                        // Server calls the client method and relays its explicit
                        // transport envelope without inspecting the response body.
                        return await Clients.Client(activeService.ConnectionId)
                            .InvokeAsync<ControllerRouteResponse>("RouteRequest", req, timeoutCts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogError($"Timeout routing request {req.RequestId} to BrokerAgent {activeService.Id}");
                    throw new AppErrorException(ErrorCodes.SysError_GatewayTimeout, "Timeout");
                }
                catch (IOException ex) when (ex.Message.Contains("does not exist"))
                {
                    // The ConnectionId in the registry is stale — the client reconnected
                    // with a new ConnectionId before re-registering. Clear it so callers
                    // can detect the service is temporarily unreachable.
                    _logger.LogWarning($"Stale ConnectionId {activeService.ConnectionId} for ServiceId={activeService.Id}. Clearing registry entry.");
                    // force disconnection from this connectionId

                    MainRepo.InvalidateDevice(activeService.Id, activeService.ConnectionId);
                    throw new AppErrorException(ErrorCodes.SysError_BadGateway, "Invalid Connection");
                }
                catch (AppErrorException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Exception routing request {req.RequestId}, Route={req.Route}, DestinationServiceId={req.DestinationServiceId}, DestinationRole={req.DestinationRole}");
                    throw new AppErrorException(ErrorCodes.ApiService_General, "", ex.Message);
                }
            }
            else
            {
                _logger.LogWarning($"No connected Client found. DestinationServiceId={req.DestinationServiceId}, DestinationRole={req.DestinationRole}");
                throw new AppErrorException(ErrorCodes.SysError_BadGateway, "No connected Client found");
            }
        }

        public async Task<ControllerRouteResponse> SendToLocalController(ControllerRouteRequest req, CancellationToken cancellationToken = default)
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
                "The Controller local HTTP client is not configured.");
        }

        private static ControllerRouteResponse CreateFailureResponse(
            ControllerRouteRequest req,
            Exception exception)
        {
            int statusCode = exception is AppErrorException appError
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

            return ControllerRouteResponse.Failure(
                req.RequestId,
                statusCode,
                exception.Message);
        }


    }
}

