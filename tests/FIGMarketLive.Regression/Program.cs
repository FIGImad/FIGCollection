using System.Text.Json;
using FIG.Studies;
using FIGCommon.Models;
using FIGMarketLive;
using FIGSignalExSvc.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using FIGCommon.Models.FIGProviderAPI;

if(args.FirstOrDefault()=="--verify-sql-monitors")
{
    var json=File.ReadAllText(args[1]);var settings=JsonSerializer.Deserialize<MarketLiveSettings>(json)!;
    using var document=JsonDocument.Parse(json);
    if(document.RootElement.TryGetProperty("Monitors",out _))throw new Exception("Active JSON still contains Monitors.");
    var plan=await new SqlMonitorLoader(settings.ConnectionString).LoadAsync(default);
    if(plan.Issues.Count>0)throw new Exception(string.Join("; ",plan.Issues.Select(i=>i.Message)));
    foreach(var monitor in plan.Monitors)
    {
        var snapshot=await new BarStudyStore(settings.ConnectionString).ReadAsync(monitor,default);
        var studyPrices=PriceBarAggregation.Build(snapshot.Prices,monitor.SourceInterval,monitor.Interval);
        Console.WriteLine($"StudyCol {monitor.StudyCol.Id}: DataSet {monitor.DataSetId}, {monitor.SourceInterval.Id} -> {monitor.Interval.Id}, {snapshot.Prices.Count} source bars / {studyPrices.Count} study bars.");
    }
    Console.WriteLine($"Verified {plan.Monitors.Count} enabled SQL monitor(s); JSON has no Monitors. Dry run only; no studies were written.");return;
}

if(args.FirstOrDefault()=="--verify-provider-settings")
{
    var currentText=File.ReadAllText(args[1]);
    var current=JsonSerializer.Deserialize<MarketLiveSettings>(currentText)!;
    var previous=JsonSerializer.Deserialize<MarketLiveSettings>(File.ReadAllText(args[2]))!;
    using var document=JsonDocument.Parse(currentText);
    if(document.RootElement.TryGetProperty("DataSelections",out _))throw new Exception("Active JSON still contains DataSelections.");
    var check=await new DatabaseSetupService(current.ConnectionString).CheckAsync(default);
    if(!check.Ready)throw new Exception(check.Message);
    var expected=TickerProviderStore.BuildLegacyMappings(previous.LegacyDataSelections??[]);
    foreach(var mapping in expected)
    {
        var actual=await new TickerProviderStore(current.ConnectionString).ReadInstrumentAsync(mapping.TickerId,mapping.ProviderType,default);
        if(actual.Symbol!=mapping.ProviderSymbol||actual.ProviderInstrumentId!=mapping.ProviderInstrumentId)throw new Exception("SQL provider mapping does not match legacy backup.");
    }
    var datasets=await new DataSetDeletionStore(current.ConnectionString).ListAsync(default);
    Console.WriteLine($"Verified {expected.Count} SQL provider mappings against backup; schema checks passed; {datasets.Count} configured datasets loaded directly from SQL; active JSON has no DataSelections.");
    return;
}

