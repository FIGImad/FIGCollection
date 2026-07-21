using Microsoft.Data.SqlClient;
using FIGCommon.DataAccess;
using FIGCommon.Utilities;

namespace FIGCommon.Models
{
    public class EventRS : IDbEntity<EventRS>
    {
        public int Id { get; set; } = -1;
        public string EventType { get; set; } = "";
        public string EventId { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public EventRS() { }
        public EventRS(EventRS data)
        {
            Clone(data);
        }

        public void Clone(EventRS src)
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
            parameters.AddWithValue("@EventType", EventType);
            parameters.AddWithValue("@EventId", EventId);
            parameters.AddWithValue("@Message", Message);
            parameters.AddWithValue("@Timestamp", Timestamp);
        }

        public EventRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new EventRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                EventType = SqlReaderUtil.GetString(reader, nSeq++),
                EventId = SqlReaderUtil.GetString(reader, nSeq++),
                Message = SqlReaderUtil.GetString(reader, nSeq++),
                Timestamp = SqlReaderUtil.GetDateTime(reader, nSeq++)
            };
            return rec;
        }
    }
}
