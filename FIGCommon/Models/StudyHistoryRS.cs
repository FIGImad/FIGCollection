using Microsoft.Data.SqlClient;
using FIGCommon.DataAccess;
using FIGCommon.Utilities;

namespace FIGCommon.Models
{
    public class StudyHistoryRS : IDbEntity<StudyHistoryRS>
    {
        public int Id { get; set; } = -1;
        public int StudyColId { get; set; } = -1;
        public long RawTime { get; set; } = 0;
        public decimal Open { get; set; } = 0;
        public decimal High { get; set; } = 0;
        public decimal Low { get; set; } = 0;
        public decimal Close { get; set; } = 0;
        public int Volume { get; set; } = 0;
        public string Studies { get; set; } = "";

        public Dictionary<string, object?>? rawStudies = null;


        public StudyHistoryRS() { }
        public StudyHistoryRS(StudyHistoryRS data)
        {
            Clone(data);
        }

        public void Clone(StudyHistoryRS src)
        {
            var srcT = src.GetType();
            var dstT = this.GetType();
            foreach (var f in srcT.GetFields())
            {
                var dstF = dstT.GetField(f.Name);
                if (dstF == null || dstF.IsLiteral)
                    continue;
                dstF.SetValue(this, f.GetValue(src));
            }

            foreach (var f in srcT.GetProperties())
            {
                var dstF = dstT.GetProperty(f.Name);
                if (dstF == null)
                    continue;

                dstF.SetValue(this, f.GetValue(src, null), null);
            }

            // clone rawStudies by creating new object
            if (src.rawStudies != null)
            {
                rawStudies = new Dictionary<string, object?>();
                foreach (var item in src.rawStudies)
                {
                    rawStudies.Add(item.Key, item.Value);
        }
            }
        }


        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@StudyColId", StudyColId);
            parameters.AddWithValue("@RawTime", RawTime);
            parameters.AddWithValue("@Open", Open);
            parameters.AddWithValue("@High", High);
            parameters.AddWithValue("@Low", Low);
            parameters.AddWithValue("@Close", Close);
            parameters.AddWithValue("@Volume", Volume);
            parameters.AddWithValue("@Studies", Studies);
        }

        public StudyHistoryRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new StudyHistoryRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                StudyColId = SqlReaderUtil.GetInt32(reader, nSeq++),
                RawTime = (long) SqlReaderUtil.GetInt32(reader, nSeq++),
                Open = SqlReaderUtil.GetDecimal(reader, nSeq++),
                High = SqlReaderUtil.GetDecimal(reader, nSeq++),
                Low = SqlReaderUtil.GetDecimal(reader, nSeq++),
                Close = SqlReaderUtil.GetDecimal(reader, nSeq++),
                Volume = SqlReaderUtil.GetInt32(reader, nSeq++),
                Studies = SqlReaderUtil.GetString(reader, nSeq++)
            };
            return rec;
        }

        public PriceDataRS ToPriceData()
        {
            PriceDataRS priceDataRS = new PriceDataRS();
            priceDataRS.RawTime = this.RawTime;
            priceDataRS.Open = this.Open;
            priceDataRS.High = this.High;
            priceDataRS.Low = this.Low;
            priceDataRS.Close = this.Close;
            priceDataRS.Volume = this.Volume;
            priceDataRS.PriceDate = DateTimeUtil.ConvertUnixTimeToDateTime(this.RawTime);
            return priceDataRS;
        }
    }
}
