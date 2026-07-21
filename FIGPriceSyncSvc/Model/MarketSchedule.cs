namespace FIGPriceSyncSvc.Models
{
    /// <summary>
    /// Defines the weekly trading schedule for an exchange.
    /// All times are in the exchange's local timezone (TimeZoneId).
    /// CME Globex example: daily break 17:00–18:00 CT, weekend break Fri 17:00 – Sun 18:00 CT.
    /// </summary>
    public class MarketSchedule
    {
        /// <summary>Windows timezone id for the exchange, e.g. "Central Standard Time" for CME.</summary>
        public string TimeZoneId { get; set; } = "Central Standard Time";

        /// <summary>Day of week the weekly (weekend) break starts, e.g. "Friday".</summary>
        public string WeeklyBreakCloseDay { get; set; } = "Friday";

        /// <summary>Time of day the weekly break starts (HH:mm), e.g. "17:00".</summary>
        public string WeeklyBreakCloseTime { get; set; } = "17:00";

        /// <summary>Day of week the weekly break ends, e.g. "Sunday".</summary>
        public string WeeklyBreakOpenDay { get; set; } = "Sunday";

        /// <summary>Time of day the weekly break ends (HH:mm), e.g. "18:00".</summary>
        public string WeeklyBreakOpenTime { get; set; } = "18:00";

        /// <summary>Time of day the daily maintenance break starts (HH:mm), e.g. "17:00".</summary>
        public string DailyBreakCloseTime { get; set; } = "17:00";

        /// <summary>Time of day the daily maintenance break ends (HH:mm), e.g. "18:00".</summary>
        public string DailyBreakOpenTime { get; set; } = "18:00";

        public MarketSchedule() { }

        public MarketSchedule(MarketSchedule rec)
        {
            TimeZoneId = rec.TimeZoneId;
            WeeklyBreakCloseDay = rec.WeeklyBreakCloseDay;
            WeeklyBreakCloseTime = rec.WeeklyBreakCloseTime;
            WeeklyBreakOpenDay = rec.WeeklyBreakOpenDay;
            WeeklyBreakOpenTime = rec.WeeklyBreakOpenTime;
            DailyBreakCloseTime = rec.DailyBreakCloseTime;
            DailyBreakOpenTime = rec.DailyBreakOpenTime;
        }
    }
}
