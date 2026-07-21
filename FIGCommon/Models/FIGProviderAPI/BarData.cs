
namespace FIGCommon.Models.FIGProviderAPI
{
    public class BarData
    {
        public BarData(long time, double open, double high, double low, double close, decimal volume)
        {
            RawTime = time;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }

        public long RawTime { get; private set; }
        public double Open { get; private set; }
        public double High { get; private set; }
        public double Low { get; private set; }
        public double Close { get; private set; }
        public decimal Volume { get; private set; }
    }
}

