namespace FIGSignalExSvc.Publication;

public sealed class SignalPublicationOptions
{
    public string Mode { get; set; } = "SharedDatabase";
    public string ProducerId { get; set; } = "";
    public string DestinationServiceId { get; set; } = "";
    public int ReconciliationSeconds { get; set; } = 60;
    public int RetrySeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 100;
    public bool IsRemote => Mode == "RemotePublisher";

    public void Validate()
    {
        if (Mode != "SharedDatabase" && Mode != "RemotePublisher")
            throw new InvalidOperationException("SignalPublication:Mode must be SharedDatabase or RemotePublisher.");
        if (IsRemote && (string.IsNullOrWhiteSpace(ProducerId) || ProducerId.Length > 50
            || ProducerId != ProducerId.Trim() || string.IsNullOrWhiteSpace(DestinationServiceId)))
            throw new InvalidOperationException("RemotePublisher requires a stable ProducerId and explicit DestinationServiceId.");
        if (ReconciliationSeconds is < 1 or > 3600 || RetrySeconds is < 1 or > 60 || BatchSize is < 1 or > 1000)
            throw new InvalidOperationException("Invalid signal publication timer or batch size.");
    }
}
