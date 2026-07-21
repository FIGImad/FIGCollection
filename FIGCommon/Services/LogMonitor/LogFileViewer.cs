using FIGCommon.Models.LogMonitor;
using System.Text;

namespace FIGCommon.Services.LogMonitor
{
    public class LogFileViewer
    {
        private readonly IConfiguration _config;

        public LogFileViewer(
            IConfiguration config
        )
        {
            _config = config;
        }

        public string GetLogFilePath()
        {
            var logPath = _config["LogFile:logPath"] ?? "";
            if (logPath == null || logPath.Length == 0)
            {
                logPath = _config["Serilog:WriteTo:0:Args:path"] ?? "";
            }
            if (logPath.Length > 0)
            {
                return logPath;
            }
            throw new Exception("Logfile is not configured");
        }

        public string GetLogDirectory()
        {
            // Extract log path from Serilog configuration
            var logPath = GetLogFilePath();
            return Path.GetDirectoryName(logPath) ?? "Logs";
        }

        public string GetCurrentLogFilePath()
        {
            var logPath = GetLogFilePath();

            var baseName = Path.GetFileNameWithoutExtension(logPath);
            var extension = Path.GetExtension(logPath);

            // Get the most recent log file based on rolling interval
            var pattern = $"{baseName}*{extension}";
            var directory = GetLogDirectory();

            return Directory.GetFiles(directory, pattern)
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .FirstOrDefault() ?? Path.Combine(directory, $"{baseName}{extension}");
        }

        public IEnumerable<string> GetAllLogFiles()
        {
            try
            {
                var logPath = GetLogFilePath();
                var baseName = Path.GetFileNameWithoutExtension(logPath);
                var pattern = $"{baseName}*{Path.GetExtension(logPath)}";

                return Directory.EnumerateFiles(GetLogDirectory(), pattern)
                              .OrderByDescending(f => new FileInfo(f).LastWriteTime);
            }
            catch (IOException)
            {
                // Handle transient file system errors
                return Enumerable.Empty<string>();
            }
        }
        public async Task<LogContentDto> GetCurrentLogContentAsync(long startPos, long numBytes)
        {
            var filePath = GetCurrentLogFilePath();
            return await ReadLogChunkAsync(filePath, startPos, numBytes).ConfigureAwait(false);
        }

        public async Task<LogContentDto> GetArchivedLogContentAsync(string filename, long startPos, long numBytes)
        {
            var directory = GetLogDirectory();
            var filePath = Path.Combine(directory, filename);

            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified log file was not found");

            return await ReadLogChunkAsync(filePath, startPos, numBytes).ConfigureAwait(false);
        }

        public List<string> GetListOfLogFiles()
        {
            return GetAllLogFiles()
                .Select(f => Path.GetFileName(f))
                .ToList();
        }

        private async Task<LogContentDto> ReadLogChunkAsync(string filePath, long startPos, long numBytes)
        {
            const long maxChunkSize = 1 * 1024 * 1024;
            numBytes = Math.Min(numBytes, maxChunkSize);

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                                         bufferSize: 4096, useAsync: true))
            {
                if (startPos < 0) startPos = 0;
                if (startPos > fs.Length) startPos = Math.Max(0, fs.Length - numBytes);

                long bytesToRead = Math.Min(numBytes, fs.Length - startPos);
                if (bytesToRead <= 0) return new LogContentDto();

                fs.Seek(startPos, SeekOrigin.Begin);
                byte[] buffer = new byte[bytesToRead];
                int bytesRead = await fs.ReadAsync(buffer, 0, (int)bytesToRead);

                return new LogContentDto
                {
                    StartPos = startPos,
                    EndPos = startPos + bytesRead,
                    Content = Encoding.UTF8.GetString(buffer, 0, bytesRead)
                };
            }
        }
    }
}
