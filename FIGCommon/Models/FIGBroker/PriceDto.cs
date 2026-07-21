namespace FIGCommon.Models.FIGBroker
{
    public partial class PriceDto
    {
        public int Id { get; set; }
        public string Ticker { get; set; }
        public string Interval { get; set; }
        public long RawTime { get; set; }
        public Decimal Open { get; set; }
        public Decimal High { get; set; }
        public Decimal Low { get; set; }
        public Decimal Close { get; set; }
        public int Volume { get; set; }

        public PriceDto()
        {
            Id = -1;
            Ticker = string.Empty;
            Interval = string.Empty;
            RawTime = 0;
            Open = 0;
            High = 0;
            Low = 0;
            Close = 0;
            Volume = 0;
        }

        public PriceDto(PriceDto rec)
        {
            this.Id = rec.Id;
            this.Ticker = rec.Ticker;
            this.Interval = rec.Interval;
            this.RawTime = rec.RawTime;
            this.Open = rec.Open;
            this.High = rec.High;
            this.Low = rec.Low;
            this.Close = rec.Close;
            this.Volume = rec.Volume;
        }
    }
}
