using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models.Alert
{
    public class AlertRecipientRS : IDbEntity<AlertRecipientRS>
    {
        public int Id { get; set; }
        public string Alias { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string? PushoverKey { get; set; }
        public string TimeZoneId { get; set; } = "UTC";
        public bool Enabled { get; set; } = true;

        public List<AlertRecipientSubscriptionRS> Subscriptions{ get; set; }

        public AlertRecipientRS()
        {
            this.Id = -1;
            this.Alias = "";
            this.Name = "";
            this.Email = "";
            this.PushoverKey = null;
            this.Subscriptions = new List<AlertRecipientSubscriptionRS>();
        }

        public AlertRecipientRS(AlertRecipientRS rec)
        {
            this.Id = rec.Id;
            this.Alias = rec.Alias;
            this.Name = rec.Name;
            this.Email = rec.Email;
            this.PushoverKey = rec.PushoverKey;
            this.Enabled = rec.Enabled;
            this.TimeZoneId = rec.TimeZoneId;
            this.Subscriptions = new List<AlertRecipientSubscriptionRS>();
            foreach (var s in rec.Subscriptions)
            {
                this.Subscriptions.Add(new AlertRecipientSubscriptionRS(s));
            }
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@TimeZoneId", TimeZoneId);
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@Alias", Alias);
            parameters.AddWithValue("@Name", Name);
            parameters.AddWithValue("@Email", Email);
            parameters.AddWithValue("@PushoverKey", (object?)PushoverKey ?? DBNull.Value);
            parameters.Add("@Enabled", System.Data.SqlDbType.Bit).Value = Enabled;
        }

        public AlertRecipientRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new AlertRecipientRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                Alias = SqlReaderUtil.GetString(reader, nSeq++),
                Name = SqlReaderUtil.GetString(reader, nSeq++),
                Email = SqlReaderUtil.GetString(reader, nSeq++),
                PushoverKey = SqlReaderUtil.GetNullableString(reader, nSeq++),
                TimeZoneId = reader.GetString(reader.GetOrdinal("TimeZoneId")),
                Enabled = SqlReaderUtil.GetBoolean(reader, reader.GetOrdinal("Enabled"))
            };
            return rec;
        }

    }
}
