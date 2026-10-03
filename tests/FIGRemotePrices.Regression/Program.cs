using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using FIGCommon.Exceptions;
using FIGCommon.Interfaces;
using FIGCommon.Models;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Utilities.FIGProviderAPI;
using FIGPriceSyncSvc.Controllers;
using FIGPriceSyncSvc.Services;
using FIGPriceDataSvc.PriceSync;
using FIGCommon.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

int passed=0;
void Check(bool condition,string label) {if(!condition) throw new Exception("FAIL: "+label); Console.WriteLine("PASS: "+label); passed++;}
async Task Reject(Func<Task> action,string label)
{
    try {await action();} catch(Exception e) when(e is ArgumentException or InvalidOperationException or SqlException or ProviderException or HistoricalPriceBusyException)
    {Check(true,label);return;}
    throw new Exception("FAIL: expected rejection: "+label);
}
HistoricalDataRequest Request()=>new(){Ticker=new(){Symbol="NQ",SecType="FUT",Exchange="CME",Currency="USD",ContractMonth=20261218},IntervalId="1 min",StartRawTime=120000,Duration="600 S"};
var provider=DispatchProxy.Create<IProviderService,ProviderStub>();
var stub=(ProviderStub)(object)provider;
stub.History=_=>[new BarData(119940,10,12,9,11,4),new BarData(120000,11,12,9,10,1)];
var gateway=new HistoricalPriceService(()=>provider);
var result=await gateway.RequestAsync(Request(),default);
Check(stub.TimeoutMs==45_000,"Legacy historical requests retain the provider timeout default");
var timedRequest=Request();timedRequest.ProviderTimeoutMs=47_000;
await gateway.RequestAsync(timedRequest,default);
Check(stub.TimeoutMs==47_000,"Remote historical request passes its timeout to the provider");
timedRequest.ProviderTimeoutMs=-1;
await Reject(()=>gateway.RequestAsync(timedRequest,default),"Unbounded provider timeout rejected");
Check(result.Bars.Count==1 && result.FromRawTime==119400 && result.Bars[0].RawTime==119940,"Provider endpoint returns requested window and excludes end boundary");
Check(JsonSerializer.Deserialize<HistoricalPricesResponse>(JsonSerializer.Serialize(result))!.Bars[0].Close==11
    && Newtonsoft.Json.JsonConvert.DeserializeObject<HistoricalPricesResponse>(JsonSerializer.Serialize(result))!.Bars[0].RawTime==119940,
    "Price response round-trips through both controller serializers");
