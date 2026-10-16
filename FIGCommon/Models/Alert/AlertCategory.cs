namespace FIGCommon.Models.Alert;

public static class AlertCategory
{
    public const string SystemAlertProducer = "FIGAutoTradeExSvc.Services.SystemAlertMonitorService";

    public static bool IsSystemAlert(string? source, string? level) =>
        string.Equals(source, SystemAlertProducer, StringComparison.Ordinal)
        && (string.Equals(level, "ALT", StringComparison.OrdinalIgnoreCase)
            || string.Equals(level, "ALERT", StringComparison.OrdinalIgnoreCase));
}
