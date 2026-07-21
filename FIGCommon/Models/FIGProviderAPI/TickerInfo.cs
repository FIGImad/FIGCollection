namespace FIGCommon.Models.FIGProviderAPI
{
    public class TickerInfo
    {
        public string Symbol { get; set; } = "";
        public string SecType { get; set; } = "";
        public string Currency { get; set; } = "";
        public string Exchange { get; set; } = "";
        public int ContractMonth { get; set; } = 0;

        public TickerInfo()
        {
            Symbol = "";
            SecType = "";
            Currency = "";
            Exchange = "";
            ContractMonth = 0;
        }

        public TickerInfo(TickerInfo tickerInfo)
        {
            Symbol = tickerInfo.Symbol;
            SecType = tickerInfo.SecType;
            Currency = tickerInfo.Currency;
            Exchange = tickerInfo.Exchange;
            ContractMonth = tickerInfo.ContractMonth;
        }

        public string GetTickerKey()
        {
            if (SecType.Equals("FUT", StringComparison.OrdinalIgnoreCase))
            {
                if (ContractMonth > 999999)
                {
                    return $"{Symbol}:{SecType}:{ContractMonth / 100}";
                }
                return $"{Symbol}:{SecType}:{ContractMonth / 100}";
            }
            return $"{Symbol}:{SecType}";
        }
    }
}
