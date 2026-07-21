namespace FIGCommon.Models.LogMonitor
{
    public static class LogLevel
    {
        public const string NONE = "";
        public const string DBG = "DBG";
        public const string ERR = "ERR";
        public const string INF = "INF";
        public const string WRN = "WRN";
        public const string FTL = "FTL";
    }


    public class LogRec
    {
        public long RawTime { get; set; } = 0;
        public int MSec { get; set; } = 0;
        public string Level { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public int ThreadId { get; set; } = -1;
        public string Source { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime TimeStamp { get; set; } = new();

        public LogRec() { }
        public LogRec(LogRec data)
        {
            Clone(data);
        }

        public void Clone(LogRec src)
        {
            var srcT = src.GetType();
            var dstT = this.GetType();
            foreach (var f in srcT.GetFields())
            {
                var dstF = dstT.GetField(f.Name);
                if (dstF == null || dstF.IsLiteral)
                    continue;
                dstF.SetValue(this, f.GetValue(src));
            }

            foreach (var f in srcT.GetProperties())
            {
                var dstF = dstT.GetProperty(f.Name);
                if (dstF == null)
                    continue;

                dstF.SetValue(this, f.GetValue(src, null), null);
            }
        }
    }
}
