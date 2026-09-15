using FIGCommon.DataAccess;
using FIGCommon.Models;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Services
{
    public class EventMonitorService : IDisposable
    {
        protected ILogger? _logger;
        protected readonly IConfiguration? _config;
        protected SqlDependency? _dependency;
        protected readonly object _operationLock = new();
        protected bool _isDisposed;
        protected readonly CancellationTokenSource _cts = new();
        protected readonly SemaphoreSlim _listenerSetupSemaphore = new(1, 1);
        protected readonly SemaphoreSlim _changeProcessingSemaphore = new(1, 1);
        protected readonly Dictionary<string, List<Action<EventRS?>>> _subscriptions = new();
        // Change the `eventList` field from `readonly` to a regular field to allow assignment outside the constructor.
        protected List<EventRS> eventList = new();
        protected Task? retryTask = null;
        protected Task? _periodicRefreshTask;
        protected readonly object _retryLock = new();
        protected string _eventTableName = "";

        // Database-Specific Operations
        protected readonly RepositoryBase RBASE = new RepositoryBase();

        public EventMonitorService() {
        }

        // protected constructor
        public EventMonitorService(IConfiguration config,
                                   ILogger<EventMonitorService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config;

            string? connectionString = _config["EventMonitor:ConnectionString"];
            if (connectionString == null || connectionString == "")
            {
                throw new ArgumentNullException("EventMonitor:ConnectionString");
            }
            string eventTableName = _config["EventMonitor:EventTable"] ?? "Event";
            if (eventTableName == null || eventTableName == "")
            {
                throw new ArgumentNullException("EventMonitor:EventTable");
            }
            InitializeDB(connectionString, eventTableName);
            _ = InitializeAsync(); // Fire-and-forget initialization
        }

        #region Properties
        protected string ConnectionString
        {
            get
            {
                return RBASE.ConnectionString;
            }
        }

        protected string EventTableName
        {
            get
            {
                return _eventTableName.Length > 0 ? _eventTableName : _config?["EventMonitor:EventTable"] ?? "Event";
            }
        }

        public string DatabaseName
        {
            get
            {
                return RBASE.DatabaseName;
            }
        }

        #endregion Properties

        #region DatabaseAccess
        public bool IsBrokerEnabled()
        {
            string statement = $"SELECT [name],[database_id],[is_broker_enabled] FROM sys.databases WHERE [name] = '{DatabaseName}'";
            SysDatabasesRS? rs = RBASE.Select<SysDatabasesRS>(new SysDatabasesRS(), statement);
            return rs?.Is_broker_enabled ?? false;
        }
        public List<EventRS> GetEvents()
        {
            string statement = $"SELECT [Id],[EventType],[EventId],[Message],[TimeStamp] FROM [{EventTableName}]";
            return RBASE.SelectMulti<EventRS>(new EventRS(), statement);
        }
        public List<EventRS> GetActiveEvents(int duration)
        {
            string statement = $"DECLARE @TimeSince datetime = DATEADD(SECOND, -{duration}, GETDATE());\r\n";
            statement += $"SELECT [Id],[EventType],[EventId],[Message],[TimeStamp] FROM [{EventTableName}] WHERE [TimeStamp] > @TimeSince;";
            return RBASE.SelectMulti<EventRS>(new EventRS(), statement);
        }
        #endregion DatabaseAccess


        protected void InitializeDB(string connectionString, string tableName)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));
            _eventTableName = tableName;
            RBASE.Initialize(connectionString, _logger);
        }

        protected async Task InitializeAsync()
        {
            try
            {
                await StartMonitoringAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Initialization failed");
            }
        }

        public async Task StartMonitoringAsync()
        {
            try
            {
                if (!IsBrokerEnabled())
                {
                    _logger?.LogCritical("Service Broker is not enabled");
                    throw new InvalidOperationException("Service Broker is not enabled");
                }

                // get initial list of events 
                var initialEvents = GetEvents();
                lock (_operationLock)
                {
                    eventList = initialEvents;

                    // Start exactly one refresh loop. StartMonitoringAsync can be
                    // entered again by retry logic after a transient database error.
                    if (_periodicRefreshTask == null)
                    {
                        _periodicRefreshTask = Task.Run(StartPeriodicRefresh, _cts.Token);
                    }
                }

            }
            catch(Exception exDBAccess)
            {
                _logger?.LogError("Error accessing database will try again in 1 minute- {0}", exDBAccess.Message);
                _ = Task.Run(() => RetryStartMonitoringAsync(20), _cts.Token);
                return;
            }

            lock (_operationLock)
            {
                if (_dependency != null || _isDisposed) return;
            }

            try
            {
                SqlDependency.Start(ConnectionString);
                _logger?.LogInformation("SQL Dependency listening started");

                await SetupDatabaseListener();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start SQL Dependency");
                _ = Task.Run(() => RetryStartMonitoringAsync(20), _cts.Token);
            }
        }

        protected async Task SetupDatabaseListener()
        {
            try
            {
                // SqlDependency registrations are one-shot. If a callback arrives
                // while another registration is being created, wait and create the
                // next registration instead of dropping the request and potentially
                // leaving the service with no active listener.
                await _listenerSetupSemaphore.WaitAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync(_cts.Token);

                using var command = new SqlCommand(
                    "SELECT [Id], [EventType], [EventId], [Message], [TimeStamp] FROM dbo.[Event]",
                    connection);

                lock (_operationLock)
                {
                    if (_isDisposed) return;

                    if (_dependency != null)
                    {
                        _dependency.OnChange -= OnDatabaseChange;
                    }
                    _dependency = new SqlDependency(command);
                    _dependency.OnChange += OnDatabaseChange;
                }

                using (var reader = await command.ExecuteReaderAsync(_cts.Token))
                {
                    // reader is disposed immediately; SqlDependency is already attached to the command
                }
                _logger?.LogDebug("Database listener established");
            }
            catch (OperationCanceledException)
            {
                _logger?.LogInformation("Monitoring cancelled");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to setup database listener");
                await RetryStartMonitoringAsync(10);
            }
            finally
            {
                _listenerSetupSemaphore.Release();
            }
        }

        protected async Task RetryStartMonitoringAsync(int secs)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(secs), _cts.Token);
                lock (_retryLock)
                {
                    if (retryTask != null && !retryTask.IsCompleted)
                    {
                        return;
                        //// force cancelation of task
                        //retryTask.Wait();
                        //retryTask.Dispose();
                        //retryTask = null;
                    }
                    Destroy();
                    //retryTask = Task.Run(() => StartMonitoringAsync(), _cts.Token);
                    retryTask = Task.Run(async () =>
                    {
                        try
                        {
                            await StartMonitoringAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Retry attempt failed");
                            // Schedule another retry with exponential backoff
                            var nextDelay = Math.Min(secs * 2, 300); // Cap at 5 minutes
                            await RetryStartMonitoringAsync(nextDelay);
                        }
                    }, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                _logger?.LogInformation("Retry cancelled");
            }
        }

        protected async void OnDatabaseChange(object sender, SqlNotificationEventArgs e)
        {
            _logger?.LogInformation("Database change detected. Type: {Type}, Source: {Source}, Info: {Info}",
                e.Type, e.Source, e.Info);

            // Handle timeout scenarios
            if (e.Type == SqlNotificationType.Subscribe && e.Info == SqlNotificationInfo.Expired)
            {
                _logger?.LogWarning("SQL Dependency conversation expired - reinitializing");
                await RetryStartMonitoringAsync(1);
                return;
            }

            // Immediately re-establish listener
            await SetupDatabaseListener();

            if (e.Type == SqlNotificationType.Change &&
                e.Info != SqlNotificationInfo.Invalid)
            {
                var processingLockTaken = false;
                try
                {
                    // A newly registered dependency can fire while the previous
                    // callback is still reading Event rows. Serialize the scan and
                    // eventList update so the same row version cannot be published
                    // twice by racing callbacks.
                    await _changeProcessingSemaphore.WaitAsync(_cts.Token);
                    processingLockTaken = true;

                    if (e.Info == SqlNotificationInfo.Insert
                        || e.Info == SqlNotificationInfo.Alter
                        || e.Info == SqlNotificationInfo.Update
                        || e.Info == SqlNotificationInfo.Merge
                        )
                    {
                        var changes = GetActiveEvents(120);  // changes within 2 minutes
                        foreach (var change in changes.Where(c => c != null))
                        {
                            EventRS? existingEvent;
                            //find the record in eventList, if not found add it, if found check if change has newer time stamp
                            lock (_operationLock )
                            {
                                existingEvent = eventList?.FirstOrDefault(e => e != null && e.Id == change.Id);
                            }
                            if (existingEvent == null)
                            {
                                lock (_operationLock)
                                {
                                    eventList?.Add(change);
                                }
                                Publish(change.EventType, change);
                            }
                            else if (change.Timestamp > existingEvent.Timestamp
                                || (change.Timestamp == existingEvent.Timestamp
                                    && !string.Equals(change.Message, existingEvent.Message, StringComparison.Ordinal)))
                            {
                                lock (_operationLock)
                                {
                                    eventList?.Remove(existingEvent);
                                    eventList?.Add(change);
                                }
                                Publish(change.EventType, change);
                            } // ignore if the timestamp is older
                        }
                    } else if (e.Info == SqlNotificationInfo.Delete || e.Info == SqlNotificationInfo.Truncate)
                    {
                        var newList = GetEvents();  // get all events again
                        if (newList == null)
                        {
                            // I don't like that... restart the monitoring
                            await RetryStartMonitoringAsync(10);
                            return;
                        }
                        foreach (var rec in eventList)
                        {
                            //find the record in newList, if not found, this record is deleted
                            var existingEvent = newList?.FirstOrDefault(e => e != null && e.Id == rec.Id);
                            //var existingEvent = newList.FirstOrDefault(e => e.Id == rec.Id);
                            if (existingEvent == null)
                            {
                                // deleted event, notify subscriber
                                Publish(rec.EventType, null);
                            }
                        }
                        eventList.Clear();
                        if (newList != null) eventList.AddRange(newList);
                    } else if (e.Info == SqlNotificationInfo.Error)
                    {
                        _logger?.LogError("Error Event Monitoring - Should not happen");
                        await RetryStartMonitoringAsync(10);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error processing changed events");
                }
                finally
                {
                    if (processingLockTaken)
                    {
                        _changeProcessingSemaphore.Release();
                    }
                }
            }
        }

        public void Subscribe(string eventType, Action<EventRS?> handler)
        {
            if (string.IsNullOrEmpty(eventType))
                throw new ArgumentNullException(nameof(eventType));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_operationLock)
            {
                if (!_subscriptions.TryGetValue(eventType, out var handlers))
                {
                    handlers = new List<Action<EventRS?>>();
                    _subscriptions[eventType] = handlers;
                }
                if (!handlers.Contains(handler))
                {
                    handlers.Add(handler);
                }
            }
        }

        public void Unsubscribe(string eventType, Action<EventRS?> handler)
        {
            if (string.IsNullOrEmpty(eventType)) return;
            if (handler == null) return;

            lock (_operationLock)
            {
                if (_subscriptions.TryGetValue(eventType, out var handlers))
                {
                    handlers.RemoveAll(existingHandler => existingHandler == handler);
                    if (handlers.Count == 0)
                    {
                        _subscriptions.Remove(eventType);
                    }
                }
            }
        }

        protected void Publish(string eventType, EventRS? data)
        {
            List<Action<EventRS?>>? handlersToInvoke = null;

            lock (_operationLock)
            {
                if (_subscriptions.TryGetValue(eventType, out var handlers))
                {
                    handlersToInvoke = new List<Action<EventRS?>>(handlers);
                }
            }

            handlersToInvoke?.ForEach(handler =>
            {
                try
                {
                    handler(data);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Event handler failed for {EventType}", eventType);
                }
            });
        }

        public void Dispose()
        {
            lock (_operationLock)
            {
                if (_isDisposed) return;
                _isDisposed = true;
            }

            _cts.Cancel();
            _cts.Dispose();

            Destroy();

            GC.SuppressFinalize(this);
        }

        protected void Destroy()
        {

            lock (_operationLock)
            {
                if (_dependency != null)
                {
                    try
                    {
                        _dependency.OnChange -= OnDatabaseChange;
                        _dependency = null;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error cleaning up SqlDependency");
                    }
                }
            }
            try
            {
                SqlDependency.Stop(ConnectionString);
                _logger?.LogInformation("SQL Dependency listening stopped");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to stop SQL Dependency");
            }

        }

        protected async Task StartPeriodicRefresh()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), _cts.Token); // Refresh every 5 minutes
                    _logger?.LogDebug("Performing periodic refresh of SQL Dependency");
                    await SetupDatabaseListener();
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Periodic refresh failed");
                }
            }
        }
    }
}
