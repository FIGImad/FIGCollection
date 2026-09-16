using FIGCommon.DataAccess;
using FIGCommon.Models;
using FIGCommon.Models.Alert;
using FIGCommon.Models.LogMonitor;
using System.Collections.Concurrent;

namespace FIGAlertSvc.Services
{
    public class AlertProcessingService : IHostedService
    {
        private readonly ILogger<AlertProcessingService> _logger;
        private readonly IConfiguration _config;

        private readonly ConcurrentDictionary<string, DeviceRS> _deviceMap = new();
        private DateTime _deviceMapLastRefresh = DateTime.MinValue;
        private static readonly TimeSpan _deviceMapCacheDuration = TimeSpan.FromMinutes(5);

        private List<AlertRuleRS> _alertRules = new();
        private DateTime _alertRulesLastRefresh = DateTime.MinValue;
        private static readonly TimeSpan _alertRulesCacheDuration = TimeSpan.FromMinutes(5);

        private CancellationTokenSource _serviceCts = new();
        private readonly static object _lockAccess = new object();

        private string _serviceId = string.Empty;

        public AlertProcessingService(
            ILogger<AlertProcessingService> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider
            )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        protected string ServiceId
        {
            get
            {
                lock (_lockAccess)
                {
                    if (string.IsNullOrEmpty(_serviceId))
                    {
                        string key = "ControllerConfig:Id";
                        string defVal = "";
                        string? val = _config[key];

                        // trim from spaces and LF and CR and Tabs
                        _serviceId = (val ?? defVal).Trim(' ', '\r', '\n', '\t').ToUpper();
                    }
                    return _serviceId;
                }
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _serviceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            // Load device list 
            _logger?.LogDebug("Alert Processing Service is starting up");
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), _serviceCts.Token);

                    // Get device list from controller
                    List<DeviceRS>? lst = MainRepo.GetDevices();
                    if (lst != null)
                    {
                        foreach (var item in lst)
                        {
                            _deviceMap[item.Id] = item;
                        }
                        _deviceMapLastRefresh = DateTime.UtcNow;
                    }

