using System.Data;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed record ConfiguredDataSet(int DataSetId,int TickerId,string Symbol,string LocalSymbol,string SecurityType,string IntervalId,string IntervalName,int StudyCollections,bool Monitor=false,int ExpiryDate=0,string Exchange="")
{
    public string StudyUsage=>StudyCollections>0?"In Use":"NOT Used";
    public string SyncStatus=>Monitor?"Enabled":"Disabled";
    public string Expiry=>ExpiryDate>0?ExpiryDate.ToString(System.Globalization.CultureInfo.InvariantCulture):"";
}
public sealed record DataSetDeletionPreview(ConfiguredDataSet DataSet,IReadOnlyList<int> StudyColIds,long PriceRows,long StudyHistoryRows,bool RemoveTicker=false);

public sealed class DataSetDeletionStore(string connectionString)
{
    public async Task<List<ConfiguredDataSet>> ListAsync(CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("""
            SELECT d.Id,d.TickerId,t.Symbol,t.LocalSymbol,t.SecurityType,d.IntervalId,i.Name,
                d.Monitor,t.ExpiryDate,t.Exchange
            FROM dbo.DataSet d JOIN dbo.Ticker t ON t.Id=d.TickerId JOIN dbo.Interval i ON i.Id=d.IntervalId ORDER BY t.Symbol,i.Seq,d.Id
            """,c);
        await using var r=await cmd.ExecuteReaderAsync(ct);var rows=new List<ConfiguredDataSet>();
        while(await r.ReadAsync(ct))rows.Add(new(r.GetInt32(0),r.GetInt32(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetString(6),0,r.GetBoolean(7),r.GetInt32(8),r.GetString(9)));
        await r.DisposeAsync();
        var catalog=await StudyConfigurationStore.ReadAsync(c,null,ct);
        return rows.Select(row=>
        {var ids=StudyIntervalCompatibility.DependentStudyIds(row.DataSetId,catalog);return row with{StudyCollections=ids.Length};}).ToList();
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
            SELECT d.Id,d.TickerId,t.Symbol,t.LocalSymbol,t.SecurityType,d.IntervalId,i.Name,d.Monitor,t.ExpiryDate,t.Exchange
            FROM dbo.DataSet d{locks} JOIN dbo.Ticker t ON t.Id=d.TickerId JOIN dbo.Interval i ON i.Id=d.IntervalId WHERE d.Id=@Id;
            SELECT COUNT_BIG(*) FROM dbo.PriceData WHERE DataSetId=@Id;
            """,c,tx);
        cmd.Parameters.AddWithValue("@Id",id);
        await using var r=await cmd.ExecuteReaderAsync(ct);
        if(!await r.ReadAsync(ct))throw new InvalidDataException("The selected dataset no longer exists. Refresh the dataset list.");
        int dataSetId=r.GetInt32(0),tickerId=r.GetInt32(1);
        string symbol=r.GetString(2),local=r.GetString(3),type=r.GetString(4),interval=r.GetString(5),name=r.GetString(6);
        bool monitor=r.GetBoolean(7);
        int expiry=r.GetInt32(8);
        string exchange=r.GetString(9);
        await r.NextResultAsync(ct);await r.ReadAsync(ct);long prices=r.GetInt64(0);
        await r.DisposeAsync();
        var catalog=await StudyConfigurationStore.ReadAsync(c,tx,ct);
        bool removeTicker=!catalog.DataSets.Any(d=>d.TickerId==tickerId&&d.Id!=dataSetId);
        var dependentIds=StudyIntervalCompatibility.DependentStudyIds(dataSetId,catalog)
            .Concat(removeTicker?catalog.Studies.Where(s=>s.TickerId==tickerId).Select(s=>s.Id):[]).Distinct().Order().ToArray();
        if(removeTicker)
        {
            await using var allStudies=new SqlCommand($"SELECT Id FROM dbo.StudyCol{locks} WHERE TickerId=@Ticker ORDER BY Id",c,tx);
            allStudies.Parameters.AddWithValue("@Ticker",tickerId);await using var allReader=await allStudies.ExecuteReaderAsync(ct);var allIds=new List<int>();
            while(await allReader.ReadAsync(ct))allIds.Add(allReader.GetInt32(0));dependentIds=allIds.ToArray();
        }
        await using var count=new SqlCommand("SELECT COUNT_BIG(*) FROM dbo.StudyHistory WHERE StudyColId IN(SELECT CONVERT(int,[value]) FROM OPENJSON(@Ids))",c,tx);
        count.Parameters.AddWithValue("@Ids",System.Text.Json.JsonSerializer.Serialize(dependentIds));
        long studies=Convert.ToInt64(await count.ExecuteScalarAsync(ct));
        return new(new(dataSetId,tickerId,symbol,local,type,interval,name,dependentIds.Length,monitor,expiry,exchange),dependentIds,prices,studies,removeTicker);
    }

    public static void ValidateUnchanged(DataSetDeletionPreview confirmed,DataSetDeletionPreview current)
    {
        if(confirmed.DataSet!=current.DataSet || !confirmed.StudyColIds.SequenceEqual(current.StudyColIds)||confirmed.RemoveTicker!=current.RemoveTicker)
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
                ((referenced_object_id=OBJECT_ID('dbo.StudyCol') AND parent_object_id NOT IN(COALESCE(OBJECT_ID('dbo.StudyHistory'),-1),COALESCE(OBJECT_ID('dbo.StudyRegeneration'),-1),COALESCE(OBJECT_ID('dbo.StudyTailCheckpoint'),-1))) OR
                 (referenced_object_id=OBJECT_ID('dbo.DataSet') AND parent_object_id<>OBJECT_ID('dbo.PriceData')) OR
                 (@RemoveTicker=1 AND referenced_object_id=OBJECT_ID('dbo.Ticker') AND parent_object_id NOT IN(OBJECT_ID('dbo.DataSet'),OBJECT_ID('dbo.StudyCol'),OBJECT_ID('dbo.TickerProvider'))) OR
                 referenced_object_id IN(OBJECT_ID('dbo.PriceData'),COALESCE(OBJECT_ID('dbo.StudyHistory'),-1))))
                THROW 51040, 'Unexpected cascading relationships exist. Dataset deletion was cancelled; no rows were removed.', 1;
            DECLARE @Studies TABLE(Id int PRIMARY KEY);
            INSERT @Studies SELECT CONVERT(int,[value]) FROM OPENJSON(@StudyIds);
            IF OBJECT_ID('dbo.StudyHistory','U') IS NOT NULL
                EXEC sys.sp_executesql N'DELETE FROM dbo.StudyHistory WHERE StudyColId IN(SELECT CONVERT(int,[value]) FROM OPENJSON(@Ids))',N'@Ids nvarchar(max)',@Ids=@StudyIds;
            DELETE s FROM dbo.StudyCol s JOIN @Studies target ON target.Id=s.Id;
            DELETE FROM dbo.PriceData WHERE DataSetId=@Id;
            DELETE FROM dbo.DataSet WHERE Id=@Id;
            IF @RemoveTicker=1
            BEGIN
                IF EXISTS(SELECT 1 FROM dbo.DataSet WITH(UPDLOCK,HOLDLOCK) WHERE TickerId=@Ticker)
                    THROW 51041,'Another dataset now uses this ticker. Refresh and confirm deletion again.',1;
                DELETE FROM dbo.TickerProvider WHERE TickerId=@Ticker;
                DELETE FROM dbo.Ticker WHERE Id=@Ticker;
            END;
            """,c,tx);
        cmd.Parameters.AddWithValue("@Id",current.DataSet.DataSetId);
        cmd.Parameters.AddWithValue("@StudyIds",System.Text.Json.JsonSerializer.Serialize(current.StudyColIds));
        cmd.Parameters.AddWithValue("@Ticker",current.DataSet.TickerId);cmd.Parameters.AddWithValue("@RemoveTicker",current.RemoveTicker);
        await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
    }

}
