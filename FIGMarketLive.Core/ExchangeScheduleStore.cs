using System.Globalization;
using FIGCommon.Models;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed record ExchangePriceSchedule(string Exchange,bool Enabled,string TimeZoneId,
    string WeeklyBreakCloseDay,string WeeklyBreakCloseTime,string WeeklyBreakOpenDay,string WeeklyBreakOpenTime,
    string DailyBreakCloseTime,string DailyBreakOpenTime)
{
    public string WeeklyBreak=>$"{WeeklyBreakCloseDay} {WeeklyBreakCloseTime} – {WeeklyBreakOpenDay} {WeeklyBreakOpenTime}";
    public string DailyBreak=>$"{DailyBreakCloseTime} – {DailyBreakOpenTime}";
    public MarketSchedule ToMarketSchedule()=>new(){TimeZoneId=TimeZoneId,WeeklyBreakCloseDay=WeeklyBreakCloseDay,
        WeeklyBreakCloseTime=WeeklyBreakCloseTime,WeeklyBreakOpenDay=WeeklyBreakOpenDay,WeeklyBreakOpenTime=WeeklyBreakOpenTime,
        DailyBreakCloseTime=DailyBreakCloseTime,DailyBreakOpenTime=DailyBreakOpenTime};
    public bool IsOpen(DateTime utcNow)
    {Validate();return Enabled&&MarketScheduleHelper.IsMarketOpen(ToMarketSchedule(),utcNow);}
    public DateTime? NextOpenUtc(DateTime utcNow)
    {
        Validate();if(!Enabled)return null;
        var schedule=ToMarketSchedule();
        if(MarketScheduleHelper.IsMarketOpen(schedule,utcNow))return utcNow;
        // Search UTC minute boundaries so overlapping breaks and DST gaps/repeated hours
        // use exactly the same open predicate as capture, without ambiguous local conversion.
        var candidate=new DateTime(utcNow.Ticks-utcNow.Ticks%TimeSpan.TicksPerMinute,DateTimeKind.Utc).AddMinutes(1);
        for(int minute=0;minute<8*24*60;minute++,candidate=candidate.AddMinutes(1))
            if(MarketScheduleHelper.IsMarketOpen(schedule,candidate))return candidate;
        return null;
    }
    public string WaitingStatus(DateTime utcNow)
    {
        var next=NextOpenUtc(utcNow);
        if(next is null)return $"{Exchange}: Waiting for exchange schedule — {(Enabled?"no opening in the next eight days":"schedule disabled")}.";
        var zone=TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        var local=TimeZoneInfo.ConvertTime(new DateTimeOffset(next.Value),zone);
        return $"{Exchange}: Waiting for exchange schedule — next scheduled start: {local:yyyy-MM-dd HH:mm zzz} ({TimeZoneId}).";
    }
    public void Validate()
    {
        if(string.IsNullOrWhiteSpace(Exchange)||Exchange.Length>50||TimeZoneId.Length>128)throw new InvalidDataException("Exchange and a valid timezone are required.");
        try{TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);}catch(Exception ex) when(ex is TimeZoneNotFoundException or InvalidTimeZoneException){throw new InvalidDataException("Select a valid exchange timezone.",ex);}
        if(!Enum.TryParse<DayOfWeek>(WeeklyBreakCloseDay,out var close)||!Enum.IsDefined(close)||!Enum.TryParse<DayOfWeek>(WeeklyBreakOpenDay,out var open)||!Enum.IsDefined(open))throw new InvalidDataException("Select valid weekly break weekdays.");
        foreach(var value in new[]{WeeklyBreakCloseTime,WeeklyBreakOpenTime,DailyBreakCloseTime,DailyBreakOpenTime})
            if(!TimeOnly.TryParseExact(value,"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out _))throw new InvalidDataException("Schedule times must be HH:mm (24-hour exchange-local time).");
        if(DailyBreakCloseTime==DailyBreakOpenTime||close==open&&WeeklyBreakCloseTime==WeeklyBreakOpenTime)
            throw new InvalidDataException("Break start and end must differ.");
    }
}

public sealed class ExchangeScheduleStore(string connectionString)
{
    public async Task<List<string>> ExchangesAsync(CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("SELECT DISTINCT UPPER(LTRIM(RTRIM(Exchange))) Exchange FROM dbo.Ticker WHERE LTRIM(RTRIM(Exchange))<>'' ORDER BY Exchange",c);
        await using var reader=await cmd.ExecuteReaderAsync(ct);var result=new List<string>();
        while(await reader.ReadAsync(ct))result.Add(reader.GetString(0));return result;
    }
    public async Task EnsureSchemaAsync(CancellationToken ct)
    {
        using var stream=typeof(ExchangeScheduleStore).Assembly.GetManifestResourceStream("FIGMarketLive.Core.Database.CreateExchangeSchedules.sql")??throw new InvalidDataException("Missing exchange schedule schema.");
        using var reader=new StreamReader(stream);string sql=await reader.ReadToEndAsync(ct);
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);await using var cmd=new SqlCommand(sql,c);await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task<List<ExchangePriceSchedule>> ListAsync(CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("SELECT Exchange,Enabled,TimeZoneId,WeeklyBreakCloseDay,WeeklyBreakCloseTime,WeeklyBreakOpenDay,WeeklyBreakOpenTime,DailyBreakCloseTime,DailyBreakOpenTime FROM dbo.ExchangePriceSchedule ORDER BY Exchange",c);
        await using var r=await cmd.ExecuteReaderAsync(ct);var rows=new List<ExchangePriceSchedule>();
        while(await r.ReadAsync(ct))rows.Add(new(r.GetString(0),r.GetBoolean(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetString(6),r.GetString(7),r.GetString(8)));return rows;
    }
    public async Task<ExchangePriceSchedule?> FindAsync(string exchange,CancellationToken ct)
        =>(await ListAsync(ct)).SingleOrDefault(s=>s.Exchange.Equals(exchange.Trim(),StringComparison.OrdinalIgnoreCase));
    public async Task<bool> IsOpenAsync(string exchange,CancellationToken ct)
    {
        var schedule=await FindAsync(exchange,ct)??throw new InvalidDataException($"No price-sync schedule configured for exchange {exchange}.");
        return schedule.IsOpen(DateTime.UtcNow);
    }
    public async Task SaveAsync(ExchangePriceSchedule row,CancellationToken ct,bool creating=false)
    {
        row.Validate();await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("""
            SET XACT_ABORT ON;BEGIN TRANSACTION;
            IF @New=1 AND EXISTS(SELECT 1 FROM dbo.ExchangePriceSchedule WITH(UPDLOCK,HOLDLOCK) WHERE Exchange=@Exchange)
                THROW 51081,'An exchange schedule already exists. Reload and select it to edit.',1;
            UPDATE dbo.ExchangePriceSchedule WITH(UPDLOCK,SERIALIZABLE) SET Enabled=@Enabled,TimeZoneId=@Zone,
                WeeklyBreakCloseDay=@CloseDay,WeeklyBreakCloseTime=@CloseTime,WeeklyBreakOpenDay=@OpenDay,WeeklyBreakOpenTime=@OpenTime,
                DailyBreakCloseTime=@DailyClose,DailyBreakOpenTime=@DailyOpen WHERE Exchange=@Exchange;
            IF @@ROWCOUNT=0 INSERT dbo.ExchangePriceSchedule VALUES(@Exchange,@Enabled,@Zone,@CloseDay,@CloseTime,@OpenDay,@OpenTime,@DailyClose,@DailyOpen);
            COMMIT;
            """,c);
        foreach(var (name,value) in new (string,object)[]{("@Exchange",row.Exchange.Trim().ToUpperInvariant()),("@Enabled",row.Enabled),("@Zone",row.TimeZoneId),
            ("@CloseDay",row.WeeklyBreakCloseDay),("@CloseTime",row.WeeklyBreakCloseTime),("@OpenDay",row.WeeklyBreakOpenDay),("@OpenTime",row.WeeklyBreakOpenTime),("@DailyClose",row.DailyBreakCloseTime),("@DailyOpen",row.DailyBreakOpenTime)})cmd.Parameters.AddWithValue(name,value);
        cmd.Parameters.AddWithValue("@New",creating);
        await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task DeleteAsync(string exchange,CancellationToken ct)
    {
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var cmd=new SqlCommand("""
            SET XACT_ABORT ON;BEGIN TRANSACTION;
            IF EXISTS(SELECT 1 FROM dbo.Ticker WITH(UPDLOCK,HOLDLOCK) WHERE UPPER(LTRIM(RTRIM(Exchange)))=@Exchange)
                THROW 51082,'This exchange is used by configured tickers. Disable the schedule instead of deleting it.',1;
            DELETE dbo.ExchangePriceSchedule WHERE Exchange=@Exchange;
            COMMIT;
            """,c);
        cmd.Parameters.AddWithValue("@Exchange",exchange.Trim().ToUpperInvariant());await cmd.ExecuteNonQueryAsync(ct);
    }
}
