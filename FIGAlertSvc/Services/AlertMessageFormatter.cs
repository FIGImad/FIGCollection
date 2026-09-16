using System.Globalization;
using System.Text.RegularExpressions;
using FIGCommon.Models.Alert;

namespace FIGAlertSvc.Services;

internal static class AlertMessageFormatter
{
    // The SystemAlert producer puts its UTC event time in this header. The log
    // record's own time can be much later when old SystemAlert rows are replayed.
    private static readonly Regex HeaderTime = new(
        @"(?<prefix>Source: [^,\r\n]+, Time: )(?<time>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3})",
        RegexOptions.CultureInvariant);

    internal static DateTimeOffset? OriginalTime(string source, string message)
    {
        if (source != "FIGAutoTradeExSvc.Services.SystemAlertMonitorService") return null;
        var match = HeaderTime.Match(message);
        return match.Success && DateTimeOffset.TryParseExact(match.Groups["time"].Value,
            "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var time) ? time : null;
    }

    internal static string Format(PendingAlertRS row)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(row.TimeZoneId);
        var time = TimeZoneInfo.ConvertTime(
            DateTimeOffset.FromUnixTimeSeconds(row.RawTime).AddMilliseconds(row.MSec), zone);
        var formatted = time.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture)
            + " (" + zone.Id + ")";
        var message = HeaderTime.Replace(row.FriendlyMessage, m => m.Groups["prefix"].Value + formatted, 1);
        return message.Replace("{{alert_time}}", formatted, StringComparison.OrdinalIgnoreCase);
    }
}
