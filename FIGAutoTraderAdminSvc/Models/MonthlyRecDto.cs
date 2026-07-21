namespace FIGAutoTraderAdminSvc.Models
{
    public class MonthlyRecDto
    {
        public int YearMonth { get; set; }
        public List<MonthlyDataDto> strategyData { get; set; }
        public MonthlyDataDto netData { get; set; }

        public MonthlyRecDto()
        {
            strategyData = new();
            netData = new();
        }

        public MonthlyRecDto(MonthlyRecDto data)
        {
            YearMonth = data.YearMonth;
            strategyData = new();
            data.strategyData.ForEach(data => strategyData.Add(new MonthlyDataDto(data)));
            netData = new MonthlyDataDto(data.netData);
        }
    }
}
