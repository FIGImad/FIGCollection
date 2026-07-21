using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models
{
    public class ArchivedAutoTradeSignalRS : IDbEntity<ArchivedAutoTradeSignalRS>
    {
        public int AutoTradeId { get; set; } = -1;
        public string AutoTradeName { get; set; } = "";
        public string AutoTradeGrp { get; set; } = "";
        public string AutoTradeSignalTag { get; set; } = "";
        public string SignalTag { get; set; } = "";
        public string Strategy { get; set; } = "";
        public decimal Side { get; set; } = 0m;
        public int BotId { get; set; } = -1;
        public string BotAccountId { get; set; } = "";
        public int TickerId { get; set; } = -1;
        public string TickerSymbol { get; set; } = "";
        public string TickerLocalSymbol { get; set; } = "";
        public string TickerName { get; set; } = "";
        public string OpenRequestRef { get; set; } = "";
        public string OpenStatus { get; set; } = "";
        public int OpenStatusCode { get; set; } = -1;
        public string OpenStatusCodeDesc { get; set; } = "";
        public long OpenTime { get; set; } = 0;
        public int OpenQty { get; set; } = 0;
        public int OpenQtyFilled { get; set; } = 0;
        public string OpenBrokerRef { get; set; } = "";
        public decimal OpenAvgPrice { get; set; } = 0m;
        public long OpenLastUpdated { get; set; } = 0;
        public string CloseRequestRef { get; set; } = "";
        public string CloseStatus { get; set; } = "";
        public int CloseStatusCode { get; set; } = -1;
        public string CloseStatusCodeDesc { get; set; } = "";
        public long CloseTime { get; set; } = 0;
        public int CloseQty { get; set; } = 0;
        public int CloseQtyFilled { get; set; } = 0;
        public string CloseBrokerRef { get; set; } = "";
        public decimal CloseAvgPrice { get; set; } = 0m;
        public long CloseLastUpdated { get; set; } = 0;
        public int OrigQty { get; set; } = 0;
        public int FilledQty { get; set; } = 0;
        public int ManualQty { get; set; } = -1;
        public long LastUpdated { get; set; } = 0;

        public ArchivedAutoTradeSignalRS() { }

        public void ToSqlCommandParameters(SqlParameterCollection parameters) { }

        public ArchivedAutoTradeSignalRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            return new ArchivedAutoTradeSignalRS
            {
                AutoTradeId          = SqlReaderUtil.GetInt32(reader, nSeq++),
                AutoTradeName        = SqlReaderUtil.GetString(reader, nSeq++),
                AutoTradeGrp         = SqlReaderUtil.GetString(reader, nSeq++),
                AutoTradeSignalTag   = SqlReaderUtil.GetString(reader, nSeq++),
                SignalTag            = SqlReaderUtil.GetString(reader, nSeq++),
                Strategy             = SqlReaderUtil.GetString(reader, nSeq++),
                Side                 = SqlReaderUtil.GetDecimal(reader, nSeq++),
                BotId                = SqlReaderUtil.GetInt32(reader, nSeq++),
                BotAccountId         = SqlReaderUtil.GetString(reader, nSeq++),
                TickerId             = SqlReaderUtil.GetInt32(reader, nSeq++),
                TickerSymbol         = SqlReaderUtil.GetString(reader, nSeq++),
                TickerLocalSymbol    = SqlReaderUtil.GetString(reader, nSeq++),
                TickerName           = SqlReaderUtil.GetString(reader, nSeq++),
                OpenRequestRef       = SqlReaderUtil.GetString(reader, nSeq++),
                OpenStatus           = SqlReaderUtil.GetString(reader, nSeq++),
                OpenStatusCode       = SqlReaderUtil.GetInt32(reader, nSeq++),
                OpenStatusCodeDesc   = SqlReaderUtil.GetString(reader, nSeq++),
                OpenTime             = (long)SqlReaderUtil.GetInt32(reader, nSeq++),
                OpenQty              = SqlReaderUtil.GetInt32(reader, nSeq++),
                OpenQtyFilled        = SqlReaderUtil.GetInt32(reader, nSeq++),
                OpenBrokerRef        = SqlReaderUtil.GetString(reader, nSeq++),
                OpenAvgPrice         = SqlReaderUtil.GetDecimal(reader, nSeq++),
                OpenLastUpdated      = (long)SqlReaderUtil.GetInt32(reader, nSeq++),
                CloseRequestRef      = SqlReaderUtil.GetString(reader, nSeq++),
                CloseStatus          = SqlReaderUtil.GetString(reader, nSeq++),
                CloseStatusCode      = SqlReaderUtil.GetInt32(reader, nSeq++),
                CloseStatusCodeDesc  = SqlReaderUtil.GetString(reader, nSeq++),
                CloseTime            = (long)SqlReaderUtil.GetInt32(reader, nSeq++),
                CloseQty             = SqlReaderUtil.GetInt32(reader, nSeq++),
                CloseQtyFilled       = SqlReaderUtil.GetInt32(reader, nSeq++),
                CloseBrokerRef       = SqlReaderUtil.GetString(reader, nSeq++),
                CloseAvgPrice        = SqlReaderUtil.GetDecimal(reader, nSeq++),
                CloseLastUpdated     = (long)SqlReaderUtil.GetInt32(reader, nSeq++),
                OrigQty              = SqlReaderUtil.GetInt32(reader, nSeq++),
                FilledQty            = SqlReaderUtil.GetInt32(reader, nSeq++),
                ManualQty            = SqlReaderUtil.GetInt32(reader, nSeq++),
                LastUpdated          = (long)SqlReaderUtil.GetInt32(reader, nSeq++)
            };
        }
    }
}
