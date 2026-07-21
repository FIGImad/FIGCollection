using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models
{
    public partial class DataSetRS : IDbEntity<DataSetRS>
    {
        public int Id { get; set; }
        public int TickerId { get; set; }
        public string IntervalId { get; set; }
        public TickerRS Ticker { get; set; }
        public IntervalRS Interval { get; set; }

        public DataSetRS()
        {
            this.Id = -1;
            this.TickerId = -1;
            this.IntervalId = "";
            Ticker = new TickerRS();
            Interval = new IntervalRS();
        }

        public DataSetRS(DataSetRS rec)
        {
            this.Id = rec.Id;
            this.TickerId = rec.TickerId;
            this.IntervalId = rec.IntervalId;
            this.Ticker = new TickerRS(rec.Ticker);
            this.Interval = new IntervalRS(rec.Interval);
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@TickerId", TickerId);
            parameters.AddWithValue("@IntervalId", IntervalId);
        }

        public DataSetRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new DataSetRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                TickerId = SqlReaderUtil.GetInt32(reader, nSeq++),
                IntervalId = SqlReaderUtil.GetString(reader, nSeq++)
            };
            return rec;
        }

        public bool SameAs(DataSetRS rec)
        {
            return TickerId == rec.TickerId
                && IntervalId == rec.IntervalId;
        }
    }
}
