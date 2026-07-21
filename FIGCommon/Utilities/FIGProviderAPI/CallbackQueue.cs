using FIGCommon.Interfaces;
using FIGCommon.Models.FIGBroker;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace FIGCommon.Utilities.FIGProviderAPI
{
    public class CallbackQueue : ICallbackQueue
    {
        private readonly int _capacity;
        private readonly ConcurrentDictionary<Guid, Subscription> _subscriptions = new();

        public CallbackQueue(int capacity = 1000)
        {
            _capacity = capacity;
        }

        public bool TryEnqueue(BrokerCallbackMessage message)
        {
            bool delivered = false;
            foreach (var sub in _subscriptions.Values)
            {
                if (sub.Accepts(message.Type))
                    delivered |= sub.Channel.Writer.TryWrite(message);
            }
            return delivered;
        }

        public IAsyncEnumerable<BrokerCallbackMessage> Subscribe(params BrokerCallbackMessageType[] messageTypes)
        {
            var sub = new Subscription(_capacity, messageTypes);
            _subscriptions[sub.Id] = sub;
            return sub.ReadAllAsync();
        }

        private sealed class Subscription
        {
            private readonly HashSet<BrokerCallbackMessageType> _filter;

            public Guid Id { get; } = Guid.NewGuid();
            public Channel<BrokerCallbackMessage> Channel { get; }

            public Subscription(int capacity, BrokerCallbackMessageType[] messageTypes)
            {
                _filter = [.. messageTypes];
                Channel = System.Threading.Channels.Channel.CreateBounded<BrokerCallbackMessage>(
                    new BoundedChannelOptions(capacity)
                    {
                        // Drop the oldest message when full instead of blocking the producer
                        FullMode = BoundedChannelFullMode.DropOldest,
                        SingleReader = true,
                        SingleWriter = false
                    });
            }

            public bool Accepts(BrokerCallbackMessageType type) =>
                _filter.Count == 0 || _filter.Contains(type);

            public async IAsyncEnumerable<BrokerCallbackMessage> ReadAllAsync(
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await foreach (var message in Channel.Reader.ReadAllAsync(cancellationToken))
                {
                    if (message.IsExpired)
                        continue;

                    yield return message;
                }
            }
        }
    }
}
