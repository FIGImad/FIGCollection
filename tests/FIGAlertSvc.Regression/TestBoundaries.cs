// In-memory boundaries: regression tests never connect to SQL, SMTP or Pushover.
namespace FIGCommon.Models.Alert
{
    public class PendingAlertRS
    {
        public bool IsImmediate { get; set; }
        public int AlertId { get; set; }
        public int RecipientId { get; set; }
        public int AlertMethods { get; set; } = 2;
        public bool RecipientEnabled { get; set; } = true;
        public string Email { get; set; } = "";
        public string? PushoverKey { get; set; } = "test-user";
        public string FriendlyMessage { get; set; } = "test";
        public int RawTime { get; set; }
        public int MSec { get; set; }
        public string TimeZoneId { get; set; } = "UTC";
    }
}
namespace FIGCommon.DataAccess
{
    public static class AlertRepo
    {
        public static List<Models.Alert.PendingAlertRS> Rows = [];
        public static HashSet<int> Sent = [];
        public static int Lookback;
        public static List<Models.Alert.PendingAlertRS> GetPendingAlerts(int lookbackMinutes = 30)
        { Lookback = lookbackMinutes; return Rows.Where(r => !Sent.Contains(r.AlertId)).ToList(); }
        public static List<Models.Alert.PendingAlertRS> GetPendingImmediateAlerts(int maxRec = 100, int lookbackMinutes = 30)
            => GetPendingAlerts(lookbackMinutes);
        public static void MarkAlertSent(int id) => Sent.Add(id);
    }
}
namespace FIGAlertSvc.Services
{
    public class SmtpMessageService
    {
        public Task Send(string address, string subject, string message) => Task.CompletedTask;
    }
}
