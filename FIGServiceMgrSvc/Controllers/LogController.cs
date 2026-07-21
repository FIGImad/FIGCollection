using FIGCommon.Models.LogMonitor;
using FIGCommon.Services.LogMonitor;

namespace FIGServiceMgrSvc.Controllers
{
    /// <summary>
    /// Handles all routes under /log:
    ///   GET /log/current/{startpos}/{numbytes}             – content from the current log file
    ///   GET /log/archive/{filename}/{startpos}/{numbytes}  – content from an archived log file
    ///   GET /log/list                                      – list of all log files
    /// </summary>
    internal sealed class LogController : ISignalRController
    {
        private readonly LogFileViewer _logFileViewer;

        public LogController(LogFileViewer logFileViewer)
        {
            _logFileViewer = logFileViewer ?? throw new ArgumentNullException(nameof(logFileViewer));
        }

        public bool CanHandle(string method, string[] segments)
            => segments.Length >= 1 && Is("log", segments[0]);

        public async Task<string?> HandleAsync(string method, string[] segments)
        {
            // GET /log/list
            if (segments.Length == 2 && Is("list", segments[1]) && method == HttpMethod.Get.Method)
            {
                List<string> files = _logFileViewer.GetListOfLogFiles();
                return await SignalRResult.Ok(files);
            }

            // GET /log/current/{startpos}/{numbytes}
            if (segments.Length == 4 && Is("current", segments[1]) && method == HttpMethod.Get.Method)
            {
                if (!long.TryParse(segments[2], out long startPos) || !long.TryParse(segments[3], out long numBytes))
                    return await SignalRResult.Fail(new { Error = "Invalid startpos or numbytes" });

                LogContentDto logContent = await _logFileViewer.GetCurrentLogContentAsync(startPos, numBytes);
                return await SignalRResult.Ok(logContent);
            }

            // GET /log/archive/{filename}/{startpos}/{numbytes}
            if (segments.Length == 5 && Is("archive", segments[1]) && method == HttpMethod.Get.Method)
            {
                string filename = segments[2];
                if (!long.TryParse(segments[3], out long startPos) || !long.TryParse(segments[4], out long numBytes))
                    return await SignalRResult.Fail(new { Error = "Invalid startpos or numbytes" });

                LogContentDto logContent = await _logFileViewer.GetArchivedLogContentAsync(filename, startPos, numBytes);
                return await SignalRResult.Ok(logContent);
            }

            return await SignalRResult.Fail(new { Error = $"Unhandled log route: {string.Join("/", segments)}" });
        }

        private static bool Is(string expected, string segment)
            => segment.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }
}
