namespace FIGAlertSvc.Services
{
    public class PushOverMessageService : IMessageService
    {
        private readonly ILogger<PushOverMessageService> _logger;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public PushOverMessageService(ILogger<PushOverMessageService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            _httpClient = new HttpClient();
        }

        public async Task Send(string pushoverUserKey, string subject, string message)
        {
            var token = _config["PushOver:APIToken"];
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(pushoverUserKey))
                return;

            var payload = new Dictionary<string, string>
            {
                { "token", token },
                { "user", pushoverUserKey },
                { "title", subject },
                { "message", message }
            };

            try
            {
                var response = await _httpClient.PostAsync(
                    "https://api.pushover.net/1/messages.json",
                    new FormUrlEncodedContent(payload));

                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning("PushOver send failed: {StatusCode}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending PushOver notification");
            }
        }
    }
}
