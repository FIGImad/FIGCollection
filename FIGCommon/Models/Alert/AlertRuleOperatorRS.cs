using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models.Alert
{
    public class AlertRuleOperatorRS : IDbEntity<AlertRuleOperatorRS>
    {
        public string OperatorCode { get; set; }
        public string OperatorType { get; set; }
        public string Description { get; set; }

        public AlertRuleOperatorRS()
        {
            this.OperatorCode = "";
            this.OperatorType = "";
            this.Description = "";
        }

        public AlertRuleOperatorRS(AlertRuleOperatorRS rec)
        {
            this.OperatorCode = rec.OperatorCode;
            this.OperatorType = rec.OperatorType;
            this.Description = rec.Description;
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@OperatorCode", OperatorCode);
            parameters.AddWithValue("@OperatorType", OperatorType);
            parameters.AddWithValue("@Description", Description);
        }

        public AlertRuleOperatorRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new AlertRuleOperatorRS()
            {
                OperatorCode = SqlReaderUtil.GetString(reader, nSeq++),
                OperatorType = SqlReaderUtil.GetString(reader, nSeq++),
                Description  = SqlReaderUtil.GetString(reader, nSeq++)
            };
            return rec;
        }
    }
}
