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
            this.Subscriptions = new List<AlertRecipientSubscriptionRS>();
            foreach (var s in rec.Subscriptions)
            {
                this.Subscriptions.Add(new AlertRecipientSubscriptionRS(s));
            }
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@Alias", Alias);
            parameters.AddWithValue("@Name", Name);
            parameters.AddWithValue("@Email", Email);
            parameters.AddWithValue("@PushoverKey", (object?)PushoverKey ?? DBNull.Value);
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
                PushoverKey = SqlReaderUtil.GetNullableString(reader, nSeq++)
            };
            return rec;
        }

    }
}
