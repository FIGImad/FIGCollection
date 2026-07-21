using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models
{
    public partial class IntervalRS : IDbEntity<IntervalRS>
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Seq { get; set; }
        public int IntervalLen { get; set; }

        public IntervalRS()
        {
            this.Id = "";
            this.Name = "";
            this.Seq = 0;
            this.IntervalLen = 0;
        }

        public IntervalRS(IntervalRS rec)
        {
            this.Id = rec.Id;
            this.Name = rec.Name;
            this.Seq = rec.Seq;
            this.IntervalLen = rec.IntervalLen;
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@Name", Name);
            parameters.AddWithValue("@Seq", Seq);
            parameters.AddWithValue("@IntervalLen", IntervalLen);
        }

        public IntervalRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new IntervalRS()
            {
                Id = SqlReaderUtil.GetString(reader, nSeq++),
                Name = SqlReaderUtil.GetString(reader, nSeq++),
                Seq = SqlReaderUtil.GetInt32(reader, nSeq++),
                IntervalLen = SqlReaderUtil.GetInt32(reader, nSeq++)
            };
            return rec;
        }

        static public void GetIntervalParams(string intervalID, out string intervalType, out int intervalValue)
        {
            intervalType = "";
            intervalValue = 0;
            // get last character of the id
            string intervalId = intervalID.Trim().ToUpper();
            char lastChar = intervalId.Length > 0 ? intervalId[intervalId.Length - 1] : '\0';
            switch (lastChar)
            {
                case 'S':
                    {
                        intervalType = "SEC";
                        intervalValue = (int)(TypeConvertUtil.GetIntegerValue(intervalId.Substring(0, intervalId.Length - 1)) ?? 0);

                        break;
                    }
                case 'D':
                    {
                        intervalType = "DAY";
                        intervalValue = (int)(TypeConvertUtil.GetIntegerValue(intervalId.Substring(0, (intervalId.Length - 1))) ?? 0);
                        break;
                    }
                case 'W':
                    {
                        intervalType = "WEEK";
                        intervalValue = (int)(TypeConvertUtil.GetIntegerValue(intervalId.Substring(0, (intervalId.Length - 1))) ?? 0);
                        break;
                    }
                case 'M':
                    {
                        intervalType = "MONTH";
                        intervalValue = (int)(TypeConvertUtil.GetIntegerValue(intervalId.Substring(0, (intervalId.Length - 1))) ?? 0);
                        break;
                    }
                case 'Y':
                    {
                        intervalType = "YEAR";
                        intervalValue = (int)(TypeConvertUtil.GetIntegerValue(intervalId.Substring(0, (intervalId.Length - 1))) ?? 0);
                        break;
                    }
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    {
                        intervalType = "MIN";
                        intervalValue = (int)(TypeConvertUtil.GetIntegerValue(intervalId) ?? 0);
                        break;
                    }
            }

        }

        public void GetIntervalParams(out string intervalType, out int intervalValue)
        {
            GetIntervalParams(this.Id, out intervalType, out intervalValue);
        }
    }
}
