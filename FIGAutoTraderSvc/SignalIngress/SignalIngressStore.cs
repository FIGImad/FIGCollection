using System.Data;
using System.Text.Json;
using FIGCommon.DataAccess;
using FIGCommon.Models.SignalPublication;
using Microsoft.Data.SqlClient;

namespace FIGAutoTradeExSvc.SignalIngress;

public interface ISignalIngressStore
{
    Task<SignalPublicationAck> ReceiveAsync(SignalPublicationMessage message, CancellationToken ct);
}

public sealed class SignalIngressStore : ISignalIngressStore
{
    private readonly Func<string> connectionString;
    public SignalIngressStore() : this(() => MainRepo.ConnectionString) { }
    public SignalIngressStore(Func<string> connectionString) => this.connectionString = connectionString;

    public async Task<SignalPublicationAck> ReceiveAsync(SignalPublicationMessage message, CancellationToken ct)
    {
        message.Validate();
        await using var connection = new SqlConnection(connectionString());
        await connection.OpenAsync(ct);
        await using var tx = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        // All receivers for this producer serialize, including concurrent retry/reconciliation requests.
        await using (var command = new SqlCommand("""
            DECLARE @Result int;
            EXEC @Result=sys.sp_getapplock @Resource=@Resource, @LockMode='Exclusive',
                @LockOwner='Transaction', @LockTimeout=10000;
            IF @Result<0 THROW 51000, 'Signal ingress lock unavailable', 1;
            IF EXISTS (SELECT 1 FROM dbo.SignalPublicationConfig WHERE Id=1 AND Enabled=1)
                THROW 51000, 'Receiver database cannot also be a remote publication source', 1;
            """, connection, tx))
        {
            command.Parameters.AddWithValue("@Resource", "SignalIngress:" + message.ProducerId);
            await command.ExecuteNonQueryAsync(ct);
        }

        int? signalId = null;
        long previousRevision = 0;
        PublishedSignal? previousPayload = null;
        await using (var command = new SqlCommand("""
            SELECT SignalId, Revision, PayloadHash, Payload FROM dbo.SignalPublicationReceipt WITH (UPDLOCK,HOLDLOCK)
            WHERE ProducerId=@Producer AND SignalTag=@Tag;
            """, connection, tx))
        {
            command.Parameters.AddWithValue("@Producer", message.ProducerId);
            command.Parameters.AddWithValue("@Tag", message.Signal.Tag);
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                signalId = reader.GetInt32(0); previousRevision = reader.GetInt64(1);
                previousPayload = JsonSerializer.Deserialize<PublishedSignal>(reader.GetString(3));
            }
        }
        if (previousRevision > message.Revision)
            throw new InvalidOperationException("Source revision is behind the receiver. Restore/reseed must be reconciled explicitly.");
        var hash = message.Signal.Hash();
        // Normalize persisted payloads too: older receipts may contain the removed Canceled field.
        if (previousRevision == message.Revision && previousPayload?.Hash() != hash)
            throw new InvalidOperationException("A published revision is immutable; its payload cannot change.");
        previousPayload?.ValidateSuccessor(message.Signal);

        // Verify execution metadata without transferring the private study configuration.
        await using (var command = new SqlCommand("""
            SELECT COUNT(*) FROM dbo.StrategyConfig sc
            JOIN dbo.StudyCol col ON col.Id=sc.StudyColId
            JOIN dbo.Interval i ON i.Id=col.IntervalId
            WHERE sc.Strategy=@Strategy AND i.IntervalLen>0;
            """, connection, tx))
        {
            command.Parameters.AddWithValue("@Strategy", message.Signal.Strategy);
            if (Convert.ToInt32(await command.ExecuteScalarAsync(ct)) != 1)
                throw new InvalidOperationException("Central strategy and interval metadata must be provisioned before publication.");
        }

        // Tag-based adoption supports moving an existing shared database installation to a private copy.
        var existing = await ReadSignal(connection, tx, signalId, message.Signal.Tag, ct);
        if (existing != null)
        {
            if (signalId == null) existing.Value.Signal.ValidateSuccessor(message.Signal);
            signalId = existing.Value.Id;
            await using var owner = new SqlCommand("""
                SELECT COUNT(*) FROM dbo.SignalPublicationReceipt
                WHERE SignalId=@Id AND (ProducerId<>@Producer OR SignalTag<>@Tag);
                """, connection, tx);
            owner.Parameters.AddWithValue("@Id", signalId);
            owner.Parameters.AddWithValue("@Producer", message.ProducerId);
            owner.Parameters.AddWithValue("@Tag", message.Signal.Tag);
            if (Convert.ToInt32(await owner.ExecuteScalarAsync(ct)) != 0)
                throw new InvalidOperationException("Signal already belongs to a different publication identity.");
        }

