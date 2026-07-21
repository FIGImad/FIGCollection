using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models
{
    public partial class AutoTradeRS : IDbEntity<AutoTradeRS>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Grp { get; set; }
        public int Ver { get; set; }
        public int TickerId { get; set; }
        public int BotId{ get; set; }
        public string Strategy { get; set; }
        public int Qty { get; set; }
        public int Status { get; set; }
        public decimal AUM { get; set; }
        public decimal Commission { get; set; }
        public AutoTradeExecStatus ExecStatus { get; set; }
        public TickerRS Ticker { get; set; }
        public BotRS Bot { get; set; }

        public AutoTradeRS()
        {
            this.Id = -1;
            this.Name = "";
            this.Grp = "";
            this.Ver = 1;
            this.TickerId = -1;
            this.BotId = -1;
            this.Strategy = "";
            this.Qty = 0;
            this.Status = 0;
            this.AUM = 0.01M;
            this.Commission = 0.0M;
            this.ExecStatus = new(this.Id);
            ExecStatus.Reset();
            Ticker = new TickerRS();
            Bot = new BotRS();

        }

        public AutoTradeRS(AutoTradeRS rec)
        {
            this.Id = rec.Id;
            this.Name = rec.Name;
            this.Grp = rec.Grp;
            this.Ver = rec.Ver;
            this.TickerId = rec.TickerId;
            this.BotId = rec.BotId;
            this.Strategy = rec.Strategy;
            this.Qty = rec.Qty;
            this.Status = rec.Status;
            this.AUM = rec.AUM;
            this.Commission = rec.Commission;

            this.ExecStatus = new AutoTradeExecStatus(rec.ExecStatus);
            this.Ticker = new TickerRS(rec.Ticker);
            this.Bot = new BotRS(rec.Bot);
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@Name", Name);
            parameters.AddWithValue("@Grp", Grp);
            parameters.AddWithValue("@Ver", Ver);
            parameters.AddWithValue("@TickerId", TickerId);
            parameters.AddWithValue("@BotId", BotId);
            parameters.AddWithValue("@Strategy", Strategy);
            parameters.AddWithValue("@Qty", Qty);
            parameters.AddWithValue("@Status", Status);
            parameters.AddWithValue("@AUM", AUM);
            parameters.AddWithValue("@Commission", Commission);
        }

        public AutoTradeRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new AutoTradeRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                Name = SqlReaderUtil.GetString(reader, nSeq++),
                Grp = SqlReaderUtil.GetString(reader, nSeq++),
                Ver = SqlReaderUtil.GetInt32(reader, nSeq++),
                TickerId = SqlReaderUtil.GetInt32(reader, nSeq++),
                BotId = SqlReaderUtil.GetInt32(reader, nSeq++),
                Strategy = SqlReaderUtil.GetString(reader, nSeq++),
                Qty = SqlReaderUtil.GetInt32(reader, nSeq++),
                Status = SqlReaderUtil.GetInt32(reader, nSeq++),
                AUM = SqlReaderUtil.GetDecimal(reader, nSeq++),
                Commission = SqlReaderUtil.GetDecimal(reader, nSeq++)
            };
            return rec;
        }

    }
}

