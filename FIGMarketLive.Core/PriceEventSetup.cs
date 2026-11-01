using System.Data;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed class PriceEventSetup(string connectionString)
{
    public const string EventType="MARKETLIVE_PRICEDATA";
    public async Task EnsureSchemaAsync(CancellationToken ct)
    {
        using var stream=typeof(PriceEventSetup).Assembly.GetManifestResourceStream("FIGMarketLive.Core.Database.CreatePriceEvents.sql")??throw new InvalidDataException("Missing price event schema.");
        using var reader=new StreamReader(stream);string sql=await reader.ReadToEndAsync(ct);
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand(sql,c){CommandTimeout=60};await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task<DatabaseCheck> CheckNotificationsAsync(CancellationToken ct)
    {
        var brokerProblem=await EnsureBrokerAsync(ct);
        if(brokerProblem is not null)return new(false,brokerProblem);
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using(var check=new SqlCommand("""
            SELECT is_broker_enabled FROM sys.databases WHERE database_id=DB_ID();
            SELECT HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','SUBSCRIBE QUERY NOTIFICATIONS');
            SELECT COUNT(*) FROM sys.triggers WHERE object_id=OBJECT_ID('dbo.trigMarketLivePriceDataChange') AND parent_id=OBJECT_ID('dbo.PriceData') AND is_disabled=0;
            """,c))
        {
            await using var r=await check.ExecuteReaderAsync(ct);await r.ReadAsync(ct);
            if(!r.GetBoolean(0))return new(false,"SQL Service Broker is disabled. A database administrator must enable it with ALTER DATABASE [your database] SET ENABLE_BROKER, then test again. FIGMarketLive will not disconnect database users to enable it.");
            await r.NextResultAsync(ct);await r.ReadAsync(ct);
            if(r.IsDBNull(0)||r.GetInt32(0)!=1)return new(false,"The database user needs SUBSCRIBE QUERY NOTIFICATIONS permission. Grant it and test again.");
            await r.NextResultAsync(ct);await r.ReadAsync(ct);
            if(r.GetInt32(0)!=1)return new(false,"The PriceData event trigger is missing or disabled. Run database setup again.");
        }
        // Verify delivery with this login, not only broker flags. The uniquely owned
        // probe event is committed and then removed; no prices/studies are modified.
        string id=Guid.NewGuid().ToString("N");bool started=false;
        string notificationConnection=new SqlConnectionStringBuilder(connectionString).ConnectionString;
        SqlDependency? dependency=null;
        var completion=new TaskCompletionSource<SqlNotificationEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Changed(object sender,SqlNotificationEventArgs args)=>completion.TrySetResult(args);
        try
        {
            SqlDependency.Start(notificationConnection);started=true;
            await using(var options=new SqlCommand("SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET ARITHABORT ON; SET NUMERIC_ROUNDABORT OFF;",c))
                await options.ExecuteNonQueryAsync(ct);
            await using var query=new SqlCommand("SELECT Id,EventType,EventId,Message,[TimeStamp] FROM dbo.Event WHERE EventType=@Type AND EventId=@Id",c);
            query.Parameters.Add("@Type",SqlDbType.VarChar,50).Value="MARKETLIVE_NOTIFICATION_TEST";query.Parameters.Add("@Id",SqlDbType.VarChar,100).Value=id;
            dependency=new SqlDependency(query);dependency.OnChange+=Changed;
            await using(var r=await query.ExecuteReaderAsync(ct)){while(await r.ReadAsync(ct)){} }
            await using var insert=new SqlCommand("INSERT dbo.Event(EventType,EventId,Message,[TimeStamp]) VALUES('MARKETLIVE_NOTIFICATION_TEST',@Id,'Notification test',GETDATE())",c);
            insert.Parameters.AddWithValue("@Id",id);await insert.ExecuteNonQueryAsync(ct);
            var result=await completion.Task.WaitAsync(TimeSpan.FromSeconds(10),ct);
            return result.Type==SqlNotificationType.Change&&result.Info==SqlNotificationInfo.Insert
                ?new(true,"SQL Service Broker, notification permissions and end-to-end notification delivery passed. PriceData events are written in the price transaction.")
                :new(false,$"SQL notification registration/delivery failed: {result.Type}, {result.Source}, {result.Info}. Check query notification permissions and database SET options.");
        }
        catch(OperationCanceledException)when(ct.IsCancellationRequested){throw;}
        catch(Exception ex){return new(false,$"SQL notifications are not ready: {ex.Message}. Check Service Broker, CREATE QUEUE / CREATE SERVICE and notification permissions. Backup reconciliation remains available.");}
        finally
        {
            if(dependency is not null)dependency.OnChange-=Changed;
            await using var cleanup=new SqlCommand("DELETE dbo.Event WHERE EventType='MARKETLIVE_NOTIFICATION_TEST' AND EventId=@Id",c);cleanup.Parameters.AddWithValue("@Id",id);
            try{await cleanup.ExecuteNonQueryAsync(CancellationToken.None);}
            finally{if(started)SqlDependency.Stop(notificationConnection);}
        }
    }
    private async Task<string?> EnsureBrokerAsync(CancellationToken ct)
    {
        // Never force rollback or evict active sessions to enable Broker. Use a
        // nonpooled inspection connection, then ALTER from master with a short timeout.
        var builder=new SqlConnectionStringBuilder(connectionString){Pooling=false};
        await using var inspect=new SqlConnection(builder.ConnectionString);await inspect.OpenAsync(ct);
        string database=inspect.Database;
        await using(var query=new SqlCommand("SELECT is_broker_enabled FROM sys.databases WHERE database_id=DB_ID()",inspect))
            if(Convert.ToBoolean(await query.ExecuteScalarAsync(ct)))return null;
        await inspect.CloseAsync();builder.InitialCatalog="master";
        // Release this application's idle pooled connections, which otherwise
        // retain database sessions after setup. Active operations are not aborted.
        using(var pool=new SqlConnection(connectionString))SqlConnection.ClearPool(pool);
        string quoted="["+database.Replace("]","]]",StringComparison.Ordinal)+"]";
        try
        {
            await using var master=new SqlConnection(builder.ConnectionString);await master.OpenAsync(ct);
            await using var enable=new SqlCommand($"SET LOCK_TIMEOUT 5000; ALTER DATABASE {quoted} SET ENABLE_BROKER;",master){CommandTimeout=8};
            await enable.ExecuteNonQueryAsync(ct);return null;
        }
        catch(OperationCanceledException)when(ct.IsCancellationRequested){throw;}
        catch(SqlException ex)
        {
            return "SQL Service Broker is disabled and could not be enabled without interrupting database sessions. "+
                $"Close other database connections and test again, or ask an administrator to run ALTER DATABASE {quoted} SET ENABLE_BROKER when connections permit it. "+
                $"No sessions were forcibly disconnected. SQL Server: {ex.Message}";
        }
    }
}
