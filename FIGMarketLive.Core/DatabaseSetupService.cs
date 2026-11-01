using System.Data;
using System.Globalization;
using FIGCommon.Models;
using FIGCommon.Models.FIGProviderAPI;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed record DatabaseCheck(bool Ready,string Message);
public sealed record DataSetChoice(int Id,int TickerId,string IntervalId)
{
    public override string ToString()=>$"DataSet {Id} — interval {IntervalId}";
}

public sealed class DatabaseSetupService(string connectionString)
{
    public Task<DataSetChoice> EnsureDataSetAsync(MarketData.MarketInstrument instrument,string localSymbol,string intervalId,CancellationToken ct,string? providerType=null)
        =>EnsureDataSetAsync(new ContractInfo {Symbol=instrument.Symbol,LocalSymbol=instrument.LocalSymbol,Name=instrument.Name,SecType=instrument.SecurityType,
            Exchange=instrument.Exchange,Currency=instrument.Currency,LastTradeDateOrContractMonth=instrument.Expiry,MinTick=instrument.MinimumTick,
            Multiplier=instrument.ContractMultiplier.ToString(CultureInfo.InvariantCulture)},localSymbol,intervalId,ct,
            providerType is null?null:TickerProviderStore.FromInstrument(1,providerType,instrument));
    // Column checks cover the schema consumed by setup, ingestion and binary study publication.
    public static readonly IReadOnlyDictionary<string,string[]> RequiredColumns=new Dictionary<string,string[]>
    {
        ["Ticker"]=["Id","Symbol","LocalSymbol","Name","SecurityType","Currency","ExpiryDate","MinTick","ContractSize","Exchange","PeriodMultiplier"],
        ["TickerProvider"]=["TickerId","ProviderType","ProviderInstrumentId","ProviderSymbol","HistoryStartRawTime"],
        ["Interval"]=["Id","Name","Seq","IntervalLen"],
        ["DataSet"]=["Id","TickerId","IntervalId","Monitor"],
        ["ExchangePriceSchedule"]=["Exchange","Enabled","TimeZoneId","WeeklyBreakCloseDay","WeeklyBreakCloseTime","WeeklyBreakOpenDay","WeeklyBreakOpenTime","DailyBreakCloseTime","DailyBreakOpenTime"],
        ["PriceData"]=["Id","DataSetId","RawTime","Open","High","Low","Close","Volume"],
        ["Event"]=["Id","EventType","EventId","Message","TimeStamp"],
        ["StudyCol"]=["Id","TickerId","IntervalId","ColType","OrderSeq","ColParams","Enabled"],
        ["ColType"]=["Type","DLLName","Description"],
        ["StudyHistory"]=["Id","StudyColId","RawTime","Open","High","Low","Close","Volume","Studies","StudiesOverride","StudiesBaseDefinitionHash","CheckpointState","CheckpointSavedAt","MarketLiveDefinitionHash","MarketLivePrefixHash","MarketLivePrefixCount"]
    };

