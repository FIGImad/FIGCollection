namespace FIGCommon.Models.FIGProviderAPI
{
    public class ProviderOptions
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "IBKR";
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 7496;
        public int ClientId { get; set; } = -1;
        public int TrackingClientId { get; set; } = -1;
        public bool EnableFrozenData { get; set; } = false;
        public int CallbackQueueCapacity { get; set; } = 1000;
    }
}
