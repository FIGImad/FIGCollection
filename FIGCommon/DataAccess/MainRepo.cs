using FIGCommon.Exceptions;
using FIGCommon.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FIGCommon.DataAccess
{
    public class MainRepo
    {
        private static ILogger<MainRepo>? _logger;
        private static readonly Object lockPriceSrcAccess = new();
        private static readonly List<PriceSrcRS> priceSrcCache = new();

        private static readonly Object lockAccess = new();

        private static readonly RepositoryBase RBASE = new RepositoryBase();
        private static readonly RepositoryBase RBASEADMIN = new RepositoryBase();

        public static void Initialize(IServiceProvider serviceProvider, string connectionTag = "DefaultConnection")
        {
            string connectionString = "";
            string adminConnectionString = "";
            lock (lockAccess)
            {
                _logger = serviceProvider.GetRequiredService<ILogger<MainRepo>>();

                // get connection string from appsettings.json
                var config = serviceProvider.GetRequiredService<IConfiguration>();
                if (config != null)
                {
                    connectionString = config.GetConnectionString(connectionTag) ?? "";
                    adminConnectionString = config.GetConnectionString("AdminConnection") ?? "";
                }
                if (config == null || connectionString == "")
                {
                    _logger?.LogError($"Database {connectionTag} Configuration is invalid");
                    throw new SqlConfigException();
                }
                // Access RBASE directly as it is a static field
                RBASE.Initialize(connectionString, _logger);
                if (adminConnectionString != "")
                {
                    RBASEADMIN.Initialize(adminConnectionString, _logger);
                }
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

        public static int AcquireAutoTradeAppLock(int autoTradeId, SqlTransaction tx, int lockTimeoutMs = 0)
        {
            if (tx.Connection == null) return -1;

            try
            {
                using var cmd = tx.Connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                            DECLARE @result int;
                            EXEC @result = sp_getapplock
                                @Resource = @resource,
                                @LockMode = 'Exclusive',
                                @LockOwner = 'Transaction',
                                @LockTimeout = @lockTimeout;
                            SELECT @result;";
                cmd.Parameters.AddWithValue("@resource", $"AutoTradeProcess_{autoTradeId}");
                cmd.Parameters.AddWithValue("@lockTimeout", lockTimeoutMs);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to acquire AutoTrade app lock - {ex.Message}");
                return -1;
            }
        }

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

        #region Tickers

        public static List<TickerRS> GetTickers()
        {
            return RBASE.SelectMulti<TickerRS, int>(new TickerRS(), "usp_ticker_select", "@Id", -1);
        }

        public static TickerRS? GetTicker(int tickerId)
        {
            var ticker = RBASE.Select<TickerRS, int>(new TickerRS(), "usp_ticker_select", "@Id", tickerId);
            return ticker;
        }

        public static TickerRS? GetTickerBySymbol(string symbol)
        {
            symbol = symbol.Trim().ToUpper();
            var ticker = RBASE.Select<TickerRS, string>(new TickerRS(), "usp_ticker_select_by_Symbol", "@Symbol", symbol);
            return ticker;
        }

        public static TickerRS UpsertTicker(TickerRS rec)
        {
            var symbol = rec.Symbol.Trim().ToUpper();
            long newId = RBASE.Upsert<TickerRS>(rec, "usp_ticker_upsert", "@IdNew");
            rec.Symbol = rec.Symbol.Trim().ToUpper();
            rec.Id = (int)newId;
            return rec;
        }

        #endregion Tickers

        #region AutoTrade

        public static List<AutoTradeRS> GetAutoTrades()
        {
            List<AutoTradeRS> lst = RBASE.SelectMulti<AutoTradeRS, int>(new AutoTradeRS(), "usp_autotrade_select", "@Id", -1);
            lst.ForEach(autoTrade => UpdateAutoTradeRec(autoTrade));
            return lst;
        }

        public static AutoTradeRS? GetAutoTrade(int autoTradeId)
        {
            var autoTrade = RBASE.Select<AutoTradeRS, int>(new AutoTradeRS(), "usp_autotrade_select", "@Id", autoTradeId);
            if (autoTrade != null)
            {
                UpdateAutoTradeRec(autoTrade);
            }
            return autoTrade;
        }

        private static void UpdateAutoTradeRec(AutoTradeRS autoTrade)
        {
            var ticker = GetTicker(autoTrade.TickerId);
            if (ticker != null)
            {
                autoTrade.Ticker = new TickerRS(ticker);
            }
            var bot = GetBot(autoTrade.BotId);
            if (bot != null)
            {
                autoTrade.Bot = new BotRS(bot);
            }
        }

        public static AutoTradeRS UpsertAutoTrade(AutoTradeRS rec)
        {
            long newId = RBASE.Upsert<AutoTradeRS>(rec, "usp_autotrade_upsert", "@IdNew");
            rec.Id = (int)newId;
            return rec;
        }

        public static void DeleteAutoTrade(int id)
        {
            RBASE.Delete<int, int, int>("usp_autotrade_delete", "@Id", id);
        }

        public static void UpdateAutoTradeStatus(int autoTradeId, int status)
        {
            RBASE.ExecuteScalar<int, int>("usp_autotrade_update_status", "@Id", autoTradeId, "@Status", status);
        }
        #endregion AutoTrade

        #region StrategyConfig

        public static List<StrategyConfigRS> GetStrategyConfigs()
        {
            var lst = RBASE.SelectMulti<StrategyConfigRS, string>(new StrategyConfigRS(), "usp_strategy_config_select", "@StrategyName", "");
            foreach (var strategyConfig in lst)
            {
                strategyConfig.StudyCol = GetStudyCol(strategyConfig.StudyColId);
            }
            return lst;
        }
        public static List<StrategyConfigRS> QueryStrategyConfigs(int studyColId)
        {
            var lst = RBASE.SelectMulti<StrategyConfigRS, int>(new StrategyConfigRS(), "usp_strategy_config_query", "@StudyColId", studyColId);
            foreach (var strategyConfig in lst)
            {
                strategyConfig.StudyCol = GetStudyCol(strategyConfig.StudyColId);
            }
            return lst;
        }

        public static StrategyConfigRS? GetStrategyConfig(string strategyName, SqlTransaction? tx = null)
        {
            if (tx != null)
            {
                var strategyConfig = RepositoryBase.Select<StrategyConfigRS, string>(tx.Connection, new StrategyConfigRS(), "usp_strategy_config_select", "@StrategyName", strategyName, tx);
                if (strategyConfig != null)
                {
                    strategyConfig.StudyCol = GetStudyCol(strategyConfig.StudyColId);
                }
                return strategyConfig;
            }
            else
            {
                var strategyConfig = RBASE.Select<StrategyConfigRS, string>(new StrategyConfigRS(), "usp_strategy_config_select", "@StrategyName", strategyName);
                if (strategyConfig != null)
                {
                    strategyConfig.StudyCol = GetStudyCol(strategyConfig.StudyColId);
                }
                return strategyConfig;
            }

        }

        public static StrategyConfigRS UpsertStrategyConfig(StrategyConfigRS rec)
        {
            long newId = RBASE.Upsert<StrategyConfigRS>(rec, "usp_strategy_config_upsert", "@IdNew");
            return rec;
        }

        public static void DeleteStrategyConfig(string strategyName)
        {
            RBASE.Delete<string>("usp_strategy_config_delete", "@StrategyName", strategyName);
        }

        #endregion StrategyConfig

        //#region Strategies
        //public static List<StrategyRS> GetStrategies()
        //{
        //    try
        //    {
        //        return RBASE.SelectMulti<StrategyRS, string>(new StrategyRS(), "usp_strategy_select", "@Name", "");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger?.LogError("GetStrategies: {0}", ex.Message);
        //        return new List<StrategyRS>();
        //    }
        //}

        //public static StrategyRS? GetStrategy(string name)
        //{
        //    try
        //    {
        //        return RBASE.Select<StrategyRS, string>(new StrategyRS(), "usp_strategy_select", "@Name", name);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger?.LogError("GetStrategy: {0}", ex.Message);
        //        return null;
        //    }
        //}

        //public static StrategyRS UpsertStrategy(StrategyRS rec)
        //{
        //    RBASE.Upsert<StrategyRS>(rec, "usp_strategy_upsert", "@IdNew");
        //    return rec;
        //}

        //public static void DeleteStrategy(string name)
        //{
        //    RBASE.Delete<string>("usp_strategy_delete", "@name", name);
        //}


        //#endregion Strategies

        #region DataSet

        public static DataSetRS? GetDataSet(int dataSetId)
        {
            var dataSet = RBASE.Select<DataSetRS, int>(new DataSetRS(), "usp_dataset_select", "@Id", dataSetId);
            if (dataSet != null)
            {
                var ticker = GetTicker(dataSet.TickerId);
                if (ticker != null)
                {
                    dataSet.Ticker = new TickerRS(ticker);
                }
                var interval = GetInterval(dataSet.IntervalId);
                if (interval != null)
                {
                    dataSet.Interval = new IntervalRS(interval);
                }
            }
            return dataSet;
        }

        public static List<DataSetRS> GetDataSets()
        {
            var dataSets = RBASE.SelectMulti<DataSetRS, int>(new DataSetRS(), "usp_dataset_select", "@Id", -1);
            if (dataSets.Count > 0)
            {
                dataSets.ForEach(dataSet =>
                {
                    var ticker = GetTicker(dataSet.TickerId);
                    if (ticker != null)
                    {
                        dataSet.Ticker = new TickerRS(ticker);
                    }
                    var interval = GetInterval(dataSet.IntervalId);
                    if (interval != null)
                    {
                        dataSet.Interval = new IntervalRS(interval);
                    }
                });
            }
            return dataSets;
        }

        public static DataSetRS? QueryDataSet(int tickerId, string intervalId)
        {
            var dataSet = RBASE.Select<DataSetRS, int, string>(new DataSetRS(), "usp_dataset_query", "@TickerId", tickerId, "@IntervalId", intervalId);
            if (dataSet != null)
            {
                var ticker = GetTicker(dataSet.TickerId);
                if (ticker != null)
                {
                    dataSet.Ticker = new TickerRS(ticker);
                }
                var interval = GetInterval(dataSet.IntervalId);
                if (interval != null)
                {
                    dataSet.Interval = new IntervalRS(interval);
                }
            }
            return dataSet;
        }

        public static DataSetRS UpsertDataSet(DataSetRS rec)
        {
            long newId = RBASE.Upsert<DataSetRS>(rec, "usp_dataset_upsert", "@IdNew");
            rec.Id = (int)newId;
            var ticker = GetTicker(rec.TickerId);
            if (ticker != null)
            {
                rec.Ticker = new TickerRS(ticker);
            }
            var interval = GetInterval(rec.IntervalId);
            if (interval != null)
            {
                rec.Interval = new IntervalRS(interval);
            }
            return rec;
        }
        #endregion DataSet

        #region Interval

        public static List<IntervalRS> GetIntervals()
        {
            return RBASE.SelectMulti<IntervalRS, string>(new IntervalRS(), "usp_interval_select", "@Id", "");
        }

        public static IntervalRS? GetInterval(string intervalId)
        {
            var interval = RBASE.Select<IntervalRS, string>(new IntervalRS(), "usp_interval_select", "@Id", intervalId);
            return interval;
        }
        #endregion Interval

        #region PriceData

        public static int PriceTruncate(int dataSetId, long startTime = 0)
        {
            return RBASE.ExecuteScalar<int, long>("usp_price_data_truncate", "@DataSetId", dataSetId, "@StartTime", startTime);
        }

        public static List<PriceDataRS> GetPriceData(int dataSetId, long startTime, int maxRec = 500000)
        {
            return RBASE.SelectMulti<PriceDataRS, int, long, int>(new PriceDataRS(), "usp_price_data_select", "@DataSetId", dataSetId, "@StartTime", startTime, "@MaxRec", maxRec);
        }

        public static PriceDataRS? GetPriceDataRec(int dataSetId, long startTime, SqlTransaction? tx = null)
        {
            if (tx == null)
            {
                return RBASE.Select<PriceDataRS, int, long, int>(new PriceDataRS(), "usp_price_data_select_exact", "@DataSetId", dataSetId, "@StartTime", startTime, "@MaxRec", 1);
            }
            else
            {
                return RepositoryBase.Select<PriceDataRS, int, long, int>(tx.Connection, new PriceDataRS(), "usp_price_data_select_exact", "@DataSetId", dataSetId, "@StartTime", startTime, "@MaxRec", 1, tx);
            }
        }

        public static List<PriceDataRS> GetPriceDataUntil(int dataSetId, long untilTime, int maxRec = 500000)
        {
            return RBASE.SelectMulti<PriceDataRS, int, long, int>(new PriceDataRS(), "usp_price_data_select_until", "@DataSetId", dataSetId, "@UntilTime", untilTime, "@MaxRec", maxRec);
        }

        public static PriceDataRS? GetLastPriceData(int dataSetId)
        {
            return RBASE.Select<PriceDataRS, int>(new PriceDataRS(), "usp_price_data_select_last", "@DataSetId", dataSetId);
        }

        //public static List<PriceDataRS> GetPriceDataFromList(List<PriceDataRS> lst, long startTime, int numRec)
        //{
        //    var newPriceLst = new List<PriceDataRS>();
        //    int recCounter = 0;
        //    for (int i = 0; i < lst.Count; i++)
        //    {
        //        var rs = lst[i];
        //        if (recCounter < numRec && rs.RawTime >= startTime)
        //        {
        //            newPriceLst.Add(rs);
        //            recCounter++;
        //        }
        //        else if (recCounter >= numRec)
        //        {
        //            break;
        //        }
        //    }
        //    return newPriceLst;
        //}

        public static PriceDataRS UpsertPriceData(PriceDataRS rec)
        {
            long newId = RBASE.Upsert<PriceDataRS>(rec, "usp_price_data_upsert", "@IdNew");
            rec.Id = (int)newId;
            return rec;
        }

        public static List<PriceDataRS> UpsertPriceData(List<PriceDataRS> list)
        {
            SqlConnection? connection = null;
            SqlTransaction? transaction = null;
            try
            {
                connection = new SqlConnection(RBASE.ConnectionString);
                connection.Open();
                transaction = connection.BeginTransaction();

                //transaction = RBASE.OpenTransaction();
                if (transaction == null)
                {
                    _logger?.LogError("Could not open Transaction");
                    throw new SqlUpsertException("Could not open Transaction");
                }

                list.ForEach(rec =>
                {
                    long newId = RepositoryBase.Upsert<PriceDataRS, long>(connection, rec, "usp_price_data_upsert", "@IdNew", transaction);
                    rec.Id = (int)newId;
                });
                transaction.Commit();
                return list;
            }
            catch (Exception ex)
            {
                try
                {
                    transaction?.Rollback();
                }
                catch (Exception rollbackEx)
                {
                    _logger?.LogError($"Rollback failed - {rollbackEx.Message}");
                }
                _logger?.LogError($"UpsertPriceData Error - {ex.Message}");
                throw; // Re-throw to let caller know operation failed
            }
            finally
            {
                // Dispose transaction FIRST AND its connection
                transaction?.Dispose();
                connection?.Dispose();
            }
        }


        public static int BulkPriceAppendEx(List<PriceDataRS> data)
        {
            int dataSetId = data.Count > 0 ? data[0].DataSetId : -1;
            if (dataSetId > 0 && data.Count > 0)
            {
                string tableName = "PriceData";
                DataTable table = new(tableName);
                table.Columns.Add(new DataColumn("Id", typeof(int)));
                table.Columns.Add(new DataColumn("DataSetId", typeof(int)));
                table.Columns.Add(new DataColumn("RawTime", typeof(long)));
                table.Columns.Add(new DataColumn("PriceDate", typeof(DateTime)));
                table.Columns.Add(new DataColumn("Open", typeof(decimal)));
                table.Columns.Add(new DataColumn("High", typeof(decimal)));
                table.Columns.Add(new DataColumn("Low", typeof(decimal)));
                table.Columns.Add(new DataColumn("Close", typeof(decimal)));
                table.Columns.Add(new DataColumn("Volume", typeof(int)));

                var _createRow = new Func<PriceDataRS, DataRow>(rec =>
                {
                    DataRow row = table.NewRow();
                    row["Id"] = -1;
                    row["DataSetId"] = rec.DataSetId;
                    row["RawTime"] = rec.RawTime;
                    row["PriceDate"] = rec.PriceDate;
                    row["Open"] = rec.Open;
                    row["High"] = rec.High;
                    row["Low"] = rec.Low;
                    row["Close"] = rec.Close;
                    row["Volume"] = rec.Volume;
                    return row;
                });

                using (IEnumerator<PriceDataRS> recEnumerator = data.GetEnumerator())
                {
                    while (recEnumerator.MoveNext())
                    {
                        table.Rows.Add(_createRow(recEnumerator.Current));
                    }
                }
                RBASE.WriteTable(table);
            }
            return 0;
        }

        #endregion PriceData

        #region Event
        public static EventRS? GetEvent(int Id)
        {
            return RBASE.Select<EventRS, int>(new EventRS(), "usp_event_select", "@Id", Id);
        }

        public static EventRS? GetEvent(string eventType, string eventId)
        {
            return RBASE.Select<EventRS, string, string>(new EventRS(), "usp_event_query", "@EventType", eventType, "@EventId", eventId);
        }

        public static List<EventRS> GetEvents()
        {
            return RBASE.SelectMulti<EventRS, int>(new EventRS(), "usp_event_select", "@Id", -1);
        }

        public static List<EventRS> GetActiveEvents(int duration)
        {
            return RBASE.SelectMulti<EventRS, int>(new EventRS(), "usp_event_select_active", "@Seconds", duration);
        }

        public static void TouchPriceDataEvent(int datasetId)
        {
            RBASE.ExecuteScalar<string, string>("usp_event_touch", "@EventType", "PRICEDATA", "@EventId", $"{datasetId}");
        }

        public static void TouchEvent(string eventType, string eventId)
        {
            RBASE.ExecuteScalar<string, string>("usp_event_touch", "@EventType", eventType, "@EventId", eventId);
        }

        public static void CleanUpEvents(string eventType, int duration)
        {
            RBASE.ExecuteScalar<string, int>("usp_event_cleanup", "@EventType", eventType, "@DurationInMin", duration);
        }
        #endregion Event

        #region PriceSrc
        public static List<PriceSrcRS> GetPriceSrcs()
        {
            lock (lockPriceSrcAccess)
            {
                if (priceSrcCache.Count > 0)
                {
                    return priceSrcCache;
                }

                var priceSrc = RBASE.SelectMulti<PriceSrcRS>(new PriceSrcRS(), "usp_price_src_select");
                priceSrc.ForEach(param => priceSrcCache.Add(param));
                return priceSrc;
            }
        }

        public static PriceSrcRS? GetPriceSrc(int id)
        {
            lock (lockPriceSrcAccess)
            {
                for (int ndx = 0; ndx < priceSrcCache.Count; ndx++)
                {
                    if (priceSrcCache[ndx].Id == id)
                    {
                        return priceSrcCache[ndx];
                    }
                }
                return null;
            }
        }

        #endregion PriceSrc

        #region StudyCol
        public static List<StudyColRS> GetStudyColList()
        {
            var recs = RBASE.SelectMulti<StudyColRS, int>(new StudyColRS(), "usp_study_col_select", "@Id", -1);
            recs.ForEach(rec =>
            {
                rec.Ticker = GetTicker(rec.TickerId) ?? new TickerRS();
                rec.Interval = GetInterval(rec.IntervalId) ?? new IntervalRS();
            });
            return recs;
        }

        public static StudyColRS? GetStudyCol(int id)
        {
            var rec = RBASE.Select<StudyColRS, int>(new StudyColRS(), "usp_study_col_select", "@Id", id);
            if (rec == null) return null;
            rec.Ticker = GetTicker(rec.TickerId) ?? new TickerRS();
            rec.Interval = GetInterval(rec.IntervalId) ?? new IntervalRS();
            return rec;
        }
        public static StudyColRS? GetStudyCol(int tickerId, string intervalId, string colType)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@TickerId", tickerId },
                { "@IntervalId", intervalId },
                { "@ColType", colType}
            };
            var rec = RBASE.Select<StudyColRS>(new StudyColRS(), "usp_study_col_query", parameters);
            return rec;
        }

        public static StudyColRS UpsertStudyCol(StudyColRS rec)
        {
            long newId = RBASE.Upsert<StudyColRS>(rec, "usp_study_col_upsert", "@IdNew");
            rec.Id = (int)newId;
            return rec;
        }

        public static void DeleteStudyCol(int id)
        {
            RBASE.Delete<int>("usp_study_col_delete", "@Id", id);
        }

        #endregion StudyCol

        #region StudyHistory

        public static List<StudyHistoryRS> GetTopStudyHistory(int id, int maxRec)
        {
            return RBASE.SelectMulti<StudyHistoryRS, int, int>(new StudyHistoryRS(), "usp_study_history_select_top", "@StudyColId", id, "@MaxRec", maxRec);
        }

        public static StudyHistoryRS? GetLasttudyHistory(int id)
        {
            return RBASE.Select<StudyHistoryRS, int, int>(new StudyHistoryRS(), "usp_study_history_select_top", "@StudyColId", id, "@MaxRec", 1);
        }

        public static List<StudyHistoryRS> GetStudyHistory(int id, long startTime, int maxRec)
        {
            return RBASE.SelectMulti<StudyHistoryRS, int, long, int>(new StudyHistoryRS(), "usp_study_history_select", "@StudyColId", id, "@StartTime", startTime, "@MaxRec", maxRec);
        }

        public static int GetStudyHistoryRecordCount(int id, long startTime)
        {
            return RBASE.ExecuteScalar<int, long>("usp_study_history_count", "@StudyColId", id, "@StartTime", startTime);
        }

        public static bool UpdateStudyHistoryList(List<StudyHistoryRS> data)
        {
            try
            {
                using (SqlConnection connection = new(ConnectionString))
                {
                    connection.Open();

                    data.ForEach(rec =>
                    {
                        RepositoryBase.Update<StudyHistoryRS>(connection, rec, "usp_study_history_update");
                    });
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Update of StudyHistory table failed - {ex.Message}");
                return false;
            }
        }

        public static bool BulkInsertStudyHistory(List<StudyHistoryRS> data, SqlTransaction? tx = null)
        {
            DataTable tableStudyHistory = new("StudyHistory");
            try
            {
                tableStudyHistory.Columns.Add(new DataColumn("Id", typeof(int)));
                tableStudyHistory.Columns.Add(new DataColumn("StudyColId", typeof(int)));
                tableStudyHistory.Columns.Add(new DataColumn("RawTime", typeof(long)));
                tableStudyHistory.Columns.Add(new DataColumn("Open", typeof(decimal)));
                tableStudyHistory.Columns.Add(new DataColumn("High", typeof(decimal)));
                tableStudyHistory.Columns.Add(new DataColumn("Low", typeof(decimal)));
                tableStudyHistory.Columns.Add(new DataColumn("Close", typeof(decimal)));
                tableStudyHistory.Columns.Add(new DataColumn("Volume", typeof(int)));
                tableStudyHistory.Columns.Add(new DataColumn("Studies", typeof(string)));

                var _createRow = new Func<StudyHistoryRS, DataRow>(rec =>
                {
                    DataRow row = tableStudyHistory.NewRow();
                    row["Id"] = -1;
                    row["StudyColId"] = rec.StudyColId;
                    row["RawTime"] = rec.RawTime;
                    row["Open"] = rec.Open;
                    row["High"] = rec.High;
                    row["Low"] = rec.Low;
                    row["Close"] = rec.Close;
                    row["Volume"] = rec.Volume;
                    row["Studies"] = rec.Studies;
                    return row;
                });

                using (IEnumerator<StudyHistoryRS> recEnumerator = data.GetEnumerator())
                {
                    while (recEnumerator.MoveNext())
                    {
                        tableStudyHistory.Rows.Add(_createRow(recEnumerator.Current));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Update of StudyHistory table failed - {ex.Message}");
                throw; // Re-throw to let caller know operation failed
            }

            SqlConnection? localConnection = null;
            try
            {
                SqlConnection connection;
                if (tx == null)
                {
                    localConnection = new SqlConnection(RBASE.ConnectionString);
                    localConnection.Open();
                    connection = localConnection;
                }
                else
                {
                    connection = tx.Connection;
                }

                // Update tableStudyHistory records first
                using (SqlBulkCopy bulkCopy = new(connection, SqlBulkCopyOptions.FireTriggers, tx))
                {
                    bulkCopy.BulkCopyTimeout = 600; // in seconds
                    bulkCopy.DestinationTableName = tableStudyHistory.TableName;
                    bulkCopy.WriteToServer(tableStudyHistory);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"BulkInsertStudyHistory Exception - {ex.Message}");
                throw; // Re-throw to let caller know operation failed
            }
            finally
            {
                // Dispose connection only if created locally
                localConnection?.Dispose();
            }
        }

        public static bool UpdateStudyHistory(List<StudyHistoryRS> data, SqlTransaction? tx = null)
        {
            SqlConnection? connection = null;
            try
            {
                connection = tx == null ? new SqlConnection(RBASE.ConnectionString) : tx.Connection;
                if (tx == null)
                {
                    connection.Open();
                }

                // Update tableStudyHistory records first
                data.ForEach(rec =>
                {
                    RepositoryBase.Update<StudyHistoryRS>(connection, rec, "usp_study_history_update", tx);
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"UpdateStudyHistory Exception - {ex.Message}");
                throw; // Re-throw to let caller know operation failed
            }
            finally
            {
                // Dispose connection if created locally
                if (tx == null)
                {
                    connection?.Dispose();
                }
            }
        }

        #endregion StudyHistory

        #region Setting

        public static SettingRS? GetSetting(string grp, string tag)
        {
            return RBASE.Select<SettingRS, string, string>(new SettingRS(), "usp_setting_select", "@Group", grp, "@Tag", tag);
        }

        public static List<SettingRS> GetSettings(string grp)
        {
            return RBASE.SelectMulti<SettingRS, string, string>(new SettingRS(), "usp_setting_select", "@Group", grp, "@Tag", "");
        }

        public static SettingRS UpsertSetting(SettingRS rec)
        {
            long newId = RBASE.Upsert<SettingRS>(rec, "usp_setting_upsert", "@IdNew");
            rec.Id = (int)newId;
            return rec;
        }

        public static void DeleteSetting(int id)
        {
            RBASE.Delete<int, string, string>("usp_setting_delete", "@Id", id, "@Group", "", "@Tag", "");
        }
        public static void DeleteSetting(string grp, string tag)
        {
            RBASE.Delete<int, string, string>("usp_setting_delete", "@Id", -1, "@Group", grp, "@Tag", tag);
        }

        #endregion Setting

        #region Bot

        public static List<BotRS> GetAutotradeBots()
        {
            return RBASE.SelectMulti<BotRS, int>(new BotRS(), "usp_bot_select", "@Id", -1);
        }

        public static BotRS? GetBotByRefId(string refId)
        {
            return RBASE.Select<BotRS, string>(new BotRS(), "usp_bot_query", "@RefId", refId);
        }

        public static BotRS? GetBot(int id)
        {
            return RBASE.Select<BotRS, int>(new BotRS(), "usp_bot_select", "@Id", id);
        }

        public static BotRS UpsertAutotradeBot(BotRS rec)
        {
            long newId = RBASE.Upsert<BotRS>(rec, "usp_bot_upsert", "@IdNew");
            rec.Id = (int)newId;
            return rec;
        }
        public static int DeleteAutotradeBot(int id)
        {
            return RBASE.ExecuteScalar<int>("usp_bot_delete", "@Id", id);
        }
        public static int SetBotStatus(int id, string status)
        {
            return RBASE.ExecuteScalar<int, string>("usp_bot_set_status", "@Id", id, "@Status", status);
        }

        #endregion Bot

        #region Signal

        public static List<SignalRS> GetSignals()
        {
            var lst = RBASE.SelectMulti<SignalRS, int>(new SignalRS(), "usp_signal_select", "@Id", -1);
            foreach (var item in lst)
            {
                item.StrategyConfig = GetStrategyConfig(item.Strategy);
            }
            return lst;
        }

        public static SignalRS? GetSignal(int id, SqlTransaction? tx = null)
        {
            if (tx != null)
            {
                var signal = RepositoryBase.Select<SignalRS, int>(tx.Connection, new SignalRS(), "usp_signal_select", "@Id", id, tx);
                if (signal != null)
                {
                    signal.StrategyConfig = GetStrategyConfig(signal.Strategy, tx);
                }
                return signal;
            }
            else
            {
                var signal = RBASE.Select<SignalRS, int>(new SignalRS(), "usp_signal_select", "@Id", id);
                if (signal != null)
                {
                    signal.StrategyConfig = GetStrategyConfig(signal.Strategy, tx);
                }
                return signal;
            }
        }

        public static SignalRS? QueryLastSignal(string strategyName, SqlTransaction? tx = null)
        {
            if (tx != null)
            {
                var signal = RepositoryBase.Select<SignalRS, string>(tx.Connection, new SignalRS(), "usp_signal_query_last", "@StrategyName", strategyName, tx);
                if (signal != null)
                {
                    signal.StrategyConfig = GetStrategyConfig(signal.Strategy, tx);
                }
                return signal;
            }
            else
            {
                var signal = RBASE.Select<SignalRS, string>(new SignalRS(), "usp_signal_query_last", "@StrategyName", strategyName);
                if (signal != null)
                {
                    signal.StrategyConfig = GetStrategyConfig(signal.Strategy, tx);
                }
                return signal;
            }
        }

        public static List<SignalRS> QueryLastSignals()
        {
            var lst = RBASE.SelectMulti<SignalRS, string>(new SignalRS(), "usp_signal_query_last", "@StrategyName", "");
            foreach (var item in lst)
            {
                item.StrategyConfig = GetStrategyConfig(item.Strategy);
            }
            return lst;
        }

        public static SignalRS UpsertSignal(SignalRS rec, SqlTransaction? tx = null)
        {
            if (tx != null)
            {
                var idNew = RepositoryBase.Upsert<SignalRS, int>(tx.Connection, rec, "usp_signal_upsert", "@IdNew", tx);
                rec.Id = idNew;
            }
            else
            {
                var idNew = RBASE.Upsert<SignalRS>(rec, "usp_signal_upsert", "@IdNew");
                rec.Id = (int) idNew;
            }
            return rec;
        }


        #endregion Signal

        #region AutoTradeSignal

        public static AutoTradeSignalRS? GetAutoTradeSignal(int signalId, SqlTransaction? tx = null)
        {
            if (tx == null)
            {
                var rec = RBASE.Select<AutoTradeSignalRS, int>(new AutoTradeSignalRS(), "usp_autotrade_signal_select", "@Id", signalId);
                if (rec != null)
                {
                    rec.LastOrder = QueryLastAutoTradeSignalOrder(rec.Id, tx);
                }
                return rec;
            }
            else
            {
                var rec = RepositoryBase.Select<AutoTradeSignalRS, int>(tx.Connection, new AutoTradeSignalRS(), "usp_autotrade_signal_select", "@Id", signalId, tx);
                if (rec != null)
                {
                    rec.LastOrder = QueryLastAutoTradeSignalOrder(rec.Id, tx);
                }
                return rec;
            }
        }
        public static AutoTradeSignalRS? GetTopAutoTradeSignal(int autoTradeId, SqlTransaction? tx = null)
        {
            if (tx == null)
            {
                var rec = RBASE.Select<AutoTradeSignalRS, int>(new AutoTradeSignalRS(), "usp_autotrade_signal_query_last", "@AutoTradeId", autoTradeId);
                if (rec != null)
                {
                    rec.LastOrder = QueryLastAutoTradeSignalOrder(rec.Id, tx);
                }
                return rec;
            }
            else
            {
                var rec = RepositoryBase.Select<AutoTradeSignalRS, int>(tx.Connection, new AutoTradeSignalRS(), "usp_autotrade_signal_query_last", "@AutoTradeId", autoTradeId, tx);
                if (rec != null)
                {
                    rec.LastOrder = QueryLastAutoTradeSignalOrder(rec.Id, tx);
                }
                return rec;
            }
        }
        public static List<AutoTradeSignalRS> GetTopAutoTradeSignals(int autoTradeId, SqlTransaction? tx = null)
        {
            if (tx == null)
            {
                var lst = RBASE.SelectMulti<AutoTradeSignalRS, int>(new AutoTradeSignalRS(), "usp_autotrade_signal_query_top", "@AutoTradeId", autoTradeId);
                lst.ForEach(rec => rec.LastOrder = QueryLastAutoTradeSignalOrder(rec.Id, tx));
                return lst;
            }
            else
            {
                var lst = RepositoryBase.SelectMulti<AutoTradeSignalRS, int>(tx.Connection, new AutoTradeSignalRS(), "usp_autotrade_signal_query_top", "@AutoTradeId", autoTradeId, tx);
                lst.ForEach(rec => rec.LastOrder = QueryLastAutoTradeSignalOrder(rec.Id, tx));
                return lst;
            }
        }
        public static List<AutoTradeSignalRS> GetAutoTradeSignalsFromSingalId(int signalId, SqlTransaction? tx = null)
        {
            if (tx == null)
            {
                var lst = RBASE.SelectMulti<AutoTradeSignalRS, int>(new AutoTradeSignalRS(), "usp_autotrade_signal_query_signalid", "@SignalId", signalId);
                lst.ForEach(rec => rec.LastOrder = QueryLastAutoTradeSignalOrder(rec.Id, tx));
                return lst;
            }
            else
            {
                var lst = RepositoryBase.SelectMulti<AutoTradeSignalRS, int>(tx.Connection, new AutoTradeSignalRS(), "usp_autotrade_signal_query_signalid", "@SignalId", signalId, tx);
                lst.ForEach(rec => rec.LastOrder = QueryLastAutoTradeSignalOrder(rec.Id, tx));
                return lst;
            }
        }

        public static AutoTradeSignalRS UpsertAutoTradeSignal(AutoTradeSignalRS rec, SqlTransaction? tx = null)
        {
            if (tx == null)
            {
                int idNew = (int) RBASE.Upsert<AutoTradeSignalRS>(rec, "usp_autotrade_signal_upsert", "@IdNew");
                rec.Id = idNew;
                return rec;
            }
            else
            {
                int idNew = RepositoryBase.Upsert<AutoTradeSignalRS, int>(tx.Connection, rec, "usp_autotrade_signal_upsert", "@IdNew", tx);
                rec.Id = idNew;
                return rec;
            }
        }


        public static int SetAutoTradeSignalManualQty(int id, int manualQty, SqlTransaction? tx = null)
        {
            if (tx == null)
            {
                return RBASE.ExecuteScalar<int, int>("usp_autotrade_signal_set_manual_qty", "@Id", id, "@ManualQty", manualQty);
            }
            else
            {
                return RepositoryBase.ExecuteScalar<int, int>(tx.Connection, "usp_autotrade_signal_set_manual_qty", "@Id", id, "@ManualQty", manualQty, tx);
            }
        }

        public static List<ArchivedSignalRS> GetArchivedAutoTradeSignals()
        {
            return RBASE.SelectMulti<ArchivedSignalRS>(new ArchivedSignalRS(), "usp_autotrade_signal_archive_query_signals");
        }

        public static List<ArchivedAutoTradeSignalRS> GetArchivedAutoTradeSignal(string signalTag)
        {
            return RBASE.SelectMulti<ArchivedAutoTradeSignalRS, string>(new ArchivedAutoTradeSignalRS(), "usp_autotrade_signal_archive_query", "@SignalTag", signalTag);
        }

        #endregion AutoTradeSignal

        #region AutoTradeSignalOrder
        public static List<AutoTradeSignalOrderRS> GetAutoTradeSignalOrdersByAutoTradeSignalId(int autoTradeSignalId, SqlTransaction? tx = null)
        {
            if (tx == null)
            {
                return RBASE.SelectMulti<AutoTradeSignalOrderRS, int>(new AutoTradeSignalOrderRS(), "usp_autotrade_signal_order_query_by_autotrade_signal_id", "@AutoTradeSignalId", autoTradeSignalId);
            }
            else
            {
                return RepositoryBase.SelectMulti<AutoTradeSignalOrderRS, int>(tx.Connection, new AutoTradeSignalOrderRS(), "usp_autotrade_signal_order_query_by_autotrade_signal_id", "@AutoTradeSignalId", autoTradeSignalId, tx);
            }
        }

        public static AutoTradeSignalOrderRS? GetAutoTradeSignalOrder(int id, SqlTransaction? tx = null)
        {
            if (tx == null)
            {
                return RBASE.Select<AutoTradeSignalOrderRS, int>(new AutoTradeSignalOrderRS(), "usp_autotrade_signal_order_select", "@Id", id);
            }
            else
            {
                return RepositoryBase.Select<AutoTradeSignalOrderRS, int>(tx.Connection, new AutoTradeSignalOrderRS(), "usp_autotrade_signal_order_select", "@Id", id, tx);
            }
        }
        public static AutoTradeSignalOrderRS? GetAutoTradeSignalOrderByRequestRef(string requestRef, SqlTransaction? tx = null)
        {
            if (tx == null)
            {
                return RBASE.Select<AutoTradeSignalOrderRS, string>(new AutoTradeSignalOrderRS(), "usp_autotrade_signal_order_query_by_requestref", "@RequestRef", requestRef);
            }
            else
            {
                return RepositoryBase.Select<AutoTradeSignalOrderRS, string>(tx.Connection, new AutoTradeSignalOrderRS(), "usp_autotrade_signal_order_query_by_requestref", "@RequestRef", requestRef, tx);
            }
        }
        public static AutoTradeSignalOrderRS? QueryLastAutoTradeSignalOrder(int autoTradeSignalId, SqlTransaction? tx = null)
        {
            if (tx == null)
            {
                return RBASE.Select<AutoTradeSignalOrderRS, int>(new AutoTradeSignalOrderRS(), "usp_autotrade_signal_order_query_last", "@AutoTradeSignalId", autoTradeSignalId);
            }
            else
            {
                return RepositoryBase.Select<AutoTradeSignalOrderRS, int>(tx.Connection, new AutoTradeSignalOrderRS(), "usp_autotrade_signal_order_query_last", "@AutoTradeSignalId", autoTradeSignalId, tx);
            }
        }

        public static AutoTradeSignalOrderRS UpsertAutoTradeSignalOrder(AutoTradeSignalOrderRS rec, SqlTransaction? tx = null)
        {
            if (tx == null)
            {
                int idNew = (int)RBASE.Upsert<AutoTradeSignalOrderRS>(rec, "usp_autotrade_signal_order_upsert", "@IdNew");
                rec.Id = idNew;
                return rec;
            }
            else
            {
                int idNew = RepositoryBase.Upsert<AutoTradeSignalOrderRS, int>(tx.Connection, rec, "usp_autotrade_signal_order_upsert", "@IdNew", tx);
                rec.Id = idNew;
                return rec;
            }
        }

        public static int CancelAutoTradeSignalOrder(int id, SqlTransaction? tx = null)
        {
            if (id <= 0)  return -1;
            if (tx == null)
            {
                return RBASE.ExecuteScalar<int>("usp_autotrade_signal_order_cancel", "@Id", id);
            }
            else
            {
                return RepositoryBase.ExecuteScalar<int>(tx.Connection, "usp_autotrade_signal_order_cancel", "@Id", id, tx);
            }
        }
        #endregion AutoTradeSignalOrder

        #region StatusCode

        public static List<StatusCodeRS> GetStatusCodes()
        {
            return RBASE.SelectMulti<StatusCodeRS>(new StatusCodeRS(), "usp_statuscode_select");
        }

        #endregion StatusCode

        #region DatabaseAdmin
        public static DBUserStatusRS? Master_CheckUserConfigured(string username)
        {
            string sql = $"SELECT '{username}' AS LoginUser, CASE WHEN EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '{username}') THEN 1 ELSE 0 END AS LoginExists;";
            return RBASEADMIN.Select<DBUserStatusRS>(new DBUserStatusRS(), sql);
        }

        public static List<DBUserValidationRS> ValidateUserOnDatabase(string username, List<string> dbNames)
        {
            // create a string of comma separated database names
            string dbNamesCsv = string.Join(",", dbNames);
            return RBASEADMIN.SelectMulti<DBUserValidationRS, string , string>(new DBUserValidationRS(), "usp_admin_check_login_membership", "@LoginName", username, "@DbListCsv", dbNamesCsv);
        }

        public static bool Master_RegisterUserToDB(string username, string dbName)
        {
            try
            {
                string sql = $"USE [{dbName}];\r\nCREATE USER [{username}] FOR LOGIN [{username}];\r\n;";
                RBASEADMIN.ExecutScript(sql);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool Master_FixUserSIDMapping(string username, string dbName)
        {
            try
            {
                string sql = $"USE [{dbName}];";
                sql += $"IF NOT EXISTS(SELECT 1 FROM sys.database_principals WHERE name = '{username}')\r\n";
                sql += $"BEGIN CREATE USER[{username}] FOR LOGIN[{username}]; END\r\n";
                sql += $"ELSE BEGIN ALTER USER[{username}] WITH LOGIN = [{username}]; END\r\n";
                RBASEADMIN.ExecutScript(sql);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool Master_SetUserOwnership(string username, string dbName)
        {
            try
            {
                //string sql = $"ALTER AUTHORIZATION ON DATABASE::[{dbName}] TO[{username}];";
                string sql = $"USE[{dbName}]; ALTER ROLE[db_owner] ADD MEMBER[{username}];";
                RBASEADMIN.ExecutScript(sql);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static DBBrokerValidationRS? ValidateDBBroker(string username, string dbName)
        {
            return RBASEADMIN.Select<DBBrokerValidationRS, string, string>(new DBBrokerValidationRS(), "usp_admin_get_query_notification_status", "@DbName", dbName, "@LoginName", username);
        }

        public static bool FixDBBroker(string username, string dbName)
        {
            try
            {
                //Enable Broker on database and grany user permission for "Subscribe Query Notifications"
                string sql = $"USE master; ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; ALTER DATABASE [{dbName}] SET ENABLE_BROKER; ALTER DATABASE [{dbName}] SET MULTI_USER;\r\n";
                sql += $"USE [{dbName}]; GRANT SUBSCRIBE QUERY NOTIFICATIONS TO {username};\r\n";
                RBASEADMIN.ExecutScript(sql);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static List<UserRS> GetUsers(string usersDB)
        {
            return RBASEADMIN.SelectMulti<UserRS>(new UserRS(), $"[{usersDB}].[dbo].usp_user_select_all");
        }

        public static UserRS? GetUser(string username, string usersDB)
        {
            return RBASEADMIN.Select<UserRS, string>(new UserRS(), $"[{usersDB}].[dbo].usp_user_query", "@UserName", username);
        }

        //public static bool IsBrokerEnabled()
        //{
        //    SysDatabasesRS? rs = RBASEADMIN.Select<SysDatabasesRS, string>(new SysDatabasesRS(), "usp_admin_databases_select", "@DBName", DatabaseName);
        //    return rs?.Is_broker_enabled ?? false;
        //}

        #endregion DatabaseAdmin

        #region Devices
        public static DeviceRS? GetDeviceByConnection(string connectionId)
        {
            return RBASE.Select<DeviceRS, string>(new DeviceRS(), $"usp_device_query_by_connection", "@ConnectionId", connectionId);
        }

        public static void UpsertDevice(DeviceRS device)
        {
            RBASE.Upsert<DeviceRS>(device, "usp_device_upsert", "@IdNew");
        }

        public static DeviceRS? GetDevice(string id)
        {
            return RBASE.Select<DeviceRS, string>(new DeviceRS(), $"usp_device_select", "@Id", id);
        }

        public static List<DeviceRS> GetDevicesByRole(int role)
        {
            return RBASE.SelectMulti<DeviceRS, int>(new DeviceRS(), $"usp_device_query_by_role", "@Role", role);
        }

        public static List<DeviceRS> GetActiveDevices()
        {
            return RBASE.SelectMulti<DeviceRS>(new DeviceRS(), $"usp_device_select_active");
        }

        public static void InvalidateDevice(string id, string connectionId)
        {
            RBASE.ExecuteScalar<string, string>("usp_device_invalidate", "@Id", id, "@ConnectionId", connectionId);
        }

        public static int SetDeviceEnabled(string id, bool enabled)
        {
            return RBASE.ExecuteScalar<string, bool>("usp_device_set_enabled", "@Id", id, "@Enabled", enabled);
        }

        public static int DeleteDevice(string id)
        {
            return RBASE.ExecuteScalar<string>("usp_device_delete", "@Id", id);
        }

        public static List<DeviceRS> GetDevices()
        {
            return RBASE.SelectMulti<DeviceRS, string>(new DeviceRS(), $"usp_device_select", "@Id", "");
        }


        #endregion Devices

        #region SystemAlert
        public static List<SystemAlertRS> GetPendingSystemAlerts(int maxRec)
        {
            return RBASE.SelectMulti<SystemAlertRS, int>(new SystemAlertRS(), "usp_systemalert_select_pending", "@MaxRec", maxRec);
        }
        #endregion SystemAlert


    }
}

