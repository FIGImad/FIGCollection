using FIGCommon.Exceptions;
using FIGCommon.Models.FIGController;
using FIGCommon.Models.LogMonitor;
using FIGCommon.Services;
using FIGCommon.Services.LogMonitor;
using FIGCommon.Utilities;
using Microsoft.Extensions.Logging.Abstractions;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
}
var transport = new FakeTransport();
using var router = new LogAlertRouter(NullLogger<LogAlertRouter>.Instance,
    new LogAnalyzerSink(), new ConfigurationBuilder().Build(), transport);
var alert = new LogAlertMessage { Message = "IBKR connection refused", Level = "Error" };
Check(await router.ForwardAlertAsync(alert, default), "Initial delivery");
transport.Failures.Enqueue(401);
Check(await router.ForwardAlertAsync(alert, default), "401 renews and delivers original alert");
Check(HttpUtils.Logins == 2 && transport.Requests.Select(r => r.Token).SequenceEqual(new[] { "token-1", "token-1", "token-2" }), "Rejected token replaced");
Check(transport.Requests[^1].Load == transport.Requests[^2].Load, "Retry preserves alert payload");
Check(await router.ForwardAlertAsync(alert, default) && HttpUtils.Logins == 2, "Renewed token reused");

transport.Failures.Enqueue(401); transport.Failures.Enqueue(401);
int before = transport.Requests.Count;
Check(!await router.ForwardAlertAsync(alert, default), "Repeated 401 fails safely");
Check(transport.Requests.Count == before + 2, "Only one retry");
Check(await router.ForwardAlertAsync(alert, default) && HttpUtils.Logins == 4, "Next alert renews second rejected token");

foreach (int status in new[] { 403, 500 })
{
    before = transport.Requests.Count;
    transport.Failures.Enqueue(status);
    Check(!await router.ForwardAlertAsync(alert, default), "Non-auth failure reported");
    Check(transport.Requests.Count == before + 1 && HttpUtils.Logins == 4, "Non-auth failure does not retry or renew");
}
transport.Failures.Enqueue(401); HttpUtils.FailLogin = true;
before = transport.Requests.Count;
Check(!await router.ForwardAlertAsync(alert, default) && transport.Requests.Count == before + 1, "Login outage does not resend rejected token");
HttpUtils.FailLogin = false;
Check(await router.ForwardAlertAsync(alert, default), "Later alert recovers after login outage");
using var canceled = new CancellationTokenSource(); canceled.Cancel();
before = transport.Requests.Count;
Check(!await router.ForwardAlertAsync(alert, canceled.Token) && transport.Requests.Count == before, "Cancellation stops forwarding");
Check(LogAnalyzerSink.SuppressionDepth == 0, "Suppression released after all paths");
var splitConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
    { ["LogAlerts:SeparateSystemAlerts"] = "true" }).Build();
using var splitRouter = new LogAlertRouter(NullLogger<LogAlertRouter>.Instance, new LogAnalyzerSink(), splitConfig, transport);
var systemAlert = new LogAlertMessage { Source = FIGCommon.Models.Alert.AlertCategory.SystemAlertProducer, Level = "ALT" };
Check(await splitRouter.ForwardAlertAsync(systemAlert, default), "Split SystemAlert forwarded");
Check(transport.Requests[^1].DestinationRole == (int)ControllerClientRole.SystemAlert, "SystemAlert routes to distinct role");
systemAlert.Level = "Error";
Check(await splitRouter.ForwardAlertAsync(systemAlert, default), "Monitor error forwarded");
Check(transport.Requests[^1].DestinationRole == (int)ControllerClientRole.Alert, "Monitor error routes to ordinary alerts");
systemAlert.Level = "ALT";
Check(await router.ForwardAlertAsync(systemAlert, default), "Legacy SystemAlert forwarded");
Check(transport.Requests[^1].DestinationRole == (int)ControllerClientRole.Alert, "Legacy routing remains combined");
systemAlert.SystemAlertId = 42;
Check(!await router.ForwardSystemAlertAsync(systemAlert, default), "Empty success response is not a durable receipt");
Check(transport.Requests[^1].DestinationRole == (int)ControllerClientRole.SystemAlert
    && transport.Requests[^1].Route == "/api/alert/systemrec", "Durable delivery always routes directly to SystemAlert");