int checks=0;
void Check(bool value,string name) { if(!value) throw new Exception(name); checks++; Console.WriteLine($"PASS {name}"); }
void Reject(Action action,string name)
{
    try { action(); } catch(InvalidDataException) { Check(true,name); return; }
    throw new Exception($"Expected rejection: {name}");
}
var values=new Dictionary<string,object?>
{
    ["missing"]=null, ["decimal"]=12345678901234567890.12345678m, ["double"]=Math.PI,
    ["integer"]=long.MaxValue, ["bool"]=true, ["text"]= "é漢字\0", ["int32"]=42, ["single"]=1.25f,
    ["nan"]=double.NaN, ["infinity"]=double.PositiveInfinity
};
var encoded=StudyBinaryCodec.Encode(values);
var decoded=StudyBinaryCodec.Decode(encoded);
Check(values.All(p=>Equals(p.Value,decoded[p.Key])),"All supported types, precision, missing values and Unicode survive binary round trip");
Check(encoded.SequenceEqual(StudyBinaryCodec.Encode(values.Reverse().ToDictionary())),"Encoding is deterministic regardless of dictionary order");
for(int n=0;n<encoded.Length;n++) Reject(()=>StudyBinaryCodec.Decode(encoded[..n]),$"Truncation at {n} rejected");
Reject(()=>StudyBinaryCodec.Decode([..encoded,0]),"Trailing bytes rejected");
var badVersion=(byte[])encoded.Clone(); badVersion[4]=99;
Reject(()=>StudyBinaryCodec.Decode(badVersion),"Unknown version rejected");
Reject(()=>StudyBinaryCodec.Encode(new Dictionary<string,object?> { ["unsupported"]=new object() }),"Unsupported types fail explicitly");
Reject(()=>StudyBinaryCodec.Decode(new byte[StudyBinaryCodec.MaxPayloadBytes+1]),"Oversized input rejected before allocation by decoder");
var prices=Enumerable.Range(0,120).Select(i=>new PriceDataRS
{
    DataSetId=1,RawTime=1700000040+i*60,Open=100+i,High=104+i,Low=98+i,Close=102+i,Volume=1000+i
}).ToList();
var context=new StudyCollectionContext(new StudyColRS { ColType="fixture",ColParams="{}" },new TickerRS(),
    new IntervalRS { IntervalLen=60 },NullLoggerFactory.Instance);
var direct=new FixtureCollection(context).Calc(prices,0);
var replay=StudyReplay.Calculate(()=>new FixtureCollection(context),prices);
Check(direct.Zip(replay).All(p=>StudyBinaryCodec.Encode(p.First.Studies).SequenceEqual(p.Second.Studies)),"Per-bar replay matches clean batch calculation");
Check(!new FixtureCollection(context).SupportsCheckpoint,"Fixture explicitly has no checkpoint support");
prices[^1].Close+=0.5m;
var revised=StudyReplay.Calculate(()=>new FixtureCollection(context),prices);
Check(!replay[^1].Studies.SequenceEqual(revised[^1].Studies),"Forming-bar replacement changes output");
Check(revised.Zip(StudyReplay.Calculate(()=>new FixtureCollection(context),prices)).All(p=>p.First.Studies.SequenceEqual(p.Second.Studies)),"Repeated forming-bar calculation is stable");
prices[30].Close+=0.75m;
var corrected=StudyReplay.Calculate(()=>new FixtureCollection(context),prices);
var reference=new FixtureCollection(context).Calc(prices,0);
Check(reference.Zip(corrected).All(p=>StudyBinaryCodec.Encode(p.First.Studies).SequenceEqual(p.Second.Studies)),"Older correction and restart replay match full reference calculation");
Reject(()=>StudyReplay.Calculate(()=>new FixtureCollection(context),[prices[1],prices[0]]),"Unordered source rejected");
using(var cancelled=new CancellationTokenSource())
{
    cancelled.Cancel(); bool stopped=false;
    try { StudyReplay.Calculate(()=>new FixtureCollection(context),prices,cancelled.Token); }
    catch(OperationCanceledException) { stopped=true; }
    Check(stopped,"Replay observes cancellation");
}

