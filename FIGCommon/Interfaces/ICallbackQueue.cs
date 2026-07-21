using FIGCommon.Models.FIGBroker;
using System.Runtime.CompilerServices;

namespace FIGCommon.Interfaces
{
    public interface ICallbackQueue
    {
        //ValueTask EnqueueAsync(BrokerCallbackMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribe to receive only the specified message types.
        /// Returns a dedicated async-enumerable for this subscriber.
        /// </summary>
        IAsyncEnumerable<BrokerCallbackMessage> Subscribe(params BrokerCallbackMessageType[] messageTypes);
        bool TryEnqueue(BrokerCallbackMessage message);
    }
}
