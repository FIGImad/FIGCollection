using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models
{
    public partial class PriceDataRemoteRS : IDbEntity<PriceDataRemoteRS>
    {
        public int Id { get; set; }
        public long RawTime { get; set; }
        public Decimal Open { get; set; }
        public Decimal High { get; set; }
        public Decimal Low { get; set; }
        public Decimal Close { get; set; }
        public int Volume { get; set; }

        public PriceDataRemoteRS()
        {
            Id = 0;
            RawTime = 0;
            Open = 0;
            High = 0;
            Low = 0;
            Close = 0;
            Volume = 0;
        }

        public PriceDataRemoteRS(PriceDataRemoteRS rec)
        {
            this.Id = rec.Id;
            this.RawTime = rec.RawTime;
            this.Open = rec.Open;
            this.High = rec.High;
            this.Low = rec.Low;
            this.Close = rec.Close;
            this.Volume = rec.Volume;
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
        }

        public PriceDataRemoteRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new PriceDataRemoteRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                RawTime = (long) SqlReaderUtil.GetInt32(reader, nSeq++),
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
