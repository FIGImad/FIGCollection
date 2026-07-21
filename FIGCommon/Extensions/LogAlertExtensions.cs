using Serilog.Context;

namespace FIGCommon.Extensions
{
    /// <summary>
    /// Adds an <c>Alert</c> logging method to <see cref="ILogger"/> that writes at
    /// <see cref="Microsoft.Extensions.Logging.LogLevel.Warning"/> level but stamps
    /// every event with <c>AlertType = "ALT"</c> so the Serilog expression template
    /// renders the token as <c>ALT</c> instead of <c>WRN</c>, and
    /// <see cref="FIGCommon.Services.LogMonitor.LogAnalyzerSink"/> can forward it
    /// to the host controller independently of ordinary warnings.
    /// </summary>
    public static class LogAlertExtensions
    {
        public static void Alert(this ILogger logger, string messageTemplate, params object?[] args)
        {
            using (LogContext.PushProperty("AlertType", "ALT"))
                logger.LogWarning(messageTemplate, args);
        }

        public static void Alert(this ILogger logger, Exception ex, string messageTemplate, params object?[] args)
        {
            using (LogContext.PushProperty("AlertType", "ALT"))
                logger.LogWarning(ex, messageTemplate, args);
        }
    }
}
