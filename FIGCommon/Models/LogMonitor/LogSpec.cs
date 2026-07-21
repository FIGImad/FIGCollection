using System.Linq.Expressions;

namespace FIGCommon.Models.LogMonitor
{
    public class LogSpec
    {
        public string LogName { get; set; } = "";
        public string FolderPath { get; set; } = "";
        public string FileFilter { get; set; } = "";
        public string Format { get; set; } = "";
        public string Expression { get; set; } = "";
        public string LastLogFile { get; set; } = "";
        public long LastPosition { get; set; } = 0;
        public string MinLevel { get; set; } = LogLevel.NONE;
        public LogLineFormat LineFormat { get; set; } = new LogLineFormat("");

        public LogSpec(string logName, string path, string format, string expression, string minLevel)
        {
            LastLogFile = "";
            LastPosition = 0;
            LogName = logName;
            FolderPath = path;
            FileFilter = "*.*";
            Format = format;
            this.Expression = expression;
            MinLevel = minLevel;
            LineFormat = new LogLineFormat(format);
            if (!string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    string? folderPath = Path.GetDirectoryName(path);
                    string? fileFilter = Path.GetFileName(path);
                    if (!string.IsNullOrWhiteSpace(folderPath))
                    {
                        FolderPath = folderPath;
                    }
                    if (!string.IsNullOrWhiteSpace(fileFilter))
                    {
                        FileFilter = fileFilter.Replace("-.txt", "-*.txt");
                    }
                }
                catch { }
            }
        }

    }
}
