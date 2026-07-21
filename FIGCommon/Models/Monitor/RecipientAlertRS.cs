using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models.Monitor
{
    public class RecipientAlertRS : IDbEntity<RecipientAlertRS>
    {
        public int Id { get; set; }
        public int RecipientId { get; set; }
        public string Label { get; set; }
        public string AlertType { get; set; }
        public string AlertGroup { get; set; }
        public string Rules{ get; set; }

        //public AlertRule AlertMethods { get; set; }
        //public List<AlertRS> AlertRS { get; set; }

        public RecipientAlertRS()
        {
            this.Id = -1;
            this.RecipientId = -1;
            this.Label = "";
            this.AlertType = "";
            this.AlertGroup = "";
            this.Rules = "";
        }

        public RecipientAlertRS(RecipientAlertRS rec)
        {
            this.Id = rec.Id;
            this.RecipientId = rec.RecipientId;
            this.Label = rec.Label;
            this.AlertType = rec.AlertType;
            this.AlertGroup = rec.AlertGroup;
            this.Rules = rec.Rules;
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@RecipientId", RecipientId);
            parameters.AddWithValue("@Label", Label);
            parameters.AddWithValue("@AlertType", AlertType);
            parameters.AddWithValue("@AlertGroup ", AlertGroup);
            parameters.AddWithValue("@Rules", Rules);
        }

        public RecipientAlertRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new RecipientAlertRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                RecipientId = SqlReaderUtil.GetInt32(reader, nSeq++),
                Label = SqlReaderUtil.GetString(reader, nSeq++),
                AlertType = SqlReaderUtil.GetString(reader, nSeq++),
                AlertGroup = SqlReaderUtil.GetString(reader, nSeq++),
                Rules = SqlReaderUtil.GetString(reader, nSeq++)
            };
            return rec;
        }

    }
}
