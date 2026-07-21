
namespace FIGCommon.Models.LogMonitor
{
    public class LogLineFormat
    {
        public string Format { get; set; } = "";
        public string TimeFormat { get; set; } = "";
        public Dictionary<string, int> FormatIndex { get; set; } = new();
        public bool Status { get; set; } = false;

        public LogLineFormat(string format)
        {
            this.Format = format;
            try
            {
                var parts = format.Split(';');
                for (int i = 0; i < parts.Length; i++)
                {
                    // trim '{' from the start of the string parts[i] and '}' from the end of the string parts[i]
                    parts[i] = parts[i].TrimStart('{');
                    parts[i] = parts[i].TrimEnd('}');
                    parts[i] = parts[i].TrimStart('@');
                    parts[i] = parts[i].Trim(' ');
                    if (parts[i].StartsWith("time"))
                    {
                        // remove the "time" from the string parts[i]
                        parts[i] = parts[i].Substring("time".Length);
                        parts[i] = parts[i].TrimStart(':');  // trim deimiter if exists
                        // trim all spaces
                        TimeFormat = parts[i].Trim();
                        FormatIndex.Add("time", i);
                    }
                    else
                    {
                        FormatIndex.Add(parts[i], i);
                    }
                }
                Status = true;
            }
            catch
            {
                TimeFormat = "";
                Status = false;
            }
        }

    }
}
