namespace FIGAlertSvc.Services
{
    internal static class SystemAlertSourceResolver
    {
        internal static string Resolve(string loggerSource, string message)
        {
            // SystemAlertMonitorService wraps the database source in its log message.
            // Only unwrap that known producer; ordinary log messages retain their source.
            if (loggerSource != "FIGAutoTradeExSvc.Services.SystemAlertMonitorService")
                return loggerSource;

            var header = message.AsSpan();
            if (header.StartsWith("Alert: ", StringComparison.Ordinal))
                header = header[7..];
            if (!header.StartsWith("Source: ", StringComparison.Ordinal))
                return loggerSource;

            header = header[8..];
            int delimiter = header.IndexOf(",", StringComparison.Ordinal);
            if (delimiter < 0) return loggerSource;
            var source = header[..delimiter].Trim();
            if (source.Length == 0 || source.Length > 50 || source.Contains('\r') || source.Contains('\n'))
                return loggerSource;

            return source.ToString();
        }
    }
}
