using FIGCommon.Models;
using FIGCommon.Models.FIGProviderAPI;
using Microsoft.Data.SqlClient;

namespace FIGCommon.DataAccess;

public partial class MainRepo
{
    /// <summary>
    /// Finalize unsubmitted closes whose signal stopped while the AutoTrade is stopped.
    /// The existing upsert applies the shared BOT suspect transition in this transaction.
    /// </summary>
    public static int FailStoppedAutoTradeSignals(int autoTradeId)
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using var tx = connection.BeginTransaction();

        // Hold the AutoTrade row until commit, including against another process starting it.
        using var state = new SqlCommand("SELECT Status FROM dbo.AutoTrade WITH (UPDLOCK, HOLDLOCK) WHERE Id = @Id", connection, tx);
        state.Parameters.AddWithValue("@Id", autoTradeId);
        if (state.ExecuteScalar() is not int status || status != (int)EAutoTradeStatus.Stopped)
        {
            tx.Commit();
            return 0;
        }

        using var pending = new SqlCommand("""
            SELECT a.Id
            FROM dbo.AutoTradeSignal a WITH (UPDLOCK, HOLDLOCK)
            JOIN dbo.Signal s ON s.Id = a.SignalId
            WHERE a.AutoTradeId = @AutoTradeId
              AND a.CloseStatus = 'NONE' AND s.Status = 'STOP'
              AND NOT EXISTS (
                  SELECT 1 FROM dbo.AutoTradeSignalOrder o WITH (UPDLOCK, HOLDLOCK)
                  WHERE o.AutoTradeSignalId = a.Id AND o.OrderTag = 'CLOSE')
            ORDER BY a.Id;
            """, connection, tx);
        pending.Parameters.AddWithValue("@AutoTradeId", autoTradeId);
        var ids = new List<int>();
        using (var reader = pending.ExecuteReader())
            while (reader.Read()) ids.Add(reader.GetInt32(0));

        foreach (int id in ids)
        {
            var signal = GetAutoTradeSignal(id, tx)
                ?? throw new InvalidOperationException($"AutoTradeSignal({id}) disappeared during reconciliation.");
            signal.CloseStatusCode = OrderStatusCodes.FAIL_AUTOTRADE_STOPPED;
            signal.CloseStatus = OrderStatus.FAILED;
            signal.LastUpdated = -1;
            UpsertAutoTradeSignal(signal, tx);
        }
        tx.Commit();
        foreach (int id in ids)
            _logger?.LogWarning("AutoTrade({AutoTradeId}) - AutoTradeSignal({AutoTradeSignalId}) close failed: FAIL_AUTOTRADE_STOPPED", autoTradeId, id);
        return ids.Count;
    }
}
