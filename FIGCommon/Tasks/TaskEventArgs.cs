namespace FIGCommon.Tasks
{
    public class TaskEventArgs : EventArgs
    {
        public string EventName { get; }
        public object?[] Args { get; }

        public TaskEventArgs(string eventName, object?[] args)
        {
            EventName = eventName;
            Args = args;
        }
    }
}
