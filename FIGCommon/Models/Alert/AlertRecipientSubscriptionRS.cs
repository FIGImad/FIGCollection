using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models.Alert
{
    public class AlertRecipientSubscriptionRS : IDbEntity<AlertRecipientSubscriptionRS>
    {
        public int RecipientId { get; set; }
        public int AlertRuleId { get; set; }
        public int AlertMethods { get; set; }

        public AlertRecipientSubscriptionRS()
        {
            this.RecipientId = -1;
            this.AlertRuleId = -1;
            this.AlertMethods = 0;
        }

        public AlertRecipientSubscriptionRS(AlertRecipientSubscriptionRS rec)
        {
            this.RecipientId = rec.RecipientId;
            this.AlertRuleId = rec.AlertRuleId;
            this.AlertMethods = rec.AlertMethods;
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@RecipientId", RecipientId);
            parameters.AddWithValue("@AlertRuleId", AlertRuleId);
            parameters.AddWithValue("@AlertMethods", AlertMethods);
        }

        public AlertRecipientSubscriptionRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new AlertRecipientSubscriptionRS()
            {
                RecipientId = SqlReaderUtil.GetInt32(reader, nSeq++),
                AlertRuleId = SqlReaderUtil.GetInt32(reader, nSeq++),
                AlertMethods = SqlReaderUtil.GetInt32(reader, nSeq++)
            };
            return rec;
        }

    }
}
