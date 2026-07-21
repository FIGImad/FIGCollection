using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;


namespace FIGCommon.Models
{
    public class AutoTradeSignalRS : IDbEntity<AutoTradeSignalRS>
    {
        public int Id { get; set; } = -1;
        public int AutoTradeId { get; set; } = -1;
        public int SignalId { get; set; } = -1;
        public string Tag { get; set; } = "";
        public int BotId { get; set; } = -1;
        public int TickerId { get; set; } = -1;
        public string OpenStatus { get; set; } = OrderStatus.NONE;
        public int OpenStatusCode { get; set; } = -1;
        public decimal OpenAvgPrice { get; set; } = 0M;
        public string CloseStatus { get; set; } = OrderStatus.NONE;
        public int CloseStatusCode { get; set; } = -1;
        public decimal CloseAvgPrice { get; set; } = 0M;
        public int OrigQty { get; set; } = 0;
        public int FilledQty { get; set; } = 0;
        public int ManualQty { get; set; } = -1;
        public long LastUpdated { get; set; } = 0;

        public AutoTradeSignalOrderRS? LastOrder { get; set; } = null;

        public AutoTradeSignalRS() { }

        public AutoTradeSignalRS(AutoTradeSignalRS data)
        {
            Clone(data);
        }

        public void Clone(AutoTradeSignalRS src)
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

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@AutoTradeId", AutoTradeId);
            parameters.AddWithValue("@SignalId", SignalId);
            parameters.AddWithValue("@Tag", Tag);
            parameters.AddWithValue("@BotId", BotId);
            parameters.AddWithValue("@TickerId", TickerId);
            parameters.AddWithValue("@OpenStatus", OpenStatus);
            parameters.AddWithValue("@OpenStatusCode", OpenStatusCode);
            parameters.AddWithValue("@OpenAvgPrice", OpenAvgPrice);
            parameters.AddWithValue("@CloseStatus", CloseStatus);
            parameters.AddWithValue("@CloseStatusCode", CloseStatusCode);
            parameters.AddWithValue("@CloseAvgPrice", CloseAvgPrice);
            parameters.AddWithValue("@OrigQty", OrigQty);
            parameters.AddWithValue("@FilledQty", FilledQty);
            parameters.AddWithValue("@ManualQty", ManualQty);
            parameters.AddWithValue("@LastUpdated", LastUpdated);
        }

        public AutoTradeSignalRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            AutoTradeSignalRS rec = new AutoTradeSignalRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                AutoTradeId = SqlReaderUtil.GetInt32(reader, nSeq++),
                SignalId = SqlReaderUtil.GetInt32(reader, nSeq++),
                Tag = SqlReaderUtil.GetString(reader, nSeq++),
                BotId = SqlReaderUtil.GetInt32(reader, nSeq++),
                TickerId = SqlReaderUtil.GetInt32(reader, nSeq++),
                OpenStatus = SqlReaderUtil.GetString(reader, nSeq++),
                OpenStatusCode = SqlReaderUtil.GetInt32(reader, nSeq++),
                OpenAvgPrice = SqlReaderUtil.GetDecimal(reader, nSeq++),
                CloseStatus = SqlReaderUtil.GetString(reader, nSeq++),
                CloseStatusCode = SqlReaderUtil.GetInt32(reader, nSeq++),
                CloseAvgPrice = SqlReaderUtil.GetDecimal(reader, nSeq++),
                OrigQty = SqlReaderUtil.GetInt32(reader, nSeq++),
                FilledQty = SqlReaderUtil.GetInt32(reader, nSeq++),
                ManualQty = SqlReaderUtil.GetInt32(reader, nSeq++),
                LastUpdated = (long) SqlReaderUtil.GetInt32(reader, nSeq++)
            };
            return rec;
        }
    }
}
