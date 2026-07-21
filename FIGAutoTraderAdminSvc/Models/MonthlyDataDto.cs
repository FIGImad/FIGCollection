namespace FIGAutoTraderAdminSvc.Models
{
    public class MonthlyDataDto
    {
        public string Strategy { get; set; } = "";
        public int YearMonth { get; set; } = 0;
        public decimal Side { get; set; } = 0;
        public int NumBars { get; set; } = 0;
        public int NumTrades { get; set; } = 0;
        public decimal ProfitLoss { get; set; } = 0;

        public MonthlyDataDto() { }
        public MonthlyDataDto(MonthlyDataDto data)
        {
            Strategy = data.Strategy;
            YearMonth = data.YearMonth;
            Side = data.Side;
            NumBars = data.NumBars;
            NumTrades = data.NumTrades;
            ProfitLoss = data.ProfitLoss;
        }
    }
}
