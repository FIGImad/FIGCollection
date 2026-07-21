using FIGCommon.Interfaces;

namespace FIGCommon.Utilities.FIGProviderAPI
{
    public class ProviderConnectionSignal : IProviderConnectionSignal
    {
        // Completed when connected, reset when disconnected
        private volatile TaskCompletionSource _connectTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        // Completed when disconnected, reset when connected
        private volatile TaskCompletionSource _disconnectTcs;

        public ProviderConnectionSignal()
        {
            // Start in disconnected state: disconnect latch is already signalled
            _disconnectTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _disconnectTcs.TrySetResult();
        }

        public Task WaitForConnectionAsync(CancellationToken cancellationToken = default)
            => WaitAsync(_connectTcs, cancellationToken);

        public Task WaitForDisconnectionAsync(CancellationToken cancellationToken = default)
            => WaitAsync(_disconnectTcs, cancellationToken);

        public void NotifyConnected()
        {
            // Reset the disconnect latch (block future WaitForDisconnectionAsync callers)
            Interlocked.Exchange(ref _disconnectTcs, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
            // Signal the connect latch (release all WaitForConnectionAsync awaiters)
            _connectTcs.TrySetResult();
        }

        public void NotifyDisconnected()
        {
            // Reset the connect latch (block future WaitForConnectionAsync callers)
            Interlocked.Exchange(ref _connectTcs, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
            // Signal the disconnect latch (release all WaitForDisconnectionAsync awaiters)
            _disconnectTcs.TrySetResult();
        }

        private static Task WaitAsync(TaskCompletionSource tcs, CancellationToken cancellationToken)
        {
            if (tcs.Task.IsCompleted)
                return tcs.Task;

            if (!cancellationToken.CanBeCanceled)
                return tcs.Task;

            return WaitWithCancellationAsync(tcs.Task, cancellationToken);
        }

        private static async Task WaitWithCancellationAsync(Task task, CancellationToken cancellationToken)
        {
            var cancelTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var _ = cancellationToken.Register(() => cancelTcs.TrySetCanceled(cancellationToken));
            await Task.WhenAny(task, cancelTcs.Task).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
