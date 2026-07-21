using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models.FIGBroker
{
    public partial class BrokerAccountTickerRS : IDbEntity<BrokerAccountTickerRS>
    {

        public int BrokerAccountId { get; set; } = -1;
        public int TickerId { get; set; } = -1;

        public bool Allowed { get; set; } = false;

        public int LongLimit { get; set; } = 0;
        public int ShortLimit { get; set; } = 0;

        public TickerRS? ticker { get; set; } = null;

        public BrokerAccountTickerRS()
        {
            BrokerAccountId = -1;
            TickerId = 0;
            Allowed = false;
            ticker = null;
            LongLimit = 0;
            ShortLimit = 0;
        }

        public BrokerAccountTickerRS(BrokerAccountTickerRS rec)
        {
            BrokerAccountId = rec.BrokerAccountId;
            TickerId = rec.TickerId;
            Allowed = rec.Allowed;
            LongLimit = rec.LongLimit;
            ShortLimit = rec.ShortLimit;

            if (rec.ticker != null)
            {
                ticker = new TickerRS(rec.ticker);
            }
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@BrokerAccountId", BrokerAccountId);
            parameters.AddWithValue("@TickerId", TickerId);
            parameters.AddWithValue("@Allowed", Allowed);
            parameters.AddWithValue("@LongLimit", LongLimit);
            parameters.AddWithValue("@ShortLimit", ShortLimit);
        }

        public BrokerAccountTickerRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new BrokerAccountTickerRS()
            {
                BrokerAccountId = SqlReaderUtil.GetInt32(reader, nSeq++),
                TickerId = SqlReaderUtil.GetInt32(reader, nSeq++),
                Allowed = SqlReaderUtil.GetBoolean(reader, nSeq++),
                LongLimit = SqlReaderUtil.GetInt32(reader, nSeq++),
                ShortLimit = SqlReaderUtil.GetInt32(reader, nSeq++)
            };

            return rec;
        }
    }
}
