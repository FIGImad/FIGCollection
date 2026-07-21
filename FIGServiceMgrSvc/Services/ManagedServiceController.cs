using System.Runtime.Versioning;
using System.ServiceProcess;

namespace FIGServiceMgrSvc.Services
{
    /// <summary>
    /// Wraps <see cref="ServiceController"/> to start, stop, and query the status of
    /// Windows services.  Only services listed in <see cref="AllowedServices"/> are
    /// accepted – all others are rejected to prevent unauthorised control.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ManagedServiceController
    {
        private readonly ILogger<ManagedServiceController> _logger;
        private readonly HashSet<string> _allowed;

        // How long to wait for a service to reach the target state.
        private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(30);

        public ManagedServiceController(
            ILogger<ManagedServiceController> logger,
            IEnumerable<string> allowedServices)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _allowed = new HashSet<string>(
                allowedServices ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyCollection<string> AllowedServices => _allowed;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Returns the <see cref="ServiceControllerStatus"/> of a service.</summary>
        public ServiceControllerStatus GetStatus(string serviceName)
        {
            //EnsureAllowed(serviceName);
            using var sc = new ServiceController(serviceName);
            return sc.Status;
        }

        /// <summary>Returns the status string of a service (e.g. "Running").</summary>
        public string GetStatusString(string serviceName)
            => GetStatus(serviceName).ToString();

        /// <summary>
        /// Returns the status of every managed service as a dictionary keyed by service name.
        /// Unreachable services are reported as "Unknown".
        /// </summary>
        public Dictionary<string, string> GetAllStatuses()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in _allowed)
            {
                try
                {
                    result[name] = GetStatusString(name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not query status of service '{ServiceName}'.", name);
                    result[name] = "Unknown";
                }
            }
            return result;
        }

        /// <summary>Starts a service and waits for it to reach <c>Running</c>.</summary>
        public void Start(string serviceName)
        {
            EnsureAllowed(serviceName);
            _logger.LogInformation("Starting service '{ServiceName}'.", serviceName);

            using var sc = new ServiceController(serviceName);
            if (sc.Status == ServiceControllerStatus.Running)
            {
                _logger.LogInformation("Service '{ServiceName}' is already running.", serviceName);
                return;
            }

            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running, OperationTimeout);
            _logger.LogInformation("Service '{ServiceName}' started successfully.", serviceName);
        }

        /// <summary>Stops a service and waits for it to reach <c>Stopped</c>.</summary>
        public void Stop(string serviceName)
        {
            EnsureAllowed(serviceName);
            _logger.LogInformation("Stopping service '{ServiceName}'.", serviceName);

            using var sc = new ServiceController(serviceName);
            if (sc.Status == ServiceControllerStatus.Stopped)
            {
                _logger.LogInformation("Service '{ServiceName}' is already stopped.", serviceName);
                return;
            }

            sc.Stop();
            sc.WaitForStatus(ServiceControllerStatus.Stopped, OperationTimeout);
            _logger.LogInformation("Service '{ServiceName}' stopped successfully.", serviceName);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void EnsureAllowed(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("Service name must not be empty.", nameof(serviceName));

            if (!_allowed.Contains(serviceName))
                throw new InvalidOperationException(
                    $"Service '{serviceName}' is not in the managed-services allow-list.");
        }
    }
}
