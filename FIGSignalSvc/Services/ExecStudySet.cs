using FIGCommon.DataAccess;
using FIGCommon.Models;
using FIGCommon.Utilities;
using FIG.Studies;
using FIGSignalExSvc.Plugins;
using FIGSignalExSvc.Utilities;
using System.Text.Json;

namespace FIGSignalExSvc.Services
{

    public class ExecStudySet
    {
        private readonly ILogger<ExecStudySet> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IStudyPluginCatalog _pluginCatalog;

        protected object lockObj = new object();
        public StudyColRS studyColRec = new StudyColRS();
        public TickerRS ticker = new TickerRS();
        public IntervalRS studyInterval = new IntervalRS();

        protected StudyColBase? studyParams = null;
        protected int minNumStudies = 10;
        protected int numStudyHistoryRecToUdate = 3;
        //protected int numFetch = 500000;
        protected int numFetch = 100000;
        //protected int numFetch = 1000;  // to test

        protected DataSetRS? oneMinPriceDataSet = null;
        protected List<PriceDataRS> oneMinPriceData = new();

        private readonly StudySignal _studySignal;

        public ExecStudySet(StudyColRS studyCol, IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider?.GetRequiredService<ILogger<ExecStudySet>>() ?? throw new ArgumentNullException(nameof(_logger)); ;
            _loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            _pluginCatalog = _serviceProvider.GetRequiredService<IStudyPluginCatalog>();
            InitStudySet(studyCol);
            _studySignal = new StudySignal(studyCol, serviceProvider);
        }

        private void InitStudySet(StudyColRS studyCol)
        {
            try
            {
                studyColRec = new StudyColRS(studyCol);
                ticker = MainRepo.GetTicker(studyColRec.TickerId) ?? new TickerRS();
                studyInterval = MainRepo.GetInterval(studyColRec.IntervalId) ?? new IntervalRS();
                if (ticker.Id == -1 || studyInterval.Id == "")
                {
                    throw new Exception($"Ticker Id \"{studyColRec.TickerId}\" or Interval Id \"{studyColRec.IntervalId}\" is not found");
                }

                if (!studyCol.Enabled)
                {
                    throw new Exception($"StudyCol Id \"{studyColRec.Id}\" is not enabled");
                }
                var collectionContext = new StudyCollectionContext(
                    studyColRec,
                    ticker,
                    studyInterval,
                    _loggerFactory);
                studyParams = _pluginCatalog.Create(studyColRec.ColType, collectionContext);

                if (oneMinPriceDataSet == null)
                {
                    oneMinPriceDataSet = MainRepo.QueryDataSet(studyColRec.TickerId, "1");
                }
                if (oneMinPriceDataSet == null)
                {
                    throw new Exception($"1Min DataSet for Ticker Id \"{studyColRec.TickerId}\" is not found");
                }
            }
            catch (Exception ex) {
                _logger.LogDebug("Error creating new StudySet for studyCol id ({0}) - {1}", studyCol.Id, ex.Message);
                throw;
            }
        }

        public void Process()
        {
            if (ProcessInternal())
            {
                Task.Delay(100);
                // check if there is still much more entries to process
                var lastStudyHistory = MainRepo.GetTopStudyHistory(studyColRec.Id, 1);
                var lastPriceData = MainRepo.GetLastPriceData(oneMinPriceDataSet?.Id ?? 0);
                if (lastStudyHistory.Count > 0 && lastPriceData?.RawTime > lastStudyHistory[0].RawTime)
                {
                    if ((lastPriceData.RawTime - lastStudyHistory[0].RawTime) > studyInterval.IntervalLen * 2)
                    {
                        Process();
                    }
                }
            }
        }
        public bool ProcessInternal()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                Converters = { new DecimalFormatConverter(), new DecimalNullableFormatConverter() }
            };

            if (studyParams == null)
            {
                throw new Exception($"StudySet for StudyCol Id \"{studyColRec.Id}\" is not initalized");
            }

