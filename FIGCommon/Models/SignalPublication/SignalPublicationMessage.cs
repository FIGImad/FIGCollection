using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FIGCommon.Models.SignalPublication;

// Deliberately excludes study parameters, study results and database identity IDs.
public sealed record PublishedSignal
{
    public string Tag { get; init; } = "";
    public string Strategy { get; init; } = "";
    public string Status { get; init; } = "";
    public decimal Side { get; init; }
    public long? StartTime { get; init; }
    public long? StopTime { get; init; }
    public decimal? StartPrice { get; init; }
    public decimal? StopPrice { get; init; }
    public long LastUpdated { get; init; }

    public string Hash() => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(new { Tag, Strategy, Status,
            Side = Side.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture),
            StartTime, StopTime,
            StartPrice = StartPrice?.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture),
            StopPrice = StopPrice?.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture),
            LastUpdated }))));

    public void Validate()
    {
        if (!Guid.TryParse(Tag, out _) || Tag.Length > 50 || string.IsNullOrWhiteSpace(Strategy)
            || Strategy.Length > 100 || Strategy != Strategy.Trim())
            throw new ArgumentException("Signal tag must be a GUID and strategy must be a nonempty stable key.");
        if (Status != SignalStatus.Start && Status != SignalStatus.Stop)
            throw new ArgumentException("Signal status must be START or STOP.");
        if (Side == 0 || StartTime is null or < 0 or > int.MaxValue
            || StopTime is < 0 or > int.MaxValue || LastUpdated < 0 || LastUpdated > int.MaxValue
            || (Status == SignalStatus.Stop && (StopTime == null || StopTime < StartTime))
            || (Status == SignalStatus.Start && StopTime != null))
            throw new ArgumentException("Invalid signal direction or timestamps.");
        foreach (var value in new decimal?[] { Side, StartPrice, StopPrice })
            if (value.HasValue && (decimal.Round(value.Value, 4) != value.Value
                || Math.Abs(value.Value) > 99999999999999.9999m))
                throw new ArgumentException("Signal numeric fields must fit decimal(18,4) exactly.");
    }

    public void ValidateSuccessor(PublishedSignal next)
    {
        if (Tag != next.Tag || Strategy != next.Strategy || Side != next.Side || StartTime != next.StartTime)
            throw new InvalidOperationException("Signal identity, strategy, side and start time are immutable.");
        if (Status == SignalStatus.Stop && (next.Status != SignalStatus.Stop || StopTime != next.StopTime))
            throw new InvalidOperationException("STOP is final. Only informational prices and update time may change.");
    }
}

public sealed record SignalPublicationMessage
{
    public int ContractVersion { get; init; } = 1;
    public string ProducerId { get; init; } = "";
    public Guid MessageId { get; init; }
    public long Revision { get; init; }
    public PublishedSignal Signal { get; init; } = new();
    public void Validate()
    {
        if (ContractVersion != 1 || string.IsNullOrWhiteSpace(ProducerId) || ProducerId.Length > 50
            || ProducerId != ProducerId.Trim() || MessageId == Guid.Empty || Revision <= 0 || Signal == null)
            throw new ArgumentException("Invalid signal publication envelope.");
        Signal.Validate();
    }
}

public sealed record SignalPublicationAck(string ProducerId, Guid MessageId, string SignalTag,
    long Revision, string PayloadHash, int CentralSignalId)
{
    public bool Matches(SignalPublicationMessage message) => ProducerId == message.ProducerId
        && MessageId == message.MessageId && SignalTag == message.Signal.Tag && Revision == message.Revision
        && PayloadHash == message.Signal.Hash() && CentralSignalId > 0;
}
