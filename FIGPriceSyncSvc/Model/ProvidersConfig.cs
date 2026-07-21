namespace FIGPriceSyncSvc.Models
{
    public class ProvidersConfig
    {
        public string ActiveProvider { get; set; } = string.Empty;
        public int NumPriceBarsToUpdate { get; set; } = 3;
        public int NumDaysToCatchUpWhenNoData { get; set; } = 0;
        public int TimeOffsetSec { get; set; } = 0;
        public List<ProviderConfig> Providers { get; set; } = new();
        public MarketSchedule MarketSchedule { get; set; } = new();

        public ProvidersConfig()
        {
            this.ActiveProvider = "";
            this.NumPriceBarsToUpdate = 3;
            this.NumDaysToCatchUpWhenNoData = 3;
            this.TimeOffsetSec = 3;
            this.Providers = new();
            this.MarketSchedule = new();
        }

        public ProvidersConfig(ProvidersConfig rec)
        {
            this.ActiveProvider = rec.ActiveProvider;
            this.NumPriceBarsToUpdate = rec.NumPriceBarsToUpdate;
            this.NumDaysToCatchUpWhenNoData = rec.NumDaysToCatchUpWhenNoData;
            this.TimeOffsetSec = rec.TimeOffsetSec;
            this.Providers = new();
            this.MarketSchedule = new MarketSchedule(rec.MarketSchedule);

            this.Providers = rec.Providers.Select(item => item.Clone()).ToList();
        }

    }
}
