namespace FIGCommon.Models.FIGProviderAPI
{
    public class InstrumentLookupRequest
    {
        public string Symbol { get; set; } = "";
        public string SecType { get; set; } = "";
        public string Exchange { get; set; } = "";
        public string Currency { get; set; } = "";
        public string LastTradeDateOrContractMonth { get; set; } = "";
    }
}