// A genuine DLL loaded through the same loader compiled into FIGSignalSvc.
var fixtureRoot=Path.Combine(Path.GetFullPath(args.FirstOrDefault() ?? ".test-temp/marketlive"),Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(fixtureRoot);
var assembly=typeof(FixtureFactory).Assembly.Location;
File.Copy(assembly,Path.Combine(fixtureRoot,Path.GetFileName(assembly)));
File.WriteAllText(Path.Combine(fixtureRoot,"plugin.json"),JsonSerializer.Serialize(new
    { colType="fixture",assembly=Path.GetFileName(assembly),apiVersion=1,version="1" }));
var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["StudyPlugins:Path"]=fixtureRoot }).Build();
var catalog=new StudyPluginCatalog(config,NullLogger<StudyPluginCatalog>.Instance);
Check(catalog.Registrations.Count==1,"Service loader discovers managed fixture DLL");
var loaded=StudyReplay.Calculate(()=>catalog.Create("fixture",context),prices);
Check(loaded.Zip(corrected).All(p=>p.First.Studies.SequenceEqual(p.Second.Studies)),"Loaded plugin produces identical binary study output");
Reject(()=>BrokerLookupClient.ValidateBaseUrl("file:///tmp/broker"),"Non-HTTP broker URL rejected");
Reject(()=>BrokerLookupClient.ValidateBaseUrl("https://user:secret@host/"),"Credentials in URL rejected");
Check(BrokerLookupClient.ValidateBaseUrl("https://host/service").AbsolutePath=="/service/","Broker base path is preserved");
var requests=new List<string>();
using(var client=new HttpClient(new StubHandler(async (request,ct)=>
{
    requests.Add(request.RequestUri!.AbsolutePath);
    Check(request.Headers.Authorization?.ToString()=="Bearer test-token","Broker request sends bearer token");
    if(request.Method==HttpMethod.Post)
    {
        var body=await request.Content!.ReadAsStringAsync(ct);
        Check(body.Contains("test-account",StringComparison.Ordinal) && body.Contains("202612",StringComparison.Ordinal),"Lookup contains account and explicit futures expiry");
        return new(HttpStatusCode.OK){Content=new StringContent("[{\"symbol\":\"NQ\",\"secType\":\"FUT\"}]")};
    }
    return new(HttpStatusCode.OK){Content=new StringContent(request.RequestUri.AbsolutePath.EndsWith("ping")?"{\"pong\":\"Pong\"}":"[]")};
})))
{
    var adapter=new BrokerLookupClient(client);await adapter.PingAsync("https://host/service","test-token",default);
    var matches=await adapter.LookupAsync("https://host/service","test-token",new("test-account","NQ","FUT","CME","USD","202612"),false,default);
    Check(matches.Single().Symbol=="NQ","Broker contract JSON decodes");
    Check(requests.SequenceEqual(new[]{"/service/net/ping","/service/ticker/all","/service/marketlookup/search"}),"Ping, authorization and search use actual service routes");
}
using(var client=new HttpClient(new StubHandler((_,_)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)))))
{
    bool refused=false;try{await new BrokerLookupClient(client).PingAsync("https://host/","expired",default);}
    catch(HttpRequestException ex) when(ex.StatusCode==HttpStatusCode.Unauthorized){refused=true;}
    Check(refused,"Unauthorized broker cannot pass readiness");
}
using(var client=new HttpClient(new StubHandler((_,_)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{\"pong\":\"not-fig\"}")}))))
{
    bool refused=false;try{await new BrokerLookupClient(client).PingAsync("https://host/","",default);}
    catch(InvalidDataException){refused=true;}
    Check(refused,"Wrong ping response cannot pass readiness");
}
var contract=new ContractInfo{Symbol="NQ",LocalSymbol="NQZ6",SecType="FUT",Currency="USD",Exchange="CME",LastTradeDateOrContractMonth="202612",MinTick=0.25m,Multiplier="20"};
DatabaseSetupService.ValidateContract(contract,"NQZ6");Check(true,"Resolved futures contract accepted");
contract.Multiplier="0.5";Reject(()=>DatabaseSetupService.ValidateContract(contract,"NQZ6"),"Fractional multiplier cannot silently truncate");
contract.Multiplier="20";contract.LastTradeDateOrContractMonth="";Reject(()=>DatabaseSetupService.ValidateContract(contract,"NQZ6"),"Unresolved future cannot be persisted");
if(args.Length >= 3)
{
    var realConfig=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["StudyPlugins:Path"]=Path.GetFullPath(args[1]) }).Build();
    var realCatalog=new StudyPluginCatalog(realConfig,NullLogger<StudyPluginCatalog>.Instance);
    var registration=realCatalog.Registrations.Single();
    var realContext=new StudyCollectionContext(new StudyColRS { ColType=registration.ColType,ColParams=File.ReadAllText(args[2]) },
        new TickerRS { Symbol="NQ",MinTick=0.25m,ContractSize=20 },new IntervalRS { IntervalLen=60 },NullLoggerFactory.Instance);
    var actual=StudyReplay.Calculate(()=>realCatalog.Create(registration.ColType,realContext),prices);
    var batch=realCatalog.Create(registration.ColType,realContext).Calc(prices,0);
    Check(actual.Zip(batch).All(p=>p.First.Studies.SequenceEqual(StudyBinaryCodec.Encode(p.Second.Studies))),$"Real {registration.ColType} DLL replay matches batch without checkpoints");
    Check(actual.All(b=>StudyBinaryCodec.Encode(StudyBinaryCodec.Decode(b.Studies)).SequenceEqual(b.Studies)),"Real DLL outputs survive binary storage round trip");
}
var targetDataSet=new ConfiguredDataSet(7,3,"NQZ6","NQZ6","FUT","1","1 Min",2);
var deletion=new DataSetDeletionPreview(targetDataSet,new[]{40,41},100,200);
DataSetDeletionStore.ValidateSelectedIdentity(targetDataSet with{Symbol="NQ",LocalSymbol="",IntervalName="1",StudyCollections=0},targetDataSet);
Check(true,"First delete accepts refreshed SQL labels for the same dataset, ticker and interval");
Reject(()=>DataSetDeletionStore.ValidateSelectedIdentity(targetDataSet,targetDataSet with{DataSetId=8}),"Selection refresh cannot substitute another dataset ID");
Reject(()=>DataSetDeletionStore.ValidateSelectedIdentity(targetDataSet,targetDataSet with{IntervalId="5"}),"Selection refresh cannot substitute another interval");
Reject(()=>DataSetDeletionStore.ValidateUnchanged(deletion,deletion with{StudyColIds=new[]{40,42}}),"Deletion refuses changed study IDs even when dependency count is unchanged");
Reject(()=>DataSetDeletionStore.ValidateUnchanged(deletion,deletion with{DataSet=targetDataSet with{TickerId=4}}),"Deletion refuses a changed dataset identity");
DataSetDeletionStore.ValidateUnchanged(deletion,deletion with{PriceRows=101,StudyRows=201});
Check(true,"Confirmed whole-dataset deletion permits new rows but requires unchanged study dependencies");
Check(!JsonSerializer.Serialize(new MarketLiveSettings()).Contains("\"Monitors\"",StringComparison.Ordinal),"New settings omit the local Monitors section");
Check(JsonSerializer.Deserialize<MarketLiveSettings>("{\"Monitors\":[{\"DataSetId\":7,\"StudyCol\":{\"Id\":40}}]}")!.LegacyMonitors!.Single().StudyCol.Id==40,"Legacy monitors remain readable for SQL validation before removal");
var minute=new IntervalRS{Id="1",Name="1 Min",Seq=1,IntervalLen=60};
var five=new IntervalRS{Id="5",Name="5 Min",Seq=5,IntervalLen=300};
var ten=new IntervalRS{Id="10",Name="10 Min",Seq=10,IntervalLen=600};
var hour=new IntervalRS{Id="60",Name="1 Hour",Seq=60,IntervalLen=3600};
var day=new IntervalRS{Id="D",Name="1 Day",Seq=100,IntervalLen=86400};
var month=new IntervalRS{Id="M",Name="1 Month",Seq=101,IntervalLen=2592000};
Check(StudyIntervalCompatibility.CanAggregate(minute,hour)&&StudyIntervalCompatibility.CanAggregate(five,ten)&&StudyIntervalCompatibility.CanAggregate(five,day),"Minute/hour/day aggregation compatibility accepts whole source bars");
Check(!StudyIntervalCompatibility.CanAggregate(five,minute)&&!StudyIntervalCompatibility.CanAggregate(ten,new(){Id="15",IntervalLen=900}),"Cannot fabricate smaller bars or split source bars across non-multiple intervals");
Check(StudyIntervalCompatibility.CanAggregate(day,month)&&!StudyIntervalCompatibility.CanAggregate(month,day),"Calendar compatibility uses boundaries rather than treating a month as fixed seconds");
var compatible=StudyIntervalCompatibility.Choices(12,[minute,five,ten,hour,day,month],[new(1,12,"1"),new(5,12,"5"),new(10,12,"10"),new(99,99,"1")]);
Check(compatible.Single(c=>c.Interval.Id=="10").Source.Id==10&&!compatible.Single(c=>c.Interval.Id=="10").Aggregate,"Exact source dataset is preferred over aggregation");
Check(compatible.Single(c=>c.Interval.Id=="60").Source.Id==10,"Aggregation selects the closest compatible source for this ticker");
Reject(()=>StudyIntervalCompatibility.NormalizeParameters("[]"),"Study parameters must be a JSON object");
Reject(()=>StudyIntervalCompatibility.NormalizeParameters("{\"a\":1,\"A\":2}"),"Ambiguous case-insensitive study parameter keys are rejected");
Check(StudyIntervalCompatibility.NormalizeParameters("{\"period\":5}").Contains("5"),"Valid study parameters normalize for SQL storage");
var studyCatalog=new StudyConfigurationCatalog([new(12,"NQZ6")],[minute,five,ten],
    [new(5,12,"5")],[new("L2","L2.dll","")],[new(201,12,"10","L2","{}",true)]);
