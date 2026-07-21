using FIGCommon.Models;

namespace FIG.Studies
{
    public class SourceData
    {
        private static readonly Dictionary<string, Func<PriceDataRS, decimal>> _priceSelectors =
            new Dictionary<string, Func<PriceDataRS, decimal>>(StringComparer.OrdinalIgnoreCase)
            {
                ["open"] = p => p.Open,
                ["high"] = p => p.High,
                ["low"] = p => p.Low,
                ["close"] = p => p.Close,
                ["hl2"] = p => (p.High + p.Low) / 2m,
                ["oc2"] = p => (p.Open + p.Close) / 2m,
                ["hlc3"] = p => (p.High + p.Low + p.Close) / 3m,
                ["ohlc4"] = p => (p.Open + p.High + p.Low + p.Close) / 4m,
                ["hlcc4"] = p => (p.High + p.Low + 2 * p.Close) / 4m
            };


        public SourceData() { }

        static public List<PriceDataRS> GetDataByInterval(List<PriceDataRS> data, string intervalId)
        {
            var dataInterval = new List<PriceDataRS>();
            string intervalType;
            int intervalValue;
            IntervalRS.GetIntervalParams(intervalId, out intervalType, out intervalValue);

            if (intervalValue <= 0 || intervalType == "" || !(intervalType == "DAY" || intervalType == "MIN" || intervalType == "SEC"))
            {
                return dataInterval;
            }
            // check if interval is more than 1
            intervalType = intervalType.ToUpper();
            bool isEOD = intervalType.Equals("DAY");
            bool isMIN = intervalType.Equals("MIN");
            bool isSEC = intervalType.Equals("SEC");

            if (data.Count > 0)
            {
                dataInterval = new List<PriceDataRS>();
                // starts from the begining of the day
                int ndx = 0;
                int dataId = 1;
                //string dateStr = $"{data[0].Date}";
                DateTime dateFrom = data[0].PriceDate;
                if (isMIN)
                {
                    // ensure to start from the beginning of the hour
                    dateFrom = new DateTime(dateFrom.Year, dateFrom.Month, dateFrom.Day, dateFrom.Hour, 0, 0);
                }
                else if (isEOD)
                {
                    // ensure to start from the beginning of the day
                    dateFrom = new DateTime(dateFrom.Year, dateFrom.Month, dateFrom.Day, 0, 0, 0);
                }
                DateTime dateTo =
                    isEOD ? dateFrom.AddDays((double)intervalValue) :
                    isMIN ? dateFrom.AddMinutes((double)intervalValue) :
                    dateFrom.AddSeconds((double)intervalValue);
                PriceDataRS curData = data[ndx];
                PriceDataRS newData = new ();
                newData.Id = dataId++;
                newData.DataSetId = curData.DataSetId;
                DateTime curDate = curData.PriceDate;
                do
                {
                    if (dateFrom < curDate && dateTo <= curDate)
                    {
                        // add previous value (if any)
                        if (newData.Close != 0)
                        {
                            dataInterval.Add(newData);
                            newData = new ();
                        }

                        // increment dateFrom and DateTo and keep marching
                        dateFrom = dateTo;
                        dateTo =
                            isEOD ? dateFrom.AddDays((double)intervalValue) :
                            isMIN ? dateFrom.AddMinutes((double)intervalValue) :
                            dateFrom.AddSeconds((double)intervalValue);
                        continue;
                    }
                    if (newData.Open == 0)
                    {
                        newData.Open = curData.Open;
                        newData.High = curData.High;
                        newData.Low = curData.Low;
                        newData.Close = curData.Close;
                        newData.Id = curData.Id;
                        newData.PriceDate = dateTo;
                        newData.RawTime = SourceData.ConvertToUnixTimestamp(newData.PriceDate, false);  // Price data is already in UTC
                        newData.DataSetId = curData.DataSetId;
                        newData.Volume = curData.Volume;
                    }
                    else
                    {
                        if (curData.High > newData.High)
                        {
                            newData.High = curData.High;
                        }
                        if (curData.Low < newData.Low)
                        {
                            newData.Low = curData.Low;
                        }
                        newData.Close = curData.Close;
                        newData.Volume += curData.Volume;
                    }

                    // increment ndx
                    ndx++;

                    if (ndx >= data.Count)
                    {
                        if (newData.Open != 0)
                        {
                            dataInterval.Add(newData);
                        }
                        break;
                    }

                    curData = data[ndx];
                    curDate = curData.PriceDate;

                } while (true);
            }

            return dataInterval;
        }

        public static long ConvertToUnixTimestamp(DateTime date, bool convertToUTC)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = (convertToUTC ? date.ToUniversalTime() : date) - origin;
            return (long) Math.Floor(diff.TotalSeconds);
        }


        public static List<decimal> GetSourceData(List<PriceDataRS> priceList, string expression)
        {
            var ret = new List<Decimal>();
            priceList.ForEach(data => {
                ret.Add(GetSourceDataItem(data, expression));
            });
            return ret;
        }

        public static decimal GetSourceDataItem(PriceDataRS price, string expression)
        {
            if (_priceSelectors.TryGetValue(expression, out var selector))
            {
                return selector(price);
            }
            return 0.0m;
            
            //Decimal val = 0.0m;
            //if (expression == "open")
            //{
            //    val = price.Open;
            //}
            //else if (expression == "high")
            //{
            //    val = price.High;
            //}
            //else if (expression == "low")
            //{
            //    val = price.Low;
            //}
            //else if (expression == "close")
            //{
            //    val = price.Close;
            //}
            //else if (expression == "hl2")
            //{
            //    val = (price.High + price.Low) / 2m;
            //}
            //else if (expression == "oc2")
            //{
            //    val = (price.Open + price.Close) / 2m;
            //}
            //else if (expression == "hlc3")
            //{
            //    val = (price.High + price.Low + price.Close) / 3m;
            //}
            //else if (expression == "ohlc4")
            //{
            //    val = (price.Open + price.High + price.Low + price.Close) / 4m;
            //}
            //else if (expression == "hlcc4")
            //{
            //    val = (price.High + price.Low + 2 * price.Close) / 4m;
            //}
            //return val;
        }
    }
}


