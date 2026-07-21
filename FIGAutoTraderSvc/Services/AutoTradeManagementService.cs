using FIGCommon.DataAccess;
using FIGCommon.Models;
using FIGCommon.Services;

namespace FIGAutoTradeExSvc.Services
{
    public class AutoTradeManagementService : IAutoTradeManagementService
    {
        private const string SRC_NAME = "AutoTradeManagementService";

        private readonly ILogger<AutoTradeManagementService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITaskSchedulerService _taskScheduler;

        private readonly object _lockObj = new();
        private Dictionary<int, IAutoTradeService> _mapAutoTrades = new Dictionary<int, IAutoTradeService>();


        public AutoTradeManagementService(
            ILogger<AutoTradeManagementService> logger,
            IServiceProvider serviceProvider,
            ITaskSchedulerService taskScheduler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _taskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
            _logger.LogInformation("Service is created.");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service Started");
            lock (_lockObj)
            {
                InitializeService();
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service is Stopping");
            _taskScheduler.StopAllTasksAsync(SRC_NAME).Wait();
            return Task.CompletedTask;
        }

        private void InitializeService()
        {
            try
            {
                // Start live autoTrade Tasks
                var autoTrades = MainRepo.GetAutoTrades();
                if (autoTrades != null && autoTrades.Count > 0)
                {
                    autoTrades.ForEach(autoTrade =>
                    {
                        if (autoTrade.Status == (int)EAutoTradeStatus.Started)
                        {
                            Start(autoTrade);
                        }
                        else
                        {
                            autoTrade.Status = (int)EAutoTradeStatus.Idle;
                            MainRepo.UpsertAutoTrade(autoTrade);
                        }
                    });
                }
            }
            catch(Exception ex)
            {
                _logger.LogError("Error initializing service: {0}", ex.Message);
            }
        }

        public void Wakeup(int autoTradeId)
        {
            lock(_lockObj)
            {
                // check if AutoTrade is already started. Get the instance first and axamine if it is already started
                if (_mapAutoTrades.ContainsKey(autoTradeId))
                {
                    var autoTradeInstance = _mapAutoTrades[autoTradeId];
                    autoTradeInstance.WakeUp();
                }
            }
        }

        public void WakeupAll()
        {
            lock (_lockObj)
            {
                // iterate through all autoTrade instances and wake them up
                foreach (var autoTradeInstance in _mapAutoTrades.Values)
                {
                    autoTradeInstance.WakeUp();
                }
            }
        }

        private EAutoTradeStatus Start(AutoTradeRS autoTrade)
        {
            // check if AutoTrade is already started. Get the instance first and axamine if it is already started
            if (_mapAutoTrades.ContainsKey(autoTrade.Id))
            {
                var autoTradeInstance = _mapAutoTrades[autoTrade.Id];
                if (autoTradeInstance.IsRunning)
                {
                    _logger.LogDebug($"AutoTrade Id {autoTrade.Id} is already started.");
                    return EAutoTradeStatus.Started;
                }
                else
                {
                    // remove from map
                    _mapAutoTrades.Remove(autoTrade.Id);
                }
            }
            try
            {
                IAutoTradeService? autoTradeService = null;
                autoTradeService = _serviceProvider.GetRequiredService<AutoTradeLiveService>();
                if (autoTradeService == null)
                {
                    string message = $"Service \"AutoTradeLiveService\" is not registered in the service provider.";
                    throw new Exception(message);
                }
                _mapAutoTrades.Add(autoTrade.Id, autoTradeService);
                autoTradeService.Init(autoTrade);
                // Touch the event "BOT" just in case if it uses a new bot configuration
                // so that to force monitoring the bot by the RemoteBotInstructionManagementService
                MainRepo.TouchEvent("BOT", $"{autoTrade.BotId}");
                return autoTradeService.Start();
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"AutoTrade({autoTrade.Id}) - Exception starting AutoTrade - {ex.Message}");
                // remove from map if added
                if (_mapAutoTrades.ContainsKey(autoTrade.Id))
                {
                    _mapAutoTrades.Remove(autoTrade.Id);
                }
                return EAutoTradeStatus.Failed;
            }
        }

        public EAutoTradeStatus StartExec(int autoTradeId)
        {
            var autoTrade = MainRepo.GetAutoTrade(autoTradeId);
            if (autoTrade == null)
            {
                return EAutoTradeStatus.NotAvailable;
            }
            lock (_lockObj)
            {
                return Start(autoTrade);
            }
        }



        public EAutoTradeStatus StopExec(int autoTradeId)
        {
            lock (_lockObj)
            {
                // check if AutoTrade is already started. Get the instance first and axamine if it is already started
                if (_mapAutoTrades.ContainsKey(autoTradeId))
                {
                    var autoTradeInstance = _mapAutoTrades[autoTradeId];
                    return autoTradeInstance.Stop();
                }
            }
            return EAutoTradeStatus.NotAvailable;
        }

        public AutoTradeExecStatus? GetExecStatus(int autoTradeId)
        {
            lock (_lockObj)
            {
                if (_mapAutoTrades.ContainsKey(autoTradeId))
                {
                    var autoTradeInstance = _mapAutoTrades[autoTradeId];
                    return autoTradeInstance.GetExecStatus();
                }
            }
            return null;
        }

        public List<AutoTradeExecStatus> GetExecStatusFromList(List<int> ids)
        {
            // check if thread is already started for this task
            lock (_lockObj)
            {
                try
                {
                    List<AutoTradeExecStatus> taskExecStatuses = new();
                    ids.ForEach(id =>
                    {
                        if (_mapAutoTrades.ContainsKey(id))
                        {
                            var autoTradeInstance = _mapAutoTrades[id];
                            var status = autoTradeInstance?.GetExecStatus();
                            if (status != null)
                            {
                                taskExecStatuses.Add(status);
                            }
                        }
                    });
                    return taskExecStatuses;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Exception getting status for id list {0}", ex.Message);
                    return new List<AutoTradeExecStatus>();
                }
            }
        }
    }


}