using System.Data;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed record ConfiguredDataSet(int DataSetId,int TickerId,string Symbol,string LocalSymbol,string SecurityType,string IntervalId,string IntervalName,int StudyCollections,int Monitors=0);
public sealed record DataSetDeletionPreview(ConfiguredDataSet DataSet,IReadOnlyList<int> StudyColIds,long PriceRows,long StudyRows);

public sealed class DataSetDeletionStore(string connectionString)
{
    public async Task<List<ConfiguredDataSet>> ListAsync(CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("""
            SELECT d.Id,d.TickerId,t.Symbol,t.LocalSymbol,t.SecurityType,d.IntervalId,i.Name,
                (SELECT COUNT(*) FROM dbo.StudyCol s WHERE s.TickerId=d.TickerId AND s.IntervalId=d.IntervalId)
            FROM dbo.DataSet d JOIN dbo.Ticker t ON t.Id=d.TickerId JOIN dbo.Interval i ON i.Id=d.IntervalId ORDER BY t.Symbol,i.Seq,d.Id
            """,c);
        await using var r=await cmd.ExecuteReaderAsync(ct);var rows=new List<ConfiguredDataSet>();
        while(await r.ReadAsync(ct))rows.Add(new(r.GetInt32(0),r.GetInt32(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetString(6),r.GetInt32(7)));
        await r.DisposeAsync();
        var catalog=await StudyConfigurationStore.ReadAsync(c,null,ct);
        return rows.Select(row=>
        {var ids=StudyIntervalCompatibility.DependentStudyIds(row.DataSetId,catalog);return row with{StudyCollections=ids.Length,Monitors=catalog.Studies.Count(s=>s.Enabled&&ids.Contains(s.Id))};}).ToList();
    }

    public async Task<DataSetDeletionPreview> PreviewAsync(int dataSetId,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        return await ReadAsync(c,null,dataSetId,ct);
    }

    private static async Task<DataSetDeletionPreview> ReadAsync(SqlConnection c,SqlTransaction? tx,int id,CancellationToken ct)
    {
        if(id<1)throw new InvalidDataException("Select a valid dataset.");
        string locks=tx is null?"":" WITH(UPDLOCK,HOLDLOCK)";
        await using var cmd=new SqlCommand($"""
            SELECT d.Id,d.TickerId,t.Symbol,t.LocalSymbol,t.SecurityType,d.IntervalId,i.Name
            FROM dbo.DataSet d{locks} JOIN dbo.Ticker t ON t.Id=d.TickerId JOIN dbo.Interval i ON i.Id=d.IntervalId WHERE d.Id=@Id;
            SELECT s.Id FROM dbo.StudyCol s{locks} JOIN dbo.DataSet d ON d.TickerId=s.TickerId AND d.IntervalId=s.IntervalId WHERE d.Id=@Id ORDER BY s.Id;
            SELECT COUNT_BIG(*) FROM dbo.PriceData WHERE DataSetId=@Id;
            SELECT COUNT_BIG(*) FROM dbo.BarStudy b JOIN dbo.StudyCol s ON s.Id=b.StudyColId JOIN dbo.DataSet d ON d.TickerId=s.TickerId AND d.IntervalId=s.IntervalId WHERE d.Id=@Id;
            """,c,tx);
        cmd.Parameters.AddWithValue("@Id",id);
        await using var r=await cmd.ExecuteReaderAsync(ct);
        if(!await r.ReadAsync(ct))throw new InvalidDataException("The selected dataset no longer exists. Refresh the dataset list.");
        int dataSetId=r.GetInt32(0),tickerId=r.GetInt32(1);
        string symbol=r.GetString(2),local=r.GetString(3),type=r.GetString(4),interval=r.GetString(5),name=r.GetString(6);
        await r.NextResultAsync(ct);var ids=new List<int>();while(await r.ReadAsync(ct))ids.Add(r.GetInt32(0));
        await r.NextResultAsync(ct);await r.ReadAsync(ct);long prices=r.GetInt64(0);
        await r.NextResultAsync(ct);await r.ReadAsync(ct);long studies=r.GetInt64(0);
        await r.DisposeAsync();
        var catalog=await StudyConfigurationStore.ReadAsync(c,tx,ct);
        var dependentIds=StudyIntervalCompatibility.DependentStudyIds(dataSetId,catalog);
        await using var count=new SqlCommand("SELECT COUNT_BIG(*) FROM dbo.BarStudy WHERE StudyColId IN(SELECT CONVERT(int,[value]) FROM OPENJSON(@Ids))",c,tx);
        count.Parameters.AddWithValue("@Ids",System.Text.Json.JsonSerializer.Serialize(dependentIds));
        studies=Convert.ToInt64(await count.ExecuteScalarAsync(ct));
        return new(new(dataSetId,tickerId,symbol,local,type,interval,name,dependentIds.Length,catalog.Studies.Count(s=>s.Enabled&&dependentIds.Contains(s.Id))),dependentIds,prices,studies);
    }

    public static void ValidateUnchanged(DataSetDeletionPreview confirmed,DataSetDeletionPreview current)
    {
        if(confirmed.DataSet!=current.DataSet || !confirmed.StudyColIds.SequenceEqual(current.StudyColIds))
            throw new InvalidDataException("Dataset or study dependencies changed after confirmation. Refresh and confirm deletion again.");
    }

    public static void ValidateSelectedIdentity(ConfiguredDataSet selected,ConfiguredDataSet current)
    {
        // Before confirmation, local display metadata may be older or use the provider's root symbol.
        // Identity must still match; the confirmation presents the authoritative SQL metadata.
        if(selected.DataSetId!=current.DataSetId || selected.TickerId!=current.TickerId || selected.IntervalId!=current.IntervalId)
            throw new InvalidDataException("The dataset identity changed. Select the intended dataset again from the refreshed list.");
    }

    /// <summary>Call only after confirming this exact dataset and StudyCol dependency list with the user.</summary>
    public async Task DeleteConfirmedAsync(DataSetDeletionPreview confirmed,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var current=await ReadAsync(c,tx,confirmed.DataSet.DataSetId,ct);
        ValidateUnchanged(confirmed,current);
        await using var cmd=new SqlCommand("""
            -- Do not silently cascade through unrelated application tables in a shared database.
            IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE delete_referential_action<>0 AND
                ((referenced_object_id=OBJECT_ID('dbo.StudyCol') AND parent_object_id<>OBJECT_ID('dbo.BarStudy')) OR
                 (referenced_object_id=OBJECT_ID('dbo.DataSet') AND parent_object_id<>OBJECT_ID('dbo.PriceData')) OR
                 referenced_object_id IN(OBJECT_ID('dbo.PriceData'),OBJECT_ID('dbo.BarStudy'))))
                THROW 51040, 'Unexpected cascading relationships exist. Dataset deletion was cancelled; no rows were removed.', 1;
            DECLARE @Studies TABLE(Id int PRIMARY KEY);
            INSERT @Studies SELECT CONVERT(int,[value]) FROM OPENJSON(@StudyIds);
            DELETE b FROM dbo.BarStudy b JOIN @Studies s ON s.Id=b.StudyColId;
            DELETE s FROM dbo.StudyCol s JOIN @Studies target ON target.Id=s.Id;
            DELETE FROM dbo.PriceData WHERE DataSetId=@Id;
            DELETE FROM dbo.DataSet WHERE Id=@Id;
            """,c,tx);
        cmd.Parameters.AddWithValue("@Id",current.DataSet.DataSetId);
        cmd.Parameters.AddWithValue("@StudyIds",System.Text.Json.JsonSerializer.Serialize(current.StudyColIds));
        await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
    }

}