    public async Task<DatabaseCheck> CheckAsync(CancellationToken ct)
    {
        var builder=new SqlConnectionStringBuilder(connectionString);
        if(string.IsNullOrWhiteSpace(builder.InitialCatalog)) return new(false,"Specify the FIGMarketLive database using Database / Initial Catalog.");
        await using var connection=new SqlConnection(connectionString);
        try { await connection.OpenAsync(ct); }
        catch(SqlException ex) when(ex.Number is 4060 or 911 or 18456)
        {
            var master=new SqlConnectionStringBuilder(connectionString) { InitialCatalog="master" };
            try
            {
                await using var server=new SqlConnection(master.ConnectionString); await server.OpenAsync(ct);
                await using var cmd=new SqlCommand("SELECT COUNT(*) FROM sys.databases WHERE name=@Name",server);
                cmd.Parameters.AddWithValue("@Name",builder.InitialCatalog);
                bool visible=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct))>0;
                return new(false,visible?"Database exists but is not accessible with these credentials.":"Database does not exist or is not visible to this login.");
            }
            catch(SqlException) { return new(false,"Cannot access the database or verify its existence. Check server, database name and login permissions."); }
        }
        var columns=new Dictionary<string,(string Type,int Length,byte Scale)>();
        await using(var cmd=new SqlCommand("SELECT t.name,c.name,TYPE_NAME(c.system_type_id),c.max_length,c.scale FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.columns c ON c.object_id=t.object_id WHERE s.name='dbo'",connection))
        await using(var reader=await cmd.ExecuteReaderAsync(ct))
            while(await reader.ReadAsync(ct)) columns[reader.GetString(0)+"."+reader.GetString(1)]=(reader.GetString(2),reader.GetInt16(3),reader.GetByte(4));
        var problems=new List<string>();
        if(columns.ContainsKey("PriceData.PriceDate"))problems.Add("PriceData.PriceDate is obsolete. Apply --migrate-price-schema to use RawTime-only prices.");
        if(columns.TryGetValue("DataSet.Monitor",out var monitor)&&monitor.Type!="bit")problems.Add("DataSet.Monitor must be bit.");
        foreach(var (table,names) in RequiredColumns)
            foreach(var name in names)
                if(!columns.ContainsKey(table+"."+name)) problems.Add($"Missing dbo.{table}.{name}");
        foreach(var table in RequiredColumns.Keys)
        {
            await using var permission=new SqlCommand("SELECT COALESCE(HAS_PERMS_BY_NAME(@Table,'OBJECT','SELECT'),0)",connection);
            permission.Parameters.AddWithValue("@Table","dbo."+table);
            if(Convert.ToInt32(await permission.ExecuteScalarAsync(ct))!=1)problems.Add($"SELECT access is missing for dbo.{table}.");
        }
        if(columns.ContainsKey("BarStudy.Studies"))problems.Add("Legacy BarStudy still exists. Run database setup to consolidate study storage.");
        if(columns.TryGetValue("StudyHistory.Studies",out var studies) && (studies.Type!="varbinary" || studies.Length!=-1))
            problems.Add("StudyHistory.Studies must be varbinary(max); run database setup to migrate JSON storage.");
        if(columns.TryGetValue("StudyHistory.StudiesOverride",out var overlay) && (overlay.Type!="varbinary" || overlay.Length!=-1))
            problems.Add("StudyHistory.StudiesOverride must be varbinary(max).");
        foreach(var table in new[]{"PriceData","StudyHistory"})
        {
            if(columns.TryGetValue(table+".RawTime",out var time) && time.Type is not ("int" or "bigint")) problems.Add($"{table}.RawTime must be int or bigint.");
            if(columns.TryGetValue(table+".Volume",out var volume) && volume.Type!="int") problems.Add($"{table}.Volume must currently be int for the managed price contract.");
            foreach(var price in new[]{"Open","High","Low","Close"})
                if(columns.TryGetValue(table+"."+price,out var type) && (type.Type is not ("decimal" or "numeric") || type.Scale<4)) problems.Add($"{table}.{price} must be decimal/numeric with scale >= 4.");
        }
        foreach(var (table,first,second) in new[]{("DataSet","TickerId","IntervalId"),("PriceData","DataSetId","RawTime"),("StudyHistory","StudyColId","RawTime")})
        {
            await using var cmd=new SqlCommand("""
                SELECT COUNT(*) FROM sys.indexes i WHERE i.object_id=OBJECT_ID(@Table) AND i.is_unique=1 AND i.is_disabled=0 AND i.has_filter=0
                AND (SELECT COUNT(*) FROM sys.index_columns ic WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0)=2
                AND EXISTS(SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
                    WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1 AND c.name=@First)
                AND EXISTS(SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
                    WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=2 AND c.name=@Second)
                """,connection);
            cmd.Parameters.AddWithValue("@Table","dbo."+table); cmd.Parameters.AddWithValue("@First",first);cmd.Parameters.AddWithValue("@Second",second);
            if(Convert.ToInt32(await cmd.ExecuteScalarAsync(ct))==0) problems.Add($"Missing unique index {table}({first},{second}).");
        }
        return new(problems.Count==0,$"Database '{builder.InitialCatalog}' exists and is accessible.{Environment.NewLine}"+
            (problems.Count==0?"Required columns, price types, binary study storage and composite unique indexes passed.":string.Join(Environment.NewLine,problems)));
    }

    public async Task<List<IntervalRS>> IntervalsAsync(CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("SELECT Id,Name,Seq,IntervalLen FROM dbo.Interval WHERE IntervalLen>0 ORDER BY Seq,Id",c);
        await using var r=await cmd.ExecuteReaderAsync(ct);var result=new List<IntervalRS>();
        while(await r.ReadAsync(ct)) result.Add(new(){Id=r.GetString(0),Name=r.GetString(1),Seq=r.GetInt32(2),IntervalLen=r.GetInt32(3)});
        return result;
    }

    public async Task<List<DataSetChoice>> DataSetsAsync(string localSymbol,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("SELECT d.Id,d.TickerId,d.IntervalId FROM dbo.DataSet d JOIN dbo.Ticker t ON t.Id=d.TickerId WHERE t.Symbol=@Symbol ORDER BY d.Id",c);
        cmd.Parameters.AddWithValue("@Symbol",localSymbol);await using var r=await cmd.ExecuteReaderAsync(ct);var result=new List<DataSetChoice>();
        while(await r.ReadAsync(ct)) result.Add(new(r.GetInt32(0),r.GetInt32(1),r.GetString(2)));return result;
    }

    public async Task<DataSetChoice> EnsureDataSetAsync(ContractInfo contract,string localSymbol,string intervalId,CancellationToken ct,TickerProviderMapping? mapping=null)
    {
        ValidateContract(contract,localSymbol);
        if(mapping is not null)await new TickerProviderStore(connectionString).EnsureSchemaAsync(ct);
        if(!int.TryParse(contract.LastTradeDateOrContractMonth,out var expiry)) expiry=0;
        int multiplier=string.IsNullOrWhiteSpace(contract.Multiplier)?1:int.Parse(contract.Multiplier,CultureInfo.InvariantCulture);
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        await using var cmd=new SqlCommand("""
            DECLARE @TickerId int;
            SELECT @TickerId=Id FROM dbo.Ticker WITH(UPDLOCK,HOLDLOCK) WHERE Symbol=@Symbol;
            IF @TickerId IS NOT NULL AND EXISTS(SELECT 1 FROM dbo.Ticker WHERE Id=@TickerId AND
                (LocalSymbol<>@LocalSymbol OR SecurityType<>@Type OR Currency<>@Currency OR ExpiryDate<>@Expiry OR Exchange<>@Exchange
                OR MinTick<>@Tick OR ContractSize<>@Size))
                THROW 51030, 'Local symbol already identifies a different contract or different metadata. Choose another local database symbol.', 1;
            IF @TickerId IS NULL
            BEGIN
                INSERT dbo.Ticker(Symbol,LocalSymbol,Name,SecurityType,Currency,ExpiryDate,MinTick,ContractSize,Exchange,PeriodMultiplier)
                VALUES(@Symbol,@LocalSymbol,@Name,@Type,@Currency,@Expiry,@Tick,@Size,@Exchange,92);
                SET @TickerId=CONVERT(int,SCOPE_IDENTITY());
            END;
            IF NOT EXISTS(SELECT 1 FROM dbo.Interval WHERE Id=@Interval) THROW 51031, 'Selected interval no longer exists.', 1;
            IF NOT EXISTS(SELECT 1 FROM dbo.DataSet WITH(UPDLOCK,HOLDLOCK) WHERE TickerId=@TickerId AND IntervalId=@Interval)
                INSERT dbo.DataSet(TickerId,IntervalId) VALUES(@TickerId,@Interval);
            SELECT Id,TickerId,IntervalId FROM dbo.DataSet WHERE TickerId=@TickerId AND IntervalId=@Interval;
            """,c,tx);
        foreach(var (name,value) in new (string,object)[]{("@Symbol",localSymbol.Trim()),("@LocalSymbol",contract.LocalSymbol),("@Name",contract.Name),
            ("@Type",contract.SecType),("@Currency",contract.Currency),("@Expiry",expiry),("@Exchange",contract.Exchange),
            ("@Tick",contract.MinTick),("@Size",multiplier),("@Interval",intervalId)}) cmd.Parameters.AddWithValue(name,value);
        DataSetChoice result;
        await using(var r=await cmd.ExecuteReaderAsync(ct))
        { if(!await r.ReadAsync(ct)) throw new InvalidDataException("Dataset was not created.");result=new(r.GetInt32(0),r.GetInt32(1),r.GetString(2)); }
        if(mapping is not null)await TickerProviderStore.UpsertAsync(c,tx,mapping with{TickerId=result.TickerId},true,ct);
        await tx.CommitAsync(ct);return result;
    }

    public static void ValidateContract(ContractInfo c,string alias)
    {
        if(string.IsNullOrWhiteSpace(alias) || alias.Trim().Length>50 || c.SecType is not ("FUT" or "STK") ||
            string.IsNullOrWhiteSpace(c.LocalSymbol) || c.LocalSymbol.Length>50 || string.IsNullOrWhiteSpace(c.Currency) ||
            string.IsNullOrWhiteSpace(c.Exchange) || c.MinTick<=0 || c.MinTick!=decimal.Round(c.MinTick,6))
            throw new InvalidDataException("Resolved contract requires valid local symbol, exchange, currency and minimum tick.");
        if(c.SecType=="FUT" && (!int.TryParse(c.LastTradeDateOrContractMonth,out _) || c.LastTradeDateOrContractMonth.Length is not (6 or 8)))
            throw new InvalidDataException("Futures contract requires an explicit expiry.");
        if(!string.IsNullOrWhiteSpace(c.Multiplier) && (!int.TryParse(c.Multiplier,NumberStyles.Integer,CultureInfo.InvariantCulture,out var multiplier) || multiplier<1))
            throw new InvalidDataException("This database requires a positive integer contract multiplier.");
    }
}