Check(StudyIntervalCompatibility.DependentStudyIds(5,studyCatalog).SequenceEqual(new[]{201}),"Dataset dependency checks include a study aggregated from its source dataset");
studyCatalog.DataSets.Add(new(10,12,"10"));
Check(StudyIntervalCompatibility.DependentStudyIds(5,studyCatalog).Length==0&&StudyIntervalCompatibility.DependentStudyIds(10,studyCatalog).SequenceEqual(new[]{201}),"An exact dataset takes ownership of the inferred study dependency");
Reject(()=>StudyConfigurationStore.ValidateType(new("L2","L2.exe","")),"ColType requires a DLL filename");
var legacyMappings=TickerProviderStore.BuildLegacyMappings([
    new(){TickerId=12,DataSetId=5,IntervalId="5",ProviderId="IBKR",Instrument=new(){Symbol="NQ",ProviderInstrumentId="12345"}},
    new(){TickerId=12,DataSetId=10,IntervalId="10",ProviderId="ibkr",Instrument=new(){Symbol="NQ",ProviderInstrumentId="12345"}}]);
Check(legacyMappings.Count==1&&legacyMappings[0].ProviderSymbol=="NQ"&&legacyMappings[0].ProviderType=="IBKR","Migration deduplicates ticker mappings across datasets and preserves the provider root symbol");
Reject(()=>TickerProviderStore.BuildLegacyMappings([
    new(){TickerId=12,ProviderId="IBKR",Instrument=new(){Symbol="NQ",ProviderInstrumentId="1"}},
    new(){TickerId=12,ProviderId="IBKR",Instrument=new(){Symbol="NQ",ProviderInstrumentId="2"}}]),"Conflicting legacy instrument IDs cannot be silently migrated");
