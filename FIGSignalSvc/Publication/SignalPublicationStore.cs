using FIGCommon.DataAccess;
using FIGCommon.Models.SignalPublication;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace FIGSignalExSvc.Publication;

public sealed record PendingSignal(int SignalId, SignalPublicationMessage Message);

public interface ISignalPublicationStore
{
    Task InitializeAsync(string producerId, CancellationToken ct);
    Task<List<PendingSignal>> ReadAsync(int afterId, int count, bool pendingOnly, CancellationToken ct);
    Task AcknowledgeAsync(PendingSignal item, SignalPublicationAck ack, CancellationToken ct);
    Task RecordFailureAsync(PendingSignal item, string error, CancellationToken ct);
}

public sealed class SignalPublicationStore : ISignalPublicationStore
{
    private readonly Func<string> connectionString;
    public SignalPublicationStore() : this(() => MainRepo.ConnectionString) { }
    public SignalPublicationStore(Func<string> connectionString) => this.connectionString = connectionString;
    private async Task<SqlConnection> Open(CancellationToken ct)
    {
        var connection = new SqlConnection(connectionString());
        try { await connection.OpenAsync(ct); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }
    public async Task InitializeAsync(string producerId, CancellationToken ct)
    {
        await using var connection = await Open(ct);
        await using var cmd = new SqlCommand("dbo.usp_signal_publication_initialize", connection)
        { CommandType = CommandType.StoredProcedure, CommandTimeout = 120 };
        cmd.Parameters.Add("@ProducerId", SqlDbType.VarChar, 50).Value = producerId;
        await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task<List<PendingSignal>> ReadAsync(int afterId, int count, bool pendingOnly, CancellationToken ct)
    {
        await using var connection = await Open(ct);
        await using var cmd = new SqlCommand("""
            SELECT TOP (@Count) s.Id, o.MessageId, o.Revision, o.Payload, c.ProducerId
            FROM dbo.Signal s
            JOIN dbo.SignalPublicationOutbox o ON o.SignalId=s.Id AND o.Revision=s.SyncVersion
            CROSS JOIN dbo.SignalPublicationConfig c
            WHERE c.Id=1 AND c.Enabled=1 AND s.Id>@AfterId
              AND (@PendingOnly=0 OR s.SyncedVersion<s.SyncVersion)
            ORDER BY s.Id;
            """, connection);
        cmd.Parameters.AddWithValue("@Count", count);
        cmd.Parameters.AddWithValue("@AfterId", afterId);
        cmd.Parameters.AddWithValue("@PendingOnly", pendingOnly);
        var result = new List<PendingSignal>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            result.Add(new(reader.GetInt32(0), new SignalPublicationMessage
            {
                MessageId = reader.GetGuid(1), Revision = reader.GetInt64(2),
                Signal = JsonSerializer.Deserialize<PublishedSignal>(reader.GetString(3))
                    ?? throw new InvalidOperationException("Invalid durable signal payload."),
                ProducerId = reader.GetString(4)
            }));
        return result;
    }
    public async Task AcknowledgeAsync(PendingSignal item, SignalPublicationAck ack, CancellationToken ct)
    {
        if (!ack.Matches(item.Message)) throw new InvalidOperationException("Mismatched acknowledgement.");
        await using var connection = await Open(ct);
        await using var cmd = new SqlCommand("""
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;
            UPDATE dbo.Signal SET SyncedVersion=@Revision, SyncedAt=SYSUTCDATETIME(), SyncError=NULL
            WHERE Id=@Id AND SyncVersion=@Revision AND SyncedVersion<=@Revision;
            UPDATE dbo.SignalPublicationOutbox SET AcknowledgedAt=SYSUTCDATETIME()
            WHERE SignalId=@Id AND Revision<=@Revision AND AcknowledgedAt IS NULL;
            COMMIT;
            """, connection);
        cmd.Parameters.AddWithValue("@Id", item.SignalId);
        cmd.Parameters.AddWithValue("@Revision", item.Message.Revision);
        await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task RecordFailureAsync(PendingSignal item, string error, CancellationToken ct)
    {
        await using var connection = await Open(ct);
        await using var cmd = new SqlCommand("""
            UPDATE dbo.Signal SET SyncedVersion=0, SyncError=@Error
            WHERE Id=@Id AND SyncVersion=@Revision;
            """, connection);
        cmd.Parameters.AddWithValue("@Id", item.SignalId);
        cmd.Parameters.AddWithValue("@Revision", item.Message.Revision);
        cmd.Parameters.Add("@Error", SqlDbType.NVarChar, 1000).Value = error[..Math.Min(error.Length, 1000)];
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
