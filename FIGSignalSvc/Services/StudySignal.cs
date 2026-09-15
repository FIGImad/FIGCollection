using FIGCommon.DataAccess;
using FIGCommon.Models;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGSignalExSvc.Services
{
    // structure to hold Side, Price
    public struct BarInfoStruct { public decimal? Side; public decimal? Price; public string Event; }

    public class StudySignal
    {
        private readonly ILogger<StudySignal> _logger;
        private readonly IServiceProvider _serviceProvider;
        protected readonly IConfiguration _config;

        //protected object lockObj = new object();
        protected StudyColRS studyColRec = new StudyColRS();
        protected TickerRS ticker;
        protected IntervalRS studyInterval;
        protected DataSetRS oneMinDataSet;

        protected List<StrategyConfigRS> strategies = new List<StrategyConfigRS>();

        protected Dictionary<string, StrategyConfigRS> strategyConfMap = new Dictionary<string, StrategyConfigRS>();
        protected Dictionary<string, BarInfoStruct> activeBarEvents = new Dictionary<string, BarInfoStruct>();
        protected Dictionary<string, List<SignalRS>?> activeSignalsMap = new Dictionary<string, List<SignalRS>?>();
        protected long lastRawTime = -1;

        protected int timeOffset = 5;  // configurable from init file
        protected bool isLive = true;

        public StudySignal(StudyColRS studyCol, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider?.GetRequiredService<ILogger<StudySignal>>() ?? throw new ArgumentNullException(nameof(_logger));
            _config = serviceProvider.GetRequiredService<IConfiguration>() ?? throw new ArgumentNullException(nameof(_config));
            try
            {
                studyColRec = new StudyColRS(studyCol);
                ticker = MainRepo.GetTicker(studyColRec.TickerId) ?? new TickerRS();
                studyInterval = MainRepo.GetInterval(studyColRec.IntervalId) ?? new IntervalRS();
                oneMinDataSet = MainRepo.QueryDataSet(studyColRec.TickerId, "1") ?? new DataSetRS();
                if (ticker.Id == -1 || studyInterval.Id == "")
                {
                    throw new Exception($"Ticker Id \"{studyColRec.TickerId}\" or Interval Id \"{studyColRec.IntervalId}\" is not found");
                }
                if (oneMinDataSet.Id == -1)
                {
                    throw new Exception($"Ticker Id \"{studyColRec.TickerId}\" DataSet for 1 min interval is not found");
                }
                strategies = MainRepo.GetStrategyConfigs();
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error creating new StudySignal for studyCol id ({0}) - {1}", studyCol.Id, ex.Message);
                throw;
            }
            timeOffset = _config.GetValue<int?>("SingalProcessing:TimeOffset") ?? timeOffset;
            isLive = _config.GetValue<bool?>("SingalProcessing:LiveMode") ?? isLive;

            StudyHistoryRS? lastStudyHistoryBar = MainRepo.GetLasttudyHistory(studyColRec.Id);
            if (lastStudyHistoryBar != null) lastRawTime = lastStudyHistoryBar.RawTime;

            // Get Strategy Configs for this studyColRec.Id and populate strategyConfMap
            var strategyConfigs = MainRepo.QueryStrategyConfigs(studyColRec.Id);
            if (strategyConfigs != null)
            {
                foreach (var config in strategyConfigs)
                {
                    strategyConfMap[config.Strategy] = config;
                }
            }
        }

        protected EnumBarStatus GetBarStatus(StudyHistoryRS studyHistoryRec)
        {
            int intervalLen = studyInterval.IntervalLen;

            long currentRawTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Shift "now" forward so 04:04:55 can already be considered part of 04:05:00 bar
            long effectiveNow = currentRawTime + timeOffset;

            // This is the bar that should currently be active according to your shifted clock
            long effectiveBarStart = (effectiveNow / intervalLen) * intervalLen;

            if (studyHistoryRec.RawTime < effectiveBarStart)
            {
                return EnumBarStatus.NewBar;      // older/completed bar
            }

            if (studyHistoryRec.RawTime == effectiveBarStart)
            {
                return EnumBarStatus.CurrentBar;  // currently forming bar
            }

            // Future bar relative to effective time; unusual case
            return EnumBarStatus.CurrentBar;
        }

        //protected EnumBarStatus GetBarStatusOld(StudyHistoryRS studyHistoryRec)
        //{
        //    int intervalLen = studyInterval.IntervalLen;
        //    // determine status of Bar

        //    // get current EPOCH time in seconds
        //    long currentRawTime = (long)(DateTime.UtcNow.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

        //    if (!isLive)
        //    {
        //        var lastPrice = MainRepo.GetLastPriceData(oneMinDataSet.Id);
        //        currentRawTime = lastPrice != null ? (lastPrice.RawTime + 61 - timeOffset) : long.MaxValue;
        //    }

        //    // next check, check if studyHistoryRec.RawTime is a new bar
        //    long studyRawTime = (currentRawTime / intervalLen) * intervalLen;
        //    long nextStudyRawTime = ((currentRawTime + timeOffset) / intervalLen) * intervalLen;
        //    if ((studyHistoryRec.RawTime + intervalLen) < studyRawTime)
        //    {
        //        return EnumBarStatus.NewBar;
        //    }
        //    else if (nextStudyRawTime > studyRawTime)
        //    {
        //        return EnumBarStatus.NewBar;

        //    }
        //    return EnumBarStatus.CurrentBar;
        //}

        protected void AddSignal(StudyHistoryRS study, string strategy, decimal side)
        {
            // this is a new signal
            long startTime = study.RawTime /* + studyInterval.IntervalLen */;
            // get StrategyConfigId from strategyConfMap using strategy as key, if not found, log error and return
            if (!strategyConfMap.ContainsKey(strategy))
            {
                return;
            }
            var strategyConf = strategyConfMap[strategy];
            var signal = new SignalRS()
            {
                Strategy = strategyConf.Strategy,
                Tag = Guid.NewGuid().ToString(),
                Side = side,
                Status = SignalStatus.Start,
                StartTime = startTime,
                StartPrice = study.Close,
                StopTime = null,
                StopPrice = null,
                LastUpdated = -1

            };
            
            var signalList = activeSignalsMap.ContainsKey(strategy) ? activeSignalsMap[strategy] : null;
            if (signalList != null)
            {
                // add signal
                signalList.Add(signal);
            }
            //MainRepo.UpsertSignal(signal);
        }

        protected void CloseSignal(StudyHistoryRS study, SignalRS lastSignal)
        {
            long stopTime = study.RawTime /* + studyInterval.IntervalLen */;

            lastSignal.Status = SignalStatus.Stop;
            lastSignal.StopTime = stopTime;
            lastSignal.StopPrice = study.Close;
            lastSignal.LastUpdated = -1;
            //MainRepo.UpsertSignal(lastSignal);
        }

        public void ProcessSignals(List<StudyHistoryRS> studyHistory, bool updateOnly)
        {
            SqlTransaction? tx = null;
            List<StudyHistoryRS> studyHistoryList = studyHistory;
            activeSignalsMap.Clear();
            if (updateOnly && studyHistory.Count > 0)
            {
                studyHistoryList = new List<StudyHistoryRS>();
                // add last item only 
                studyHistoryList.Add(studyHistory[studyHistory.Count - 1]);
            }
            try
            {
                if (strategies.Count != 0)
                {
                    foreach (var study in studyHistoryList)
                    {
                        // process only last study history record
                        //if (lastRawTime == -1)
                        //{
                        //    StudyHistoryRS? lastStudyHistoryBar = MainRepo.GetLasttudyHistory(studyColRec.Id);
                        //    if (lastStudyHistoryBar != null) lastRawTime = lastStudyHistoryBar.RawTime;
                        //}
                        if (study.RawTime < lastRawTime) continue;

                        EnumBarStatus barStatus = GetBarStatus(study);
                        if (barStatus != EnumBarStatus.NewBar) continue;

                        //_logger.LogInformation($"Processing StudyHistory for StudyColId {studyColRec.Id} at RawTime {study.RawTime} with BarStatus {barStatus}");

                        var studies = study.rawStudies;
                        if (studies != null && studies.Count > 0)
                        {
                            foreach (var strategy in strategies)
                            {
                                string key = $"{strategy.Strategy}_SIDE";
                                if (!studies.ContainsKey(key)) continue;
                                var studySide = studies[key];
                                var side = Get<decimal?>(studySide) ?? 0.0M;
                                // find the last signal from database that matches strategy.Name and studyColRec.Id
                                 var lastSignal = QueryLastActiveSignals(strategy.Strategy);

                                // this could be a start signal or just another bar of the same signal
                                if (side != 0.0M)
                                {
                                    if (lastSignal == null)
                                    {
                                        // this is a new signal
                                        AddSignal(study, strategy.Strategy, side);
                                    }
                                    // Check if side is the same, if not close the signal and start a new one
                                    else if (lastSignal.Side != side)
                                    {
                                        // wierd case, should not happen but just in case
                                        _logger.LogError($"Signal {lastSignal.Id} has different side ({lastSignal.Side}) than the new signal ({side}) that is not closed properly");

                                        // cancel last open signal
                                        CloseSignal(study, lastSignal);

                                        // insert new signal
                                        AddSignal(study, strategy.Strategy, side);
                                    }
                                }
                                // otherwise, it could be a stop signal or simply no signal
                                else
                                {
                                    // find the last signal from database that matches strategy.Name and studyColRec.Id
                                    // Check if side is the same, if not close the signal and start a new one
                                    if (lastSignal != null && lastSignal.Status == SignalStatus.Start)
                                    {
                                        // close signal
                                        CloseSignal(study, lastSignal);
                                    }
                                }
                            }
                            lastRawTime = study.RawTime;
                        }
                    }
                }

                List<SignalRS> allSignals = new List<SignalRS>();
                foreach (var signalList in activeSignalsMap.Values)
                {
                    if (signalList == null) continue;
                    signalList.ForEach(signal =>
                    {
                        // only add newly inserted signals (signal.Id == "") or 
                        // signals that changed status to other than Start (Stop or Cancel)
                        if (signal.Id == -1 || signal.Status != SignalStatus.Start)
                        {
                            allSignals.Add(signal);
                        }
                    });
                }

                SqlConnection? txConn = null;
                try
                {
                    tx = MainRepo.OpenTransaction();
                    txConn = tx?.Connection; // capture BEFORE Commit/Rollback clears it
                    if (updateOnly)
                    {
                        MainRepo.UpdateStudyHistory(studyHistoryList, tx);
                    }
                    else
                    {
                        MainRepo.BulkInsertStudyHistory(studyHistoryList, tx);
                    }
                    foreach (var signal in allSignals)
                    {
                        _logger.LogInformation($"Upserting Signal for Strategy {signal.Strategy} with Side {signal.Side} and Status {signal.Status}");
                        MainRepo.UpsertSignal(signal, tx);
                    }
                    tx?.Commit();
                }
                catch(Exception exTx)
                {
                    _logger.LogError(exTx, "Error in ProcessSignals transaction, rolling back");
                    _logger.LogCritical(exTx, "Error in ProcessSignals transaction, rolling back");
                    tx?.Rollback();
                }
                finally
                {
                    tx?.Dispose();
                    txConn?.Dispose(); // returns connection to pool
                }
            }
            catch (Exception ex) 
            { 
                _logger.LogError(ex, "Error in ProcessSignals"); 
                return; 
            }
        }

        public SignalRS? QueryLastActiveSignals(string strategyName)
        {
            var signalList = activeSignalsMap.ContainsKey(strategyName) ? activeSignalsMap[strategyName] : null;
            if (signalList == null)
            {
                // lookup last signal from from database
                SignalRS? lastSignal = MainRepo.QueryLastSignal(strategyName);
                if (lastSignal != null && lastSignal.Status == SignalStatus.Start)  // still active signal
                {
                    // add list
                    activeSignalsMap.Add(strategyName, new List<SignalRS>() { lastSignal });
                    return lastSignal;
                }
                else
                {
                    // add empty list
                    activeSignalsMap.Add(strategyName, new List<SignalRS>());
                    return null;
                }
            }
            var isLastSignalActive = signalList.Count > 0 && signalList[signalList.Count - 1] != null && signalList[signalList.Count - 1].Status == SignalStatus.Start;
            return isLastSignalActive ? signalList[signalList.Count - 1] : null;
        }


        public static T? Get<T>(object? value)
        {
            // If the value is already of type T
            if (value is T tValue)
            {
                return tValue;
            }

            // Handle null values
            if (value == null)
            {
                return default;
            }

            // Special handling for numeric conversions
            try
            {
                Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                // Handle decimal/double conversions explicitly
                if (targetType == typeof(decimal) && value is double d)
                {
                    return (T)(object)Convert.ToDecimal(d);
                }

                if (targetType == typeof(decimal) && value is float f)
                {
                    return (T)(object)Convert.ToDecimal(f);
                }

                // Enum from string or int
                if (targetType.IsEnum)
                {
                    if (value is string s)
                        return (T)Enum.Parse(targetType, s, ignoreCase: true);

                    if (IsNumericType(value.GetType()))
                        return (T)Enum.ToObject(targetType, value);
                }

                // Guid from string
                if (targetType == typeof(Guid) && value is string guidStr)
                    return (T)(object)Guid.Parse(guidStr);

                // DateTimeOffset from string
                if (targetType == typeof(DateTimeOffset) && value is string dtoStr)
                    return (T)(object)DateTimeOffset.Parse(dtoStr);

                // Handle other conversions
                return (T)Convert.ChangeType(value, targetType);
            }
            catch
            {
                return default;
            }
        }

        private static bool IsNumericType(Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16 or
                TypeCode.UInt32 or TypeCode.UInt64 or TypeCode.Int16 or
                TypeCode.Int32 or TypeCode.Int64 or TypeCode.Decimal or
                TypeCode.Double or TypeCode.Single => true,
                _ => false,
            };
        }

    }
}


