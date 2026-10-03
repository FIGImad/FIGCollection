using FIGCommon.Models;

namespace FIGPriceDataSvc.PriceSync;

public sealed class RemotePriceSyncOptions
{
    public bool Enabled {get;set;}=true;
    public string DestinationServiceId {get;set;}="";
    public int[] DataSetIds {get;set;}=[];
    public int InitialHistoryDays {get;set;}=14;
    public int OverlapBars {get;set;}=3;
    public int WindowBars {get;set;}=1000;
    public int TimeOffsetSec {get;set;}=5;
    public MarketSchedule MarketSchedule {get;set;}=new()
    {
        WeeklyBreakCloseTime="16:00", WeeklyBreakOpenTime="17:00",
        DailyBreakCloseTime="16:00", DailyBreakOpenTime="17:00"
    };
    public void Validate()
    {
        if(!Enabled) return;
        if(DataSetIds.Any(i=>i<=0)
            || DataSetIds.Distinct().Count()!=DataSetIds.Length
            || InitialHistoryDays is <1 or >200 || OverlapBars is <1 or >100 || WindowBars<=OverlapBars || WindowBars>1000
            || TimeOffsetSec is <0 or >59)
            throw new InvalidOperationException("RemotePriceSync requires unique positive dataset IDs and valid timing/backfill limits.");
        _=MarketScheduleHelper.GetMsUntilNextOpen(MarketSchedule,DateTime.UtcNow);
    }
}
