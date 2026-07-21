namespace FIGPriceSyncSvc.Models
{
    public class ProviderConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 0;
        public int ClientId { get; set; } = 0;
        public List<int> DataSetIds { get; set; } = new();

        public ProviderConfig()
        {
            this.Id = "";
            this.Type = "";
            this.Host = "";
            this.Port = 0;
            this.ClientId = 0;
            this.DataSetIds = new();
        }

        public ProviderConfig(ProviderConfig rec)
        {
            this.Id = rec.Id;
            this.Type = rec.Type;
            this.Host = rec.Host;
            this.Port = rec.Port;
            this.ClientId = rec.ClientId;
            this.DataSetIds = new List<int>(rec.DataSetIds);
        }

        public ProviderConfig Clone()
        {
            return new ProviderConfig(this);
        }

    }
}