Check(typeof(HistoricalDataController).GetCustomAttribute<AuthorizeAttribute>()?.Roles==Role.SVC,"Historical endpoint requires SVC authorization");
var bad=Request();bad.Duration="600000 S";
await Reject(()=>gateway.RequestAsync(bad,default),"Oversized requests rejected before provider call");
bad=Request();bad.Ticker.SecType="CONTFUT";
await Reject(()=>gateway.RequestAsync(bad,default),"Unpageable continuous-futures contract rejected");
bad=Request();bad.NumBars=2;
await Reject(()=>gateway.RequestAsync(bad,default),"Silent truncation prohibited");
stub.History=_=>throw new ProviderException(0,ProviderErrorCodes.NO_DATA,"No trading bars");
Check((await gateway.RequestAsync(Request(),default)).Bars.Count==0,"Explicit no-data response is a successful empty window");
stub.History=_=>throw new ProviderException(0,ProviderErrorCodes.COMM_ERR,"offline");
await Reject(()=>gateway.RequestAsync(Request(),default),"Provider outage is not converted into an empty success");
var controller=new HistoricalDataController(gateway,null!,new HttpContextAccessor(),NullLogger<HistoricalDataController>.Instance,
    new ServiceCollection().BuildServiceProvider(),new ConfigurationBuilder().Build())
{ControllerContext=new ControllerContext{HttpContext=new DefaultHttpContext()}};
Check((await controller.HistoricalDataRequest(Request(),default) as ObjectResult)?.StatusCode==503,"API reports provider failure as retryable 503");
stub.History=_=>[new BarData(119940,10,12,9,11,1.5m)];
await Reject(()=>gateway.RequestAsync(Request(),default),"Fractional volume cannot silently truncate into local integer schema");
stub.History=_=>[new BarData(119940,10,12,9,11,1),new BarData(119940,10,12,9,11,1)];
await Reject(()=>gateway.RequestAsync(Request(),default),"Duplicate provider bars rejected");
using(var release=new ManualResetEventSlim())
{
    var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    stub.History=_=>{entered.SetResult();release.Wait();return [];};
    var active=gateway.RequestAsync(Request(),default);
    await entered.Task;
    try {Check((await controller.HistoricalDataRequest(Request(),default) as ObjectResult)?.StatusCode==429,"Concurrent API request receives bounded busy response");}
    finally {release.Set();await active;}
}
var local=new MemoryStore();
var api=new MemoryApi();
var options=new RemotePriceSyncOptions{Enabled=true,DestinationServiceId="prices",DataSetIds=[7],InitialHistoryDays=1,WindowBars=10,OverlapBars=2};
options.Validate();
Check(new RemotePriceSyncOptions().Enabled,"Standalone price-data service enables its worker by default");
using var worker=new RemotePriceSyncService(local,api,options,NullLogger<RemotePriceSyncService>.Instance);
api.Fail=true;
await Reject(()=>worker.SyncOnceAsync(7,120000,default),"Transport failure leaves window retryable");
var failed=api.Requests.Last();api.Fail=false;
await worker.SyncOnceAsync(7,120000,default);
Check(api.Requests[1].StartRawTime==failed.StartRawTime && api.Requests.Last().StartRawTime==120000 && local.Saves>1,"Retry drains every catch-up window through the target before returning");
var before=api.Requests.Last().StartRawTime;
await worker.SyncOnceAsync(7,120000,default);
Check(api.Requests.Last().StartRawTime==before,"Empty market windows complete catch-up without moving past the fixed target");
local.Fail=true;
await Reject(()=>worker.SyncOnceAsync(7,120000,default),"Local transaction failure prevents advancing cursor");
before=api.Requests.Last().StartRawTime;local.Fail=false;
await worker.SyncOnceAsync(7,120000,default);
Check(api.Requests.Last().StartRawTime==before,"Failed local commit replays same window");
api.Mismatch=true;
await Reject(()=>worker.SyncOnceAsync(7,120000,default),"Mismatched response range rejected");api.Mismatch=false;
local.Latest=119880;
using(var restarted=new RemotePriceSyncService(local,api,options,NullLogger<RemotePriceSyncService>.Instance))
{
    await restarted.SyncOnceAsync(7,120030,default);
    Check(api.Requests.Last().StartRawTime==120030 && api.Requests.Last().Duration=="300 S","Restart resumes from durable last bar and includes the forming bar");
}

var anchor=new DateTimeOffset(2026,9,24,12,0,0,TimeSpan.Zero);
var preclose=PriceSyncTiming.NextFetch(anchor,60,5);
Check(preclose==anchor.AddSeconds(54.5),"Fetch is scheduled 5.5 seconds before the minute boundary");
Check(PriceSyncTiming.CorrectionAfter(preclose,5)==anchor.AddMinutes(1),"Correction is scheduled at the exact minute boundary");
Check(PriceSyncTiming.NextFetch(anchor.AddSeconds(58),60,5)==anchor.AddMinutes(1).AddSeconds(54.5),"Late work does not introduce timer drift");
Check(PriceSyncTiming.NextFetch(preclose,60,5)>preclose,"Exact trigger time does not create a zero-delay loop");
Check(!MarketScheduleHelper.IsMarketOpen(new RemotePriceSyncOptions().MarketSchedule,new DateTime(2026,9,26,12,0,0,DateTimeKind.Utc)),"Remote service observes the provider's weekend break");
local.Latest=120000;
using(var correctionWorker=new RemotePriceSyncService(local,api,options,NullLogger<RemotePriceSyncService>.Instance))
{
    var count=api.Requests.Count;
    await correctionWorker.SyncOnceAsync(7,120061,default,true);
    Check(api.Requests.Count==count,"Correction does not query prices before an initial catch-up succeeds");
    await correctionWorker.SyncOnceAsync(7,120054,default);
    api.Bars=[new HistoricalPriceBar(120000,10,12,9,11,1),new HistoricalPriceBar(120060,10,12,9,11,1)];
    await correctionWorker.SyncOnceAsync(7,120061,default,true);
    Check(local.SavedBars.Count==1 && local.SavedBars[0].RawTime==120000,"Late close correction updates only bars already stored, matching provider update-only behavior");
}
api.Bars=[];

