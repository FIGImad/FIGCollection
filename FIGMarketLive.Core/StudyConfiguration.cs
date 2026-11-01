using System.Data;
using System.Text.Json;
using FIGCommon.Models;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed record StudyTicker(int Id,string Symbol)
{public override string ToString()=>$"{Symbol} (ID {Id})";}
public sealed record StudyDataSet(int Id,int TickerId,string IntervalId,bool Monitor=false);
public sealed record CollectionType(string Type,string DLLName,string Description);
public sealed record StudyConfigurationRow(int Id,int TickerId,string IntervalId,string ColType,string ColParams,bool Enabled,string Symbol="",string IntervalName="",int OrderSeq=1);
public sealed record StudyConfigurationCatalog(List<StudyTicker> Tickers,List<IntervalRS> Intervals,List<StudyDataSet> DataSets,List<CollectionType> ColTypes,List<StudyConfigurationRow> Studies);
public sealed record CompatibleStudyInterval(IntervalRS Interval,StudyDataSet Source,bool Aggregate)
{public override string ToString()=>Interval.Name+(Aggregate?$" — aggregate from DataSet {Source.Id}":$" — DataSet {Source.Id}");}

public static class StudyIntervalCompatibility
{
    public static bool CanAggregate(IntervalRS source,IntervalRS target)
    {
        if(source.Id==target.Id)return true;
        if(source.IntervalLen<=0||target.IntervalLen<=0)return false;
        bool calendarTarget=target.Id.EndsWith('D')||target.Id.EndsWith('W')||target.Id.EndsWith('M')||target.Id.EndsWith('Y');
        bool calendarSource=source.Id.EndsWith('D')||source.Id.EndsWith('W')||source.Id.EndsWith('M')||source.Id.EndsWith('Y');
        // Calendar periods cannot be treated as fixed 30-day months or 365-day years.
        if(calendarTarget)
        {
            if(!calendarSource)return source.IntervalLen<=86400 && 86400%source.IntervalLen==0;
            if(source.Id=="D")return true;
            if(source.Id[^1]!=target.Id[^1])return false;
            int Count(string id)=>id.Length==1?1:int.TryParse(id[..^1],out int n)?n:0;
            int s=Count(source.Id),t=Count(target.Id);return s>0&&t>=s&&t%s==0;
        }
        return !calendarSource && target.IntervalLen>=source.IntervalLen && target.IntervalLen%source.IntervalLen==0;
    }

    public static List<CompatibleStudyInterval> Choices(int tickerId,IReadOnlyList<IntervalRS> intervals,IReadOnlyList<StudyDataSet> datasets)
    {
        var byId=intervals.ToDictionary(i=>i.Id,StringComparer.Ordinal);
        return intervals.OrderBy(i=>i.Seq).Select(target=>
        {
            var source=datasets.Where(d=>d.TickerId==tickerId&&byId.TryGetValue(d.IntervalId,out var interval)&&CanAggregate(interval,target))
                .OrderByDescending(d=>d.IntervalId==target.Id).ThenByDescending(d=>byId[d.IntervalId].IntervalLen).ThenBy(d=>d.Id).FirstOrDefault();
            return source is null?null:new CompatibleStudyInterval(target,source,source.IntervalId!=target.Id);
        }).OfType<CompatibleStudyInterval>().ToList();
    }

