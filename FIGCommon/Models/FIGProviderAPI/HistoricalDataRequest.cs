namespace FIGCommon.Models.FIGProviderAPI
{
    public class HistoricalDataRequest
    {
        public TickerInfo Ticker { get; set; } = new ();
        public string IntervalId { get; set; } = "1D";
        public string Duration { get; set; } = "1M";
        public int NumBars { get; set; } = 0;
        public long StartRawTime { get; set; } = 0;
    }
}
