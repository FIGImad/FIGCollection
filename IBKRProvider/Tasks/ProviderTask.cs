using IBApi;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

// Registration in DI Container
// --------------------------------
// When registering in your DI container:

// Transient lifetime since each task needs its own instance
//        services.AddTransient<ProviderTask>(provider =>
//        {
//            // This factory will be called each time you resolve ProviderTask
//            var logger = provider.GetRequiredService<ILogger<ProviderTask>>();
//            var appLifetime = provider.GetRequiredService<IHostApplicationLifetime>();
//            // Note: We're not creating EReader here - it will be provided later
//            return new ProviderTask(null, logger, appLifetime);
//        });
//
// Usage Example
//// Create multiple tasks when needed
//var task1 = serviceProvider.GetRequiredService<ProviderTask>();
//var task2 = serviceProvider.GetRequiredService<ProviderTask>();
//task1.SetReader(new EReader(...));
//task2.SetReader(new EReader(...));
//task1.StartTask();
//task2.StartTask();


namespace IBKRProvider.Tasks
{
    public class ProviderTask : IDisposable
    {
        private readonly ILogger _logger;
        private readonly BlockingCollection<TaskEvent> _queued = new();
        private readonly CancellationTokenSource _localCts = new();
        private readonly CancellationToken _appStoppingToken;
        private readonly object _lockObj = new object();
        private Task? _task = null;
        private EReader? _reader = null;

        public ProviderTask(ILogger<ProviderTask> logger,
            IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _appStoppingToken = appLifetime.ApplicationStopping;
        }

        public TaskStatus? Status
        {
            get
            {
                lock (_lockObj)
                {
                    return _task?.Status;
                }
            }
        }

        public void SetReader(EReader eReader)
        {
            lock (_lockObj)
            {
                _reader = eReader;
            }
        }

        public void StartTask()
        {
            try
            {
                lock (_lockObj)
                {
                    if (_task != null && !_task.IsCompleted)
                        return;

                    // Create linked token source (local cancellation + app shutdown)
                    var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                        _localCts.Token,
                        _appStoppingToken);

                    _task = Task.Run(() => ProcessMessages(linkedCts.Token), linkedCts.Token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start processing task");
            }
        }

        private void ProcessMessages(CancellationToken ct)
        {
            try
            {
                foreach (var evt in _queued.GetConsumingEnumerable(ct))
                {
                    lock (_lockObj)
                    {
                        if (evt.EventName == "@PROV_EVT@" && _reader != null)
                        {
                            _reader.processMsgs();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Processing task was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in message processing loop");
            }
        }


        public void Enqueue(TaskEvent msg)
        {
            lock (_lockObj)
            {
                _queued.Add(msg);
            }
        }

        public void Dispose()
        {
            lock(_lockObj)
            {
                _localCts.Cancel();
                _localCts.Dispose();
                _queued.Dispose();
                if (_task != null && (_task.IsCompleted || _task.IsFaulted || _task.IsCanceled))
                {
                    _task.Dispose();
                }
            }
        }
    }
}

