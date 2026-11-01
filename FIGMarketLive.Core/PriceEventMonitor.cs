using FIGCommon.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FIGMarketLive;

internal sealed class PriceEventMonitor : EventMonitorService
{
    public PriceEventMonitor(string connectionString,ILogger<EventMonitorService> logger,StudyPriceChanges changes)
        :base(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        {
            ["EventMonitor:ConnectionString"]=new SqlConnectionStringBuilder(connectionString).ConnectionString,
            ["EventMonitor:EventTable"]="Event",["EventMonitor:Mode"]="SqlDependency"
        }).Build(),logger,false)
    {
        Subscribe(PriceEventSetup.EventType,row=>{if(int.TryParse(row?.EventId,out int id))changes.Notify(id);});
    }
}
