using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models.Alert
{
    /// <summary>
    /// Represents one row returned by the scheduled or immediate pending-alert procedure.
    /// Each row is a unique (alert, recipient) pair ready for dispatch.
    /// </summary>
    public class PendingAlertRS : IDbEntity<PendingAlertRS>
    {
        public int RawTime { get; set; }
        public int MSec { get; set; }
        public string TimeZoneId { get; set; } = "UTC";
        public bool IsImmediate { get; set; }
        public int AlertId { get; set; }
        public string FriendlyMessage { get; set; }
        /// <summary>Bitmask: bit 1 = EMAIL, bit 2 = PUSHOVER</summary>
        public int AlertMethods { get; set; }
        public int RecipientId { get; set; }
        public bool RecipientEnabled { get; set; }
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
            // The immediate result includes Source and Level before FriendlyMessage.
            return new PendingAlertRS()
            {
                IsImmediate = reader.GetBoolean(reader.GetOrdinal("IsImmediate")),
                RawTime = reader.GetInt32(reader.GetOrdinal("RawTime")),
                MSec = Convert.ToInt32(reader["MSec"]),
                TimeZoneId = reader.GetString(reader.GetOrdinal("TimeZoneId")),
                AlertId         = SqlReaderUtil.GetInt32(reader, reader.GetOrdinal("AlertId")),
                FriendlyMessage = SqlReaderUtil.GetString(reader, reader.GetOrdinal("FriendlyMessage")),
                AlertMethods    = SqlReaderUtil.GetInt32(reader, reader.GetOrdinal("AlertMethods")),
                RecipientId     = SqlReaderUtil.GetInt32(reader, reader.GetOrdinal("RecipientId")),
                RecipientEnabled = SqlReaderUtil.GetBoolean(reader, reader.GetOrdinal("RecipientEnabled")),
                RecipientName   = SqlReaderUtil.GetString(reader, reader.GetOrdinal("RecipientName")),
                Email           = SqlReaderUtil.GetString(reader, reader.GetOrdinal("Email")),
                PushoverKey     = SqlReaderUtil.GetNullableString(reader, reader.GetOrdinal("PushoverKey")),
                RuleId          = SqlReaderUtil.GetInt32(reader, reader.GetOrdinal("RuleId"))
            };
        }
    }
}