    public static string NormalizeParameters(string json)
    {
        using var doc=JsonDocument.Parse(json,new(){CommentHandling=JsonCommentHandling.Skip});
        if(doc.RootElement.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("ColParams must be a JSON object.");
        void Validate(JsonElement element)
        {
            if(element.ValueKind==JsonValueKind.Object)
            {
                var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach(var property in element.EnumerateObject())
                {if(!names.Add(property.Name))throw new InvalidDataException($"Duplicate JSON parameter: {property.Name}");Validate(property.Value);}
            }
            else if(element.ValueKind==JsonValueKind.Array)foreach(var item in element.EnumerateArray())Validate(item);
        }
        Validate(doc.RootElement);return JsonSerializer.Serialize(doc.RootElement,new JsonSerializerOptions{WriteIndented=true});
    }

    public static int[] DependentStudyIds(int dataSetId,StudyConfigurationCatalog catalog)=>catalog.Studies
        .Where(s=>Choices(s.TickerId,catalog.Intervals,catalog.DataSets).Any(i=>i.Interval.Id==s.IntervalId&&i.Source.Id==dataSetId))
        .Select(s=>s.Id).Order().ToArray();
}

public sealed class StudyConfigurationStore(string connectionString)
{
    public async Task<StudyConfigurationCatalog> ReadAsync(CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);return await ReadAsync(c,null,ct);
    }
    internal static async Task<StudyConfigurationCatalog> ReadAsync(SqlConnection c,SqlTransaction? tx,CancellationToken ct)
    {
        await using var cmd=new SqlCommand("""
            SELECT Id,Symbol FROM dbo.Ticker t WHERE EXISTS(SELECT 1 FROM dbo.DataSet d WHERE d.TickerId=t.Id) ORDER BY Symbol,Id;
            SELECT Id,Name,Seq,IntervalLen FROM dbo.Interval WHERE IntervalLen>0 ORDER BY Seq,Id;
            SELECT Id,TickerId,IntervalId,Monitor FROM dbo.DataSet ORDER BY Id;
            SELECT Type,DLLName,Description FROM dbo.ColType ORDER BY Type;
            SELECT s.Id,s.TickerId,s.IntervalId,s.ColType,s.ColParams,s.Enabled,t.Symbol,i.Name,s.OrderSeq
            FROM dbo.StudyCol s JOIN dbo.Ticker t ON t.Id=s.TickerId JOIN dbo.Interval i ON i.Id=s.IntervalId ORDER BY s.OrderSeq,s.Id;
            """,c,tx);
        await using var r=await cmd.ExecuteReaderAsync(ct);
        var tickers=new List<StudyTicker>();while(await r.ReadAsync(ct))tickers.Add(new(r.GetInt32(0),r.GetString(1)));
        await r.NextResultAsync(ct);var intervals=new List<IntervalRS>();while(await r.ReadAsync(ct))intervals.Add(new(){Id=r.GetString(0),Name=r.GetString(1),Seq=r.GetInt32(2),IntervalLen=r.GetInt32(3)});
        await r.NextResultAsync(ct);var datasets=new List<StudyDataSet>();while(await r.ReadAsync(ct))datasets.Add(new(r.GetInt32(0),r.GetInt32(1),r.GetString(2),r.GetBoolean(3)));
        await r.NextResultAsync(ct);var types=new List<CollectionType>();while(await r.ReadAsync(ct))types.Add(new(r.GetString(0),r.GetString(1),r.GetString(2)));
        await r.NextResultAsync(ct);var studies=new List<StudyConfigurationRow>();while(await r.ReadAsync(ct))studies.Add(new(r.GetInt32(0),r.GetInt32(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetBoolean(5),r.GetString(6),r.GetString(7),r.GetInt32(8)));
        return new(tickers,intervals,datasets,types,studies);
    }

    public async Task<int> SaveStudyAsync(StudyConfigurationRow row,CancellationToken ct)
    {
        string parameters=StudyIntervalCompatibility.NormalizeParameters(row.ColParams);
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        await using(var claim=new SqlCommand("SELECT Id FROM dbo.StudyCol WITH(UPDLOCK,HOLDLOCK) ORDER BY Id",c,tx))await claim.ExecuteNonQueryAsync(ct);
        var catalog=await ReadAsync(c,tx,ct);
        if(!catalog.Tickers.Any(t=>t.Id==row.TickerId)||!StudyIntervalCompatibility.Choices(row.TickerId,catalog.Intervals,catalog.DataSets).Any(i=>i.Interval.Id==row.IntervalId))
            throw new InvalidDataException("No compatible dataset exists for this ticker and interval. Reload the editor.");
        if(!catalog.ColTypes.Any(t=>t.Type==row.ColType))throw new InvalidDataException("Selected ColType no longer exists. Reload the editor.");
        await using var cmd=new SqlCommand(row.Id==0?"""
            INSERT dbo.StudyCol(TickerId,IntervalId,ColType,OrderSeq,ColParams,Enabled) VALUES(@Ticker,@Interval,@Type,@Order,@Params,@Enabled);
            SELECT CONVERT(int,SCOPE_IDENTITY());
            """:"""
            UPDATE dbo.StudyCol SET TickerId=@Ticker,IntervalId=@Interval,ColType=@Type,ColParams=@Params,Enabled=@Enabled WHERE Id=@Id;
            IF @@ROWCOUNT<>1 THROW 51050, 'StudyCol no longer exists. Reload the editor.', 1;
            SELECT @Id;
            """,c,tx);
        cmd.Parameters.AddWithValue("@Order",row.Id==0?checked(catalog.Studies.Select(s=>s.OrderSeq).DefaultIfEmpty(0).Max()+1):row.OrderSeq);
        cmd.Parameters.AddWithValue("@Ticker",row.TickerId);cmd.Parameters.AddWithValue("@Interval",row.IntervalId);cmd.Parameters.AddWithValue("@Type",row.ColType);
        cmd.Parameters.AddWithValue("@Params",parameters);cmd.Parameters.AddWithValue("@Enabled",row.Enabled);cmd.Parameters.AddWithValue("@Id",row.Id);
        int id=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));await tx.CommitAsync(ct);return id;
    }

    public static void ValidateType(CollectionType type)
    {
        if(string.IsNullOrWhiteSpace(type.Type)||type.Type.Length>100||string.IsNullOrWhiteSpace(type.DLLName)||type.DLLName.Length>256||!type.DLLName.EndsWith(".dll",StringComparison.OrdinalIgnoreCase)||type.Description.Length>1000)
            throw new InvalidDataException("ColType requires a name (up to 100 characters), DLL filename/path (up to 256, ending .dll), and description up to 1000 characters.");
    }
    public async Task SaveTypeAsync(string? original,CollectionType type,CancellationToken ct)
    {
        ValidateType(type);
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        await using var cmd=new SqlCommand("""
            IF @Original IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.ColType WITH(UPDLOCK,HOLDLOCK) WHERE Type=@Original)
                THROW 51051, 'ColType no longer exists. Reload.', 1;
            IF @Original IS NOT NULL AND @Original<>@Type AND EXISTS(SELECT 1 FROM dbo.StudyCol WITH(UPDLOCK,HOLDLOCK) WHERE ColType=@Original)
                THROW 51052, 'Cannot rename a ColType used by StudyCol. Keep its name or change those study assignments first.', 1;
            IF @Original=@Type
                UPDATE dbo.ColType SET DLLName=@DLL,Description=@Description WHERE Type=@Original;
            ELSE
            BEGIN
                INSERT dbo.ColType(Type,DLLName,Description) VALUES(@Type,@DLL,@Description);
                IF @Original IS NOT NULL DELETE FROM dbo.ColType WHERE Type=@Original;
            END;
            """,c,tx);
        cmd.Parameters.AddWithValue("@Original",(object?)original??DBNull.Value);cmd.Parameters.AddWithValue("@Type",type.Type);
        cmd.Parameters.AddWithValue("@DLL",type.DLLName);cmd.Parameters.AddWithValue("@Description",type.Description);
        await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task DeleteTypeAsync(string type,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        await using var cmd=new SqlCommand("""
            IF EXISTS(SELECT 1 FROM dbo.StudyCol WITH(UPDLOCK,HOLDLOCK) WHERE ColType=@Type)
                THROW 51053, 'Cannot remove ColType: it is used by StudyCol rows. Change their ColType first.', 1;
            DELETE FROM dbo.ColType WHERE Type=@Type;
            """,c,tx);
        cmd.Parameters.AddWithValue("@Type",type);await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
    }
}
