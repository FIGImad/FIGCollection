using System.Net;
using FIGAlertSvc.Services;
using FIGCommon.Models.Alert;
using FIGCommon.DataAccess;
using Microsoft.Extensions.Logging.Abstractions;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]

int assertions = 0;
void Check(bool condition, string name)
{ if (!condition) throw new Exception(name); assertions++; }
var time = DateTimeOffset.Parse("2026-09-15T15:47:16Z");
var row = new PendingAlertRS { RawTime = (int)time.ToUnixTimeSeconds(), MSec = 123,
    TimeZoneId = "Eastern Standard Time", FriendlyMessage = "Alert: Source: SIGNAL, Time: 2026-09-15 15:47:16.000\nUTC: Apr 15 2025 3:35AM" };
Check(AlertMessageFormatter.Format(row).Contains("11:47:16.123 -04:00"), "Eastern DST conversion");
Check(AlertMessageFormatter.Format(row).EndsWith("UTC: Apr 15 2025 3:35AM"), "Signal body preserved");
row.RawTime = (int)DateTimeOffset.Parse("2026-01-15T15:47:16Z").ToUnixTimeSeconds();
Check(AlertMessageFormatter.Format(row).Contains("10:47:16.123 -05:00"), "Eastern winter conversion");
row.TimeZoneId = "Arabian Standard Time";
Check(AlertMessageFormatter.Format(row).Contains("19:47:16.123 +04:00"), "Dubai conversion");
row.TimeZoneId = "UTC"; row.FriendlyMessage = "At {{alert_time}}";
Check(AlertMessageFormatter.Format(row).Contains("15:47:16.123 +00:00"), "Template timestamp");
var original = AlertMessageFormatter.OriginalTime("FIGAutoTradeExSvc.Services.SystemAlertMonitorService",
    "Source: SIGNAL, Time: 2026-09-15 15:47:16.000\nbody");
Check(original == time, "Original UTC time from replay header");
Check(AlertMessageFormatter.OriginalTime("Other", "Source: SIGNAL, Time: 2026-09-15 15:47:16.000") == null, "Other producers not parsed");
Check(PushOverMessageService.SplitMessage(new string('a',1024)).Single().Html, "Boundary preserves HTML");
var text = new string('x',1023) + "😀" + new string('y',1024);
var parts = PushOverMessageService.SplitMessage("<b>" + text + "</b>");
Check(parts.All(p => p.Text.Length <= 1024 && !p.Html), "Every part within API limit");
Check(string.Concat(parts.Select(p => p.Text)) == text, "All text preserved");
Check(parts.All(p => !char.IsHighSurrogate(p.Text[^1]) && !char.IsLowSurrogate(p.Text[0])), "Unicode pair preserved");
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
{ ["PushOver:APIToken"] = "test-token", ["AlertDispatch:MaxAlertAgeMinutes"] = "17" }).Build();
var handler = new FakeHandler();
var sender = new PushOverMessageService(NullLogger<PushOverMessageService>.Instance, config, new HttpClient(handler));
await sender.Send("test-user", "Test", text);
Check(handler.Calls == 3, "Every part posted");
foreach (var response in new[] { (HttpStatusCode.BadRequest, "{\"status\":0,\"errors\":[\"bad user\"],\"request\":\"req1\"}"),
    (HttpStatusCode.OK, "{\"status\":0}"), (HttpStatusCode.BadGateway, "not json") })
{
    handler.Code = response.Item1; handler.Body = response.Item2;
    bool failed = false;
    try { await sender.Send("test-user", "Test", "message"); } catch (InvalidOperationException) { failed = true; }
    Check(failed, "Failed HTTP/API/malformed responses propagate");
}
handler.Code = HttpStatusCode.OK; handler.Body = "{\"status\":1,\"request\":\"req2\"}";
var services = new ServiceCollection().AddSingleton(sender).AddSingleton<SmtpMessageService>().BuildServiceProvider();
var dispatcher = new AlertDispatchService(NullLogger<AlertDispatchService>.Instance, services, config);
AlertRepo.Rows = [new() { AlertId = 1, RecipientId = 1 }, new() { AlertId = 1, RecipientId = 2 }, new() { AlertId = 2, RecipientId = 1 }];
handler.Calls = 0;
await dispatcher.DispatchImmediateAlertsAsync(default);
Check(handler.Calls == 3 && AlertRepo.Sent.SetEquals([1,2]), "Individual sends and all-recipient completion");
Check(AlertRepo.Lookback == 17, "Immediate configured age passed to SQL boundary");
AlertRepo.Sent.Clear(); handler.FailAt = handler.Calls + 2;
await dispatcher.DispatchImmediateAlertsAsync(default);
Check(!AlertRepo.Sent.Contains(1) && AlertRepo.Sent.Contains(2), "Partial recipient failure does not mark alert sent; other alerts proceed");
AlertRepo.Rows = [new() { AlertId = 3, TimeZoneId = "bad-zone" }, new() { AlertId = 4 }];
await dispatcher.DispatchImmediateAlertsAsync(default);
Check(!AlertRepo.Sent.Contains(3) && AlertRepo.Sent.Contains(4), "Bad timezone isolated to affected recipient");
AlertRepo.Rows = [new() { AlertId = 5, RecipientEnabled = false }];
int before = handler.Calls;
await dispatcher.DispatchImmediateAlertsAsync(default);
Check(handler.Calls == before && !AlertRepo.Sent.Contains(5), "Disabled recipient not sent or marked");
AlertRepo.Rows = [new() { AlertId = 6, FriendlyMessage = new string('z', 2050) }];
handler.FailAt = handler.Calls + 2;
await dispatcher.DispatchImmediateAlertsAsync(default);
Check(!AlertRepo.Sent.Contains(6), "Failed later part does not mark alert sent");
row.FriendlyMessage = "Source: SIGNAL, Time: {{alert_time}}";
Check(AlertMessageFormatter.Format(row).Split("(UTC)").Length == 2, "Timestamp token formatted once");
var repeated = new List<PendingAlertRS>();
for (int i = 0; i < 11; i++) repeated.Add(new() { AlertId = 100+i, RecipientId = 1,
    FriendlyMessage = $"BOT/Broker Service at BOT - {(i % 2 == 0 ? "ALALFA" : "SEEDS")} lost connection with IBKR Trade Station. Manual intervention is required" });
