namespace FIGCommon.Models.LogMonitor
{
    /// <summary>
    /// Represents an alert raised from a monitored log file entry.
    /// </summary>
    public class LogAlertMessage
    {
        /// <summary>Name of the log source (e.g. "ATS", "Interface").</summary>
        public string LogName { get; set; } = "";

        /// <summary>Machine / device that produced the log entry.</summary>
        public string ServiceId { get; set; } = "";

        /// <summary>Class or component that emitted the log entry.</summary>
        public string Source { get; set; } = "";

        /// <summary>Severity level (ERR, WRN, FTL …).</summary>
        public string Level { get; set; } = "";

        /// <summary>The raw log message text.</summary>
        public string Message { get; set; } = "";

        /// <summary>The exception text, if any, associated with the log entry.</summary>
        public string ExceptionText { get; set; } = "";

        /// <summary>UTC EPOCH rawtime of the original log entry.</summary>
        public long RawTime { get; set; } = 0;
        /// <summary>Milliseconds part of the original log entry timestamp.</summary>
        public int Milliseconds { get; set; } = 0;
    }
}
