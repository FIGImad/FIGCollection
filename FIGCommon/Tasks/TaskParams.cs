namespace FIGCommon.Tasks
{
    public class TaskParams
    {
        public string TaskId { get; }
        public string EventName { get; init; }
        public string OwnerId { get; init; }
        public CancellationTokenSource CancellationSource { get; }
        public Task? RunningTask { get; set; }
        public Func<object?, TaskEventArgs, Task> OnTaskExecuted { get; init; }

        public TaskParams(string ownerId, string eventName, Func<object?, TaskEventArgs, Task> handler, CancellationTokenSource cts)
        {
            OwnerId = ownerId;
            EventName = eventName;
            TaskId = GetTaskId(ownerId, eventName);
            CancellationSource = cts;
            OnTaskExecuted = handler;
        }

        public static string GetTaskId(string ownerId, string eventName)
        {
            return $"{ownerId}_{eventName}";
        }
    }
}