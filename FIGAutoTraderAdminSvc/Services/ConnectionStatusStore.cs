using FIGAutoTraderAdminSvc.Models;
using System.Collections.Concurrent;

namespace FIGAutoTraderAdminSvc.Services
{
    public interface IConnectionStatusStore
    {
        ConnectionStatus? Get(string key);
        List<ConnectionStatus> GetAll();
        void Set(string key, ConnectionStatus status);
        bool Remove(string key);
        void RetainKeys(IReadOnlySet<string> keys);
    }

    public sealed class ConnectionStatusStore : IConnectionStatusStore
    {
        private ConcurrentDictionary<string, ConnectionStatus> _statusMap = new ConcurrentDictionary<string, ConnectionStatus>();

        public ConnectionStatus? Get(string key)
        {
            if (_statusMap.TryGetValue(key, out var status))
            {
                return new ConnectionStatus(status);
            }
            return null;
        }

        public List<ConnectionStatus> GetAll()
        {
            // return a copy of all connection statuses in the dictionary
            return _statusMap.Values.Select(status => new ConnectionStatus(status)).ToList();
        }

        public void Set(string key, ConnectionStatus status)
        {
            var copy = new ConnectionStatus(status)
            {
                Key = key
            };

            _statusMap[key] = copy;
        }

        public bool Remove(string key)
        {
            return _statusMap.TryRemove(key, out _);
        }

        public void RetainKeys(IReadOnlySet<string> keys)
        {
            foreach (var key in _statusMap.Keys)
            {
                if (!keys.Contains(key))
                {
                    _statusMap.TryRemove(key, out _);
                }
            }
        }
    }
}
