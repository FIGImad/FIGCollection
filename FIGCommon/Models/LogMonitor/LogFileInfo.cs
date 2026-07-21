
namespace FIGCommon.Models.LogMonitor
{
    public class LogFileInfo
    {
        protected string fullPath = "";
        protected string folderPath = "";
        protected string fileName = "";
        protected string tag = "";
        protected long lastFilePos = 0;
        protected long lastRawTime = 0;
        protected int fileId = -1;
        protected int fileDate = -1;
        private readonly object _lock = new();

        public string FullPath { get { lock (_lock) return fullPath; } set { lock (_lock) fullPath = value; } }
        public string FolderPath { get { lock (_lock) return folderPath; } set { lock (_lock) folderPath = value; } }
        public string FileName { get { lock (_lock) return fileName; } set { lock (_lock) fileName = value; } }
        public string Tag { get { lock (_lock) return tag; } set { lock (_lock) tag = value; } }
        public long LastFilePos { get { lock (_lock) return lastFilePos; } set { lock (_lock) lastFilePos = value; } }
        public long LastRawTime { get { lock (_lock) return lastRawTime; } set { lock (_lock) lastRawTime = value; } }
        public int FileId { get { lock (_lock) return fileId; } set { lock (_lock) fileId = value; } }
        public int FileDate { get { lock (_lock) return fileDate; } set { lock (_lock) fileDate = value; } }

        public LogFileInfo(string fullPath)
        {
            this.fullPath = fullPath;
            fileName = "";
            folderPath = "";
            tag = "";
            lastFilePos = 0;
            lastRawTime = 0;
            fileId = -1;
            fileDate = 0;
            if (!string.IsNullOrWhiteSpace(fullPath))
            {
                try
                {
                    string? folderPath = Path.GetDirectoryName(fullPath);
                    string? fileName = Path.GetFileName(fullPath);
                    if (!string.IsNullOrWhiteSpace(folderPath))
                    {
                        FolderPath = folderPath;
                    }
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        FileName = fileName;
                    }
                }
                catch { }
            }
        }

    }
}
