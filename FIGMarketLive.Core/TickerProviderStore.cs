using System.Data;
using System.Globalization;
using FIGMarketLive.MarketData;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed record TickerProviderMapping(int TickerId,string ProviderType,string ProviderInstrumentId,string ProviderSymbol);

public sealed class TickerProviderStore(string connectionString)
{
    public const string CreateSchemaSql="""
        SET XACT_ABORT ON;
        DECLARE @Lock int;
        EXEC @Lock=sys.sp_getapplock @Resource=N'FIGMarketLive.TickerProvider.Schema',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
        IF @Lock<0 THROW 51060, 'Could not acquire TickerProvider schema lock.', 1;
        IF OBJECT_ID('dbo.Ticker','U') IS NULL THROW 51061, 'Ticker table is missing in the configured database.', 1;
        IF OBJECT_ID('dbo.TickerProvider','U') IS NULL
            CREATE TABLE dbo.TickerProvider(
                TickerId int NOT NULL,
                ProviderType varchar(50) NOT NULL,
                ProviderInstrumentId nvarchar(128) NOT NULL,
                ProviderSymbol nvarchar(100) NOT NULL,
                CONSTRAINT PK_TickerProvider PRIMARY KEY(TickerId,ProviderType),
                CONSTRAINT FK_TickerProvider_Ticker FOREIGN KEY(TickerId) REFERENCES dbo.Ticker(Id));
        IF COL_LENGTH('dbo.TickerProvider','HistoryStartRawTime') IS NULL
            ALTER TABLE dbo.TickerProvider ADD HistoryStartRawTime bigint NULL;
        """;

    public static TickerProviderMapping FromInstrument(int tickerId,string provider,MarketInstrument instrument)
    {
        var mapping=new TickerProviderMapping(tickerId,provider.Trim().ToUpperInvariant(),instrument.ProviderInstrumentId.Trim(),instrument.Symbol.Trim());
        if(mapping.TickerId<1||mapping.ProviderType.Length is <1 or >50||mapping.ProviderInstrumentId.Length>128||mapping.ProviderSymbol.Length is <1 or >100)
            throw new InvalidDataException("Provider mapping requires a valid ticker, provider type and provider symbol within schema limits.");
        return mapping;
    }

    public static List<TickerProviderMapping> BuildLegacyMappings(IReadOnlyList<MarketDataSelection> selections)
    {
        var result=new Dictionary<(int,string),TickerProviderMapping>();
        foreach(var selection in selections)
        {
            var instrument=selection.Instrument??new MarketInstrument
            {Symbol=selection.Contract.Symbol,ProviderInstrumentId=selection.Contract.ConId>0?selection.Contract.ConId.ToString(CultureInfo.InvariantCulture):""};
            // Old Contract.ConId identifies IBKR. Otherwise an explicit provider identity is required.
            string provider=!string.IsNullOrWhiteSpace(selection.ProviderId)?selection.ProviderId:selection.Contract.ConId>0?"IBKR":"";
            var mapping=FromInstrument(selection.TickerId,provider,instrument);
            var key=(mapping.TickerId,mapping.ProviderType);
            if(result.TryGetValue(key,out var prior))
            {
                if(prior.ProviderSymbol!=mapping.ProviderSymbol || (!string.IsNullOrEmpty(prior.ProviderInstrumentId)&&!string.IsNullOrEmpty(mapping.ProviderInstrumentId)&&prior.ProviderInstrumentId!=mapping.ProviderInstrumentId))
                    throw new InvalidDataException($"Conflicting legacy provider mappings for ticker {mapping.TickerId}. JSON was not removed.");
                if(string.IsNullOrEmpty(mapping.ProviderInstrumentId))mapping=mapping with{ProviderInstrumentId=prior.ProviderInstrumentId};
            }
            result[key]=mapping;
        }
        return result.Values.ToList();
    }

    private void ValidateDatabase()
    {if(string.IsNullOrWhiteSpace(new SqlConnectionStringBuilder(connectionString).InitialCatalog))throw new InvalidDataException("Specify the FIGMarketLive database before creating provider mappings.");}

    public async Task EnsureSchemaAsync(CancellationToken ct)
    {
        ValidateDatabase();await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(ct);
        await using var cmd=new SqlCommand(CreateSchemaSql,c,tx);await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
    }

