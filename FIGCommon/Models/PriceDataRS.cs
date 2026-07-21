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
            int nSeq = 0;
            var rec = new PriceDataRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                DataSetId = SqlReaderUtil.GetInt32(reader, nSeq++),
                RawTime = SqlReaderUtil.GetInt32(reader, nSeq++),
                PriceDate = SqlReaderUtil.GetDateTime(reader, nSeq++),
                Open = SqlReaderUtil.GetDecimal(reader, nSeq++),
                High = SqlReaderUtil.GetDecimal(reader, nSeq++),
                Low = SqlReaderUtil.GetDecimal(reader, nSeq++),
                Close = SqlReaderUtil.GetDecimal(reader, nSeq++),
                Volume = SqlReaderUtil.GetInt32(reader, nSeq++)
            };
            return rec;
        }

    }
}
