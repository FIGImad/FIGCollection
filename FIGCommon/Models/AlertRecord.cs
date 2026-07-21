namespace FIGCommon.Models
{
    public static class AlertGroups
    {
        public const string NOT_ASSIGNED = "";
        public const string TRADE = "Trade Signal";
    }


    public class AlertRecord
    {
        public string Group { get; set; } = AlertGroups.NOT_ASSIGNED;
        public string Alert { get; set; } = "";
        public string Detail { get; set; } = "";
    }
}
