using System.Security.Claims;
using System.Text.RegularExpressions;
using FIGAutoTradeExSvc.SignalIngress;
using FIGCommon.Models;
using FIGCommon.Models.SignalPublication;
using FIGCommon.Services;
using FIGSignalExSvc.Publication;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

int passed = 0;
void Check(bool value, string name) { if (!value) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); passed++; }
async Task Reject(Func<Task> action, string name)
{
    try { await action(); } catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or SqlException) { Check(true, name); return; }
    throw new Exception("FAIL: expected rejection: " + name);
}
var signal = new PublishedSignal { Tag=Guid.NewGuid().ToString(), Strategy="test", Status="START", Side=1, StartTime=100, StartPrice=10, LastUpdated=100 };
var message = new SignalPublicationMessage { ProducerId="test-site", MessageId=Guid.NewGuid(), Revision=1, Signal=signal };
message.Validate();
Check(signal.Hash()==(signal with { Side=1.0000m, StartPrice=10.0000m }).Hash(), "Hash ignores decimal representation scale");
Check(!new SignalPublicationOptions().IsRemote && !new SignalIngressOptions().Enabled, "Shared database remains the default; ingress opt-in");
var stopped = signal with { Status="STOP", StopTime=200, StopPrice=12, LastUpdated=200 };
stopped.ValidateSuccessor(stopped with { StopPrice=13, LastUpdated=201 });
Check(true, "STOP permits informational corrections");
await Reject(() => { stopped.ValidateSuccessor(signal); return Task.CompletedTask; }, "STOP cannot reopen");
await Reject(() => { signal.ValidateSuccessor(signal with { Strategy="other" }); return Task.CompletedTask; }, "Signal identity cannot change");
await Reject(() => { (message with { Revision=0 }).Validate(); return Task.CompletedTask; }, "Invalid envelope rejected");
var ack = new SignalPublicationAck(message.ProducerId,message.MessageId,signal.Tag,1,signal.Hash(),9);
Check(ack.Matches(message) && !(ack with { Revision=2 }).Matches(message), "Acknowledgements bind identity, revision and content");
var ingressOptions = new SignalIngressOptions { Enabled=true, ProducerId="test-site", Subject="svc-user", Strategies=["test"] };
ingressOptions.Validate();
var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,"svc-user")],"test"));
Check(ingressOptions.Allows(principal,message) && !ingressOptions.Allows(principal,message with {ProducerId="other"})
    && !ingressOptions.Allows(principal,message with {Signal=signal with {Strategy="other"}})
    && !ingressOptions.Allows(new ClaimsPrincipal(),message), "Ingress checks authenticated subject, producer and strategy");
Check(PrivateSignalControllerClient.IsHealthRequest("GET","/net/ping")
    && !PrivateSignalControllerClient.IsHealthRequest("GET","/api/studies")
    && !PrivateSignalControllerClient.IsHealthRequest("POST","/net/ping")
    && !PrivateSignalControllerClient.IsHealthRequest("GET","/net/ping/../api/studies"), "Private host only accepts exact health routes");