var grouped = AlertBatchBuilder.Build(repeated, false);
Check(grouped.Count == 1 && grouped[0].Rows.Count == 11, "Screenshot repeats grouped in one batch");
Check(grouped[0].Message.Contains("Occurrences: 6") && grouped[0].Message.Contains("Occurrences: 5"), "Separate service counts preserved");
Check(grouped[0].Message.Split("ALALFA").Length == 2 && grouped[0].Message.Split("SEEDS").Length == 2, "Each identical message appears once");
Check(AlertBatchBuilder.Build(repeated, true).Count == 11, "Immediate duplicates stay individual");
var boundary = new List<PendingAlertRS> { new() { AlertId = 200, FriendlyMessage = new string('a', 511) },
    new() { AlertId = 201, FriendlyMessage = new string('b', 511) }, new() { AlertId = 202, FriendlyMessage = "c" } };
var packed = AlertBatchBuilder.Build(boundary, false);
Check(packed.Count == 2 && packed[0].Message.Length == 1024 && packed[1].Message == "c", "Split at entry boundaries including separators");
Check(packed.SelectMany(b => b.Rows).Select(r => r.AlertId).SequenceEqual(new[] {200,201,202}), "No alerts lost when splitting");
Check(AlertBatchBuilder.Build([new() { RecipientId = 1 },new() { RecipientId = 2 }], false).Count == 2, "Recipients isolated");
Check(AlertBatchBuilder.Build([new() { AlertMethods = 1 },new() { AlertMethods = 2 }], false).Count == 2, "Subscription channel masks isolated");
Check(AlertBatchBuilder.Build([new() { IsImmediate = true },new() { IsImmediate = true }], false).Count == 2, "Immediate fallback stays individual");
AlertRepo.Rows = repeated; AlertRepo.Sent.Clear(); handler.FailAt = -1; before = handler.Calls;
await dispatcher.DispatchPendingAlertsAsync(default);
Check(handler.Calls == before + 1 && AlertRepo.Sent.Count == 11, "Scheduled grouped dispatch marks every represented alert");
AlertRepo.Rows = boundary; AlertRepo.Sent.Clear(); handler.FailAt = handler.Calls + 2;
await dispatcher.DispatchPendingAlertsAsync(default);
Check(AlertRepo.Sent.SetEquals([200,201]), "Failed later batch does not mark its alerts; earlier batch completes");
Console.WriteLine($"Passed {assertions} regression assertions. No external messages or database writes.");

sealed class FakeHandler : HttpMessageHandler
{
    public int Calls; public int FailAt = -1;
    public HttpStatusCode Code = HttpStatusCode.OK;
    public string Body = "{\"status\":1,\"request\":\"test-request\"}";
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        Calls++;
        return Task.FromResult(new HttpResponseMessage(Calls == FailAt ? HttpStatusCode.BadRequest : Code)
        { Content = new StringContent(Calls == FailAt ? "{\"status\":0,\"errors\":[\"rejected\"]}" : Body) });
    }
}
