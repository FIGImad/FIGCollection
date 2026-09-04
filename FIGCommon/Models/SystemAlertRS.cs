using Microsoft.Data.SqlClient;
using FIGCommon.DataAccess;
using FIGCommon.Utilities;

namespace FIGCommon.Models
{
    public class SystemAlertRS : IDbEntity<SystemAlertRS>
    {
        public int Id { get; set; } = -1;
        public long RawTime { get; set; } = 0;
        public int MSec { get; set; } = 0;
        public string Source { get; set; } = "";
        public string Level { get; set; } = "";
        public string Message { get; set; } = "";
        public long DeliveryTime { get; set; } = 0;

        public SystemAlertRS() { }
        public SystemAlertRS(SystemAlertRS data)
        {
            Clone(data);
        }

        public void Clone(SystemAlertRS src)
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
            parameters.AddWithValue("@RawTime", RawTime);
            parameters.AddWithValue("@MSec", MSec);
            parameters.AddWithValue("@Source", Source);
            parameters.AddWithValue("@Level", Level);
            parameters.AddWithValue("@Message", Message);
            parameters.AddWithValue("@DeliveryTime", DeliveryTime);
        }

        public SystemAlertRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new SystemAlertRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                RawTime = (long) SqlReaderUtil.GetInt32(reader, nSeq++),
                MSec = SqlReaderUtil.GetInt32(reader, nSeq++),
                Source = SqlReaderUtil.GetString(reader, nSeq++),
                Level = SqlReaderUtil.GetString(reader, nSeq++),
                Message = SqlReaderUtil.GetString(reader, nSeq++),
                DeliveryTime = (long) SqlReaderUtil.GetInt32(reader, nSeq++)
            };
            return rec;
        }
    }
}

