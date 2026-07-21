using FIGCommon.Models;

namespace FIGPriceSyncSvc.Model
{
    public class IBKRDataSet
    {
        public DataSetRS Dataset { get; set; }
        public string IBKRInterval { get; set; }
        public long lastSyncTime { get; set; } = 0;

        public IBKRDataSet(DataSetRS dataset, string ibkrInterval)
        {
            Dataset = dataset;
            IBKRInterval = ibkrInterval;
        }
    }
}
