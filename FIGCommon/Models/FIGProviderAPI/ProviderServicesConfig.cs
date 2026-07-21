
namespace FIGCommon.Models.FIGProviderAPI
{
    public class ProviderServicesConfig
    {
        public string ActiveProvider { get; set; } = "";
        public int NumPriceBarsToUpdate { get; set; } = 1;
        public int NumDaysToCatchUpWhenNoData { get; set; } = 14;
        public List<ProviderOptions> Providers { get; set; } = new();
    }
}

