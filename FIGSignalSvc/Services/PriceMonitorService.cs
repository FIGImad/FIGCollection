using FIGCommon.DataAccess;
using FIGCommon.Models;
using FIGCommon.Services;
using FIGCommon.Utilities;

namespace FIGSignalExSvc.Services
{
    public class PriceMonitorService : IHostedService, IDisposable
    {
        private const string SRC_NAME = "SignalPriceMonitor";

        private readonly ILogger<PriceMonitorService> _logger;
        protected readonly IServiceProvider _serviceProvider;

        private readonly EventMonitorService _eventMonitor;

        private List<StudyColRS> studyColList = new();
        private readonly object accessListLock = new();
        private DateTime nextStudyColListRefresh = DateTime.MinValue;
        private static Dictionary<int, DataSetRS> datasetMap = new();
        private readonly object accessDatasetLock = new();

        private static Dictionary<int, ExecStudySet?> execStudyMap = new();
        private readonly object accessStudySetLock = new();


        public PriceMonitorService(
            ILogger<PriceMonitorService> logger,
            EventMonitorService eventMonitor,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventMonitor = eventMonitor ?? throw new ArgumentNullException(nameof(eventMonitor));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            RefreshStudyColList();
            ProcessStudyCols();
            _eventMonitor.Subscribe("PRICEDATA", OnPriceChange);
            _logger.LogInformation("Subscribed to PRICEDATA changes");
            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _eventMonitor.Unsubscribe("PRICEDATA", OnPriceChange);
            _logger.LogInformation("Unsubscribed from PRICEDATA changes");
            return Task.CompletedTask;
        }

        private void OnPriceChange(EventRS? eventData)
        {
            try
            {
                _logger.LogInformation("Received EVENTA change: {0}", eventData?.EventId ?? "-1");

                // Process the event data
                ProcessPriceChangeEvent(eventData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing EVENTA change");
            }
        }

        private void ProcessPriceChangeEvent(EventRS? eventData)
        {
            if (eventData == null || eventData.EventType != "PRICEDATA")
            {
                _logger.LogDebug("Invalid Event type: {0}", eventData?.EventType ?? "NULL");
                return;
            }
            int dataSetId = ((int?)TypeConvertUtil.GetIntegerValue(eventData.EventId) ?? -1);
            if (dataSetId == -1)
            {
                _logger.LogDebug("Invalid DataSetId: {0} received", eventData.EventId);
                return;
            }

            DataSetRS? dataSet = GetDataSet(dataSetId);
            if (dataSet == null)
            {
                _logger.LogDebug("Invalid DataSetId: {0} received", dataSetId);
                return;
            }
            if (dataSet.IntervalId != "1")
            {
                _logger.LogDebug("DataSetId: {0} is not a One-min bar, it is \"{1}\" bar", dataSetId, dataSet.IntervalId);
                return;
            }

            // 1- refresh studyColList if needed
            RefreshStudyColList();

            // 2- for each studyCol, check if the studyCol item is enabled and eventData is relevant
            lock (accessListLock)
            {
                foreach (var studyCol in studyColList)
                {
                    if (studyCol.TickerId == dataSet.TickerId && studyCol.Enabled)
                    {
                        // convert eventData EventId to int
                        if (!int.TryParse(eventData.EventId, out int datasetId))
                        {
                            _logger.LogWarning("Invalid EventId: {EventId}", eventData.EventId);
                            continue;
                        }
                        var dataset = GetDataSet(datasetId);
                        if (dataset == null)
                        {
                            continue;
                        }
                        // ensure to process on change of one minute data only
                        if (studyCol.TickerId == dataset.TickerId && dataset.IntervalId == "1")
                        {
                            // process the studyCol
                            _logger.LogDebug("Processing StudyCol Id ({0}), DatasetId ({1}), Ticker: {2}, Interval: ({3})",
                                studyCol.Id, dataset.Id, studyCol.TickerId, studyCol.IntervalId);

                            // get ExecDataSet
                            var execDataSet = GetExecStudySet(studyCol);

                            if (execDataSet != null)
                            {
                                _logger.LogDebug("Processing StudyCol Id: {0}", studyCol.Id);
                                execDataSet.Process();
                            }
                        }
                        else
                        {
                            _logger.LogWarning("StudyCol TickerId {TickerId} does not match Dataset TickerId {DatasetTickerId}", studyCol.TickerId, dataset?.TickerId);
                        }
                    }
                }
            }
        }

        private void ProcessStudyCols()
        {
            lock (accessListLock)
            {
                foreach (var studyCol in studyColList)
                {
                    // process the studyCol
                    //_logger.LogDebug("Processing StudyCol Id ({0}), Ticker: {1}, Interval: ({2})", studyCol.Id, studyCol.TickerId, studyCol.IntervalId);
                    // get ExecDataSet
                    var execDataSet = GetExecStudySet(studyCol);

                    if (execDataSet != null)
                    {
                        //_logger.LogDebug("Processing StudyCol Id: {0}", studyCol.Id);
                execDataSet.Process();
                }
            }
        }
        }

        public void Dispose()
        {
            // already cleaned up... check
            //_eventMonitor.Unsubscribe("PRICEDATA", OnPriceChange);
            _logger.LogInformation("Unsubscribed from PRICEDATA changes");
        }

        private void RefreshStudyColList()
        {
            DateTime nowTime = DateTime.Now;
            lock (accessListLock)
            {
                try
                {
                    if (studyColList.Count == 0 || DateTime.Now > nextStudyColListRefresh)
                    {
                        studyColList = MainRepo.GetStudyColList();
                        // nextStudyColListRefresh = DateTime.Now + 2 minutes
                        nextStudyColListRefresh = nowTime.AddMinutes(2);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error refreshing StudyCol list - {0}", ex.Message);
                }
            }
        }

        private DataSetRS? GetDataSet(int datasetId)
        {
            // find it from Map first
            lock (accessDatasetLock)
            {
                if (datasetMap.TryGetValue(datasetId, out DataSetRS? dataSet) && dataSet != null)
                {
                    return dataSet;
                }
            }

            try
            {
                // try to find it from the database
                var dataset = MainRepo.GetDataSet(datasetId);
                if (dataset != null)
                {
                    lock (accessDatasetLock)
                    {
                        datasetMap[datasetId] = new DataSetRS(dataset);
                    }
                }
                return dataset;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching DataSet id ({0}) - {1}", datasetId, ex.Message);
                return null;
            }
        }

        private ExecStudySet? GetExecStudySet(StudyColRS studyCol)
        {
            // find it from Map first
            lock (accessStudySetLock)
            {
                if (execStudyMap.TryGetValue(studyCol.Id, out ExecStudySet? studySet) && studySet != null)
                {
                    return studySet;
                }
            }

            try
            {
                // create a new one
                ExecStudySet? newStudySet = new ExecStudySet(studyCol, _serviceProvider);
                lock (accessStudySetLock)
                {
                    execStudyMap[studyCol.Id] = newStudySet;
                    return newStudySet;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error creating new StudySet for studyCol id ({0}) - {1}", studyCol.Id, ex.Message);
                return null;
            }
        }

    }
}
