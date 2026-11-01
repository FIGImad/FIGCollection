using System.Data;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed record StudyCollectionDeletionPreview(StudyConfigurationRow Study,long StudyHistoryRows,bool HasStudyHistory=true);

public sealed class StudyCollectionDeletionStore(string connectionString)
{
    public async Task<StudyCollectionDeletionPreview> PreviewAsync(int id,CancellationToken ct)
    {await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);return await ReadAsync(c,null,id,ct);}

    private static async Task<StudyCollectionDeletionPreview> ReadAsync(SqlConnection c,SqlTransaction? tx,int id,CancellationToken ct)
    {
        string hint=tx is null?"":" WITH(UPDLOCK,HOLDLOCK)";
        await using var cmd=new SqlCommand($"""
            SELECT s.Id,s.TickerId,s.IntervalId,s.ColType,s.ColParams,s.Enabled,
                (SELECT COUNT_BIG(*) FROM dbo.StudyHistory{hint} WHERE StudyColId=@Id)
                FROM dbo.StudyCol s{hint} WHERE s.Id=@Id;
            """,c,tx);
        cmd.Parameters.AddWithValue("@Id",id);await using var r=await cmd.ExecuteReaderAsync(ct);
        if(!await r.ReadAsync(ct))throw new InvalidDataException("StudyCol no longer exists. Reload Study Collections.");
        return new(new(r.GetInt32(0),r.GetInt32(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetBoolean(5)),r.GetInt64(6));
    }

    public static void ValidateUnchanged(StudyCollectionDeletionPreview confirmed,StudyCollectionDeletionPreview current)
    {if(confirmed!=current)throw new InvalidDataException("StudyCol or its history changed after confirmation. Reload and confirm deletion again.");}

    public async Task DeleteConfirmedAsync(StudyCollectionDeletionPreview confirmed,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        // Lock the definition first, matching the study publisher's lock order.
        await using(var claim=new SqlCommand("SELECT Id FROM dbo.StudyCol WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Id",c,tx))
        {claim.Parameters.AddWithValue("@Id",confirmed.Study.Id);await claim.ExecuteScalarAsync(ct);}
        ValidateUnchanged(confirmed,await ReadAsync(c,tx,confirmed.Study.Id,ct));
        await using var cmd=new SqlCommand("""
            IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE delete_referential_action<>0 AND
                ((referenced_object_id=OBJECT_ID('dbo.StudyCol') AND parent_object_id NOT IN(COALESCE(OBJECT_ID('dbo.StudyHistory'),-1),COALESCE(OBJECT_ID('dbo.StudyRegeneration'),-1),COALESCE(OBJECT_ID('dbo.StudyTailCheckpoint'),-1))) OR
                 referenced_object_id IN(COALESCE(OBJECT_ID('dbo.StudyHistory'),-1))))
                THROW 51080,'Unexpected cascading relationships exist. No study data was removed.',1;
            IF OBJECT_ID('dbo.StudyHistory','U') IS NOT NULL
                EXEC sys.sp_executesql N'DELETE FROM dbo.StudyHistory WHERE StudyColId=@Id',N'@Id int',@Id=@Id;
            DELETE FROM dbo.StudyCol WHERE Id=@Id;
            """,c,tx);
        cmd.Parameters.AddWithValue("@Id",confirmed.Study.Id);await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
    }
}
