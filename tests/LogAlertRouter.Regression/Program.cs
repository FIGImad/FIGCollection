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
Console.WriteLine($"Passed {checks} checks.");

sealed class FakeTransport : IClientSignalRService
{
    public Queue<int> Failures { get; } = new();
    public List<ControllerRouteRequest> Requests { get; } = new();
    public Task WaitForConnectionAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.CompletedTask; }
    public Task<string> SendToHostAsync(ControllerRouteRequest request, CancellationToken ct)
    {
        if (LogAnalyzerSink.SuppressionDepth == 0) throw new Exception("Forwarding must remain suppressed during retries");
        Requests.Add(request);
        if (Failures.TryDequeue(out int status)) throw new AppErrorException(status, "", "Rejected");
        return Task.FromResult("");
    }
}
