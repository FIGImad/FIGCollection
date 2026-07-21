using FIGCommon.DataAccess;
using FIGCommon.Exceptions;
using FIGCommon.Models;
using FIGCommon.Models.FIGBroker;
using FIGCommon.Models.FIGProviderAPI;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System.Runtime.Versioning;

namespace FIGBrokerSvc.DataAccess
{
    public class BrokerRepo
    {
        private static ILogger<BrokerRepo>? _logger;
        private static readonly Object lockAccess = new();
        private static readonly RepositoryBase RBASE = new RepositoryBase();

        public static void Initialize(IServiceProvider serviceProvider, string connectionTag = "DefaultConnection")
        {
            string connectionString = "";
            lock (lockAccess)
            {
                _logger = serviceProvider.GetRequiredService<ILogger<BrokerRepo>>();

                // get connection string from appsettings.json
                var config = serviceProvider.GetRequiredService<IConfiguration>();
                if (config != null)
                {
                    connectionString = config.GetConnectionString(connectionTag) ?? "";
                }
                if (config == null || connectionString == "")
                {
                    _logger?.LogError($"Database {connectionTag} Configuration is invalid");
                    throw new SqlConfigException();
                }
                // Access RBASE directly as it is a static field
                RBASE.Initialize(connectionString, _logger);
            }
        }

        #region Properties
        public static string ConnectionString
        {
            get
            {
                lock (lockAccess)
                {
                    return RBASE.ConnectionString;
                }
            }
        }

        public static string DatabaseName
        {
            get
            {
                lock (lockAccess)
                {
                    return RBASE.DatabaseName;
                }
            }
        }
        #endregion Properties

        #region Transactions
        public static SqlTransaction? OpenTransaction()
        {
            SqlConnection? connection = null;

            try
            {
                connection = new SqlConnection(RBASE.ConnectionString);
                connection.Open();

                var transaction = connection.BeginTransaction();
                return transaction;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to open transaction - {ex.Message}");

                // Ensure we don't leak the connection
                connection?.Dispose();
                return null;
            }
        }
        public static void CommitTransaction(SqlTransaction? transaction)
        {
            if (transaction == null) return;

            var conn = transaction.Connection;
            try
            {
                transaction.Commit();
            }
            finally
            {
                transaction.Dispose();
                conn?.Dispose(); // IMPORTANT: closes connection
            }
        }

        public static void RollbackTransaction(SqlTransaction? transaction)
        {
            if (transaction == null) return;

            var conn = transaction.Connection;
            try
            {
                transaction.Rollback();
            }
            finally
            {
                transaction.Dispose();
                conn?.Dispose(); // IMPORTANT: closes connection
            }
        }
        #endregion Transactions

        #region SQLDEPENDENCY
        public static bool IsBrokerEnabled()
        {
            SysDatabasesRS? rs = RBASE.Select<SysDatabasesRS, string>(new SysDatabasesRS(), "usp_sys_databases_select", "@DBName", DatabaseName);
            return rs?.Is_broker_enabled ?? false;
        }
        #endregion SQLDEPENDENCY

        #region Tickers

        public static List<FIGCommon.Models.FIGBroker.TickerRS> GetTickers()
        {
            return RBASE.SelectMulti<FIGCommon.Models.FIGBroker.TickerRS, int>(new FIGCommon.Models.FIGBroker.TickerRS(), "usp_ticker_select", "@Id", -1);
        }

        public static FIGCommon.Models.FIGBroker.TickerRS? GetTicker(int tickerId)
        {
            var ticker = RBASE.Select<FIGCommon.Models.FIGBroker.TickerRS, int>(new FIGCommon.Models.FIGBroker.TickerRS(), "usp_ticker_select", "@Id", tickerId);
            return ticker;
        }

