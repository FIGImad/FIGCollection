using System.Diagnostics;

namespace FIGCommon.Models
{
    public enum EAutoTradeStatus
    {
        Idle = 0,
        Unverified = 1,
        Started = 2,
        Stopped = 3,
        Failed = 4,
        Completed = 5,
        NotAvailable = 6
    }
    public class AutoTradeExecStatus : ICloneable
    {
        protected int autoTradeId = -1;
        protected int status = (int) EAutoTradeStatus.Idle;
        protected double progressPct = 0.0;
        protected string message = "";
        protected DateTime statusTime = DateTime.Now;
        protected Stopwatch stopwatchExec = new();

        // Sync object for multi-threaded exec
        protected readonly object _accessLock = new();

        public AutoTradeExecStatus()
        {
            autoTradeId = -1;
            status = (int)EAutoTradeStatus.Idle;
            progressPct = 0.0;
            message = "";
            statusTime = DateTime.Now;
            stopwatchExec = new();
        }


        public AutoTradeExecStatus(int autoStartId)
        {
            autoTradeId = autoStartId;
            status = (int) EAutoTradeStatus.Idle;
            progressPct = 0.0;
            message = "";
            statusTime = DateTime.Now;
            stopwatchExec = new();
        }

        public AutoTradeExecStatus(AutoTradeExecStatus rec)
        {
            autoTradeId = rec.autoTradeId;
            status = rec.status;
            progressPct = rec.progressPct;
            message = rec.message;
            statusTime = rec.statusTime;
            stopwatchExec = rec.stopwatchExec;
        }

        #region Accessors
        public int AutoTradeId
        {
            get { lock (_accessLock) { return autoTradeId; } }
            set { lock (_accessLock) { autoTradeId = value; } }
        }

        public int Status
        {
            get { lock (_accessLock) { return (int) status; } }
            set { lock (_accessLock) { status = value; } }
        }

        public double ProgressPct
        {
            get { lock (_accessLock) { return progressPct; } }
            set { lock (_accessLock) { progressPct = value; } }
        }

        public string Message
        {
            get { lock (_accessLock) { return message; } }
            set { lock (_accessLock) { message = value; } }
        }

        public DateTime StatusTime
        {
            get { lock (_accessLock) { return statusTime; } }
            set { lock (_accessLock) { statusTime = value; } }
        }
        #endregion Accessors

        public void Reset()
        {
            lock (_accessLock)
            {
                status = (int) EAutoTradeStatus.Idle;
                progressPct = 0.0;
                message = "";
                statusTime = DateTime.Now;
                stopwatchExec.Reset();
            }
        }

        //public void Create()
        //{
        //    lock (_accessLock)
        //    {
        //        status = (int) EAutoTradeStatus.Created;
        //        progressPct = 0.0;
        //        message = "Created";
        //        statusTime = DateTime.Now;
        //        stopwatchExec.Reset();
        //    }
        //}

        public void Started()
        {
            lock (_accessLock)
            {
                status = (int) EAutoTradeStatus.Started;
                progressPct = 0.0;
                message = "Started";
                statusTime = DateTime.Now;
                stopwatchExec.Start();
            }
        }

        public void InProgress(double progressPct)
        {
            lock (_accessLock)
            {
                status = (int) EAutoTradeStatus.Started;
                this.progressPct = progressPct;
                message = $"In-Progress (%{Math.Round(progressPct, 0)})";
                statusTime = DateTime.Now;
            }
        }

        public void Stopped()
        {
            lock (_accessLock)
            {
                status = (int)EAutoTradeStatus.Stopped;
                message = $"Stopped";
                statusTime = DateTime.Now;
                stopwatchExec.Stop();
            }
        }

        public void Failed()
        {
            lock (_accessLock)
            {
                status = (int)EAutoTradeStatus.Failed;
                progressPct = 0;
                message = "Failed";
                statusTime = DateTime.Now;
                stopwatchExec.Stop();
            }
        }

        public void Completed()
        {
            lock (_accessLock)
            {
                status = (int)EAutoTradeStatus.Completed;
                progressPct = 100;
                message = "Completed";
                statusTime = DateTime.Now;
                stopwatchExec.Stop();
            }
        }

        public object Clone()
        {
            return new AutoTradeExecStatus(this);
        }
    }
}