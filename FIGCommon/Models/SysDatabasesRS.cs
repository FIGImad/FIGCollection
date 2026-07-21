using Microsoft.Data.SqlClient;
using FIGCommon.DataAccess;
using FIGCommon.Utilities;

namespace FIGCommon.Models
{
    public class SysDatabasesRS : IDbEntity<SysDatabasesRS>
    {
        public string Name { get; set; } = "";
        public int Database_id { get; set; } = -1;
        public bool Is_broker_enabled { get; set; } = false;

        public SysDatabasesRS() { }
        public SysDatabasesRS(SysDatabasesRS data)
        {
            Clone(data);
        }

        public void Clone(SysDatabasesRS src)
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
        }

        public SysDatabasesRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new SysDatabasesRS()
            {
                Name = SqlReaderUtil.GetString(reader, nSeq++),
                Database_id = SqlReaderUtil.GetInt32(reader, nSeq++),
                Is_broker_enabled = SqlReaderUtil.GetBoolean(reader, nSeq++)
            };
            return rec;
        }
    }
}