        // Idempotent periodic checks do not UPDATE Signal and do not fire SIGNAL notifications.
        // A mismatched materialized record is repaired from the authoritative publication.
        if (existing == null || existing.Value.Signal.Hash() != hash)
        {
            await using var command = new SqlCommand(existing == null ? """
                INSERT dbo.Signal (Strategy,Tag,Status,Side,StartTime,StopTime,StartPrice,StopPrice,LastUpdated)
                VALUES (@Strategy,@Tag,@Status,@Side,@StartTime,@StopTime,@StartPrice,@StopPrice,@LastUpdated);
                SELECT CONVERT(int,SCOPE_IDENTITY());
                """ : """
                UPDATE dbo.Signal SET Strategy=@Strategy,Tag=@Tag,Status=@Status,Side=@Side,
                    StartTime=@StartTime,StopTime=@StopTime,StartPrice=@StartPrice,StopPrice=@StopPrice,
                    LastUpdated=@LastUpdated WHERE Id=@Id;
                SELECT @Id;
                """, connection, tx);
            AddSignalParameters(command, message.Signal);
            command.Parameters.AddWithValue("@Id", (object?)signalId ?? DBNull.Value);
            signalId = Convert.ToInt32(await command.ExecuteScalarAsync(ct));
        }

        await using (var command = new SqlCommand("""
            UPDATE dbo.SignalPublicationReceipt SET SignalId=@Id,Revision=@Revision,PayloadHash=@Hash,
                Payload=@Payload,LastVerifiedAt=SYSUTCDATETIME()
            WHERE ProducerId=@Producer AND SignalTag=@Tag;
            IF @@ROWCOUNT=0
                INSERT dbo.SignalPublicationReceipt(ProducerId,SignalTag,SignalId,Revision,PayloadHash,Payload,LastVerifiedAt)
                VALUES(@Producer,@Tag,@Id,@Revision,@Hash,@Payload,SYSUTCDATETIME());
            """, connection, tx))
        {
            command.Parameters.AddWithValue("@Producer", message.ProducerId);
            command.Parameters.AddWithValue("@Tag", message.Signal.Tag);
            command.Parameters.AddWithValue("@Id", signalId!.Value);
            command.Parameters.AddWithValue("@Revision", message.Revision);
            command.Parameters.AddWithValue("@Hash", hash);
            command.Parameters.AddWithValue("@Payload", JsonSerializer.Serialize(message.Signal));
            await command.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
        return new(message.ProducerId, message.MessageId, message.Signal.Tag, message.Revision, hash, signalId!.Value);
    }

    private static async Task<(int Id, PublishedSignal Signal)?> ReadSignal(SqlConnection connection,
        SqlTransaction tx, int? id, string tag, CancellationToken ct)
    {
        await using var command = new SqlCommand("""
            SELECT Id,Tag,Strategy,Status,Side,StartTime,StopTime,StartPrice,StopPrice,LastUpdated
            FROM dbo.Signal WITH (UPDLOCK,HOLDLOCK) WHERE Id=@Id OR Tag=@Tag;
            """, connection, tx);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = (object?)id ?? DBNull.Value;
        command.Parameters.AddWithValue("@Tag", tag);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        var result = (reader.GetInt32(0), new PublishedSignal
        {
            Tag=reader.GetString(1), Strategy=reader.GetString(2), Status=reader.GetString(3), Side=reader.GetDecimal(4),
            StartTime=reader.IsDBNull(5)?null:reader.GetInt32(5), StopTime=reader.IsDBNull(6)?null:reader.GetInt32(6),
            StartPrice=reader.IsDBNull(7)?null:reader.GetDecimal(7), StopPrice=reader.IsDBNull(8)?null:reader.GetDecimal(8),
            LastUpdated=reader.GetInt32(9)
        });
        if (await reader.ReadAsync(ct)) throw new InvalidOperationException("Ambiguous signal tag in central database.");
        if (result.Item2.Tag != tag)
            throw new InvalidOperationException("Recorded central signal identity is now occupied by a different tag.");
        return result;
    }

    private static void AddSignalParameters(SqlCommand command, PublishedSignal signal)
    {
        command.Parameters.AddWithValue("@Tag", signal.Tag);
        command.Parameters.AddWithValue("@Strategy", signal.Strategy);
        command.Parameters.AddWithValue("@Status", signal.Status);
        foreach (var pair in new[] { ("@Side", (decimal?)signal.Side), ("@StartPrice", signal.StartPrice), ("@StopPrice", signal.StopPrice) })
        {
            var param = command.Parameters.Add(pair.Item1, SqlDbType.Decimal); param.Precision=18; param.Scale=4;
            param.Value=(object?)pair.Item2 ?? DBNull.Value;
        }
        command.Parameters.Add("@StartTime", SqlDbType.Int).Value=(object?)signal.StartTime ?? DBNull.Value;
        command.Parameters.Add("@StopTime", SqlDbType.Int).Value=(object?)signal.StopTime ?? DBNull.Value;
        command.Parameters.Add("@LastUpdated", SqlDbType.Int).Value=signal.LastUpdated;
    }
}
