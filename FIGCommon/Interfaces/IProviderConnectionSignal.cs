namespace FIGCommon.Interfaces
{
    /// <summary>
    /// A resettable latch that any number of services can await regardless of when
    /// the provider connected. Late awaiters receive the result immediately.
    /// Resets automatically when the provider disconnects so services can re-await
    /// the next connection.
    /// </summary>
    public interface IProviderConnectionSignal
    {
        /// <summary>Waits until the provider has successfully connected.</summary>
        Task WaitForConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>Waits until the provider disconnects. Returns immediately if already disconnected.</summary>
        Task WaitForDisconnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>Called by the provider when a connection is established.</summary>
        void NotifyConnected();

        /// <summary>Called by the provider when the connection is lost.</summary>
        void NotifyDisconnected();
    }
}

