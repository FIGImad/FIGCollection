using Microsoft.Data.SqlClient;
using FIGCommon.DataAccess;
using FIGCommon.Utilities;

namespace FIGCommon.Models
{
    public class StatusCodeRS : IDbEntity<StatusCodeRS>
    {
        public int Code { get; set; } = -1;
        public string Status { get; set; } = "";
        public string Desc { get; set; } = "";
        

        public StatusCodeRS() { }
        public StatusCodeRS(StatusCodeRS data)
        {
            Clone(data);
        }

        public void Clone(StatusCodeRS src)
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
            parameters.AddWithValue("@Code", Code);
            parameters.AddWithValue("@Status", Status);
            parameters.AddWithValue("@Desc", Desc);
        }

        public StatusCodeRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new StatusCodeRS()
            {
                Code = SqlReaderUtil.GetInt32(reader, nSeq++),
                Status = SqlReaderUtil.GetString(reader, nSeq++),
                Desc = SqlReaderUtil.GetString(reader, nSeq++)
            };
            return rec;
        }
    }
}