Reject(()=>TickerProviderStore.BuildLegacyMappings([new(){TickerId=12,Instrument=new(){Symbol="NQ"}}]),"Migration cannot guess a missing provider identity");
Check(TickerProviderStore.BuildLegacyMappings([new(){TickerId=12,Contract=new(){Symbol="NQ",ConId=123}}]).Single().ProviderType=="IBKR","Legacy IBKR contract identity migrates without inventing a provider ID");
Check(!JsonSerializer.Serialize(new MarketLiveSettings()).Contains("DataSelections",StringComparison.Ordinal),"New configuration omits DataSelections entirely");
var oldSettings=JsonSerializer.Deserialize<MarketLiveSettings>("{\"DataSelections\":[{\"TickerId\":12}]}")!;
Check(oldSettings.LegacyDataSelections?.Single().TickerId==12&&JsonSerializer.Serialize(oldSettings).Contains("DataSelections",StringComparison.Ordinal),"Legacy entries remain serializable until migration commits");
checks+=await AdapterChecks.RunAsync();
checks+=AggregationChecks.Run();
Console.WriteLine($"Passed {checks} checks. Fixture: {fixtureRoot}");

public sealed class FixtureFactory : IStudyCollectionFactory
{
    public string ColType=>"fixture";
    public string Version=>"1";
    public int ApiVersion=>StudyPluginApi.CurrentVersion;
    public StudyColBase Create(StudyCollectionContext context)=>new FixtureCollection(context);
}

public sealed class FixtureCollection(StudyCollectionContext context) : StudyColBase(context,10,512)
{
    protected override void InitializeStudies()=>studyList=[new StudyMA(5,"close","test"),new StudyADX(5,5,"test")];
    public override int GetMaxLen()=>5;
}

internal sealed class StubHandler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)=>send(request,cancellationToken);
}
