using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models.Alert
{
    public class AlertTypeRS : IDbEntity<AlertTypeRS>
    {
        public string Type { get; set; }
        public string Description { get; set; }

        public AlertTypeRS()
        {
            this.Type = "";
            this.Description = "";
        }

        public AlertTypeRS(AlertTypeRS rec)
        {
            this.Type = rec.Type;
            this.Description = rec.Description;
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Type", Type);
            parameters.AddWithValue("@Description", Description);
        }

        public AlertTypeRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new AlertTypeRS()
            {
                Type = SqlReaderUtil.GetString(reader, nSeq++),
                Description = SqlReaderUtil.GetString(reader, nSeq++)
            };
            return rec;
        }
    }
}
