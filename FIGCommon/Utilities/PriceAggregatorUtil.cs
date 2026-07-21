using FIGCommon.Models;
using System.Data;

namespace FIGCommon.Utilities
{
    public class PriceAggregatorUtil
    {
        public static List<PriceDataRS> AggregatePrices(List<PriceDataRS> prices, int intervalSec)
        {
            int dataSetId = prices.Count > 0 ? prices[0].DataSetId : -1;
            // ensure that the time is UTC time from RawTime
            if (intervalSec >= 86400) // daily
            {
                int numDays = intervalSec / 86400;
                var aggregatedPrices = prices
                    .GroupBy(p =>
                    {
                        // Snap to nearest numMin interval below
                        DateTime original = p.PriceDate;
                        int days = original.Day;
                        int snappedDays= (days / numDays) * numDays;
                        return new DateTime(
                            original.Year,
                            original.Month,
                            snappedDays,
                            0,
                            0,
                            0,
                            DateTimeKind.Utc);
                    })
                    .Select(g => new PriceDataRS
                    {
                        DataSetId = dataSetId,
                        RawTime = DateTimeUtil.ToUnixTime(g.Key),
                        PriceDate = g.Key,     // Using the snapped time as PriceDate
                        Open = g.First().Open,
                        High = g.Max(p => p.High),
                        Low = g.Min(p => p.Low),
                        Close = g.Last().Close,
                        Volume = g.Sum(p => p.Volume)
                    })
                    .OrderBy(p => p.PriceDate)
                    .ToList();
                return aggregatedPrices;
            }
            else if (intervalSec >= 3600) // hourly
            {
                int numHrs = intervalSec / 3600;
                var aggregatedPrices = prices
                    .GroupBy(p =>
                    {
                        // Snap to nearest numMin interval below
                        DateTime original = p.PriceDate;
                        int hours = original.Hour;
                        int snappedHours = (hours / numHrs) * numHrs;
                        return new DateTime(
                            original.Year,
                            original.Month,
                            original.Day,
                            snappedHours,
                            0,
                            0,
                            DateTimeKind.Utc);
                    })
                    .Select(g => new PriceDataRS
                    {
                        DataSetId = dataSetId,
                        RawTime = DateTimeUtil.ToUnixTime(g.Key),
                        PriceDate = g.Key,     // Using the snapped time as PriceDate
                        Open = g.First().Open,
                        High = g.Max(p => p.High),
                        Low = g.Min(p => p.Low),
                        Close = g.Last().Close,
                        Volume = g.Sum(p => p.Volume)
                    })
                    .OrderBy(p => p.PriceDate)
                    .ToList();
                return aggregatedPrices;
            }
            else if (intervalSec >= 60) // minute
            {
                int numMin = intervalSec / 60;
                var aggregatedPrices = prices
                    .GroupBy(p =>
                    {
                        // Snap to nearest numMin interval below
                        DateTime original = p.PriceDate;
                        int minutes = original.Minute;
                        int snappedMinutes = (minutes / numMin) * numMin;
                        return new DateTime(
                            original.Year,
                            original.Month,
                            original.Day,
                            original.Hour,
                            snappedMinutes,
                            0,
                            DateTimeKind.Utc);
                    })
                    .Select(g => new PriceDataRS
                    {
                        DataSetId = dataSetId,
                        RawTime = DateTimeUtil.ToUnixTime(g.Key),
                        PriceDate = g.Key,     // Using the snapped time as PriceDate
                        Open = g.First().Open,
                        High = g.Max(p => p.High),
                        Low = g.Min(p => p.Low),
                        Close = g.Last().Close,
                        Volume = g.Sum(p => p.Volume)
                    })
                    .OrderBy(p => p.PriceDate)
                    .ToList();

                return aggregatedPrices;
            }
            else if (intervalSec < 60) // second
            {
                var aggregatedPrices = prices
                    .GroupBy(p =>
                    {
                        // Snap to nearest numMin interval below
                        DateTime original = p.PriceDate;
                        int seconds = original.Second;
                        int snappedSeconds= (seconds / intervalSec) * intervalSec;
                        return new DateTime(
                            original.Year,
                            original.Month,
                            original.Day,
                            original.Hour,
                            original.Minute,
                            snappedSeconds,
                            DateTimeKind.Utc);
                    })
                    .Select(g => new PriceDataRS
                    {
                        DataSetId = dataSetId,
                        RawTime = DateTimeUtil.ToUnixTime(g.Key),
                        PriceDate = g.Key,     // Using the snapped time as PriceDate
                        Open = g.First().Open,
                        High = g.Max(p => p.High),
                        Low = g.Min(p => p.Low),
                        Close = g.Last().Close,
                        Volume = g.Sum(p => p.Volume)
                    })
                    .OrderBy(p => p.PriceDate)
                    .ToList();
                return aggregatedPrices;
            }
            return new List<PriceDataRS>();   // empty list
        }
    }
}
