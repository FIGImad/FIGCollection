using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models.Monitor
{
    public class RecipientMethodRS : IDbEntity<RecipientMethodRS>
    {
        public int RecipientId { get; set; }
        public string AlertMethod { get; set; }
        public string Config { get; set; }

        public RecipientMethodRS()
        {
            this.RecipientId = -1;
            this.AlertMethod = "";
            this.Config = "";
        }

        public RecipientMethodRS(RecipientMethodRS rec)
        {
            this.RecipientId = rec.RecipientId;
            this.AlertMethod = rec.AlertMethod;
            this.Config = rec.Config;
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@RecipientId", RecipientId);
            parameters.AddWithValue("@AlertMethod", AlertMethod);
            parameters.AddWithValue("@Config", Config);
        }

        public RecipientMethodRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new RecipientMethodRS()
            {
                RecipientId = SqlReaderUtil.GetInt32(reader, nSeq++),
                AlertMethod = SqlReaderUtil.GetString(reader, nSeq++),
                Config = SqlReaderUtil.GetString(reader, nSeq++)
            };
            return rec;
        }

    }
}
