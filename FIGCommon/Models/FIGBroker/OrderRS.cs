using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using FIGCommon.Models.FIGProviderAPI;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace FIGCommon.Models.FIGBroker
{

    public class OrderRS : IDbEntity<OrderRS>
    {
        public int Id { get; set; } = -1;
        public int BrokerAccountId { get; set; } = -1;
        public string OrderRefId { get; set; } = "";
        public int TickerId { get; set; } = -1;
        public string OrderTagRef { get; set; } = string.Empty;
        public string OrderTag { get; set; } = OrderTagDef.UNDEFINED;
        public string OrderType { get; set; } = OrderTypes.UNDEFINED;
        public int Qty { get; set; } = 0;
		public decimal Price { get; set; } = 0.0m;
		public decimal StopPrice { get; set; } = 0.0m;
		public int OrderStatus { get; set; } = OrderStatusCodes.NEW;
        public int FillQty { get; set; } = 0;
        public decimal FillPrice { get; set; } = 0M;
        public bool CancelRequested { get; set; } = false;
		public string BrokerRef { get; set; } = string.Empty;
		public string BrokerOrderId { get; set; } = string.Empty;
		public string BrokerParams { get; set; } = string.Empty;
		public long OrderTime { get; set; } = 0;
        public long CompletionTime { get; set; } = 0;
        public long LastUpdateTime { get; set; } = 0;

        public BrokerAccountRS? brokerAccount = null;
        public TickerRS? ticker = null;

        public Dictionary<string, string> BrokerParamsMap
        {
			get
			{
				if (string.IsNullOrWhiteSpace(BrokerParams))
					return new Dictionary<string, string>();

				try
				{
					return JsonConvert.DeserializeObject<Dictionary<string, string>>(BrokerParams)
						   ?? new Dictionary<string, string>();
				}
				catch
				{
					return new Dictionary<string, string>();
				}
			}
		}

		public OrderRS()
        {
            this.Id = -1;
            this.BrokerAccountId = -1;
            this.OrderRefId = string.Empty;
            this.TickerId = -1;
            this.OrderTagRef = string.Empty;
            this.OrderTag = string.Empty;
            this.OrderType = string.Empty;
            this.Qty = 0;
            this.Price = 0;
            this.StopPrice = 0;
            this.OrderStatus = OrderStatusCodes.NEW;
            this.FillQty = 0;
            this.FillPrice = 0M;
            this.CancelRequested = false;
            this.BrokerRef = string.Empty;
            this.BrokerOrderId = string.Empty;
            this.BrokerParams = string.Empty;
            this.OrderTime = 0;
            this.CompletionTime = 0;
            this.LastUpdateTime = 0;
            this.brokerAccount = null;

		}

        public OrderRS(OrderRS rec)
        {
            this.Id = rec.Id;
            this.BrokerAccountId = rec.BrokerAccountId;
            this.OrderRefId = rec.OrderRefId;
            this.TickerId = rec.TickerId;
            this.OrderTagRef = rec.OrderTagRef;
            this.OrderTag = rec.OrderTag;
            this.OrderType = rec.OrderType;
            this.Qty = rec.Qty;
            this.Price = rec.Price;
            this.StopPrice = rec.StopPrice;
            this.OrderStatus = rec.OrderStatus;
            this.FillQty = rec.FillQty;
            this.FillPrice = rec.FillPrice;
            this.CancelRequested = rec.CancelRequested;
            this.BrokerRef = rec.BrokerRef;
            this.BrokerOrderId = rec.BrokerOrderId;
            this.BrokerParams = rec.BrokerParams;
            this.OrderTime = rec.OrderTime;
            this.CompletionTime = rec.CompletionTime;
            this.LastUpdateTime = rec.LastUpdateTime;
            this.brokerAccount = rec.brokerAccount == null ? null : new BrokerAccountRS(rec.brokerAccount);
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@BrokerAccountId", BrokerAccountId);
            parameters.AddWithValue("@OrderRefId", OrderRefId);
            parameters.AddWithValue("@TickerId", TickerId);
            parameters.AddWithValue("@OrderTagRef", OrderTagRef);
            parameters.AddWithValue("@OrderTag", OrderTag);
            parameters.AddWithValue("@OrderType", OrderType);
            parameters.AddWithValue("@Qty", Qty);
            parameters.AddWithValue("@Price", Price);
            parameters.AddWithValue("@StopPrice", StopPrice);
			parameters.AddWithValue("@OrderStatus", OrderStatus);
            parameters.AddWithValue("@FillQty", FillQty);
            parameters.AddWithValue("@FillPrice", FillPrice);
            parameters.AddWithValue("@CancelRequested", CancelRequested);
			parameters.AddWithValue("@BrokerRef", BrokerRef);
            parameters.AddWithValue("@BrokerOrderId", BrokerOrderId);
			parameters.AddWithValue("@BrokerParams", BrokerParams);
			parameters.AddWithValue("@OrderTime", OrderTime);
            parameters.AddWithValue("@CompletionTime", CompletionTime);
            parameters.AddWithValue("@LastUpdateTime", LastUpdateTime);
        }

        public OrderRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new OrderRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                BrokerAccountId = SqlReaderUtil.GetInt32(reader, nSeq++),
                OrderRefId = SqlReaderUtil.GetString(reader, nSeq++),
                TickerId = SqlReaderUtil.GetInt32(reader, nSeq++),
                OrderTagRef = SqlReaderUtil.GetString(reader, nSeq++),
                OrderTag = SqlReaderUtil.GetString(reader, nSeq++),
                OrderType = SqlReaderUtil.GetString(reader, nSeq++),
                Qty = SqlReaderUtil.GetInt32(reader, nSeq++),
                Price = SqlReaderUtil.GetDecimal(reader, nSeq++),
                StopPrice = SqlReaderUtil.GetDecimal(reader, nSeq++),
				OrderStatus = SqlReaderUtil.GetInt32(reader, nSeq++),
                FillQty = SqlReaderUtil.GetInt32(reader, nSeq++),
                FillPrice = SqlReaderUtil.GetDecimal(reader, nSeq++),
                CancelRequested = SqlReaderUtil.GetBoolean(reader, nSeq++),
				BrokerRef = SqlReaderUtil.GetString(reader, nSeq++),
                BrokerOrderId = SqlReaderUtil.GetString(reader, nSeq++),
				BrokerParams = SqlReaderUtil.GetString(reader, nSeq++),
				OrderTime = SqlReaderUtil.GetInt32(reader, nSeq++),
                CompletionTime = SqlReaderUtil.GetInt32(reader, nSeq++),
                LastUpdateTime = SqlReaderUtil.GetInt32(reader, nSeq++),
            };
            return rec;
        }

		public bool IsComplete()
        {
            return OrderStatusCodes.IsComplete(OrderStatus);
        }

        public bool HasStatusChanged(OrderStatusDto statusCode)
        {
           return this.OrderStatus != statusCode.StatusCode
				|| this.BrokerRef != statusCode.BrokerRef
				|| this.BrokerOrderId != statusCode.BrokerOrderId
				|| Math.Abs(statusCode.FilledQty) > Math.Abs(this.FillQty)
				|| (statusCode.AvgFillPrice > 0 && this.FillPrice != statusCode.AvgFillPrice)
                || this.BrokerParams != statusCode.BrokerParamsJson;
		}

		public bool HasStatusChanged(OrderStatusUpdate statusUpdate)
		{
			return this.OrderStatus != statusUpdate.StatusCode
				 || this.BrokerRef != statusUpdate.BrokerRef
				 || this.BrokerOrderId != statusUpdate.BrokerOrderId
				 || Math.Abs(statusUpdate.QtyFilled) > Math.Abs(this.FillQty)
				 || (statusUpdate.AvgFillPrice > 0 && this.FillPrice != statusUpdate.AvgFillPrice)
				 || this.BrokerParams != statusUpdate.BrokerParamsJson;
		}

	}
}