                    // Load alert classification rules from database
                    _alertRules = AlertRepo.GetAlertRules();
                    _alertRulesLastRefresh = DateTime.UtcNow;
                    _logger?.LogDebug("Loaded {Count} alert rules.", _alertRules.Count);
                }
                catch (OperationCanceledException)
                {
                    // Service stopped before 10 seconds passed
                }
            }, _serviceCts.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Cancel all ongoing operations
            _serviceCts.Cancel();
        }

        public async Task ProcessAlertAsync(LogAlertMessage alertRec)
        {
            // check if the device is in the map
            if (!_deviceMap.TryGetValue(alertRec.ServiceId, out DeviceRS? clientInfo))
            {
                // Device not found — refresh from controller only if cache has expired
                if (DateTime.UtcNow - _deviceMapLastRefresh > _deviceMapCacheDuration)
                {
                    List<DeviceRS>? freshList = MainRepo.GetDevices();
                    if (freshList != null)
                    {
                        foreach (var item in freshList)
                        {
                            _deviceMap[item.Id] = item;
                        }
                        _deviceMapLastRefresh = DateTime.UtcNow;
                    }
                    _deviceMap.TryGetValue(alertRec.ServiceId, out clientInfo);
                }
            }

            var originalTime = AlertMessageFormatter.OriginalTime(alertRec.Source, alertRec.Message);
            var rec = new AlertRS()
            {
                RawTime      = (int)(originalTime?.ToUnixTimeSeconds() ?? alertRec.RawTime),
                MSec         = originalTime?.Millisecond ?? alertRec.Milliseconds,
                ServiceId    = clientInfo?.Id          ?? alertRec.ServiceId,
                ServiceName  = clientInfo?.Name ?? "",
                ServiceRole  = clientInfo?.Role        ?? 0,
                ServiceAddress = clientInfo?.IPAddress       ?? "",
                LogName      = alertRec.LogName,
                Source       = SystemAlertSourceResolver.Resolve(alertRec.Source, alertRec.Message),
                Level        = alertRec.Level,
                Message      = alertRec.Message,
                ExceptionText = string.IsNullOrEmpty(alertRec.ExceptionText) ? null : alertRec.ExceptionText
            };

            if (clientInfo == null)
            {
                _logger.LogWarning("ProcessAlertAsync: ServiceId '{ServiceId}' not found in device map — logging with default values.", alertRec.ServiceId);
            }

            AlertRepo.UpsertAlert(rec);

            // Refresh rule cache if expired
            if (DateTime.UtcNow - _alertRulesLastRefresh > _alertRulesCacheDuration)
            {
                _alertRules = AlertRepo.GetAlertRules();
                _alertRulesLastRefresh = DateTime.UtcNow;
                _logger.LogDebug("Alert rules cache refreshed — {Count} rules loaded.", _alertRules.Count);
            }

            // Classify the alert against all enabled rules
            // Existing rules match the logging component; dispatch policy uses the original source.
            var classificationRecord = new AlertRS(rec) { Source = alertRec.Source };
            List<AlertRuleRS> matchedRules = AlertRuleEvaluator.Evaluate(classificationRecord, _alertRules);

            if (matchedRules.Count == 0)
            {
                _logger.LogDebug(
                    "Alert Id={Id} ServiceId='{ServiceId}' Source='{Source}' matched no classification rules.",
                    rec.Id, rec.ServiceId, rec.Source);
            }
            else
            {
                AlertRuleRS topRule = matchedRules.MaxBy(r => r.Severity)!;
                string friendlyMessage = BuildFriendlyMessage(topRule.FriendlyMessage, clientInfo, rec.Message);
                _logger.LogInformation(
                    "Alert Id={Id} ServiceId='{ServiceId}' Source='{Source}' matched rule '{RuleKey}' (Severity={Severity}): {FriendlyMessage}",
                    rec.Id, rec.ServiceId, rec.Source, topRule.RuleKey, topRule.Severity, friendlyMessage);

                AlertRepo.UpdateAlertMatch(rec.Id, topRule.Id, friendlyMessage);
            }
        }

        /// <summary>
        /// Replaces all <c>{{placeholder}}</c> tokens in <paramref name="template"/>:
        /// <list type="bullet">
        ///   <item><c>{{service_name}}</c> — <see cref="ClientInfo.Name"/> (falls back to empty string)</item>
        ///   <item><c>{{service_id}}</c> — <see cref="ClientInfo.Id"/> (falls back to empty string)</item>
        ///   <item><c>{{service_name_from_message}}</c> — ServiceName extracted from the structured log message</item>
        ///   <item><c>{{service_id_from_message}}</c> — ServiceId extracted from the structured log message</item>
        /// </list>
        /// </summary>
        private static string BuildFriendlyMessage(string template, DeviceRS? clientInfo, string rawMessage)
        {
            string serviceName = clientInfo?.Name ?? "";
            string serviceId   = clientInfo?.Id   ?? "";

            string nameFromMsg = ExtractMessageField(rawMessage, "ServiceName");
            string idFromMsg   = ExtractMessageField(rawMessage, "ServiceId");

            return template
                .Replace("{{service_name}}",              serviceName,   StringComparison.OrdinalIgnoreCase)
                .Replace("{{service_id}}",                serviceId,     StringComparison.OrdinalIgnoreCase)
                .Replace("{{service_name_from_message}}", nameFromMsg,   StringComparison.OrdinalIgnoreCase)
                .Replace("{{service_id_from_message}}", idFromMsg, StringComparison.OrdinalIgnoreCase)
                .Replace("{{message}}", rawMessage, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Extracts a named field value from a structured log message of the form:
        /// <c>… FieldName: "value" …</c>  or  <c>… FieldName:"value" …</c>
        /// </summary>
        private static string ExtractMessageField(string message, string fieldName)
        {
            // Matches:  ServiceId: "value"  or  ServiceId:"value"
            var match = System.Text.RegularExpressions.Regex.Match(
                message,
                fieldName + @"\s*:\s*""([^""]*?)""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return match.Success ? match.Groups[1].Value : "";
        }
    }

}
