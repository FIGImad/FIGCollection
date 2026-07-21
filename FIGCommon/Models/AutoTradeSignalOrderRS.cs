using FIGCommon.DataAccess;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;


namespace FIGCommon.Models
{

    public static class OrderStatus
    {
        public const string NONE = "NONE";
        public const string NEW = "NEW";
        public const string PROCESSING = "PROCESSING";
        public const string FAILED = "FAILED";
        public const string CANCELED = "CANCELED";
        public const string FILLED = "FILLED";
        public const string FILLED_PARTIALLY = "FILLED_PARTIALLY";
        public const string FILLED_PARTIALLY_FIN = "FILLED_PARTIALLY_FIN";
        public const string FILLED_MANUALLY = "FILLED_MANUALLY";

        public static bool IsCompleted(string status)
        {
            return status == OrderStatus.FILLED
                || status == OrderStatus.FILLED_MANUALLY
                || status == OrderStatus.FILLED_PARTIALLY_FIN
                || status == OrderStatus.CANCELED
                || status == OrderStatus.FAILED;
        }

        public static bool IsSubmitted(string status)
        {
            return status == OrderStatus.PROCESSING
                || status == OrderStatus.FILLED_PARTIALLY;
        }
        public static bool IsNew(string status)
        {
            return status == OrderStatus.NEW;
        }
    }

    public class AutoTradeSignalOrderRS : IDbEntity<AutoTradeSignalOrderRS>
    {
        public int Id { get; set; } = -1;
        public int AutoTradeSignalId { get; set; } = -1;
        public string OrderTag { get; set; } = OrderTagDef.OPEN;
        public long OrderTime { get; set; } = 0;
        public string Type { get; set; } = OrderTypes.MARKET;
        public decimal LimitPrice { get; set; } = 0M;
        public int Qty { get; set; } = 0;
        public int QtyFilled { get; set; } = 0;
        public decimal AvgFillPrice { get; set; } = 0M;
        public string Status { get; set; } = OrderStatus.NONE;
        public int StatusCode { get; set; } = -1;
        public string RequestRef { get; set; } = "";
        public string? BrokerRef { get; set; } = null;
        public bool CancelRequest { get; set; } = false;
        public int LastUpdated { get; set; } = 0;

        public AutoTradeSignalOrderRS() { }

        public AutoTradeSignalOrderRS(AutoTradeSignalOrderRS data)
        {
            Clone(data);
        }

        public void Clone(AutoTradeSignalOrderRS src)
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
            parameters.AddWithValue("@AutoTradeSignalId", AutoTradeSignalId);
            parameters.AddWithValue("@OrderTag", OrderTag);
            parameters.AddWithValue("@OrderTime", OrderTime);
            parameters.AddWithValue("@Type", Type);
            parameters.AddWithValue("@LimitPrice", LimitPrice);
            parameters.AddWithValue("@Qty", Qty);
            parameters.AddWithValue("@QtyFilled", QtyFilled);
            parameters.AddWithValue("@AvgFillPrice", AvgFillPrice);
            parameters.AddWithValue("@Status", Status);
            parameters.AddWithValue("@StatusCode", StatusCode);
            parameters.AddWithValue("@RequestRef", RequestRef);
            parameters.AddWithValue("@BrokerRef", BrokerRef ?? (object)DBNull.Value);
            parameters.AddWithValue("@CancelRequest", CancelRequest);
            parameters.AddWithValue("@LastUpdated", LastUpdated);
        }

        public AutoTradeSignalOrderRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            AutoTradeSignalOrderRS rec = new AutoTradeSignalOrderRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                AutoTradeSignalId = SqlReaderUtil.GetInt32(reader, nSeq++),
                OrderTag = SqlReaderUtil.GetString(reader, nSeq++),
                OrderTime = SqlReaderUtil.GetInt32(reader, nSeq++),
                Type = SqlReaderUtil.GetString(reader, nSeq++),
                LimitPrice = SqlReaderUtil.GetDecimal(reader, nSeq++),
                Qty = SqlReaderUtil.GetInt32(reader, nSeq++),
                QtyFilled = SqlReaderUtil.GetInt32(reader, nSeq++),
                AvgFillPrice = SqlReaderUtil.GetDecimal(reader, nSeq++),
                Status = SqlReaderUtil.GetString(reader, nSeq++),
                StatusCode = SqlReaderUtil.GetInt32(reader, nSeq++),
                RequestRef = SqlReaderUtil.GetString(reader, nSeq++),
                BrokerRef = SqlReaderUtil.GetNullableString(reader, nSeq++),
                CancelRequest = SqlReaderUtil.GetBoolean(reader, nSeq++),
                LastUpdated = SqlReaderUtil.GetInt32(reader, nSeq++)
            };
            return rec;
        }

        public bool IsCompleted()
        {
            return OrderStatus.IsCompleted(Status);
        }

        public bool IsSubmitted()
        {
            return OrderStatus.IsSubmitted(Status);
        }
        public bool IsNew()
        {
            return OrderStatus.IsNew(Status);
        }
    }
}