        public static FIGCommon.Models.FIGBroker.TickerRS? GetTickerBySymbol(string symbol)
        {
            symbol = symbol.Trim().ToUpper();
            var ticker = RBASE.Select<FIGCommon.Models.FIGBroker.TickerRS, string>(new FIGCommon.Models.FIGBroker.TickerRS(), "usp_ticker_select_by_Symbol", "@Symbol", symbol);
            return ticker;
        }

        public static FIGCommon.Models.FIGBroker.TickerRS? QueryTicker(string symbol, string exchange, int contractExpiry)
        {
            symbol = symbol.Trim().ToUpper();
            var ticker = RBASE.Select<FIGCommon.Models.FIGBroker.TickerRS, string, string, int>(new FIGCommon.Models.FIGBroker.TickerRS(), "usp_ticker_query", 
                "@Symbol", symbol, "@Exchange", exchange, "@ContractExpiry", contractExpiry);
            return ticker;
        }

        public static FIGCommon.Models.FIGBroker.TickerRS UpsertTicker(FIGCommon.Models.FIGBroker.TickerRS rec)
        {
            var symbol = rec.Symbol.Trim().ToUpper();
            long newId = RBASE.Upsert<FIGCommon.Models.FIGBroker.TickerRS>(rec, "usp_ticker_upsert", "@IdNew");
            rec.Symbol = rec.Symbol.Trim().ToUpper();
            rec.Id = (int)newId;
            return rec;
        }

        #endregion Tickers

        #region BrokerAccounts

        public static List<BrokerAccountRS> GetBrokerAccounts()
        {
            List<BrokerAccountRS> accts = RBASE.SelectMulti<BrokerAccountRS, int>(new BrokerAccountRS(), "usp_brokeraccount_select", "@Id", -1);
            accts.ForEach(a => {
                a.acctTickerPermissions = new();
                a.acctTickerPermissions = GetBrokerAccountTickers(a.Id);
            });
            return accts;
        }

        public static BrokerAccountRS? GetBrokerAccount(int id)
        {
            BrokerAccountRS? acct = RBASE.Select<BrokerAccountRS, int>(new BrokerAccountRS(), "usp_brokeraccount_select", "@Id", id);
            if (acct != null)
            {
                acct.acctTickerPermissions = new();
                acct.acctTickerPermissions = GetBrokerAccountTickers(acct.Id);
            }
            return acct;
        }

        public static BrokerAccountRS? GetBrokerAccountByAccountId(string accountId)
        {
            BrokerAccountRS? acct = RBASE.Select<BrokerAccountRS, string>(new BrokerAccountRS(), "usp_brokeraccount_select_by_AccountId", "@AccountId", accountId);
            if (acct != null)
            {
                acct.acctTickerPermissions = new();
                acct.acctTickerPermissions = GetBrokerAccountTickers(acct.Id);
            }
            return acct;
        }

        public static BrokerAccountRS? GetBrokerAccountByServiceId(string serviceId)
        {
            BrokerAccountRS? acct = RBASE.Select<BrokerAccountRS, string>(new BrokerAccountRS(), "usp_brokeraccount_select_by_ServiceId", "@ServiceId", serviceId);
            if (acct != null)
            {
                acct.acctTickerPermissions = new();
                acct.acctTickerPermissions = GetBrokerAccountTickers(acct.Id);
            }
            return acct;
        }

        public static BrokerAccountRS UpsertBrokerAccount(BrokerAccountRS rec)
        {
            long newId = RBASE.Upsert<BrokerAccountRS>(rec, "usp_brokeraccount_upsert", "@IdNew");
            rec.Id = (int)newId;
            var recDB = GetBrokerAccount(rec.Id);
            return recDB ?? rec;
        }
        public static int DeleteBrokerAccount(int id)
        {
            return RBASE.ExecuteScalar<int>("usp_brokeraccount_delete", "@Id", id);
        }

        #endregion BrokerAccounts

        #region BrokerAccountTicker

