using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

/// <summary>Consolidates legacy outputs without casting JSON text into binary bytes.</summary>
public static class StudyHistoryBinaryMigration
{
    public static async Task ResetAsync(string connectionString,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        if(new[]{"master","model","msdb","tempdb"}.Contains(c.Database,StringComparer.OrdinalIgnoreCase))throw new InvalidDataException("Study storage reset requires an application database.");
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        async Task Execute(string sql){await using var cmd=new SqlCommand(sql,c,tx);await cmd.ExecuteNonQueryAsync(ct);}
        await Execute("""
            DECLARE @Result int;EXEC @Result=sys.sp_getapplock @Resource='FIGMarketLive.StudyHistorySchema',@LockMode='Exclusive',@LockOwner='Transaction';
            IF @Result<0 THROW 51150,'Cannot lock study storage reset.',1;
            IF OBJECT_ID('dbo.BarStudy','U') IS NOT NULL
            BEGIN
                IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE referenced_object_id=OBJECT_ID('dbo.BarStudy')) OR EXISTS(SELECT 1 FROM sys.sql_expression_dependencies WHERE referenced_id=OBJECT_ID('dbo.BarStudy'))
                    THROW 51151,'SQL objects still depend on BarStudy; storage reset was cancelled.',1;
                DROP TABLE dbo.BarStudy;
            END;
            IF OBJECT_ID('dbo.StudyHistory','U') IS NOT NULL TRUNCATE TABLE dbo.StudyHistory;
            IF OBJECT_ID('dbo.StudyRegeneration','U') IS NOT NULL DELETE dbo.StudyRegeneration;
            IF OBJECT_ID('dbo.StudyTailCheckpoint','U') IS NOT NULL DELETE dbo.StudyTailCheckpoint;
            """);
        using var stream=typeof(StudyHistoryBinaryMigration).Assembly.GetManifestResourceStream("FIGMarketLive.Core.Database.CreateStudyHistoryCheckpoints.sql")!;
        using var reader=new StreamReader(stream);await Execute(await reader.ReadToEndAsync(ct));
        await Execute("""
            DECLARE @Default sysname=(SELECT dc.name FROM sys.default_constraints dc JOIN sys.columns col ON col.object_id=dc.parent_object_id AND col.column_id=dc.parent_column_id WHERE dc.parent_object_id=OBJECT_ID('dbo.StudyHistory') AND col.name='Studies');
            IF @Default IS NOT NULL BEGIN DECLARE @DropSql nvarchar(max)=N'ALTER TABLE dbo.StudyHistory DROP CONSTRAINT '+QUOTENAME(@Default); EXEC(@DropSql); END;
            ALTER TABLE dbo.StudyHistory DROP COLUMN Studies;
            ALTER TABLE dbo.StudyHistory ADD Studies varbinary(max) NOT NULL CONSTRAINT DF_StudyHistory_MarketLive_BinaryStudies DEFAULT(0x);
            """);
        await tx.CommitAsync(ct);
    }
    public static async Task<bool> RequiredAsync(string connectionString,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("SELECT CASE WHEN OBJECT_ID('dbo.BarStudy','U') IS NOT NULL OR EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.StudyHistory') AND name='Studies' AND system_type_id<>165) THEN 1 ELSE 0 END",c);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct))==1;
    }
    public static async Task MigrateAsync(string connectionString,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        async Task<object?> Scalar(string sql,SqlTransaction? tx=null){await using var cmd=new SqlCommand(sql,c,tx);return await cmd.ExecuteScalarAsync(ct);}
        bool legacyTable=Convert.ToInt32(await Scalar("SELECT CASE WHEN OBJECT_ID('dbo.BarStudy','U') IS NULL THEN 0 ELSE 1 END"))==1;
        int type=Convert.ToInt32(await Scalar("SELECT system_type_id FROM sys.columns WHERE object_id=OBJECT_ID('dbo.StudyHistory') AND name='Studies'"));
        if(type==165&&!legacyTable)return;
        if(type is not (165 or 167 or 231))throw new InvalidDataException("StudyHistory.Studies must be binary or JSON text before migration.");
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        async Task Execute(string sql){await using var cmd=new SqlCommand(sql,c,tx);await cmd.ExecuteNonQueryAsync(ct);}
        await Execute("DECLARE @Result int; EXEC @Result=sys.sp_getapplock @Resource='FIGMarketLive.StudyHistorySchema',@LockMode='Exclusive',@LockOwner='Transaction'; IF @Result<0 THROW 51150,'Cannot lock study storage migration.',1; SELECT TOP(1) Id FROM dbo.StudyHistory WITH(TABLOCKX,HOLDLOCK);");
        if(legacyTable)
        {
            await Execute("""
                IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE referenced_object_id=OBJECT_ID('dbo.BarStudy')) OR EXISTS(SELECT 1 FROM sys.sql_expression_dependencies WHERE referenced_id=OBJECT_ID('dbo.BarStudy'))
                    THROW 51151,'SQL objects still depend on BarStudy. Update those dependencies before consolidation; no data was removed.',1;
                IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.BarStudy') AND name='Studies' AND system_type_id=165 AND max_length=-1)
                    THROW 51152,'Legacy BarStudy.Studies must be FIGS binary before consolidation.',1;
                SELECT TOP(1) Id FROM dbo.BarStudy WITH(TABLOCKX,HOLDLOCK);
                IF EXISTS(SELECT 1 FROM dbo.BarStudy WHERE DATALENGTH(Studies)<10 OR SUBSTRING(Studies,1,4)<>0x46494753)
                    THROW 51153,'Legacy BarStudy contains invalid binary outputs. Migration was cancelled.',1;
                """);
        }
        string output=type==165?"Studies":"MarketLiveBinaryStudies";
        if(type!=165)
        {
            await Execute("ALTER TABLE dbo.StudyHistory ADD MarketLiveBinaryStudies varbinary(max) NULL;");
            int last=0;
            while(true)
            {
                var rows=new List<(int Id,byte[] Bytes)>();
                await using(var read=new SqlCommand("SELECT TOP(1000) h.Id,h.Studies FROM dbo.StudyHistory h WHERE h.Id>@After AND h.Studies IS NOT NULL AND LTRIM(RTRIM(h.Studies)) NOT IN('','{}') "+
                    (legacyTable?"AND NOT EXISTS(SELECT 1 FROM dbo.BarStudy b WHERE b.StudyColId=h.StudyColId AND b.RawTime=h.RawTime) ":"")+"ORDER BY h.Id",c,tx))
                {
                    read.Parameters.AddWithValue("@After",last);await using var r=await read.ExecuteReaderAsync(ct);
                    while(await r.ReadAsync(ct)){last=r.GetInt32(0);rows.Add((last,EncodeLegacy(r.GetString(1))));}
                }
                if(rows.Count==0)break;
                await Execute("CREATE TABLE #ConvertedStudies(Id int PRIMARY KEY,Studies varbinary(max) NOT NULL)");
                using var data=new DataTable();data.Columns.Add("Id",typeof(int));data.Columns.Add("Studies",typeof(byte[]));foreach(var row in rows)data.Rows.Add(row.Id,row.Bytes);
                using(var bulk=new SqlBulkCopy(c,SqlBulkCopyOptions.Default,tx){DestinationTableName="#ConvertedStudies",BulkCopyTimeout=new SqlConnectionStringBuilder(connectionString).CommandTimeout})
                {bulk.ColumnMappings.Add("Id","Id");bulk.ColumnMappings.Add("Studies","Studies");await bulk.WriteToServerAsync(data,ct);}
                await Execute("UPDATE h SET MarketLiveBinaryStudies=b.Studies FROM dbo.StudyHistory h JOIN #ConvertedStudies b ON h.Id=b.Id; DROP TABLE #ConvertedStudies;");
            }
        }
        if(legacyTable)
        {
            await Execute($"""
                UPDATE h SET [Open]=b.[Open],High=b.High,Low=b.Low,[Close]=b.[Close],Volume=b.Volume,{output}=b.Studies
                    FROM dbo.StudyHistory h JOIN dbo.BarStudy b ON h.StudyColId=b.StudyColId AND h.RawTime=b.RawTime;
                INSERT dbo.StudyHistory(StudyColId,RawTime,[Open],High,Low,[Close],Volume,{output})
                    SELECT b.StudyColId,b.RawTime,b.[Open],b.High,b.Low,b.[Close],b.Volume,b.Studies FROM dbo.BarStudy b
                    WHERE NOT EXISTS(SELECT 1 FROM dbo.StudyHistory h WHERE h.StudyColId=b.StudyColId AND h.RawTime=b.RawTime);
                """);
        }
        if(type!=165)
        {
            await Execute("""
                UPDATE dbo.StudyHistory SET MarketLiveBinaryStudies=0x WHERE MarketLiveBinaryStudies IS NULL;
                DECLARE @Default sysname=(SELECT dc.name FROM sys.default_constraints dc JOIN sys.columns col ON col.object_id=dc.parent_object_id AND col.column_id=dc.parent_column_id WHERE dc.parent_object_id=OBJECT_ID('dbo.StudyHistory') AND col.name='Studies');
                IF @Default IS NOT NULL BEGIN DECLARE @DropSql nvarchar(max)=N'ALTER TABLE dbo.StudyHistory DROP CONSTRAINT '+QUOTENAME(@Default); EXEC(@DropSql); END;
                ALTER TABLE dbo.StudyHistory DROP COLUMN Studies;
                EXEC sys.sp_rename 'dbo.StudyHistory.MarketLiveBinaryStudies','Studies','COLUMN';
                """);
            await Execute("ALTER TABLE dbo.StudyHistory ALTER COLUMN Studies varbinary(max) NOT NULL; ALTER TABLE dbo.StudyHistory ADD CONSTRAINT DF_StudyHistory_MarketLive_BinaryStudies DEFAULT(0x) FOR Studies;");
        }
        if(legacyTable)
        {
            await Execute("IF EXISTS(SELECT 1 FROM dbo.BarStudy b LEFT JOIN dbo.StudyHistory h ON h.StudyColId=b.StudyColId AND h.RawTime=b.RawTime WHERE h.Id IS NULL OR h.Studies<>b.Studies OR h.[Open]<>b.[Open] OR h.High<>b.High OR h.Low<>b.Low OR h.[Close]<>b.[Close] OR h.Volume<>b.Volume) THROW 51154,'Study output verification failed; BarStudy was not removed.',1;");
            await Execute("DROP TABLE dbo.BarStudy;");
        }
        // An old placeholder is not a calculated empty FIGS payload. Do not restore
        // a checkpoint past missing output coverage and silently skip those bars.
        await Execute("""
            ;WITH Missing AS(SELECT StudyColId,MIN(RawTime) AS FirstMissing FROM dbo.StudyHistory WHERE DATALENGTH(Studies)<10 OR SUBSTRING(Studies,1,4)<>0x46494753 GROUP BY StudyColId)
            UPDATE h SET CheckpointState=NULL,CheckpointSavedAt=NULL,MarketLivePrefixCount=NULL,MarketLivePrefixHash=NULL
                FROM dbo.StudyHistory h JOIN Missing m ON m.StudyColId=h.StudyColId WHERE h.RawTime>=m.FirstMissing AND h.CheckpointState IS NOT NULL;
            """);
        await tx.CommitAsync(ct);
    }
    private static byte[] EncodeLegacy(string json)
    {
        using var doc=JsonDocument.Parse(json);if(doc.RootElement.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Legacy studies must be JSON objects. Migration was cancelled.");
        var values=new Dictionary<string,object?>();
        foreach(var p in doc.RootElement.EnumerateObject())
        {
            object? value=p.Value.ValueKind switch
            {
                JsonValueKind.Null=>null,JsonValueKind.String=>p.Value.GetString(),JsonValueKind.True=>true,JsonValueKind.False=>false,
                JsonValueKind.Number=>Number(p.Value),
                _=>throw new InvalidDataException($"Legacy study '{p.Name}' contains an unsupported nested JSON value; migration was cancelled.")
            };
            if(!values.TryAdd(p.Name,value))throw new InvalidDataException("Legacy studies contain duplicate keys; migration was cancelled.");
        }
        return StudyBinaryCodec.Encode(values);
    }
    private static object Number(JsonElement value)
    {if(value.TryGetInt64(out long n))return n;if(value.TryGetDecimal(out decimal d))return d;return value.GetDouble();}
}
