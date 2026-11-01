using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public static class PriceSchemaMigration
{
    public static async Task MigrateAsync(string connectionString,CancellationToken ct)
    {
        using var resource=typeof(PriceSchemaMigration).Assembly.GetManifestResourceStream("FIGMarketLive.Core.Database.MigrateDataSetMonitoring.sql")
            ??throw new InvalidDataException("Missing dataset schema migration resource.");
        using var reader=new StreamReader(resource);string sql=await reader.ReadToEndAsync(ct);
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand(sql,c){CommandTimeout=120};await cmd.ExecuteNonQueryAsync(ct);
        await new ExchangeScheduleStore(connectionString).EnsureSchemaAsync(ct);
    }

    public static async Task<string> VerifyAsync(string connectionString,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("""
            IF COL_LENGTH('dbo.PriceData','PriceDate') IS NOT NULL THROW 51074,'PriceDate still exists.',1;
            IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.DataSet') AND name='Monitor' AND system_type_id=104 AND is_nullable=0)
                THROW 51075,'DataSet.Monitor must be bit NOT NULL.',1;
            SELECT COUNT(*),COALESCE(SUM(CONVERT(int,Monitor)),0) FROM dbo.DataSet;
            """,c);
        await using var r=await cmd.ExecuteReaderAsync(ct);await r.ReadAsync(ct);
        return $"Verified DataSet.Monitor (bit NOT NULL): {r.GetInt32(1)} of {r.GetInt32(0)} datasets enabled for price sync. PriceData.PriceDate removed; RawTime is the stored timestamp.";
    }

    public static async Task AuditAsync(string connectionString,string target,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("""
            SELECT QUOTENAME(OBJECT_SCHEMA_NAME(m.object_id))+'.'+QUOTENAME(OBJECT_NAME(m.object_id)),m.definition
            FROM sys.sql_modules m WHERE m.definition LIKE '%PriceDate%' AND m.definition LIKE '%PriceData%';
            SELECT i.name,c.name FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
                JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
                WHERE c.object_id=OBJECT_ID('dbo.PriceData') AND c.name='PriceDate';
            """,c);
        await using var r=await cmd.ExecuteReaderAsync(ct);await using var output=new StreamWriter(target);
        while(await r.ReadAsync(ct)){await output.WriteLineAsync("-- "+r.GetString(0));await output.WriteLineAsync(r.GetString(1));await output.WriteLineAsync("GO");}
        await r.NextResultAsync(ct);while(await r.ReadAsync(ct))await output.WriteLineAsync($"-- INDEX {r.GetString(0)} COLUMN {r.GetString(1)}");
    }
}
