namespace FIGCommon.Models;

public static class PriceSyncTiming
{
    public static DateTimeOffset NextFetch(DateTimeOffset now, int intervalSeconds, int timeOffsetSeconds)
    {
        var intervalMs=checked(intervalSeconds*1000L);
        var leadMs=checked(timeOffsetSeconds*1000L+500);
        if(intervalMs<=0 || leadMs<0 || leadMs>=intervalMs)
            throw new ArgumentOutOfRangeException(nameof(timeOffsetSeconds));
        var nowMs=now.ToUnixTimeMilliseconds();
        var fetch=(nowMs/intervalMs+1)*intervalMs-leadMs;
        if(fetch<=nowMs) fetch+=intervalMs;
        return DateTimeOffset.FromUnixTimeMilliseconds(fetch);
    }
    public static DateTimeOffset CorrectionAfter(DateTimeOffset fetch,int timeOffsetSeconds)
        => fetch.AddMilliseconds(timeOffsetSeconds*1000L+500);
}
