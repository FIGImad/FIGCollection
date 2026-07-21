using FIGCommon.Exceptions;
using FIGCommon.Interfaces;
using FIGCommon.Models;
using FIGCommon.Models.FIGController;
using FIGCommon.Utilities;
using System.Security.Claims;
using System.Text.Json;

namespace FIGCommon.Services
{
    public class ApiServiceBase
    {
        protected readonly IConfiguration _config;
        protected readonly HttpClient _httpClient;
        protected readonly ILogger _logger;
        protected readonly IClientSignalRService _controllerClient;
        protected readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly IServiceProvider _services;
        private readonly ControllerConfig _controllerConfig;
        private const int _defaultLoginTime = 5000;
        private bool _useControllerAuth = true; // true - use controller login, false - use current user token from context

        private readonly SemaphoreSlim _loginGate = new(1, 1);
        private readonly object _stateLock = new();

        private string? _token = null;

        public ApiServiceBase(HttpClient httpClient,
                             ILogger logger,
                             IClientSignalRService controllerClient,
                             IServiceProvider services,
                             IHttpContextAccessor httpContextAccessor,
                             IConfiguration config,
                             bool useControllerAuth = true)
        {
            _controllerClient = controllerClient ?? throw new ArgumentNullException(nameof(controllerClient));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpContextAccessor = httpContextAccessor;
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _useControllerAuth = useControllerAuth;

            _controllerConfig = new();
            _config.GetSection("ControllerConfig").Bind(_controllerConfig);
            _controllerConfig.Validate();
        }

        #region Accessors
        private bool IsUserAuth
        {
            get
            {
                lock (_stateLock)
                {
                    return !string.IsNullOrEmpty(_token);
                }
            }
        }

        private string ThumbPrint => _controllerConfig.CertThumbPrint;

        private string Token
        {
            get
            {
                lock (_stateLock)
                {
                    return _token ?? string.Empty;
                }
            }
            set
            {
                lock (_stateLock)
                {
                    _token = value ?? string.Empty;
                }
            }
        }


        #endregion Accessors

