namespace FIGPriceSyncSvc.Models
{
    /// <summary>
    /// Evaluates whether the market is currently open and computes the delay in
    /// milliseconds until it next opens, based on a <see cref="MarketSchedule"/>.
    /// </summary>
    public static class MarketScheduleHelper
    {
        /// <summary>
        /// Returns true when the market is open at <paramref name="utcNow"/>.
        /// </summary>
        public static bool IsMarketOpen(MarketSchedule schedule, DateTime utcNow)
        {
            var local = ToLocal(schedule, utcNow);
            return !IsInBreak(schedule, local);
        }

        /// <summary>
        /// Returns the number of milliseconds from <paramref name="utcNow"/> until the
        /// next market open.  Returns 0 when the market is already open.
        /// </summary>
        public static int GetMsUntilNextOpen(MarketSchedule schedule, DateTime utcNow)
        {
            var local = ToLocal(schedule, utcNow);
            if (!IsInBreak(schedule, local))
                return 0;

            DateTime nextOpen = ComputeNextOpen(schedule, local);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZoneId);
            DateTime nextOpenUtc = TimeZoneInfo.ConvertTimeToUtc(nextOpen, tz);

            int ms = (int)Math.Ceiling((nextOpenUtc - utcNow).TotalMilliseconds);
            return ms < 0 ? 0 : ms;
        }

        // ── helpers ──────────────────────────────────────────────────────────────

        private static DateTime ToLocal(MarketSchedule schedule, DateTime utcNow)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
        }

        private static bool IsInBreak(MarketSchedule schedule, DateTime local)
        {
            // Check weekend/weekly break first
            var weeklyCloseDay = Enum.Parse<DayOfWeek>(schedule.WeeklyBreakCloseDay);
            var weeklyOpenDay = Enum.Parse<DayOfWeek>(schedule.WeeklyBreakOpenDay);
            var weeklyCloseTime = TimeSpan.Parse(schedule.WeeklyBreakCloseTime);
            var weeklyOpenTime = TimeSpan.Parse(schedule.WeeklyBreakOpenTime);

            // Within the weekend window: Fri 17:00 through Sun 18:00
            if (IsInWeeklyBreak(local, weeklyCloseDay, weeklyCloseTime, weeklyOpenDay, weeklyOpenTime))
                return true;

            // Daily maintenance break (same HH:mm every non-weekend day)
            var dailyClose = TimeSpan.Parse(schedule.DailyBreakCloseTime);
            var dailyOpen = TimeSpan.Parse(schedule.DailyBreakOpenTime);
            if (IsInDailyBreak(local.TimeOfDay, dailyClose, dailyOpen))
                return true;

            return false;
        }

        private static bool IsInWeeklyBreak(DateTime local,
            DayOfWeek closeDay, TimeSpan closeTime,
            DayOfWeek openDay, TimeSpan openTime)
        {
            // Convert day+time to "minutes since Sunday 00:00" for a linear comparison.
            // Sunday = 0, so Fri 17:00 = 5*1440+1020 = 8220, Sun 18:00 = 0*1440+1080 = 1080.
            // When closeMin > openMin the break wraps across the Sun=0 boundary (e.g. Fri→Sun),
            // so "in break" means: nowMin >= closeMin  OR  nowMin < openMin.
            static int WeekMinutes(DayOfWeek d, TimeSpan t) => (int)d * 1440 + (int)t.TotalMinutes;

            int closeMin = WeekMinutes(closeDay, closeTime);
            int openMin  = WeekMinutes(openDay,  openTime);
            int nowMin   = WeekMinutes(local.DayOfWeek, local.TimeOfDay);

            return closeMin > openMin
                ? nowMin >= closeMin || nowMin < openMin   // wraps across Sun boundary
                : nowMin >= closeMin && nowMin < openMin;  // contained within same week
        }

        private static bool IsInDailyBreak(TimeSpan timeOfDay, TimeSpan closeTime, TimeSpan openTime)
        {
            if (closeTime < openTime)
            {
                // break stays within the same calendar day (unusual)
                return timeOfDay >= closeTime && timeOfDay < openTime;
            }
            else
            {
                // break crosses midnight: e.g. 17:00 → 18:00 next day
                return timeOfDay >= closeTime || timeOfDay < openTime;
            }
        }

        private static DateTime ComputeNextOpen(MarketSchedule schedule, DateTime local)
        {
            var weeklyCloseDay = Enum.Parse<DayOfWeek>(schedule.WeeklyBreakCloseDay);
            var weeklyOpenDay = Enum.Parse<DayOfWeek>(schedule.WeeklyBreakOpenDay);
            var weeklyCloseTime = TimeSpan.Parse(schedule.WeeklyBreakCloseTime);
            var weeklyOpenTime = TimeSpan.Parse(schedule.WeeklyBreakOpenTime);

            if (IsInWeeklyBreak(local, weeklyCloseDay, weeklyCloseTime, weeklyOpenDay, weeklyOpenTime))
            {
                // Advance to the next WeeklyBreakOpenDay at WeeklyBreakOpenTime
                int daysAhead = ((int)weeklyOpenDay - (int)local.DayOfWeek + 7) % 7;
                if (daysAhead == 0 && local.TimeOfDay >= weeklyOpenTime)
                    daysAhead = 7;
                return local.Date.AddDays(daysAhead) + weeklyOpenTime;
            }

            // Must be in daily break — advance to same day's open time (may be next calendar day)
            var dailyOpen = TimeSpan.Parse(schedule.DailyBreakOpenTime);
            DateTime candidate = local.Date + dailyOpen;
            if (candidate <= local)
                candidate = candidate.AddDays(1);
            return candidate;
        }
    }
}
