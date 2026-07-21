using System.Collections.Concurrent;

namespace FIGCommon.Tasks
{
    // Helper classes
    public class TaskGroup
    {
        public Dictionary<string, TaskParams> Tasks { get; } = new();
        public object SyncRoot { get; } = new object();
    }
}