            // get top 5 records from StudyHistory table (Sorted ASC)
            var lastStudyHistory = MainRepo.GetTopStudyHistory(studyColRec.Id, 5);
            bool isHistEmpty = lastStudyHistory.Count == 0;
            int minOneMinPriceCount = studyParams?.GetMaxLen() * studyInterval.IntervalLen / 60 + 500 ?? 0;
            List<BarStudy> studyList = new List<BarStudy>();

            if (oneMinPriceData.Count == 0)  // loading first-time
            {
                if (isHistEmpty)
                {
                    // build from start, get upto numFetch records
                    oneMinPriceData = MainRepo.GetPriceData(oneMinPriceDataSet?.Id ?? 0, 0, numFetch);
                    var studyPriceData = CompilePriceEx(oneMinPriceData, studyInterval.IntervalLen);
                    studyList = studyParams?.Calc(studyPriceData, 0) ?? new();
                }
                else
                {
                    // load enough data to calculate studies 
                    oneMinPriceData = MainRepo.GetPriceDataUntil(oneMinPriceDataSet?.Id ?? 0, lastStudyHistory[0].RawTime, minOneMinPriceCount);
                    var studyPriceData = CompilePriceEx(oneMinPriceData, studyInterval.IntervalLen);
                    studyList = studyParams?.Calc(studyPriceData, 0) ?? new();
                }
            }
            else // one-minute price is already loaded (not first time)
            {
                // StartRawTime is the time to get data from (starting from last study time or in the case if no study available )
                var startRawTime = lastStudyHistory.Count > 0 ? lastStudyHistory[0].RawTime : 0;
                var tickerOneMinData = MainRepo.GetPriceData(oneMinPriceDataSet?.Id ?? 0, startRawTime, numFetch);  // numFetch at a time
                AddPriceRange(oneMinPriceData, tickerOneMinData);
                var studyPriceData = CompilePriceEx(tickerOneMinData, studyInterval.IntervalLen);
                studyList = studyParams?.Calc(studyPriceData, startRawTime > 0 ? numStudyHistoryRecToUdate : 0) ?? new();
            }
            try
            {
                List<StudyHistoryRS> studyHistoryToBulkInsert = new();
                List<StudyHistoryRS> studyHistoryToUpdate = new();
                studyList.ForEach(study =>
                {
                    long minRawTimeToInsert = lastStudyHistory.Count > 0 ? lastStudyHistory[lastStudyHistory.Count - 1].RawTime + 1 : 0;
                    long minRawTimeToUpdate = lastStudyHistory.Count > 0 ? lastStudyHistory[0].RawTime : long.MaxValue;

                    if (study.Price.RawTime > minRawTimeToInsert)
                    {
                        var studyHistory = new StudyHistoryRS
                        {
                            StudyColId = studyColRec.Id,
                            RawTime = study.Price.RawTime,
                            Open = study.Price.Open,
                            High = study.Price.High,
                            Low = study.Price.Low,
                            Close = study.Price.Close,
                            Volume = study.Price.Volume,
                            Studies = JsonSerializer.Serialize(study.Studies, options),
                            rawStudies = study.Studies
                        };
                        studyHistoryToBulkInsert.Add(studyHistory);
                    }
                    else if (study.Price.RawTime >= minRawTimeToUpdate)
                    {
                        var studyHistory = new StudyHistoryRS
                        {
                            StudyColId = studyColRec.Id,
                            RawTime = study.Price.RawTime,
                            Open = study.Price.Open,
                            High = study.Price.High,
                            Low = study.Price.Low,
                            Close = study.Price.Close,
                            Volume = study.Price.Volume,
                            Studies = JsonSerializer.Serialize(study.Studies, options),
                            rawStudies = study.Studies
                        };
                        studyHistoryToUpdate.Add(studyHistory);
                    }

                });
                _logger.LogDebug("StudyCol Id ({0}) - StudyHistory to Update: {1}, to Bulk Insert: {2}", studyColRec.Id, studyHistoryToUpdate.Count, studyHistoryToBulkInsert.Count);
                if (studyHistoryToUpdate.Count > 0)
                {
                    _studySignal.ProcessSignals(studyHistoryToUpdate, true);
                    ////List<StudyHistoryRS> lastStudyHistoryToUpdate = new();
                    ////lastStudyHistoryToUpdate.Add(studyHistoryToUpdate[studyHistoryToUpdate.Count - 1]);
                    //if (MainRepo.UpdateStudyHistoryList(studyHistoryToUpdate))
                    //{
                    //    _studySignal.ProcessSignals(lastStudyHistoryToUpdate);
                    //}
                }
                if (studyHistoryToBulkInsert.Count > 0)
                {
                    _studySignal.ProcessSignals(studyHistoryToBulkInsert, false);
                    //if (MainRepo.BulkInsertStudyHistory(studyHistoryToBulkInsert))
                    //{
                    //    _studySignal.ProcessSignals(studyHistoryToBulkInsert);
                    //}
                }
                if (oneMinPriceData.Count > minOneMinPriceCount)
                {
                    // truncate oneMinPriceData to manage memory
                    oneMinPriceData.RemoveRange(0, oneMinPriceData.Count - minOneMinPriceCount);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inserting studies into StudyHistory Table - {ex.Message}");
            }
        }

