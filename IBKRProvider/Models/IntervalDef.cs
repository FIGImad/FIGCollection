namespace IBKRProvider.Models
{
    public class IntervalDef
    {
        public static string GetInterval(string interval)
        {
            switch (interval.ToUpper().Trim())
            {
                case "1": { return "1 min"; }
                case "10": { return "10 mins"; }
                case "10D": { return "10 days"; }
                case "10S": { return "10 secs"; }
                case "120": { return "2 hrs"; }
                case "15": { return "15 mins"; }
                case "15S": { return "15 secs"; }
                case "1S": { return "1 secs"; }
                case "2": { return "2 mins"; }
                case "20": { return "20 mins"; }
                case "20S": { return "20 secs"; }
                case "240": { return "4 hrs"; }
                case "3": { return "3 mins"; }
                case "30": { return "30 mins"; }
                case "30S": { return "30 secs"; }
                case "5": { return "5 mins"; }
                case "5S": { return "5 secs"; }
                case "60": { return "1 hrs"; }
                case "D": { return "1 days"; }
                case "M": { return "1 months"; }
                case "W": { return "1 weeks"; }
                default: { return ""; }
            }
        }
        public static int GetIntervalLen(string interval)
        {
            switch (interval.ToUpper().Trim())
            {
                case "1": { return 60; }
                case "10": { return 600; }
                case "10D": { return 864000; }
                case "10S": { return 10; }
                case "120": { return 7200; }
                case "15": { return 900; }
                case "15S": { return 15; }
                case "1S": { return 1; }
                case "2": { return 120; }
                case "20": { return 1200; }
                case "20S": { return 20; }
                case "240": { return 14400; }
                case "3": { return 180; }
                case "30": { return 1800; }
                case "30S": { return 30; }
                case "5": { return 300; }
                case "5S": { return 5; }
                case "60": { return 3600; }
                case "D": { return 86400; }
                case "M": { return 2592000; }
                case "W": { return 604800; }
                default: { return 0; }
            }
        }

    }
}