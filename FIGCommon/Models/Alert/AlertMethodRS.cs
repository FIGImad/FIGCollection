using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models.Alert
{
    public class AlertMethodRS : IDbEntity<AlertMethodRS>
    {
        public string Method { get; set; }
        public string Description { get; set; }

        public AlertMethodRS()
        {
            this.Method = "";
            this.Description = "";
        }

        public AlertMethodRS(AlertMethodRS rec)
        {
            this.Method = rec.Method;
            this.Description = rec.Description;
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Method", Method);
            parameters.AddWithValue("@Description", Description);
        }

        public AlertMethodRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new AlertMethodRS()
            {
                Method = SqlReaderUtil.GetString(reader, nSeq++),
                Description = SqlReaderUtil.GetString(reader, nSeq++)
            };
            return rec;
        }
    }
}
