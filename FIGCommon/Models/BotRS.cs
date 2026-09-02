using Microsoft.Data.SqlClient;
using FIGCommon.DataAccess;
using FIGCommon.Utilities;

namespace FIGCommon.Models
{
    public class BotRS : IDbEntity<BotRS>
    {
        public int Id { get; set; } = -1;
        public string Group { get; set; } = "";
        public string AccountId { get; set; } = "";
        public string BrokerServiceId { get; set; } = "";
        public string Status { get; set; } = "ACTIVE";
        public long LastUpdated { get; set; } = 0;

        public BotRS() { }
        public BotRS(BotRS data)
        {
            Clone(data);
        }

        public void Clone(BotRS src)
        {
            var srcT = src.GetType();
            var dstT = this.GetType();
            foreach (var f in srcT.GetFields())
            {
                var dstF = dstT.GetField(f.Name);
                if (dstF == null || dstF.IsLiteral)
                    continue;
                dstF.SetValue(this, f.GetValue(src));
            }

            foreach (var f in srcT.GetProperties())
            {
                var dstF = dstT.GetProperty(f.Name);
                if (dstF == null)
                    continue;

                dstF.SetValue(this, f.GetValue(src, null), null);
            }
        }

        public bool IsActive()
        {
            return Status == "ACTIVE";
        }
        public bool IsSuspect()
        {
            return Status == "SUSPECT";
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@Group", Group);
            parameters.AddWithValue("@AccountId", AccountId);
            parameters.AddWithValue("@BrokerServiceId", BrokerServiceId);
            parameters.AddWithValue("@Status", Status);
            parameters.AddWithValue("@LastUpdated", LastUpdated);
        }

        public BotRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new BotRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                Group = SqlReaderUtil.GetString(reader, nSeq++),
                AccountId = SqlReaderUtil.GetString(reader, nSeq++),
                BrokerServiceId = SqlReaderUtil.GetString(reader, nSeq++),
                Status = SqlReaderUtil.GetString(reader, nSeq++),
                LastUpdated = (long) SqlReaderUtil.GetInt32(reader, nSeq++)
            };
            return rec;
        }
    }
}
