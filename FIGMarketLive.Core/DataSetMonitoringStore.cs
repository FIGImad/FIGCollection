using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed class DataSetMonitoringStore(string connectionString)
{
    public async Task EnsureSchemaAsync(CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("""
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;
            DECLARE @Lock int;
            EXEC @Lock=sys.sp_getapplock @Resource='FIGMarketLive.DataSetSchema',@LockMode='Exclusive',@LockOwner='Transaction';
            IF @Lock<0 THROW 51070,'Cannot lock DataSet schema.',1;
            IF OBJECT_ID('dbo.DataSet','U') IS NULL THROW 51071,'Configure the database schema first.',1;
            IF COL_LENGTH('dbo.DataSet','Monitor') IS NULL
                ALTER TABLE dbo.DataSet ADD Monitor bit NOT NULL CONSTRAINT DF_DataSet_Monitor DEFAULT(0) WITH VALUES;
            COMMIT;
            """,c);
        await cmd.ExecuteNonQueryAsync(ct);
        await new ExchangeScheduleStore(connectionString).EnsureSchemaAsync(ct);
    }

    public async Task SetAsync(int dataSetId,bool enabled,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("UPDATE dbo.DataSet SET Monitor=@Enabled WHERE Id=@Id; IF @@ROWCOUNT<>1 THROW 51072,'Dataset no longer exists. Refresh the list.',1;",c);
        cmd.Parameters.AddWithValue("@Enabled",enabled);cmd.Parameters.AddWithValue("@Id",dataSetId);await cmd.ExecuteNonQueryAsync(ct);
    }
}
