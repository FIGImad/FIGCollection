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
                throw new InvalidOperationException("Pushover API token and recipient key are required.");

            var payload = new Dictionary<string, string>
            {
                { "token", token },
                { "user", pushoverUserKey },
                { "title", subject },
                // Preserve database HTML (bold/color) and native line breaks for html=1.
                { "message", message },
                { "html", "1" }
            };

            try
            {
                using var response = await _httpClient.PostAsync(
                    "https://api.pushover.net/1/messages.json",
                    new FormUrlEncodedContent(payload));

                response.EnsureSuccessStatusCode();
                using var result = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (!result.RootElement.TryGetProperty("status", out var status) || status.GetInt32() != 1)
                    throw new InvalidOperationException("Pushover did not confirm successful delivery.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending PushOver notification");
                throw;
            }
        }
    }
}
