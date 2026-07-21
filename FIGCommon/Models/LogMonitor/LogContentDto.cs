namespace FIGCommon.Models.LogMonitor
{
    public class LogContentDto
    {
        public long StartPos { get; set; } = 0;
        public long EndPos { get; set; } = 0;
        public string Content { get; set; } = string.Empty;

    }
}
