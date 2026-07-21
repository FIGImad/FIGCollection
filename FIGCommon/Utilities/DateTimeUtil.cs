using System;

namespace FIGCommon.Utilities
{
    public class DateTimeUtil
    {
        public static DateTime ConvertToDateTime(int yyyymmdd)
        {
            if (yyyymmdd < 19000101)
            {
                return DateTime.MinValue;
            }
            if (yyyymmdd > 21001231)
            {
                return DateTime.MaxValue;
            }
            // Convert the integer to a string
            string dateString = yyyymmdd.ToString();

            // Parse the year, month, and day from the string
            int year = int.Parse(dateString.Substring(0, 4));
            int month = int.Parse(dateString.Substring(4, 2));
            int day = int.Parse(dateString.Substring(6, 2));

            // Create and return a DateTime object
            return new DateTime(year, month, day);
        }

        public static int FormatDate(DateTime date, string format = "yyyyMMdd")
        {
            // Format the DateTime as a string
            string formattedDate = date.ToString(format);

            // Convert to integer
            if (int.TryParse(formattedDate, out int result))
            {
                return result;
            }
            else
            {
                throw new ArgumentException("Invalid format or date");
            }
        }

        public static long ToUnixTime(DateTime dateTime)
        {
            // Define the Unix epoch
            DateTimeOffset unixEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

            // Calculate the total milliseconds since the Unix epoch
            return (long)(dateTime - unixEpoch).TotalSeconds;
        }
        public static long CurrentUTCUnixTime()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            return now.ToUnixTimeSeconds();
        }

        public static long ToUnixTimes(DateTime date, bool convertToUTC)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = (convertToUTC ? date.ToUniversalTime() : date) - origin;
            return (long)Math.Floor(diff.TotalSeconds);
        }
        public static DateTime ConvertUnixTimeToDateTime(long rawTime)
        {

            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(rawTime);
            return dateTimeOffset.UtcDateTime;
        }

    }
}