        protected async Task<T?> ProcessRouteRequest<T>(ControllerRouteRequest req, int timeout = 500000)
        {

            try
            {
                string token;

                if (!_useControllerAuth)
                {
                    token = GetCurrentUserToken() ?? string.Empty;
                    if (token == string.Empty)
                    {
                        _logger?.LogWarning("Token mode is set to use user token but no token found in current context");
                        // throw ErrorCodes.AuthError_Login
                        throw new AppErrorException(ErrorCodes.AuthError_Login, "", "Could not get user token from current context");
                    }
                }
                else
                {
                    token = Token;
                    if (string.IsNullOrEmpty(token))
                    {
                        int timeoutLogin = timeout < _defaultLoginTime ? timeout : _defaultLoginTime;
                        var loginResult = await LoginToHost(timeoutLogin);
                        token = loginResult.Token;
                        if (string.IsNullOrEmpty(token))
                        {
                            throw new AppErrorException(ErrorCodes.AuthError_Login, "", loginResult.TransientError ? "Auth service unavailable" : "User is not authenticated");
                        }
                    }
                }
            
                req.Token = token;

                return await _controllerClient.SendToHostAsync<T>(req, timeout);
            }
            catch(AppErrorException ex)
            {
                if (ex.errorCode == ErrorCodes.AuthError_Unauthorized || ex.errorCode == ErrorCodes.AuthError_Authentication || ex.errorCode == ErrorCodes.AuthError_Login || ex.errorCode == ErrorCodes.AuthError_InvalidCred)
                {
                    if (_useControllerAuth)
                    {
                        // Do not let a late failure from a request using an old token
                        // clear a newer token that another request has already obtained.
                        InvalidateTokenIfCurrent(req.Token);
                    }
                    // Only do an immediate retry for real auth errors (e.g. stale token).
                    // If the auth service itself is down (transient), skip the retry so the
                    // caller can retry later once the service recovers.
                    bool isTransientLoginFailure = ex.errorCode == ErrorCodes.AuthError_Login
                        && ex.Message.Contains("Auth service unavailable");
                    if (_useControllerAuth && req.Attempt == 1 && !isTransientLoginFailure)
                    {
                        req.Attempt++;
                        return await ProcessRouteRequest<T>(req, timeout);
                    }
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Exception in ProcessRouteRequest response {0}", ex.Message);
                throw new AppErrorException(ErrorCodes.ApiService_NotResponding, "", ex.Message);
            }
        }

        // Returns a stable token snapshot plus whether an empty result was caused by
        // a transient auth-service failure (unreachable / 5xx).
        private async Task<(string Token, bool TransientError)> LoginToHost(int timeout = 500000, CancellationToken cancellationToken = default)
        {
            string existingToken = Token;
            if (!string.IsNullOrEmpty(existingToken))
            {
                return (existingToken, false);
            }
            await _loginGate.WaitAsync(cancellationToken);
            bool transientError = false;
            try
            {
                // Multiple callers may all observe an empty token before the first one
                // acquires the gate. Re-check after entering so only one caller logs in.
                existingToken = Token;
                if (!string.IsNullOrEmpty(existingToken))
                {
                    return (existingToken, false);
                }

                string pass = string.Empty;
                string usr = string.Empty;
                if (_useControllerAuth)
                {
                    pass = _controllerConfig.AuthPasswordFlat;
                    usr = _controllerConfig.AuthUsername;
                }
                if (usr == "" )
                {
                    _logger?.LogError("Auth username is not provided in config");
                    return (string.Empty, false);
                }
                string authBase = _controllerConfig.AuthURL;
                string url = $"{authBase}{_controllerConfig.ReqLogin}";
                string thumbPrint = _controllerConfig.CertThumbPrint;
                if (authBase == "" || _controllerConfig.ReqLogin == "" || pass == "" || usr == "")
                {
                    _logger?.LogError("Invalid login information URL or auth user details");
                    return (string.Empty, false);
                }
                LoginRequestDto req = new() { Username = usr, Password = pass };
                try
                {
                    _logger?.LogDebug("Sending Login request to controller");
                    var resp = await HttpUtils.PostJsonAsync<LoginRequestDto>(url, req, string.Empty, thumbPrint);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    LoginResponseDto? response = System.Text.Json.JsonSerializer.Deserialize<LoginResponseDto>(resp, options);
                    string accessToken = response?.AccessToken ?? string.Empty;
                    Token = accessToken;
                    return (accessToken, false);
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogError(ex, "Timeout or cancellation during ReqLogin.");
                    transientError = true;
                }
                catch (System.Text.Json.JsonException exJson)
                {
                    _logger?.LogError("Json Exception in parsing the response to ReqLogin - {0}", exJson.Message);
                }
                catch (HttpRequestException ex) when (ex.StatusCode.HasValue && (int)ex.StatusCode.Value >= 500)
                {
                    _logger?.LogWarning("Auth service unavailable during ReqLogin. StatusCode={0}", ex.StatusCode);
                    transientError = true;
                }
                catch (HttpRequestException ex) when (ex.StatusCode == null)
                {
                    // No status code = network-level failure (DNS, connection refused, etc.)
                    _logger?.LogWarning("Auth service unreachable during ReqLogin: {0}", ex.Message);
                    transientError = true;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning("Exception in ReqLogin response {0}", ex.Message);
                }
            }
            finally
            {
                _loginGate.Release();
            }
            return (string.Empty, transientError);
        }

        private void InvalidateTokenIfCurrent(string? failedToken)
        {
            lock (_stateLock)
            {
                if (string.Equals(_token, failedToken, StringComparison.Ordinal))
                {
                    _token = string.Empty;
                }
            }
        }


        protected string? GetCurrentUserToken()
        {
            if (_httpContextAccessor == null || _httpContextAccessor.HttpContext == null)
            {
                return null;
            }
            try
            {
                using (var scope = _services.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    IIdentityService identityService = scopedServices.GetRequiredService<IIdentityService>();
                    var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
                    var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                    var user = identityService.UserManager.FindByIdAsync(userId ?? "");
                    return authHeader?.Replace("Bearer ", "");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user token");
                return null;
            }
        }


    }
}
