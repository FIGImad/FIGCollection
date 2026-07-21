using FIGCommon.Tasks;
using System.Collections.Concurrent;


namespace FIGCommon.Services
{
    public interface ITaskSchedulerService : IDisposable
    {
        Task ScheduleEventAsync(string ownerId, string eventName, int delayMs, Func<object?, TaskEventArgs, Task> handler, params object?[] args);
        Task StopAllTasksAsync(string ownerId);
    }

    public class TaskSchedulerService : ITaskSchedulerService, IDisposable
    {
        private readonly ConcurrentDictionary<string, TaskGroup> _taskGroups = new();
        private readonly ILogger<TaskSchedulerService> _logger;
        private readonly CancellationToken _shutdownToken;

        public TaskSchedulerService(ILogger<TaskSchedulerService> logger, IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            // Link all tasks to the host shutdown signal so they are cancelled
            // automatically when the application stops (fixes: no shutdown integration)
            _shutdownToken = appLifetime.ApplicationStopping;
        }

        public Task ScheduleEventAsync(string ownerId, string eventName, int delayMs, Func<object?, TaskEventArgs, Task> handler, params object?[] args)
        {
            var taskGroup = _taskGroups.GetOrAdd(ownerId, _ => new TaskGroup());

            TaskParams taskParams;
            lock (taskGroup.SyncRoot)
            {
                var taskId = TaskParams.GetTaskId(ownerId, eventName);

                // Cancel and dispose previous task for this event if one exists
                if (taskGroup.Tasks.TryGetValue(taskId, out var existingTask))
                {
                    existingTask.CancellationSource.Cancel();
                    existingTask.CancellationSource.Dispose(); // fixes: CTS leak on replacement
                    taskGroup.Tasks.Remove(taskId);
                }

                // Link to app shutdown so tasks are cancelled on host stop
                var cts = CancellationTokenSource.CreateLinkedTokenSource(_shutdownToken);
                taskParams = new TaskParams(ownerId, eventName, handler, cts);
                taskGroup.Tasks[taskId] = taskParams;
            }

            // Start OUTSIDE the lock — if delayMs==0 the handler fires synchronously
            // inside RunTaskAsync; starting inside the lock would cause a deadlock if the
            // handler itself calls back into ScheduleEventAsync or StopAllTasksAsync
            taskParams.RunningTask = RunTaskAsync(taskParams, delayMs, args, taskParams.CancellationSource.Token);
            return Task.CompletedTask;
        }

        private async Task RunTaskAsync(TaskParams taskParams, int delayMs, object?[] args, CancellationToken ct)
        {
            try
            {
                if (delayMs > 0)
                    await Task.Delay(delayMs, ct);

                if (ct.IsCancellationRequested)
                    return;

                if (taskParams.OnTaskExecuted != null)
                    await taskParams.OnTaskExecuted(this, new TaskEventArgs(taskParams.TaskId, args));
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Task {TaskId} was canceled", taskParams.TaskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task {TaskId} failed", taskParams.TaskId);
            }
            finally
            {
                // Auto-clean completed task from the dictionary so stale entries never
                // accumulate and empty TaskGroups are removed to prevent unbounded growth
                if (_taskGroups.TryGetValue(taskParams.OwnerId, out var group))
                {
                    bool groupEmpty;
                    lock (group.SyncRoot)
                    {
                        group.Tasks.Remove(taskParams.TaskId);
                        groupEmpty = group.Tasks.Count == 0;
                    }

                    if (groupEmpty)
                        _taskGroups.TryRemove(taskParams.OwnerId, out _);
                }
            }
        }

        // Gracefully cancel and await all tasks for a specific owner
        public async Task StopAllTasksAsync(string ownerId)
        {
            if (!_taskGroups.TryGetValue(ownerId, out var taskGroup))
                return;

            List<Task> tasksToAwait;
            lock (taskGroup.SyncRoot)
            {
                tasksToAwait = taskGroup.Tasks.Values
                    .Select(t => t.RunningTask)
                    .Where(t => t != null)
                    .Cast<Task>()
                    .ToList();

                foreach (var task in taskGroup.Tasks.Values)
                {
                    task.CancellationSource.Cancel();
                    task.CancellationSource.Dispose(); // fixes: CTS leak in StopAllTasksAsync
                }
                taskGroup.Tasks.Clear();
            }

            // Remove the now-empty group so _taskGroups does not grow unbounded
            _taskGroups.TryRemove(ownerId, out _);

            await Task.WhenAll(tasksToAwait);
        }

        public void Dispose()
        {
            foreach (var group in _taskGroups.Values)
            {
                lock (group.SyncRoot)
                {
                    foreach (var task in group.Tasks.Values)
                    {
                        task.CancellationSource.Cancel(); // fixes: Dispose did not cancel before disposing
                        task.CancellationSource.Dispose();
                    }
                    group.Tasks.Clear();
                }
            }
            _taskGroups.Clear();
        }
    }
}
