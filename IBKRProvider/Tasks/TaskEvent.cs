using Newtonsoft.Json.Linq;

namespace IBKRProvider.Tasks
{
    public class TaskEvent
    {
        private static int _globalId = 0;
        private static Object _eventLock = 0;

        public TaskEvent()
        {
            lock (_eventLock)
            {
                _globalId++;
                _eventId = _globalId;
            }
            EventName = "";
            EventParam = new JObject();
        }
        public TaskEvent(string eventName, JObject eventParam)
        {
            lock (_eventLock)
            {
                _globalId++;
                _eventId = _globalId;
            }
            EventName = eventName;
            EventParam = eventParam;
        }

        private int _eventId = 0;
        public int Id { get { return _eventId; } }
        public string EventName { get; set; }
        public JObject EventParam { get; set; }
        public int EventId { get; set; }

    }
}
