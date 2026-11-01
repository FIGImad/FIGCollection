using System.Data;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed record NotificationSetupTarget(string Database,string DatabaseUser,string Login,bool IsOwner);

/// <summary>Explicit, one-time administrative setup. Credentials are never persisted.</summary>
public sealed class NotificationAdministratorSetup(string applicationConnection)
{
    public async Task<NotificationSetupTarget> TargetAsync(CancellationToken ct)
    {
        await using var c=new SqlConnection(new SqlConnectionStringBuilder(applicationConnection){Pooling=false}.ConnectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("SELECT DB_NAME(),USER_NAME(),ORIGINAL_LOGIN(),CONVERT(int,USER_ID())",c);
        await using var r=await cmd.ExecuteReaderAsync(ct);await r.ReadAsync(ct);
        string database=r.GetString(0);
        if(database.Equals("master",StringComparison.OrdinalIgnoreCase)||database.Equals("model",StringComparison.OrdinalIgnoreCase)||database.Equals("msdb",StringComparison.OrdinalIgnoreCase)||database.Equals("tempdb",StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Select the FIGMarketLive application database, not a system database.");
        if(r.GetInt32(3)!=1&&r.GetInt32(3)<5)throw new InvalidDataException("The application login must map to its own database user, not guest or a system principal.");
        return new(database,r.GetString(1),r.GetString(2),r.GetInt32(3)==1);
    }
    public async Task ConfigureAsync(NotificationSetupTarget reviewed,string? adminUser,string? adminPassword,CancellationToken ct)
    {
        var target=await TargetAsync(ct);
        if(target!=reviewed)throw new InvalidDataException("The target database or application user changed. Review notification setup again.");
        var settings=new SqlConnectionStringBuilder(applicationConnection){InitialCatalog="master",Pooling=false,ConnectRetryCount=0};
        if(!string.IsNullOrWhiteSpace(adminUser))
        {
            settings.Remove("Authentication");settings.IntegratedSecurity=false;settings.UserID=adminUser;settings.Password=adminPassword??"";
        }
        using(var pool=new SqlConnection(applicationConnection))SqlConnection.ClearPool(pool);
        await using var admin=new SqlConnection(settings.ConnectionString);await admin.OpenAsync(ct);
        string db=Quote(target.Database),user=Quote(target.DatabaseUser);
        // Check privileges and user mapping before any sessions are disconnected.
        await using(var preflight=new SqlCommand($"""
            USE {db};
            IF COALESCE(HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','CONTROL'),0)<>1 AND COALESCE(IS_SRVROLEMEMBER('sysadmin'),0)<>1
                THROW 51120,'Notification setup requires database CONTROL permission or a SQL Server administrator. Enter administrator credentials for this one-time operation.',1;
            IF DATABASE_PRINCIPAL_ID(@User) IS NULL
                THROW 51121,'The application database user no longer exists.',1;
            IF EXISTS(SELECT 1 FROM sys.databases WHERE database_id=DB_ID() AND user_access<>0)
                THROW 51122,'The database is already in restricted/single-user mode. Restore its access mode before running notification setup.',1;
            USE master;
            """,admin))
        {preflight.Parameters.Add("@User",SqlDbType.NVarChar,128).Value=target.DatabaseUser;await preflight.ExecuteNonQueryAsync(ct);}
        bool attempted=false;
        try
        {
            attempted=true;
            await using var enable=new SqlCommand($"""
                USE master;
                IF EXISTS(SELECT 1 FROM sys.databases WHERE name=@Database AND is_broker_enabled=0)
                BEGIN
                    BEGIN TRY
                        ALTER DATABASE {db} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        ALTER DATABASE {db} SET ENABLE_BROKER;
                        ALTER DATABASE {db} SET MULTI_USER WITH ROLLBACK IMMEDIATE;
                    END TRY
                    BEGIN CATCH
                        ALTER DATABASE {db} SET MULTI_USER WITH ROLLBACK IMMEDIATE;
                        THROW;
                    END CATCH;
                END;
                USE {db};
                """+(target.IsOwner?"":$"GRANT SUBSCRIBE QUERY NOTIFICATIONS TO {user}; GRANT CREATE QUEUE TO {user}; GRANT CREATE SERVICE TO {user}; GRANT REFERENCES ON CONTRACT::[http://schemas.microsoft.com/SQL/Notifications/PostQueryNotification] TO {user};")+" USE master;",admin){CommandTimeout=60};
            enable.Parameters.Add("@Database",SqlDbType.NVarChar,128).Value=target.Database;await enable.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            if(attempted)
            {
                // Cancellation/timeout can bypass T-SQL CATCH. Recover with a fresh
                // master connection and an independent token before returning.
                try
                {
                    await using var recovery=new SqlConnection(settings.ConnectionString);await recovery.OpenAsync(CancellationToken.None);
                    await using var restore=new SqlCommand($"IF EXISTS(SELECT 1 FROM sys.databases WHERE name=@Database AND user_access=1) ALTER DATABASE {db} SET MULTI_USER WITH ROLLBACK IMMEDIATE;",recovery){CommandTimeout=30};
                    restore.Parameters.Add("@Database",SqlDbType.NVarChar,128).Value=target.Database;await restore.ExecuteNonQueryAsync(CancellationToken.None);
                }
                catch(Exception ex){throw new InvalidOperationException($"Could not confirm restoration of MULTI_USER. An administrator must check the database and run ALTER DATABASE {db} SET MULTI_USER. {ex.Message}",ex);}
            }
        }
    }
    private static string Quote(string identifier)
    {
        if(string.IsNullOrWhiteSpace(identifier)||identifier.Length>128)throw new InvalidDataException("Invalid SQL database/user identifier.");
        return "["+identifier.Replace("]","]]",StringComparison.Ordinal)+"]";
    }
}
