
using IBApi;

namespace IBKRProvider.Models
{
    public class ReaderSignal : EReaderSignal
    {
        protected readonly Object _lockObj = new();
        private Action? action = null;

        public ReaderSignal(Action signalAction)
        {
            action = signalAction;
        }
        public void issueSignal()
        {
            lock (_lockObj)
            {
                action?.Invoke();
            }
        }

        public void waitForSignal()
        {
        }
    }
}

