using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models
{
    public partial class PriceDataRS : IDbEntity<PriceDataRS>
    {
        public int Id { get; set; }
        public int DataSetId { get; set; }
        public long RawTime { get; set; }
        public DateTime PriceDate { get; set; }
        public Decimal Open { get; set; }
        public Decimal High { get; set; }
        public Decimal Low { get; set; }
        public Decimal Close { get; set; }
        public int Volume { get; set; }

        public PriceDataRS()
        {
            Id = -1;
            DataSetId = -1;
            RawTime = 0;
            PriceDate = new DateTime();
            Open = 0;
            High = 0;
            Low = 0;
            Close = 0;
            Volume = 0;
        }

        public PriceDataRS(PriceDataRS rec)
        {
            this.Id = rec.Id;
            this.DataSetId = rec.DataSetId;
            this.RawTime = rec.RawTime;
            this.PriceDate = rec.PriceDate;
            this.Open = rec.Open;
            this.High = rec.High;
            this.Low = rec.Low;
            this.Close = rec.Close;
            this.Volume = rec.Volume;
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@DataSetId", DataSetId);
            parameters.AddWithValue("@RawTime", RawTime);
            parameters.AddWithValue("@PriceDate", PriceDate);
            parameters.AddWithValue("@Open", Open);
            parameters.AddWithValue("@High", High);
            parameters.AddWithValue("@Low", Low);
            parameters.AddWithValue("@Close", Close);
            parameters.AddWithValue("@Volume", Volume);
        }

        public PriceDataRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            var rec = new PriceDataRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, reader.GetOrdinal("Id")),
                DataSetId = SqlReaderUtil.GetInt32(reader, reader.GetOrdinal("DataSetId")),
                RawTime = Convert.ToInt64(reader["RawTime"]),
                Open = SqlReaderUtil.GetDecimal(reader, reader.GetOrdinal("Open")),
                High = SqlReaderUtil.GetDecimal(reader, reader.GetOrdinal("High")),
                Low = SqlReaderUtil.GetDecimal(reader, reader.GetOrdinal("Low")),
                Close = SqlReaderUtil.GetDecimal(reader, reader.GetOrdinal("Close")),
                Volume = SqlReaderUtil.GetInt32(reader, reader.GetOrdinal("Volume"))
            };
            rec.PriceDate=DateTimeOffset.FromUnixTimeSeconds(rec.RawTime).UtcDateTime;
            return rec;
        }

    }
}
