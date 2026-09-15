using FIGAutoTraderAdminSvc.Models;
using FIGCommon.Exceptions;
using FIGCommon.Utilities;
using FIGCommon.Models.FIGController;
using Microsoft.AspNetCore.SignalR;
using FIGCommon.Models;

namespace FIGAutoTraderAdminSvc.Services
{
    public sealed class ATSMonitorService : BackgroundService
    {
        private readonly ILogger<ATSMonitorService> _logger;
        private readonly ControllerApiService _controllerAPI;
        private readonly IConnectionStatusStore _store;
        private readonly IHubContext<ATSMonitorHub> _hubContext;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly object _pingLock = new();
        private readonly Dictionary<string, int> _pingFailCounts = new();
        private const int _logPingFailEvery = 10;
        private const int _offlineAfterFailures = 2;
        private const int _deviceListCacheTimeoutMs = 10000;
        private CacheItem<List<DeviceRS>> _deviceListCache =
            new CacheItem<List<DeviceRS>>(new List<DeviceRS>(), _deviceListCacheTimeoutMs);

        private readonly int forceUpdateEverySec = 30;
        private DateTime lastSent = DateTime.MinValue;

        public ATSMonitorService(
            ILogger<ATSMonitorService> logger,
            ControllerApiService controllerAPI,
            IConnectionStatusStore store,
            IHubContext<ATSMonitorHub> hubContext,
            IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _store = store;
            _hubContext = hubContext;
            _controllerAPI = controllerAPI;
            _appLifetime = appLifetime;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // FIGAutoTraderAdminSvc hosts RootsIdentity itself.
            // Do not call https://localhost until Kestrel is listening.
            var started = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            using var registration = _appLifetime.ApplicationStarted.Register(
                () => started.TrySetResult(true));

            if (!_appLifetime.ApplicationStarted.IsCancellationRequested)
            {
                await started.Task.WaitAsync(stoppingToken);
            }

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

            _logger.LogInformation("ATSMonitorService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var deviceList = await GetDeviceListAsync(stoppingToken);
                    var activeDeviceIds = deviceList
                        .Select(device => device.Id)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    _store.RetainKeys(activeDeviceIds);

                    // fire all pings at once and wait for all of them
                    var pingTasks = deviceList.Select(device => PingDeviceAsync(device, stoppingToken)).ToArray();
                    var pingResults = await Task.WhenAll(pingTasks);

                    bool oneConnectionChangedAtLeast = false;
                    var now = DateTime.UtcNow;

                    foreach (var result in pingResults)
                    {
                        oneConnectionChangedAtLeast = oneConnectionChangedAtLeast || ReportStatus(result.Device, result.IsConnected);
                    }

                    if (oneConnectionChangedAtLeast ||
                        (DateTime.UtcNow - lastSent).TotalSeconds >= forceUpdateEverySec)
                    {
                        var statusAll = _store.GetAll();
                        await _hubContext.Clients.All.SendAsync(
                            "ConnectionStatusChanged",
                            statusAll,
                            stoppingToken);

                        lastSent = DateTime.UtcNow;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (AppErrorException ex) when (
                    ex.errorCode == FIGCommon.Utilities.ErrorCodes.AuthError_Login ||
                    ex.errorCode == FIGCommon.Utilities.ErrorCodes.AuthError_Unauthorized ||
                    ex.errorCode == FIGCommon.Utilities.ErrorCodes.ApiService_NotResponding)
                {
                    _logger.LogWarning("ATSMonitorService loop: connectivity/auth error - {Message}", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ATSMonitorService loop.");
                }

                try
                {
                    await timer.WaitForNextTickAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("ATSMonitorService stopped.");
        }

        public bool ReportStatus(DeviceRS device, bool isConnected, bool triggerNotification = false, CancellationToken stoppingToken = default)
        {
            var now = DateTime.UtcNow;
            var current = _store.Get(device.Id);
            if (current == null)
            {
                current = new ConnectionStatus
                {
                    Key = device.Id,
                    Role = device.Role,
                    IsConnected = isConnected,
                    Message = isConnected ? "Connected" : "Disconnected",
                    LastCheckedUtc = now,
                    LastConnectedUtc = isConnected ? now : null,
                    LastDisconnectedUtc = !isConnected ? now : null,
                };

                _store.Set(device.Id, current);
            }

            // Suppress offline transition until N consecutive ping failures are confirmed.
            // A single slow ping cycle (e.g. transient SignalR latency) does not trip the device offline.
            bool effectivelyConnected = isConnected || (current.IsConnected && _pingFailCounts.TryGetValue(device.Id, out int fc) && fc < _offlineAfterFailures);

            bool changed = current.IsConnected != effectivelyConnected;

            if (changed)
            {
                var newStatus = new ConnectionStatus
                {
                    Key = device.Id,
                    Role = device.Role,
                    IsConnected = effectivelyConnected,
                    Message = effectivelyConnected ? "Connected" : "Disconnected",
                    LastCheckedUtc = now,
                    LastConnectedUtc = effectivelyConnected ? now : current.LastConnectedUtc,
                    LastDisconnectedUtc = !effectivelyConnected ? now : current.LastDisconnectedUtc
                };

                _store.Set(device.Id, newStatus);

                _logger.LogInformation(
                    "device \"{DeviceId}\" - role({RoleNames}) connection state changed: {IsConnected}",
                    device.Id,
                    ControllerConfig.GetRoleNamesCsv(device.Role),
                    effectivelyConnected);
            }
            else
            {
                // update only the LastCheckedUtc timestamp in-place without allocating a new object
                current.LastCheckedUtc = now;
                _store.Set(device.Id, current);
            }
            if (changed && triggerNotification)
            {
                var statusAll = _store.GetAll();
                _hubContext.Clients.All.SendAsync("ConnectionStatusChanged", statusAll, stoppingToken);
                lastSent = DateTime.UtcNow;
            }
            return changed;
        }

        public void InvalidateDeviceListCache()
        {
            lock (_pingLock)
            {
                _deviceListCache = new CacheItem<List<DeviceRS>>(new List<DeviceRS>(), _deviceListCacheTimeoutMs);
            }
        }

        private async Task<List<DeviceRS>> GetDeviceListAsync(CancellationToken cancellationToken = default)
        {
            List<DeviceRS>? deviceList;
            bool shouldRefresh;

            lock (_pingLock)
            {
                deviceList = _deviceListCache.GetItem();
                shouldRefresh = _deviceListCache.IsExpired() || deviceList == null || deviceList.Count == 0;

                if (!shouldRefresh)
                {
                    return new List<DeviceRS>(deviceList ?? new List<DeviceRS>());
                }
            }

            var freshList = ((await _controllerAPI.GetDeviceList(10000, cancellationToken)) ?? new List<DeviceRS>())
                .Where(device => device.Enabled)
                .ToList();

            lock (_pingLock)
            {
                deviceList = _deviceListCache.GetItem();

                if (_deviceListCache.IsExpired() || deviceList == null || deviceList.Count == 0)
                {
                    _deviceListCache.Touch(freshList);
                    deviceList = freshList;
                }

                return new List<DeviceRS>(deviceList);
            }
        }

        private async Task<DevicePingResult> PingDeviceAsync(DeviceRS device, CancellationToken stoppingToken)
        {
            try
            {
                var statusObj = await _controllerAPI.Ping(device.Id, 10000, stoppingToken);
                _pingFailCounts.Remove(device.Id);
                return new DevicePingResult(device, statusObj != null);
            }
            catch (Exception ex)
            {
                _pingFailCounts.TryGetValue(device.Id, out int failCount);
                failCount++;
                _pingFailCounts[device.Id] = failCount;

                if (failCount == 1 || failCount % _logPingFailEvery == 0)
                    _logger.LogError(ex, "Ping failed for ServiceId:\"{ServiceId}\", ServiceName: \"{ServiceName}\" (failure #{FailCount})", device.Id, device.Name, failCount);
                else
                    _logger.LogError("Ping failed for ServiceId:\"{ServiceId}\", ServiceName: \"{ServiceName}\" (failure #{FailCount})", device.Id, device.Name, failCount);

                return new DevicePingResult(device, false);
            }
        }

        private sealed record DevicePingResult(DeviceRS Device, bool IsConnected);
    }
}