    public async Task<int> MigrateAsync(IReadOnlyList<MarketDataSelection> legacy,CancellationToken ct)
    {
        var mappings=BuildLegacyMappings(legacy);ValidateDatabase();
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        await using(var schema=new SqlCommand(CreateSchemaSql,c,tx))await schema.ExecuteNonQueryAsync(ct);
        foreach(var entry in legacy)
        {
            await using var check=new SqlCommand("IF NOT EXISTS(SELECT 1 FROM dbo.DataSet WHERE Id=@DataSet AND TickerId=@Ticker AND IntervalId=@Interval) THROW 51062, 'Legacy dataset identity does not match SQL. Migration cancelled; JSON was retained.', 1;",c,tx);
            check.Parameters.AddWithValue("@DataSet",entry.DataSetId);check.Parameters.AddWithValue("@Ticker",entry.TickerId);check.Parameters.AddWithValue("@Interval",entry.IntervalId);
            await check.ExecuteNonQueryAsync(ct);
        }
        foreach(var mapping in mappings)await UpsertAsync(c,tx,mapping,false,ct);
        await tx.CommitAsync(ct);return mappings.Count;
    }

    internal static async Task UpsertAsync(SqlConnection c,SqlTransaction tx,TickerProviderMapping mapping,bool replace,CancellationToken ct)
    {
        await using var cmd=new SqlCommand("""
            IF @Replace=0 AND EXISTS(SELECT 1 FROM dbo.TickerProvider WITH(UPDLOCK,HOLDLOCK) WHERE TickerId=@Ticker AND ProviderType=@Provider AND
                (ProviderSymbol COLLATE Latin1_General_100_BIN2<>@Symbol COLLATE Latin1_General_100_BIN2 OR
                (ProviderInstrumentId<>N'' AND @Instrument<>N'' AND ProviderInstrumentId COLLATE Latin1_General_100_BIN2<>@Instrument COLLATE Latin1_General_100_BIN2)))
                THROW 51063, 'SQL contains a conflicting provider mapping. Migration cancelled; JSON was retained.', 1;
            UPDATE dbo.TickerProvider WITH(UPDLOCK,HOLDLOCK) SET ProviderSymbol=@Symbol,
                ProviderInstrumentId=CASE WHEN @Instrument=N'' AND @Replace=0 THEN ProviderInstrumentId ELSE @Instrument END
                WHERE TickerId=@Ticker AND ProviderType=@Provider;
            IF @@ROWCOUNT=0 INSERT dbo.TickerProvider(TickerId,ProviderType,ProviderInstrumentId,ProviderSymbol) VALUES(@Ticker,@Provider,@Instrument,@Symbol);
            """,c,tx);
        cmd.Parameters.AddWithValue("@Ticker",mapping.TickerId);cmd.Parameters.AddWithValue("@Provider",mapping.ProviderType);
        cmd.Parameters.AddWithValue("@Instrument",mapping.ProviderInstrumentId);cmd.Parameters.AddWithValue("@Symbol",mapping.ProviderSymbol);cmd.Parameters.AddWithValue("@Replace",replace);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<MarketInstrument> ReadInstrumentAsync(int tickerId,string provider,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        return await ReadInstrumentAsync(c,null,tickerId,provider,ct);
    }
    internal static async Task<MarketInstrument> ReadInstrumentAsync(SqlConnection c,SqlTransaction? tx,int tickerId,string provider,CancellationToken ct)
    {
        string hint=tx is null?"":" WITH(UPDLOCK,HOLDLOCK)";
        await using var cmd=new SqlCommand($"""
            SELECT p.ProviderInstrumentId,p.ProviderSymbol,t.LocalSymbol,t.Name,t.SecurityType,t.Exchange,t.Currency,t.ExpiryDate,t.MinTick,t.ContractSize,p.HistoryStartRawTime
            FROM dbo.TickerProvider p{hint} JOIN dbo.Ticker t{hint} ON t.Id=p.TickerId WHERE p.TickerId=@Ticker AND p.ProviderType=@Provider
            """,c,tx);
        cmd.Parameters.AddWithValue("@Ticker",tickerId);cmd.Parameters.AddWithValue("@Provider",provider);
        await using var r=await cmd.ExecuteReaderAsync(ct);
        if(!await r.ReadAsync(ct))throw new InvalidDataException($"No provider mapping exists for ticker {tickerId} / {provider}. Save the ticker through its adapter.");
        return new(){ProviderInstrumentId=r.GetString(0),Symbol=r.GetString(1),LocalSymbol=r.GetString(2),Name=r.GetString(3),SecurityType=r.GetString(4),Exchange=r.GetString(5),Currency=r.GetString(6),
            Expiry=r.GetString(4)=="FUT"&&r.GetInt32(7)>0?r.GetInt32(7).ToString(CultureInfo.InvariantCulture):"",MinimumTick=r.GetDecimal(8),ContractMultiplier=r.GetInt32(9),HistoryStartRawTime=r.IsDBNull(10)?null:r.GetInt64(10)};
    }
}