        public static List<BrokerAccountTickerRS> GetBrokerAccountTickers(int brokerAccountId)
        {
            List<BrokerAccountTickerRS> acctTickerPermissions = RBASE.SelectMulti<BrokerAccountTickerRS, int>(new BrokerAccountTickerRS(), "usp_brokeraccountticker_select", "@BrokerAccountId", brokerAccountId);
            acctTickerPermissions.ForEach(atp => {
                atp.ticker = GetTicker(atp.TickerId);
            });
            return acctTickerPermissions;
        }

        public static BrokerAccountTickerRS UpsertBrokerAccountTicker(BrokerAccountTickerRS rec)
        {
            long newId = RBASE.Upsert<BrokerAccountTickerRS>(rec, "usp_brokeraccountticker_upsert", "@IdNew");
            return rec;
        }

        public static int DeleteBrokerAccountTicker(int accountId, int tickerId)
        {
            return RBASE.ExecuteScalar<int, int>("usp_brokeraccountticker_delete", "@BrokerAccountId", accountId, "@TickerId", tickerId);
        }

        #endregion BrokerAccountTicker

        #region Orders

        public static List<OrderRS> GetAccountOrders(int accountId, int sinceTime)
        {
            // this will retrieve orders for this day and previous day
            List<OrderRS> orders= RBASE.SelectMulti<OrderRS, int, int>(new OrderRS(), "usp_order_query_by_brokeraccount", "@BrokerAccountId", accountId, "@FromTime", sinceTime);
            return orders;
        }

        public static OrderRS InsertOrder(OrderRS rec, SqlTransaction? tx = null)
        {
            if (tx == null)
            {
                rec.Id = (int)RBASE.Insert<OrderRS>(rec, "usp_order_insert", "@IdNew");
            }
            else
            {
                rec.Id = (int)RepositoryBase.Insert<OrderRS>(rec, tx.Connection, "usp_order_insert", "@IdNew", tx);
            }
            return rec;
        }

        public static void UpdateOrderStatus(int id, OrderStatusDto statusCode)
        {
            try
            {
                // first retrrieve existing record
                OrderRS? rec = GetOrder(id);
                if (rec == null) return;

                Dictionary<string, string> newBrokerParams = rec.BrokerParamsMap;

                // iterate through brokerParams and add to existing brokerParamsMap if does not exists

                foreach (var kvp in statusCode.BrokerParams)
                {
                    if (!newBrokerParams.ContainsKey(kvp.Key))
                    {
                        newBrokerParams.Add(kvp.Key, kvp.Value);
                    }
                }
                // convert broker ref to string as it can be null or empty and stored as varchar in DB
                string brokerParams = JsonConvert.SerializeObject(newBrokerParams);

                RBASE.ExecuteScalar<int, int, int, decimal, string, string, string>("usp_order_update_status", "@Id", id, "@OrderStatus", statusCode.StatusCode,
                    "@FilledQty", statusCode.FilledQty, "@FilledPrice", statusCode.AvgFillPrice, 
                    "@BrokerRef", statusCode.BrokerRef, "@BrokerOrderId", statusCode.BrokerOrderId, "@BrokerParams", brokerParams);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to update order status for orderId ={id} to statusCode={StatusCode}", id, statusCode.StatusCode);
            }
        }

        public static void UpdateOrderStatusSimple(int id, OrderStatusDto statusCode)
        {
            try
            {
                // convert broker ref to string as it can be null or empty and stored as varchar in DB
                string brokerParams = JsonConvert.SerializeObject(statusCode.BrokerParams);

                RBASE.ExecuteScalar<int, int, int, decimal, string, string, string>("usp_order_update_status", "@Id", id, "@OrderStatus", statusCode.StatusCode,
                    "@FilledQty", statusCode.FilledQty, "@FilledPrice", statusCode.AvgFillPrice,
                    "@BrokerRef", statusCode.BrokerRef, "@BrokerOrderId", statusCode.BrokerOrderId, "@BrokerParams", brokerParams);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to update order status for orderId ={id} to statusCode={StatusCode}", id, statusCode.StatusCode);
            }
        }