var gapStore=new MemoryStore{Latest=118800};
var gapApi=new MemoryApi{FailAtRequest=2};
using(var gapWorker=new RemotePriceSyncService(gapStore,gapApi,options,NullLogger<RemotePriceSyncService>.Instance))
{
    await Reject(()=>gapWorker.SyncOnceAsync(7,120000,default),"Failure in the middle of catch-up stops at the last committed page");
    var count=gapApi.Requests.Count;
    await gapWorker.SyncOnceAsync(7,120001,default,true);
    Check(gapApi.Requests.Count==count,"Failed catch-up cannot switch to a recent correction window");
    var failedPage=gapApi.Requests.Last().StartRawTime;
    gapApi.FailAtRequest=0;
    await gapWorker.SyncOnceAsync(7,120000,default);
    Check(gapApi.Requests[count].StartRawTime==failedPage && gapApi.Requests.Last().StartRawTime==120000,"Retry resumes the failed page and drains remaining history in order");
    count=gapApi.Requests.Count;
    await gapWorker.SyncOnceAsync(7,120061,default,true);
    Check(gapApi.Requests.Count==count,"Stale catch-up after a long outage does not permit a live correction");
}
var paused=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var releasePage=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var concurrentApi=new MemoryApi();
concurrentApi.BeforeResponse=async _=>{if(concurrentApi.Requests.Count==2){paused.SetResult();await releasePage.Task;}};
using(var concurrentWorker=new RemotePriceSyncService(new MemoryStore{Latest=118800},concurrentApi,options,NullLogger<RemotePriceSyncService>.Instance))
{
    var catchup=concurrentWorker.SyncOnceAsync(7,120000,default);
    await paused.Task;
    var update=concurrentWorker.SyncOnceAsync(7,120001,default,true);
    Check(!update.IsCompleted && concurrentApi.Requests.Count==2,"Concurrent correction waits behind the complete catch-up pass");
    releasePage.SetResult();await Task.WhenAll(catchup,update);
    Check(concurrentApi.Requests.Count==4 && concurrentApi.Requests[^2].StartRawTime==120000,"Correction is issued only after the final historical window commits");
}

var hostBuilder=Host.CreateApplicationBuilder();
hostBuilder.Services.AddHttpClient();hostBuilder.Services.AddHttpContextAccessor();
hostBuilder.Services.AddPriceDataServices(hostBuilder.Configuration);
Check(hostBuilder.Services.Any(d=>d.ServiceType==typeof(IClientSignalRService) && d.ImplementationType==typeof(ClientSignalRService)),"Standalone registration includes controller connection");
var startupApi=new MemoryApi();
hostBuilder.Services.AddSingleton<IClientSignalRService,OfflineControllerClient>();
hostBuilder.Services.AddSingleton<IRemotePriceStore>(new MemoryStore());
hostBuilder.Services.AddSingleton<IPriceSyncApiService>(startupApi);
hostBuilder.Services.AddSingleton<TimeProvider>(new FixedClock(anchor));
using(var host=hostBuilder.Build())
{
    await host.StartAsync();
    try
    {
        await startupApi.FirstRequest.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Check(true,"Real host startup enters ExecuteAsync and requests prices through registered worker");
    }
    finally { await host.StopAsync(); }
}
var disabled=new ServiceCollection();
disabled.AddPriceDataServices(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"RemotePriceSync:Enabled","false"}}).Build());
Check(!disabled.Any(d=>d.ServiceType==typeof(IHostedService) && d.ImplementationType==typeof(RemotePriceSyncService)),"Explicit disabled configuration does not register the worker");

