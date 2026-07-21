using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models.Alert
{
    public class AlertRuleRS : IDbEntity<AlertRuleRS>
    {
        public int Id { get; set; }
        public string RuleKey { get; set; }
        public int Severity { get; set; }
        public bool Enabled { get; set; }
        public string RuleJson { get; set; }
        public string FriendlyMessage { get; set; }
        public DateTime CreatedAt { get; set; }

        public AlertRuleRS()
        {
            this.Id = -1;
            this.RuleKey = "";
            this.Severity = 0;
            this.Enabled = true;
            this.RuleJson = "";
            this.FriendlyMessage = "";
            this.CreatedAt = DateTime.UtcNow;
        }

        public AlertRuleRS(AlertRuleRS rec)
        {
            this.Id = rec.Id;
            this.RuleKey = rec.RuleKey;
            this.Severity = rec.Severity;
            this.Enabled = rec.Enabled;
            this.RuleJson = rec.RuleJson;
            this.FriendlyMessage = rec.FriendlyMessage;
            this.CreatedAt = rec.CreatedAt;
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@RuleKey", RuleKey);
            parameters.AddWithValue("@Severity", Severity);
            parameters.AddWithValue("@Enabled", Enabled);
            parameters.AddWithValue("@RuleJson", RuleJson);
            parameters.AddWithValue("@FriendlyMessage", FriendlyMessage);
            parameters.AddWithValue("@CreatedAt", CreatedAt);
        }

        public AlertRuleRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new AlertRuleRS()
            {
                Id        = SqlReaderUtil.GetInt32(reader, nSeq++),
                RuleKey   = SqlReaderUtil.GetString(reader, nSeq++),
                Severity  = SqlReaderUtil.GetInt32(reader, nSeq++),
                Enabled   = SqlReaderUtil.GetBoolean(reader, nSeq++),
                RuleJson  = SqlReaderUtil.GetString(reader, nSeq++),
                FriendlyMessage = SqlReaderUtil.GetString(reader, nSeq++),
                CreatedAt = SqlReaderUtil.GetDateTime(reader, nSeq++)
            };
            return rec;
        }
    }
}