if (!args.Contains("--sql")) { Console.WriteLine($"{passed} checks passed. Add --sql for disposable LocalDB integration tests."); return; }
var root = new DirectoryInfo(Environment.CurrentDirectory);
while (root != null && !File.Exists(Path.Combine(root.FullName,"FIGCommon/DataSchema/signal_publication_source.sql"))) root=root.Parent;
if (root == null) throw new Exception("Run from the repository root.");
const string master = @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;TrustServerCertificate=true";
var prefix="FIGSignalSyncTest_"+Guid.NewGuid().ToString("N");
var sourceDb=prefix+"_source"; var receiverDb=prefix+"_receiver";
string Connection(string db) => new SqlConnectionStringBuilder(master) { InitialCatalog=db }.ConnectionString;
async Task<object?> Sql(string cs, string text)
{
    await using var c=new SqlConnection(cs); await c.OpenAsync();
    await using var cmd=new SqlCommand(text,c) {CommandTimeout=60}; return await cmd.ExecuteScalarAsync();
}
async Task Migration(string cs,string name)
{
    var sql=await File.ReadAllTextAsync(Path.Combine(root.FullName,"FIGCommon/DataSchema/"+name));
    await Script(cs,sql);
}
async Task Script(string cs,string sql)
{
    await using var connection=new SqlConnection(cs); await connection.OpenAsync();
    foreach (var batch in Regex.Split(sql,@"^\s*GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))
        if (!string.IsNullOrWhiteSpace(batch))
        {
            await using var command=new SqlCommand(batch,connection){CommandTimeout=60};
            await command.ExecuteNonQueryAsync();
        }
}
string Insert(string tag) => $"INSERT dbo.Signal(Strategy,Tag,Status,Side,StartTime,StartPrice,LastUpdated) VALUES('test','{tag}','START',1,100,10,100);";
try
{
    await Sql(master,$"CREATE DATABASE [{sourceDb}]"); await Sql(master,$"CREATE DATABASE [{receiverDb}]");
    var source=Connection(sourceDb); var target=Connection(receiverDb);
    var existingSchema=await File.ReadAllTextAsync(Path.Combine(root.FullName,"FIGCommon/DataSchema/FIGAutoTrader_Schema.sql"));
    // The only removed batch selects the production database. All DDL and seed data are executed unchanged.
    existingSchema=Regex.Replace(existingSchema,@"^USE FIGAutoTrader\s*\r?\nGO\s*\r?\n","",RegexOptions.IgnoreCase);
    if (Regex.IsMatch(existingSchema,@"^\s*(USE|CREATE DATABASE|ALTER DATABASE)\b",RegexOptions.Multiline|RegexOptions.IgnoreCase))
        throw new Exception("Unexpected database selection/management in schema fixture");
    await Script(source,existingSchema); await Script(target,existingSchema);
    Check(true,"Authoritative full schema installed in both isolated databases");
    foreach(var cs in new[]{source,target})
        await Sql(cs,"INSERT dbo.StudyCol(TickerId,IntervalId,ColType,ColParams,Enabled) VALUES(1,'1','test','{}',0); INSERT dbo.StrategyConfig VALUES('test',CONVERT(int,SCOPE_IDENTITY()));");
    await Migration(source,"signal_publication_source.sql"); await Migration(source,"signal_publication_source.sql");
    await Migration(target,"signal_publication_receiver.sql"); await Migration(target,"signal_publication_receiver.sql");
    Check(true,"Both migrations are repeatable");
    foreach(var cs in new[]{source,target})
        Check(await Sql(cs,"SELECT COL_LENGTH('dbo.Signal','Canceled')") is DBNull,
            "Migration does not add a cancellation column in "+(cs==source?"source":"receiver"));
    await Sql(source,Insert(signal.Tag));
    Check(Convert.ToInt32(await Sql(source,"SELECT COUNT(*) FROM dbo.SignalPublicationOutbox"))==0,"Source migration is inactive until configured");
    var store=new SignalPublicationStore(()=>source); var receiver=new SignalIngressStore(()=>target);
    await store.InitializeAsync("test-site",default); await store.InitializeAsync("test-site",default);
    Check(Convert.ToInt32(await Sql(source,"SELECT COUNT(*) FROM dbo.SignalPublicationOutbox"))==1,"Bootstrap captures existing rows exactly once");
    await Reject(()=>store.InitializeAsync("different-site",default),"Producer identity cannot change after restart");
    var first=(await store.ReadAsync(0,100,true,default)).Single();
    var firstAck=await receiver.ReceiveAsync(first.Message,default);
    Check(Convert.ToInt32(await Sql(target,"SELECT COUNT(*) FROM dbo.Event WHERE EventType='SIGNAL' AND EventId='test'"))==1,"Receipt activates the existing AutoTrader SIGNAL event path");
    await Sql(target,"UPDATE dbo.Event SET TimeStamp='20000101'");
    var retryAck=await receiver.ReceiveAsync(first.Message,default); // simulate lost response
    Check(firstAck.CentralSignalId==retryAck.CentralSignalId && Convert.ToInt32(await Sql(target,"SELECT COUNT(*) FROM dbo.Signal"))==1,"Lost acknowledgement and duplicate delivery do not duplicate signals");
    Check(Convert.ToInt32(await Sql(target,"SELECT YEAR(TimeStamp) FROM dbo.Event"))==2000,"Duplicate delivery does not retrigger trading notifications");
    var concurrent=await Task.WhenAll(Enumerable.Range(0,4).Select(_=>receiver.ReceiveAsync(first.Message,default)));
    Check(concurrent.All(a=>a.CentralSignalId==firstAck.CentralSignalId),"Concurrent receiver requests are idempotent");
    await Sql(source,"UPDATE dbo.Signal SET StartPrice=11,LastUpdated=101 WHERE Id=1");
    await store.AcknowledgeAsync(first,firstAck,default);
    Check(Convert.ToInt32(await Sql(source,"SELECT CONVERT(int,IsSynced) FROM dbo.Signal WHERE Id=1"))==0,"Late acknowledgement cannot mark a newer update synced");
    var second=(await store.ReadAsync(0,100,true,default)).Single();
    var secondAck=await receiver.ReceiveAsync(second.Message,default); await store.AcknowledgeAsync(second,secondAck,default);
    Check((await store.ReadAsync(0,100,true,default)).Count==0 && Convert.ToInt32(await Sql(source,"SELECT COUNT(*) FROM dbo.SignalPublicationOutbox"))==2,"Acknowledgement does not create new revisions");
    await Reject(()=>receiver.ReceiveAsync(first.Message,default),"Out-of-order older revision cannot overwrite new data");
    await Reject(()=>receiver.ReceiveAsync(second.Message with {Signal=second.Message.Signal with {StartPrice=99}},default),"Same revision with altered content is rejected");
    await Sql(source,"UPDATE dbo.Signal SET Status='STOP',StopTime=200,StopPrice=12,LastUpdated=200 WHERE Id=1");
    var final=(await store.ReadAsync(0,100,true,default)).Single();
    await store.AcknowledgeAsync(final,await receiver.ReceiveAsync(final.Message,default),default);
    await Reject(()=>Sql(source,"UPDATE dbo.Signal SET Status='START',StopTime=NULL WHERE Id=1"),"Source enforces final STOP state");
    await Reject(()=>receiver.ReceiveAsync(final.Message with {Revision=final.Message.Revision+1,Signal=signal},default),"Receiver independently enforces final STOP state");
    await Sql(source,"UPDATE dbo.Signal SET StopPrice=13,LastUpdated=201 WHERE Id=1");
    var corrected=(await store.ReadAsync(0,100,true,default)).Single();
    await store.AcknowledgeAsync(corrected,await receiver.ReceiveAsync(corrected.Message,default),default);
    Check(Convert.ToDecimal(await Sql(target,"SELECT StopPrice FROM dbo.Signal"))==13,"Post-STOP price correction is synchronized");
    await Sql(target,"UPDATE dbo.Signal SET StopPrice=99");
    var transport=new InProcessClient(receiver);
    using var monitor=new EventMonitorService();
    using var worker=new SignalPublicationService(store,transport,new SignalPublicationOptions{BatchSize=1},new SignalPublicationWakeup(),monitor,NullLogger<SignalPublicationService>.Instance);
    await worker.SweepAsync(false,default);
    Check(Convert.ToDecimal(await Sql(target,"SELECT StopPrice FROM dbo.Signal"))==13,"Full reconciliation repairs drift in acknowledged STOP records");
    await Sql(target,"DELETE dbo.Signal"); await worker.SweepAsync(false,default);
    Check(Convert.ToInt32(await Sql(target,"SELECT COUNT(*) FROM dbo.Signal"))==1,"Reconciliation restores a deleted materialized signal");
    await Sql(target,"DELETE dbo.Signal"); await Sql(target,Insert(signal.Tag));
    await worker.SweepAsync(false,default);
    Check(Convert.ToInt32(await Sql(target,"SELECT COUNT(*) FROM dbo.Signal"))==1
        && Convert.ToString(await Sql(target,"SELECT Status FROM dbo.Signal"))=="STOP","Reconciliation adopts a reinserted GUID even when its identity number changed");
    await store.RecordFailureAsync(corrected,"offline",default);
    Check(Convert.ToInt32(await Sql(source,"SELECT CONVERT(int,IsSynced) FROM dbo.Signal WHERE Id=1"))==0,"Failed verification exposes unsynchronized status");
    var restarted=new SignalPublicationStore(()=>source); await restarted.InitializeAsync("test-site",default);
    Check((await restarted.ReadAsync(0,100,true,default)).Count==1,"Pending records survive publisher restart");
    await worker.SweepAsync(true,default);
    Check((await store.ReadAsync(0,100,true,default)).Count==0,"Pending retry recovers after outage");
    await Sql(source,"BEGIN TRANSACTION; UPDATE dbo.Signal SET StopPrice=14 WHERE Id=1; ROLLBACK;");
    Check(Convert.ToDecimal(await Sql(source,"SELECT StopPrice FROM dbo.Signal WHERE Id=1"))==13
        && Convert.ToInt64(await Sql(source,"SELECT SyncVersion FROM dbo.Signal WHERE Id=1"))==corrected.Message.Revision,"Rollback rolls back signal and delivery revision atomically");
    await Sql(source,"UPDATE dbo.Signal SET StopPrice=StopPrice WHERE Id=1");
    Check(Convert.ToInt64(await Sql(source,"SELECT SyncVersion FROM dbo.Signal WHERE Id=1"))==corrected.Message.Revision,"No-op business update does not create a revision");
    await Reject(()=>Sql(source,"DELETE dbo.Signal WHERE Id=1"),"Published source rows cannot be silently deleted");
    await Sql(source,$"INSERT dbo.Signal(Strategy,Tag,Status,Side,StartTime,LastUpdated) VALUES('test','{Guid.NewGuid()}','START',1,300,300),('test','{Guid.NewGuid()}','START',-1,400,400)");
    Check((await store.ReadAsync(0,100,true,default)).Count==2,"Multirow changes for the same strategy are captured");
    transport.RejectTag=(await store.ReadAsync(0,100,true,default))[0].Message.Signal.Tag;
    await worker.SweepAsync(true,default);
    Check((await store.ReadAsync(0,100,true,default)).Count==1,"Rejected signal does not starve other pending records across pages");
    transport.RejectTag=null; await worker.SweepAsync(true,default);
    Check((await store.ReadAsync(0,100,true,default)).Count==0,"Previously rejected signal remains retryable");
    var adoptedTag=Guid.NewGuid().ToString();
    await Sql(target,Insert(adoptedTag));
    var adoptedId=Convert.ToInt32(await Sql(target,$"SELECT Id FROM dbo.Signal WHERE Tag='{adoptedTag}'"));
    var adopted=await receiver.ReceiveAsync(message with {Signal=signal with {Tag=adoptedTag}},default);
    Check(adopted.CentralSignalId==adoptedId,"Transition from shared database adopts existing GUID without duplication");
    await Reject(()=>receiver.ReceiveAsync(message with {Signal=signal with {Tag=Guid.NewGuid().ToString(),Strategy="unconfigured"}},default),"Missing central execution metadata fails before inserting signal");
    var offlineTag=Guid.NewGuid().ToString();
    await Sql(source,Insert(offlineTag));
    await Sql(source,$"UPDATE dbo.Signal SET Status='STOP',StopTime=500,LastUpdated=500 WHERE Tag='{offlineTag}'");
    var offline=(await store.ReadAsync(0,100,true,default)).Single();
    Check(offline.Message.Signal.Status=="STOP" && offline.Message.Revision==2,"Reconnect publishes latest STOP directly instead of replaying obsolete START");
    foreach(var cs in new[]{source,target})
    {
        var alertsBefore=Convert.ToInt32(await Sql(cs,"SELECT COUNT(*) FROM dbo.SystemAlert"));
        var procTag=Guid.NewGuid().ToString();
        await Sql(cs,$"""
            DECLARE @Id int;
            EXEC dbo.usp_signal_upsert @IdNew=@Id OUTPUT,@Id=-1,@Strategy='test',@Tag='{procTag}',
                @Status='START',@Side=1,@StartTime=600,@StopTime=NULL,@StartPrice=10,@StopPrice=NULL,@LastUpdated=600;
            EXEC dbo.usp_signal_upsert @IdNew=@Id OUTPUT,@Id=@Id,@Strategy='test',@Tag='{procTag}',
                @Status='STOP',@Side=1,@StartTime=600,@StopTime=700,@StartPrice=10,@StopPrice=11,@LastUpdated=700;
            """);
        Check(Convert.ToInt32(await Sql(cs,"SELECT COUNT(*) FROM dbo.SystemAlert"))==alertsBefore+2,
            "Legacy upsert retains START and STOP alerts in "+(cs==source?"source":"receiver"));
        await using var checkConnection=new SqlConnection(cs); await checkConnection.OpenAsync();
        foreach(var procedure in new[]{"usp_signal_select","usp_signal_query_last"})
        {
            await using var checkCommand=new SqlCommand(procedure,checkConnection){CommandType=System.Data.CommandType.StoredProcedure};
            if(procedure=="usp_signal_select") checkCommand.Parameters.AddWithValue("@Id",Convert.ToInt32(await Sql(cs,$"SELECT Id FROM dbo.Signal WHERE Tag='{procTag}'")));
            else checkCommand.Parameters.AddWithValue("@StrategyName","test");
            await using var checkReader=await checkCommand.ExecuteReaderAsync(); await checkReader.ReadAsync();
            var mapped=new SignalRS().CreateFromSqlDataReader(checkReader);
            Check(mapped.LastUpdated==700 && mapped.Status=="STOP",procedure+" matches application mapping in "+(cs==source?"source":"receiver"));
        }
    }
    foreach(var cs in new[]{source,target})
    {
        var countBefore=Convert.ToInt32(await Sql(cs,"SELECT COUNT(*) FROM dbo.Signal"));
        await Sql(cs,"ALTER TABLE dbo.Signal ADD Canceled bit NOT NULL DEFAULT 0;");
        await Sql(cs,"CREATE OR ALTER PROCEDURE dbo.usp_signal_select @Id int AS SELECT Id,Strategy,Tag,Status,Side,StartTime,StopTime,StartPrice,StopPrice,Canceled,LastUpdated FROM dbo.Signal WHERE Id=@Id OR @Id=-1;");
        await Migration(cs,"signal_publication_remove_canceled.sql");
        await Migration(cs,"signal_publication_remove_canceled.sql");
        Check(await Sql(cs,"SELECT COL_LENGTH('dbo.Signal','Canceled')") is DBNull
            && Convert.ToInt32(await Sql(cs,"SELECT COUNT(*) FROM dbo.Signal"))==countBefore,
            "Cleanup removes column repeatably without deleting signals in "+(cs==source?"source":"receiver"));
        Check(!Convert.ToString(await Sql(cs,"SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.usp_signal_select'))"))!.Contains("Canceled"),
            "Cleanup restores baseline signal procedure in "+(cs==source?"source":"receiver"));
    }
    await Sql(source,"UPDATE dbo.SignalPublicationOutbox SET Payload=JSON_MODIFY(Payload,'$.Canceled',CAST(0 AS bit));");
    await Sql(target,"UPDATE dbo.SignalPublicationReceipt SET Payload=JSON_MODIFY(Payload,'$.Canceled',CAST(0 AS bit)),PayloadHash=REPLICATE('0',64);");
    await worker.SweepAsync(false,default);
    Check((await store.ReadAsync(0,100,true,default)).Count==0,"Historical payloads and receipt hashes reconcile after removing obsolete field");
    await using var connection=new SqlConnection(source); await connection.OpenAsync();
    await using var cmd=new SqlCommand("SELECT Id,Strategy,Tag,Status,Side,StartTime,StopTime,StartPrice,StopPrice,LastUpdated FROM dbo.Signal WHERE Id=1",connection);
    await using var reader=await cmd.ExecuteReaderAsync(); await reader.ReadAsync();
    var record=new SignalRS().CreateFromSqlDataReader(reader);
    Check(record.LastUpdated==201,"SignalRS reads the current schema without a cancellation column");
    Console.WriteLine($"All {passed} checks passed, including SQL integration.");
}
finally
{
    SqlConnection.ClearAllPools();
    foreach(var db in new[]{sourceDb,receiverDb})
        await Sql(master,$"IF DB_ID('{db}') IS NOT NULL BEGIN ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{db}]; END");
}

sealed class InProcessClient(ISignalIngressStore receiver) : ISignalPublicationClient
{
    public string? RejectTag {get;set;}
    public Task<SignalPublicationAck> PublishAsync(SignalPublicationMessage message,CancellationToken ct)
        => message.Signal.Tag==RejectTag ? throw new InvalidOperationException("Simulated unavailable strategy") : receiver.ReceiveAsync(message,ct);
}