foreach (var body in new[] { "null", "not-json", "{}", "{\"systemAlertId\":43,\"alertId\":1}", "{\"systemAlertId\":42,\"alertId\":0}" })
{
    transport.Response = body;
    Check(!await router.ForwardSystemAlertAsync(systemAlert, default), "Invalid or mismatched receipt rejected");
}
transport.Response = "{\"systemAlertId\":42,\"alertId\":123}";
Check(await router.ForwardSystemAlertAsync(systemAlert, default), "Matching persisted receipt accepted");
transport.Failures.Enqueue(500);
Check(!await router.ForwardSystemAlertAsync(systemAlert, default), "Receiver outage leaves durable delivery unacknowledged");
Check(await router.ForwardSystemAlertAsync(systemAlert, default), "Retry succeeds after receiver recovery");
transport.Failures.Enqueue(401);
Check(await router.ForwardSystemAlertAsync(systemAlert, default), "Durable receipt survives authentication renewal");
Check(transport.Requests[^1].Load == transport.Requests[^2].Load, "Durable retry preserves source identity");
var events = new EventMonitorService();
FIGCommon.DataAccess.MainRepo.Pending[42] = new() { Id = 42, RawTime = 1700000000, MSec = 123 };
transport.Response = "";
using (var monitor = new FIGAutoTradeExSvc.Services.SystemAlertMonitorService(
    NullLogger<FIGAutoTradeExSvc.Services.SystemAlertMonitorService>.Instance, events, router, new ConfigurationBuilder().Build()))
{
    await monitor.StartAsync(default);
    await WaitUntil(() => FIGCommon.DataAccess.MainRepo.Reads > 0);
    await Task.Delay(100);
    Check(FIGCommon.DataAccess.MainRepo.Pending.ContainsKey(42), "Startup recovery retains alert without receipt");
    transport.Response = "{\"systemAlertId\":42,\"alertId\":123}";
    FIGCommon.DataAccess.MainRepo.FailAcknowledgement = true;
    events.Callback!(new());
    await Task.Delay(200);
    Check(FIGCommon.DataAccess.MainRepo.Pending.ContainsKey(42), "Failed local acknowledgement preserves pending row");
    FIGCommon.DataAccess.MainRepo.FailAcknowledgement = false;
    // No new event: periodic retry must recover and acknowledge the row.
    await WaitUntil(() => !FIGCommon.DataAccess.MainRepo.Pending.ContainsKey(42), 35000);
    Check(!FIGCommon.DataAccess.MainRepo.Pending.ContainsKey(42), "Periodic retry acknowledges after recovery without new event");
    await monitor.StopAsync(default);
    Check(events.Callback == null, "Shutdown unsubscribes event callback");
}
Console.WriteLine($"Passed {checks} checks.");

static async Task WaitUntil(Func<bool> condition, int timeoutMs = 3000)
{
    var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
    while (!condition())
    {
        if (DateTime.UtcNow >= deadline) throw new Exception("Timed out waiting for monitor");
        await Task.Delay(20);
    }
}

sealed class FakeTransport : IClientSignalRService
{
    public string Response { get; set; } = "";
    public Queue<int> Failures { get; } = new();
    public List<ControllerRouteRequest> Requests { get; } = new();
    public Task WaitForConnectionAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.CompletedTask; }
    public Task<string> SendToHostAsync(ControllerRouteRequest request, CancellationToken ct)
    {
        if (LogAnalyzerSink.SuppressionDepth == 0) throw new Exception("Forwarding must remain suppressed during retries");
        Requests.Add(request);
        if (Failures.TryDequeue(out int status)) throw new AppErrorException(status, "", "Rejected");
        return Task.FromResult(Response);
    }
}
