namespace FIGAutoTraderAdminSvc.Models
{
    public sealed class ConnectionStatus
    {
        public string Key { get; set; } = "";
        public int Role { get; set; } = 0;
        public bool IsConnected { get; set; }
        public string Message { get; set; } = "";
        public DateTime LastCheckedUtc { get; set; }
        public DateTime? LastConnectedUtc { get; set; }
        public DateTime? LastDisconnectedUtc { get; set; }

        public ConnectionStatus() { }

        // make a copy constructor
        public ConnectionStatus(ConnectionStatus other)
        {
            Key = other.Key;
            Role = other.Role;
            IsConnected = other.IsConnected;
            Message = other.Message;
            LastCheckedUtc = other.LastCheckedUtc;
            LastConnectedUtc = other.LastConnectedUtc;
            LastDisconnectedUtc = other.LastDisconnectedUtc;
        }
    }
}