        public static List<PriceDataRS> CompilePriceEx(List<PriceDataRS> priceList, int intervalLen)
        {
            List<PriceDataRS> lst = new();
            if (intervalLen == 60)
            {
                lst.AddRange(priceList);
            }
            else if (intervalLen > 60)
            {
                var tickerData = PriceAggregatorUtil.AggregatePrices(priceList, intervalLen);
                if (tickerData.Count > 0)
                {
                    lst.AddRange(tickerData);
                    if (lst.Count > 1 && (lst[0].RawTime % intervalLen) > 0)
                    {
                        lst.RemoveAt(0);
                    }
                }
            }
            return lst;
        }

        public static void AddPriceRange(List<PriceDataRS> priceData, List<PriceDataRS> tickerData)
        {
            if (priceData.Count == 0)
            {
                priceData.AddRange(tickerData);
            }
            else
            {
                tickerData.ForEach(tickerPrice =>
                {
                    if (tickerPrice != null)
                    {
                        if (tickerPrice.RawTime > priceData[priceData.Count - 1].RawTime)
                        {
                            priceData.Add(tickerPrice);
                        }
                        else
                        {
                            // update existing
                            for (int ndx = priceData.Count - 1, cntr = 0; ndx >= 0; ndx--, cntr++)
                            {
                                if (priceData[ndx].RawTime == tickerPrice.RawTime)
                                {
                                    priceData[ndx].Open = tickerPrice.Open;
                                    priceData[ndx].High = tickerPrice.High;
                                    priceData[ndx].Low = tickerPrice.Low;
                                    priceData[ndx].Close = tickerPrice.Close;
                                    priceData[ndx].Volume = tickerPrice.Volume;
                                    break;
                                }
                                if (cntr > 10)
                                {
                                    break;
                                }
                            }
                        }
                    }
                });
            }
        }

        public static PriceDataRS? FindIntervalTime(PriceDataRS oneMinPrice, List<PriceDataRS> priceData, int intervalLen, ref int lastPriceNdx)
        {
            PriceDataRS? price = null;
            if (priceData.Count == 0)
            {
                return null;
            }
            long remainderSec = oneMinPrice.RawTime % intervalLen;
            long rawTimeToFind = oneMinPrice.RawTime - remainderSec;
            int closestNdx = -1;
            for (int ndx = lastPriceNdx; ndx < priceData.Count; ndx++)
            {
                if (priceData[ndx].RawTime < rawTimeToFind)
                {
                    closestNdx = ndx;
                }
                if (priceData[ndx].RawTime == rawTimeToFind)
                {
                    lastPriceNdx = ndx;
                    return new PriceDataRS(priceData[ndx]);
                }
            }
            if (closestNdx >= 0)
            {
                lastPriceNdx = closestNdx;
                return priceData[lastPriceNdx];
            }
            lastPriceNdx = priceData.Count - 1;
            return price;
        }
    }
}


