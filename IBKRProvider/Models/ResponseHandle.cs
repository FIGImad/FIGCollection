using Newtonsoft.Json.Linq;

namespace IBKRProvider.Models
{
    public sealed class ResponseHandle : IDisposable
    {
        private ManualResetEvent _manualResetEvent = new ManualResetEvent(false);
        private readonly object _sync = new();
        private bool _disposed;
        IDisposable? dataObj;

        #region accessors

        public string Id { get; }
        public ManualResetEvent Event => _manualResetEvent;
        public JObject? Response { get; private set; }
        public ErrorResponse? Error { get; private set; }
        public bool IsCompleted => _manualResetEvent.WaitOne(0);

        #endregion accessors


        public ResponseHandle(string id, IDisposable? data = null)
        {
            Id = id;
            Response = null;
            Error = null;
            dataObj = data;
        }

        public void CompleteResponse(JObject jsonObject)
        {
            lock (_sync)
            {
                Response = jsonObject;
                Error = null;
                _manualResetEvent.Set();
            }
        }

        public void ErrorResponse(int errCode, string errMsg)
        {
            lock (_sync)
            {
                Error = new ErrorResponse
                {
                    ErrorCode = errCode,
                    ErrorMsg = errMsg
                };
                Response = null;
                _manualResetEvent.Set();
            }
        }
        public void TouchResponse(JObject jsonObject, bool signalEvent)
        {
            lock (_sync)
            {
                Response = jsonObject;
                Error = null;
                if (signalEvent) _manualResetEvent.Set();
            }
        }

        public void ResetResponse()
        {
            lock (_sync)
            {
                Response = null;
                Error = null;
                _manualResetEvent.Reset();
            }
        }

        public void Signal()
        {
            lock (_sync)
            {
                _manualResetEvent.Set();
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                    return;

                _manualResetEvent.Dispose();
                _disposed = true;
                if (dataObj != null) 
                {
                    dataObj.Dispose();
                    dataObj = null;
                }
            }
        }
    }

}
