namespace FIGCommon.Models.FIGBroker
{
    public class TickerPosDto
    {
        public string AccountId { get; set; } = "";
        public string AccountServiceId { get; set; } = "";
        public int TickerId { get; set; } = 0;
        public int? Position { get; set; } = null;
        public int? PendingPosition { get; set; } = null;
        public decimal? AvgPrice { get; set; } = null;

    }
}
