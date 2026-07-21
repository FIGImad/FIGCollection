using Microsoft.Data.SqlClient;
using FIGCommon.DataAccess;
using FIGCommon.Utilities;

namespace FIGCommon.Models
{
    public class StudyColRS : IDbEntity<StudyColRS>
    {
        public int Id { get; set; } = -1;
        public int TickerId { get; set; } = -1;
        public string IntervalId { get; set; } = "";
        public string ColType { get; set; } = "";
        public int OrderSeq { get; set; } = 0;
        public string ColParams { get; set; } = "{}";
        public bool Enabled { get; set; } = true;

        public TickerRS? Ticker { get; set; }
        public IntervalRS? Interval { get; set; }

        public StudyColRS() { }
        public StudyColRS(StudyColRS data)
        {
            Clone(data);
        }

        public void Clone(StudyColRS src)
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
        }
        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@TickerId", TickerId);
            parameters.AddWithValue("@IntervalId", IntervalId);
            parameters.AddWithValue("@ColType", ColType);
            parameters.AddWithValue("@OrderSeq", OrderSeq);
            parameters.AddWithValue("@ColParams", ColParams);
            parameters.AddWithValue("@Enabled", Enabled);
        }

        public StudyColRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new StudyColRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                TickerId = SqlReaderUtil.GetInt32(reader, nSeq++),
                IntervalId = SqlReaderUtil.GetString(reader, nSeq++),
                ColType = SqlReaderUtil.GetString(reader, nSeq++),
                OrderSeq = SqlReaderUtil.GetInt32(reader, nSeq++),
                ColParams = SqlReaderUtil.GetString(reader, nSeq++),
                Enabled = SqlReaderUtil.GetBoolean(reader, nSeq++)
            };
            return rec;
        }
    }
}