        public static OrderRS? UpdateOrderCancelStatus(int id, int statusCode)
        {
            try
            {
                RBASE.ExecuteScalar<int, int>("usp_order_update_cancel_status", "@Id", id, "@OrderStatus", statusCode);
                return GetOrder(id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to update order status for orderId ={id} to statusCode={StatusCode}", id, statusCode);
                return null;
            }
        }
        

        public static OrderRS? GetOrder(int orderId)
        {
            OrderRS? rec = RBASE.Select<OrderRS, int>(new OrderRS(), "usp_order_select", "@Id", orderId);
            if (rec != null)
            {
                rec.brokerAccount = GetBrokerAccount(rec.BrokerAccountId);
                rec.ticker = GetTicker(rec.TickerId);
            }
            return rec;
        }

        public static OrderRS? GetOrderByBrokerRef(string brokerRef)
        {
            OrderRS? rec = RBASE.Select<OrderRS, string>(new OrderRS(), "usp_order_query_by_brokerref", "@BrokerRef", brokerRef);
            if (rec != null)
            {
                rec.brokerAccount = GetBrokerAccount(rec.BrokerAccountId);
                rec.ticker = GetTicker(rec.TickerId);
            }
            return rec;
        }

        public static OrderRS? GetOrderByOrderId(string brokerOrderId)
        {
            OrderRS? rec = RBASE.Select<OrderRS, string>(new OrderRS(), "usp_order_query_by_brokerorderid", "@brokerOrderId", brokerOrderId);
            if (rec != null)
            {
                rec.brokerAccount = GetBrokerAccount(rec.BrokerAccountId);
                rec.ticker = GetTicker(rec.TickerId);
            }
            return rec;
        }

        public static OrderRS? GetOrderByOrderRefId(int accountId, string orderRefId)
        {
            OrderRS? rec = RBASE.Select<OrderRS, int, string>(new OrderRS(), "usp_order_query_by_orderref", "@BrokerAccountId", accountId, "@OrderRefId", orderRefId);
            if (rec != null)
            {
                rec.brokerAccount = GetBrokerAccount(rec.BrokerAccountId);
                rec.ticker = GetTicker(rec.TickerId);
            }
            return rec;
        }

        public static List<OrderRS> GetActiveOrders(string brokerAccountId)
        {
            List<OrderRS> orders = RBASE.SelectMulti<OrderRS, string>(new OrderRS(), "usp_order_query_active", "@BrokerAccountId", brokerAccountId);
            foreach (var order in orders)
            {
                order.brokerAccount = GetBrokerAccount(order.BrokerAccountId);
                order.ticker = GetTicker(order.TickerId);
            }
            return orders;
        }

        public static List<OrderRS> GetActiveOrdersByServiceId(string serviceId)
        {
            List<OrderRS> orders = RBASE.SelectMulti<OrderRS, string>(new OrderRS(), "usp_order_query_active_by_service_id", "@ServiceId", serviceId);
            foreach (var order in orders)
            {
                order.brokerAccount = GetBrokerAccount(order.BrokerAccountId);
                order.ticker = GetTicker(order.TickerId);
            }
            return orders;
        }

        internal static OrderRS? GetOrderByOrderTag(int accountId, string orderTagRef, string orderTag)
        {
            OrderRS? rec = RBASE.Select<OrderRS, int, string, string>(new OrderRS(), "usp_order_query_by_order_tag", "@BrokerAccountId", accountId, "@OrderTagRef", orderTagRef, "@OrderTag", orderTag);
            if (rec != null)
            {
                rec.brokerAccount = GetBrokerAccount(rec.BrokerAccountId);
                rec.ticker = GetTicker(rec.TickerId);
            }
            return rec;
        }

        #endregion Orders


    }
}

