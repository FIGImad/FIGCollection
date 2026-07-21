namespace FIGCommon.Models.FIGProviderAPI
{
    public class PositionUpdate
    {
        public string AccountId { get; set; } = string.Empty;  // PermId or broker trade id
        public TickerInfo? Ticker { get; set; } = null;  // broker orderId
        public decimal Pos { get; set; } = 0;
        public decimal PendingPos { get; set; } = 0;
        public double AvgCost { get; set; } = 0;
        public long UpdateTime { get; set; } = 0;

        public PositionUpdate()
        {
            AccountId = string.Empty;
            Ticker = null;
            Pos = 0;
            PendingPos = 0;
            AvgCost = 0;
            UpdateTime = 0;
        }

        public PositionUpdate(PositionUpdate other)
        {
            AccountId = other.AccountId;
            Ticker = other.Ticker != null ? new TickerInfo(other.Ticker) : null;
            Pos = other.Pos;
            PendingPos = other.PendingPos;
            AvgCost = other.AvgCost;
            UpdateTime = other.UpdateTime;
        }
    }
}
