namespace FIGAutoTraderAdminSvc.Models
{
    public class MarketDataDto
    {
        public int TickerId { get; set; } = -1;
        public decimal Price { get; set; } = 0;
        public int Volume { get; set; } = 0;
        public long DataTime { get; set; } = 0;

        public MarketDataDto()
        {
            TickerId = -1;
            Price = 0;
            Volume = 0;
            DataTime = 0;
        }

        public MarketDataDto(MarketDataDto other)
        {
            TickerId = other.TickerId;
            Price = other.Price;
            Volume = other.Volume;
            DataTime = other.DataTime;
        }
    }
}
