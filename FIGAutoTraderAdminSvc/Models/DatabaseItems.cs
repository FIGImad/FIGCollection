

namespace FIGAutoTraderAdminSvc.Models
{
    public class DatabaseItem
    {
        public string Name { get; set; } = "";
        public string ConnectionString { get; set; } = "";
        public bool EnableBroker { get; set; } = false;
    }

    public interface IDatabaseItems
    {
        List<DatabaseItem> Get();
        void Set(List<DatabaseItem> items);
        void SetConnectionString(string name, string connectionString);
    }

    public class DatabaseItems : IDatabaseItems
    {
        private List<DatabaseItem> _items;

        public DatabaseItems(IConfiguration config)
        {
            _items = config.GetSection("DatabaseAdmin:DatabaseInUse").Get<List<DatabaseItem>>() ?? new List<DatabaseItem>();
        }

        public List<DatabaseItem> Get() => _items;

        public void Set(List<DatabaseItem> items)
        {
            _items = items;
        }

        public void SetConnectionString(string name, string connectionString)
        {
            var item = _items.FirstOrDefault(x => x.Name == name);
            if (item != null)
            {
                item.ConnectionString = connectionString;
            }
        }

        public List<string> GetDatabaseNames()
        {
            return _items.Select(x => x.Name).ToList();
        }

        public List<string> GetBrokerDatabaseNames()
        {
            // only return items where EnableBroker is true
            _items = _items.Where(x => x.EnableBroker).ToList();
            return _items.Select(x => x.Name).ToList();
        }
    }
}


