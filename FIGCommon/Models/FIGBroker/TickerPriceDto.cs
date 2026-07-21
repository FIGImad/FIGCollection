using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models.FIGBroker
{
    public partial class TickerPriceDto
    {
        public int TickerId { get; set; }
        public long RawTime { get; set; }
        public Decimal Open { get; set; }
        public Decimal High { get; set; }
        public Decimal Low { get; set; }
        public Decimal Close { get; set; }
        public int Volume { get; set; }

        public TickerPriceDto()
        {
            TickerId = -1;
            RawTime = 0;
            Open = 0;
            High = 0;
            Low = 0;
            Close = 0;
            Volume = 0;
        }

        public TickerPriceDto(TickerPriceDto rec)
        {
            this.TickerId = rec.TickerId;
            this.RawTime = rec.RawTime;
            this.Open = rec.Open;
            this.High = rec.High;
            this.Low = rec.Low;
            this.Close = rec.Close;
            this.Volume = rec.Volume;
        }
    }
}
