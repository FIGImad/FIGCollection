using System.Data;
using System.Globalization;
using FIGMarketLive.MarketData;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed class TickerContractStore(string connectionString)
{
    public static MarketInstrument Candidate(MarketInstrument original,string localSymbol,string expiry)
    {
        localSymbol=localSymbol.Trim();expiry=expiry.Trim();
        if(localSymbol.Length is <1 or >50)throw new InvalidDataException("Contract local name must have 1–50 characters.");
        if(original.SecurityType=="FUT")
        {
            string format=expiry.Length==6?"yyyyMM":"yyyyMMdd";
            if(expiry.Length is not (6 or 8)||!DateTime.TryParseExact(expiry,format,CultureInfo.InvariantCulture,DateTimeStyles.None,out _))
                throw new InvalidDataException("Enter a valid futures expiry as YYYYMM or YYYYMMDD.");
        }
        else if(expiry.Length!=0)throw new InvalidDataException("Stocks do not have a contract expiry.");
        // A roll must resolve a NEW broker identity rather than match the old contract ID.
        return original with{LocalSymbol=localSymbol,Expiry=expiry,ProviderInstrumentId="",HistoryStartRawTime=null};
    }

    public static void ValidateResolved(MarketInstrument original,MarketInstrument candidate,MarketInstrument resolved)
    {
        Candidate(original,candidate.LocalSymbol,candidate.Expiry);
        Candidate(original,resolved.LocalSymbol,resolved.Expiry);
        bool Same(string a,string b)=>a.Equals(b,StringComparison.OrdinalIgnoreCase);
        if(!Same(original.Symbol,resolved.Symbol)||!Same(original.SecurityType,resolved.SecurityType)||!Same(original.Exchange,resolved.Exchange)||
            !Same(original.Currency,resolved.Currency)||original.MinimumTick!=resolved.MinimumTick||original.ContractMultiplier!=resolved.ContractMultiplier)
            throw new InvalidDataException("Updated contract must have the same root symbol, type, exchange, currency, tick size and multiplier. Use a separate ticker for a different instrument.");
        if(!Same(candidate.LocalSymbol,resolved.LocalSymbol)||!resolved.Expiry.StartsWith(candidate.Expiry,StringComparison.Ordinal)||string.IsNullOrWhiteSpace(resolved.ProviderInstrumentId))
            throw new InvalidDataException("The adapter resolved a different local contract name or expiry. Check the requested contract details.");
        TickerProviderStore.FromInstrument(1,"provider",resolved);
    }

    public static bool IsRollover(MarketInstrument original,MarketInstrument resolved)
        =>(!string.IsNullOrEmpty(original.ProviderInstrumentId)&&original.ProviderInstrumentId!=resolved.ProviderInstrumentId)||
            !(original.Expiry.StartsWith(resolved.Expiry,StringComparison.Ordinal)||resolved.Expiry.StartsWith(original.Expiry,StringComparison.Ordinal));

    public async Task UpdateAsync(ConfiguredDataSet selected,string provider,MarketInstrument expected,MarketInstrument candidate,MarketInstrument resolved,CancellationToken ct)
    {
        ValidateResolved(expected,candidate,resolved);
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        await using(var check=new SqlCommand("SELECT t.Symbol FROM dbo.DataSet d WITH(UPDLOCK,HOLDLOCK) JOIN dbo.Ticker t WITH(UPDLOCK,HOLDLOCK) ON t.Id=d.TickerId WHERE d.Id=@Id AND t.Id=@Ticker",c,tx))
        {
            check.Parameters.AddWithValue("@Id",selected.DataSetId);check.Parameters.AddWithValue("@Ticker",selected.TickerId);
            if(await check.ExecuteScalarAsync(ct) as string!=selected.Symbol)throw new InvalidDataException("Selected ticker/dataset changed. Refresh and edit it again.");
        }
        var current=await TickerProviderStore.ReadInstrumentAsync(c,tx,selected.TickerId,provider,ct);
        if(current!=expected)throw new InvalidDataException("Ticker contract changed while you were editing. Reopen the editor.");
        bool roll=IsRollover(expected,resolved);long? start=roll?DateTimeOffset.UtcNow.ToUnixTimeSeconds():expected.HistoryStartRawTime;
        await using(var update=new SqlCommand("UPDATE dbo.Ticker SET LocalSymbol=@Local,ExpiryDate=@Expiry WHERE Id=@Ticker",c,tx))
        {
            update.Parameters.AddWithValue("@Ticker",selected.TickerId);update.Parameters.AddWithValue("@Local",resolved.LocalSymbol);
            update.Parameters.AddWithValue("@Expiry",int.TryParse(resolved.Expiry,out int expiry)?expiry:0);await update.ExecuteNonQueryAsync(ct);
        }
        await TickerProviderStore.UpsertAsync(c,tx,TickerProviderStore.FromInstrument(selected.TickerId,provider,resolved),true,ct);
        await using(var update=new SqlCommand("""
            UPDATE dbo.TickerProvider SET HistoryStartRawTime=@Start,
                ProviderInstrumentId=CASE WHEN ProviderType=@Provider OR @Roll=0 THEN ProviderInstrumentId ELSE N'' END
                WHERE TickerId=@Ticker AND (ProviderType=@Provider OR @Roll=1);
            """,c,tx))
        {
            update.Parameters.AddWithValue("@Ticker",selected.TickerId);update.Parameters.AddWithValue("@Provider",provider);
            update.Parameters.AddWithValue("@Roll",roll);
            update.Parameters.Add("@Start",SqlDbType.BigInt).Value=(object?)start??DBNull.Value;await update.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }
}
