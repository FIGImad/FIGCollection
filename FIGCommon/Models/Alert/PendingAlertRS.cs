using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models.Alert
{
    /// <summary>
    /// Represents one row returned by <c>usp_alert_get_pending</c>.
    /// Each row is a unique (alert, recipient) pair ready for dispatch.
    /// </summary>
    public class PendingAlertRS : IDbEntity<PendingAlertRS>
    {
        public int AlertId { get; set; }
        public string FriendlyMessage { get; set; }
        /// <summary>Bitmask: bit 1 = EMAIL, bit 2 = PUSHOVER</summary>
        public int AlertMethods { get; set; }
        public int RecipientId { get; set; }
        public string RecipientName { get; set; }
        public string Email { get; set; }
        public string? PushoverKey { get; set; }
        /// <summary>The alert rule whose conditions were matched to produce this alert.</summary>
        public int RuleId { get; set; }

        public PendingAlertRS()
        {
            AlertId = -1;
            FriendlyMessage = "";
            AlertMethods = 0;
            RecipientId = -1;
            RecipientName = "";
            Email = "";
            PushoverKey = null;
            RuleId = -1;
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            // Read-only result set — no parameters needed
        }

        public PendingAlertRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            return new PendingAlertRS()
            {
                AlertId         = SqlReaderUtil.GetInt32(reader, nSeq++),
                FriendlyMessage = SqlReaderUtil.GetString(reader, nSeq++),
                AlertMethods    = SqlReaderUtil.GetInt32(reader, nSeq++),
                RecipientId     = SqlReaderUtil.GetInt32(reader, nSeq++),
                RecipientName   = SqlReaderUtil.GetString(reader, nSeq++),
                Email           = SqlReaderUtil.GetString(reader, nSeq++),
                PushoverKey     = SqlReaderUtil.GetNullableString(reader, nSeq++),
                RuleId          = SqlReaderUtil.GetInt32(reader, nSeq++)
            };
        }
    }
}
