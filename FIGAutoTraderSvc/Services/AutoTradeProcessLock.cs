namespace FIGAutoTradeExSvc.Services
{
    /// <summary>
    /// Per-AutoTrade priority lock.
    /// When the lock is released, high-priority waiters (<c>AutoTradeLiveService</c>)
    /// are always woken before low-priority waiters (<c>OrderManagementService</c>).
    /// Within the same priority tier, FIFO order is preserved.
    /// </summary>
    internal sealed class AutoTradePerIdLock
    {
        private bool _held = false;
        private readonly Queue<TaskCompletionSource<bool>> _highQueue = new();
        private readonly Queue<TaskCompletionSource<bool>> _lowQueue = new();
        private readonly object _gate = new();

        /// <summary>Acquire as high-priority (trade processing). Blocks until the lock is free.</summary>
        public void WaitHighPriority()
        {
            TaskCompletionSource<bool>? tcs = null;
            lock (_gate)
            {
                if (!_held)
                {
                    _held = true;
                    return;
                }
                tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _highQueue.Enqueue(tcs);
            }
            tcs!.Task.GetAwaiter().GetResult();
        }

        /// <summary>Acquire as low-priority (order status updates). Yields to any queued high-priority waiter on release.</summary>
        public void WaitLowPriority()
        {
            TaskCompletionSource<bool>? tcs = null;
            lock (_gate)
            {
                if (!_held)
                {
                    _held = true;
                    return;
                }
                tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _lowQueue.Enqueue(tcs);
            }
            tcs!.Task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Release the lock. The next waiter is selected from the high-priority
        /// queue first; the low-priority queue is only consulted when no
        /// high-priority waiters remain.
        /// </summary>
        public void Release()
        {
            TaskCompletionSource<bool>? next = null;
            lock (_gate)
            {
                if (_highQueue.Count > 0)
                    next = _highQueue.Dequeue();
                else if (_lowQueue.Count > 0)
                    next = _lowQueue.Dequeue();
                else
                    _held = false;
            }
            next?.SetResult(true);
        }
    }

    /// <summary>Registry of per-AutoTrade priority locks.</summary>
    internal static class AutoTradeProcessLock
    {
        private static readonly Dictionary<int, AutoTradePerIdLock> _locks = new();
        private static readonly object _registryLock = new();

        /// <summary>
        /// Caps the number of <c>ProcessInternal</c> transactions that can hold an
        /// open SQL connection simultaneously. Prevents burst scenarios (many AutoTrades
        /// woken at once) from producing cross-table SQL deadlocks.
        /// Acquire BEFORE calling <see cref="AutoTradePerIdLock.WaitHighPriority"/> to
        /// maintain a safe lock-ordering: global slot → per-AutoTrade lock.
        /// </summary>
        private static readonly SemaphoreSlim _globalProcessSlots = new SemaphoreSlim(3, 3);

        public static AutoTradePerIdLock GetLock(int autoTradeId)
        {
            lock (_registryLock)
            {
                if (!_locks.TryGetValue(autoTradeId, out var lk))
                {
                    lk = new AutoTradePerIdLock();
                    _locks[autoTradeId] = lk;
                }
                return lk;
            }
        }

        public static void AcquireGlobalSlot() => _globalProcessSlots.Wait();
        public static void ReleaseGlobalSlot() => _globalProcessSlots.Release();
    }
}