if(!args.Contains("--sql")) {Console.WriteLine($"{passed} checks passed; add --sql for full schema database tests.");return;}
var root=new DirectoryInfo(Environment.CurrentDirectory);
while(root!=null && !File.Exists(Path.Combine(root.FullName,"FIGCommon/DataSchema/FIGAutoTrader_Schema.sql"))) root=root.Parent;
if(root==null) throw new Exception("Run from repository root");
const string master=@"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;TrustServerCertificate=true";
var db="FIGRemotePricesTest_"+Guid.NewGuid().ToString("N");
var cs=new SqlConnectionStringBuilder(master){InitialCatalog=db}.ConnectionString;
async Task<object?> Sql(string connectionString,string sql)
{
    await using var connection=new SqlConnection(connectionString);await connection.OpenAsync();
    await using var command=new SqlCommand(sql,connection);return await command.ExecuteScalarAsync();
}
try
{
    await Sql(master,$"CREATE DATABASE [{db}]");
    var schema=File.ReadAllText(Path.Combine(root.FullName,"FIGCommon/DataSchema/FIGAutoTrader_Schema.sql"));
    schema=Regex.Replace(schema,@"^USE FIGAutoTrader\s*\r?\nGO\s*\r?\n","",RegexOptions.IgnoreCase);
    if(Regex.IsMatch(schema,@"^\s*(USE|CREATE DATABASE|ALTER DATABASE)\b",RegexOptions.Multiline|RegexOptions.IgnoreCase))throw new Exception("Unexpected database selection");
    await using(var connection=new SqlConnection(cs))
    {
        await connection.OpenAsync();
        foreach(var batch in Regex.Split(schema,@"^\s*GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))
            if(!string.IsNullOrWhiteSpace(batch)){await using var command=new SqlCommand(batch,connection);await command.ExecuteNonQueryAsync();}
    }
    var id=Convert.ToInt32(await Sql(cs,"IF NOT EXISTS(SELECT 1 FROM dbo.DataSet WHERE TickerId=1 AND IntervalId='1') INSERT dbo.DataSet(TickerId,IntervalId) VALUES(1,'1'); SELECT Id FROM dbo.DataSet WHERE TickerId=1 AND IntervalId='1';"));
    var store=new RemotePriceStore(()=>cs,_=>local.GetDataSet(7));
    var bar=new HistoricalPriceBar(119940,10,12,9,11,4);
    await store.SaveAsync(id,[bar],default);
    Check(await store.LastRawTimeAsync(id,default)==119940,"Real PriceData write persists local dataset and UTC bar time");
    Check(Convert.ToInt32(await Sql(cs,$"SELECT COUNT(*) FROM dbo.Event WHERE EventType='PRICEDATA' AND EventId='{id}'"))==1,"Price commit activates existing study-processing event path");
    await Sql(cs,"UPDATE dbo.Event SET TimeStamp='20000101' WHERE EventType='PRICEDATA'");
    await store.SaveAsync(id,[bar],default);
    Check(Convert.ToInt32(await Sql(cs,"SELECT COUNT(*) FROM dbo.PriceData"))==1
        && Convert.ToInt32(await Sql(cs,"SELECT YEAR(TimeStamp) FROM dbo.Event WHERE EventType='PRICEDATA'"))==2000,"Duplicate replay neither duplicates bars nor fires a spurious price event");
    await store.SaveAsync(id,[bar with{Close=12}],default);
    Check(Convert.ToDecimal(await Sql(cs,"SELECT [Close] FROM dbo.PriceData"))==12,"Overlap updates corrected historical prices");
    await Sql(cs,"CREATE TRIGGER dbo.TestRejectPrice ON dbo.PriceData AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE RawTime=120000) THROW 51011, 'Test forced failure',1;");
    await Reject(()=>store.SaveAsync(id,[bar with{Close=10},bar with{RawTime=120000}],default),"Failed insert rolls back entire mixed update/insert batch");
    Check(Convert.ToDecimal(await Sql(cs,"SELECT [Close] FROM dbo.PriceData"))==12
        && Convert.ToInt32(await Sql(cs,"SELECT COUNT(*) FROM dbo.PriceData"))==1,"Previously stored bar stays unchanged after rollback");
    await Sql(cs,"DROP TRIGGER dbo.TestRejectPrice");
    await Task.WhenAll(store.SaveAsync(id,[bar with{RawTime=120000}],default),store.SaveAsync(id,[bar with{RawTime=120000}],default));
    Check(Convert.ToInt32(await Sql(cs,"SELECT COUNT(*) FROM dbo.PriceData"))==2,"Concurrent replay preserves unique local bar identities");
    await store.SaveAsync(id,[bar with{Close=10},bar with{RawTime=120060}],default,true);
    Check(Convert.ToInt32(await Sql(cs,"SELECT COUNT(*) FROM dbo.PriceData"))==2
        && Convert.ToDecimal(await Sql(cs,"SELECT [Close] FROM dbo.PriceData WHERE RawTime=119940"))==10,
        "SQL update-only changes existing prices but cannot insert any missing bars");
    Console.WriteLine($"All {passed} remote-price checks passed, including SQL integration.");
}
finally
{
    SqlConnection.ClearAllPools();
    await Sql(master,$"IF DB_ID('{db}') IS NOT NULL BEGIN ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{db}]; END");
}

public class ProviderStub:DispatchProxy
{
    public int TimeoutMs {get;private set;}
    public Func<HistoricalDataRequest,List<BarData>?> History {get;set;}=_=>[];
    protected override object? Invoke(MethodInfo? method,object?[]? args)
    {
        if(method?.Name!="HistoricalDataRequest") throw new NotSupportedException(method?.Name);
        TimeoutMs=(int)args![1]!;
        return History((HistoricalDataRequest)args[0]!);
    }
}
sealed class MemoryStore:IRemotePriceStore
{
    public int[] GetDataSetIds()=>[7];
    public IReadOnlyList<HistoricalPriceBar> SavedBars {get;private set;}=[];
    public long? Latest {get;set;}
    public bool Fail {get;set;}
    public int Saves {get;private set;}
    public DataSetRS GetDataSet(int id)=>new(){Id=id,IntervalId="1",Interval=new(){Id="1",IntervalLen=60},Ticker=new(){LocalSymbol="NQ",SecurityType="FUT",Exchange="CME",Currency="USD",ExpiryDate=20261218}};
    public Task<long?> LastRawTimeAsync(int id,CancellationToken ct)=>Task.FromResult(Latest);
    public Task SaveAsync(int id,IReadOnlyList<HistoricalPriceBar> bars,CancellationToken ct,bool updateOnly=false){if(Fail)throw new InvalidOperationException("Local SQL down");Saves++;SavedBars=bars;return Task.CompletedTask;}
}
sealed class MemoryApi:IPriceSyncApiService
{
    public List<HistoricalPriceBar> Bars {get;set;}=[];
    public TaskCompletionSource FirstRequest {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public List<HistoricalDataRequest> Requests {get;}=[];
    public bool Fail {get;set;}
    public bool Mismatch {get;set;}
    public int FailAtRequest {get;set;}
    public Func<HistoricalDataRequest,Task>? BeforeResponse {get;set;}
    public async Task<HistoricalPricesResponse> HistoricalDataRequestAsync(HistoricalDataRequest request,CancellationToken ct)
    {
        Requests.Add(request);FirstRequest.TrySetResult();if(Fail || Requests.Count==FailAtRequest)throw new InvalidOperationException("Network unavailable");
        if(BeforeResponse!=null) await BeforeResponse(request);
        var range=HistoricalPrices.Validate(request);
        return new HistoricalPricesResponse(range.From,range.To+(Mismatch?1:0),Bars);
    }
}
sealed class FixedClock(DateTimeOffset now):TimeProvider
{
    public override DateTimeOffset GetUtcNow()=>now;
}
sealed class OfflineControllerClient(ILogger<ClientSignalRService> logger,IConfiguration config,IHostApplicationLifetime lifetime)
    :ClientSignalRService(logger,config,lifetime)
{
    public override Task StartAsync(CancellationToken ct)=>Task.CompletedTask;
    public override Task StopAsync(CancellationToken ct)=>Task.CompletedTask;
}
