using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FIGAlertSvc.Services;

public class PushOverMessageService : IMessageService
{
    private static readonly HttpClient SharedClient = new();
    private readonly ILogger<PushOverMessageService> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public PushOverMessageService(ILogger<PushOverMessageService> logger, IConfiguration config)
        : this(logger, config, SharedClient) { }

    internal PushOverMessageService(ILogger<PushOverMessageService> logger, IConfiguration config, HttpClient client)
    {
        _logger = logger;
        _config = config;
        _httpClient = client;
    }

    public Task Send(string pushoverUserKey, string subject, string message) =>
        SendAlert(pushoverUserKey, subject, message, null, null);

    internal static List<(string Text, bool Html)> SplitMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Pushover message is empty.");
        if (message.Length <= 1024) return [(message, true)];
        // Long alerts use plain text so no HTML tag/entity is cut between requests.
        var plain = Regex.Replace(message, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        plain = WebUtility.HtmlDecode(Regex.Replace(plain, @"</?(?:b|i|u|font|a)\b[^>]*>", "", RegexOptions.IgnoreCase));
        var parts = new List<(string, bool)>();
        for (int offset = 0; offset < plain.Length;)
        {
            int length = Math.Min(1024, plain.Length - offset);
            if (offset + length < plain.Length && char.IsHighSurrogate(plain[offset + length - 1])) length--;
            parts.Add((plain.Substring(offset, length), false));
            offset += length;
        }
        if (parts.Count == 0) throw new ArgumentException("Pushover message has no text.");
        return parts;
    }

    public async Task SendAlert(string pushoverUserKey, string subject, string message, string? alertIds, int? recipientId)
    {
        var token = _config["PushOver:APIToken"];
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(pushoverUserKey))
            throw new InvalidOperationException("Pushover API token and recipient key are required.");

        var parts = SplitMessage(message);
        for (int i = 0; i < parts.Count; i++)
        {
            var title = parts.Count == 1 ? subject : $"{subject} ({i + 1}/{parts.Count})";
            if (title.Length > 250) throw new ArgumentException("Pushover title exceeds 250 characters.");
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["token"] = token, ["user"] = pushoverUserKey, ["title"] = title,
                ["message"] = parts[i].Text, ["html"] = parts[i].Html ? "1" : "0"
            });
            using var response = await _httpClient.PostAsync("https://api.pushover.net/1/messages.json", content);
            var body = await response.Content.ReadAsStringAsync();
            JsonDocument result;
            try { result = JsonDocument.Parse(body); }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Pushover HTTP {(int)response.StatusCode}: invalid JSON response.", ex);
            }
            using (result)
            {
                var root = result.RootElement;
                var request = root.TryGetProperty("request", out var requestValue) ? requestValue.ToString() : "unknown";
                bool accepted = response.IsSuccessStatusCode && root.TryGetProperty("status", out var status)
                    && status.ValueKind == JsonValueKind.Number && status.TryGetInt32(out var code) && code == 1;
                if (!accepted)
                {
                    var errors = root.TryGetProperty("errors", out var errorValue) ? errorValue.ToString() : "No API error details";
                    errors = errors.Replace(token, "[redacted]").Replace(pushoverUserKey, "[redacted]");
                    throw new InvalidOperationException($"Pushover HTTP {(int)response.StatusCode}, request={request}, part={i + 1}/{parts.Count}: {errors}");
                }
                _logger.LogInformation("Pushover accepted: Alert Ids={AlertIds}, Recipient Id={RecipientId}, Part={Part}/{Parts}, HTTP={HttpStatus}, Status=1, Request={RequestId}.",
                    alertIds, recipientId, i + 1, parts.Count, (int)response.StatusCode, request);
            }
        }
    }
}
