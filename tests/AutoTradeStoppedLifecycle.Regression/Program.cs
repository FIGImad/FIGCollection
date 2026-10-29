using FIGAutoTradeExSvc.Services;
using FIGCommon.DataAccess;
using FIGCommon.Models;
using FIGCommon.Services;
using Microsoft.Extensions.Logging.Abstractions;

// Execute the production manager and locking code against in-memory boundary doubles.
var provider = new ServiceCollection().AddTransient<AutoTradeLiveService>().BuildServiceProvider();
var manager = new AutoTradeManagementService(NullLogger<AutoTradeManagementService>.Instance, provider, new Scheduler());
MainRepo.Rows.Add(new() { Id = 58, Status = (int)EAutoTradeStatus.Stopped });
MainRepo.Rows.Add(new() { Id = 48, Status = (int)EAutoTradeStatus.Started });
await manager.StartAsync(default);
Check(MainRepo.GetAutoTrade(58)!.Status == (int)EAutoTradeStatus.Stopped, "Startup preserves stopped status");
Check(MainRepo.Events.IndexOf("reconcile:58:Stopped") < MainRepo.Events.IndexOf("start:48"), "Startup reconciles stopped records before live instances start");
MainRepo.Events.Clear();
manager.WakeupAll();
Check(MainRepo.Events.SequenceEqual(new[] { "reconcile:58:Stopped", "wake:48" }), "Signal notifications reconcile stopped records without live instances and wake the running peer");
MainRepo.Events.Clear();
Check(manager.StartExec(58) == EAutoTradeStatus.Started, "Stopped instance restarts");
Check(MainRepo.Events.IndexOf("reconcile:58:Stopped") < MainRepo.Events.IndexOf("start:58"), "Restart reconciles before status becomes Started");
MainRepo.Events.Clear();
manager.StopExec(58);
Check(MainRepo.Events.SequenceEqual(new[] { "stop:58", "reconcile:58:Stopped" }), "Stop completes before reconciliation");
MainRepo.Events.Clear();
manager.WakeupAll();
Check(!MainRepo.Events.Contains("wake:58") && MainRepo.Events.Contains("wake:48"), "Stopped instance remains stopped while peer continues");
MainRepo.ThrowOnReconcile = 58;
MainRepo.Events.Clear();
manager.WakeupAll();
Check(MainRepo.Events.Contains("wake:48"), "Reconciliation failure does not prevent running peer wakeup");
Check(manager.StartExec(58) == EAutoTradeStatus.Failed, "Restart fails closed when catch-up fails");
MainRepo.ThrowOnReconcile = null;
MainRepo.Events.Clear();
manager.WakeupAll();
Check(MainRepo.Events.Contains("reconcile:58:Stopped"), "Later notification retries reconciliation");

static void Check(bool result, string name)
{
    if (!result) throw new Exception(name);
    Console.WriteLine($"PASS: {name}");
}

namespace FIGCommon.Models
{
    public enum EAutoTradeStatus { Idle, Unverified, Started, Stopped, Failed, Completed, NotAvailable }
    public class AutoTradeRS { public int Id { get; set; } public int Status { get; set; } public int BotId { get; set; } }
    public class AutoTradeExecStatus { }
}
namespace FIGCommon.DataAccess
{
    public static class MainRepo
    {
        public static readonly List<AutoTradeRS> Rows = new();
        public static readonly List<string> Events = new();
        public static int? ThrowOnReconcile;
        public static List<AutoTradeRS> GetAutoTrades() => Rows;
        public static AutoTradeRS? GetAutoTrade(int id) => Rows.Find(x => x.Id == id);
        public static void UpsertAutoTrade(AutoTradeRS row) { }
        public static void TouchEvent(string kind, string id) { }
        public static int FailStoppedAutoTradeSignals(int id)
        {
            Events.Add($"reconcile:{id}:{(EAutoTradeStatus)GetAutoTrade(id)!.Status}");
            if (ThrowOnReconcile == id) throw new Exception("Simulated database failure");
            return 0;
        }
    }
}
namespace FIGCommon.Services
{
    public interface ITaskSchedulerService { Task StopAllTasksAsync(string owner); }
    public class Scheduler : ITaskSchedulerService { public Task StopAllTasksAsync(string owner) => Task.CompletedTask; }
}
namespace FIGAutoTradeExSvc.Services
{
    public class AutoTradeLiveService : IAutoTradeService
    {
        private AutoTradeRS row = null!;
        public bool IsRunning => row.Status == (int)EAutoTradeStatus.Started;
        public void Init(AutoTradeRS? value) => row = value!;
        public EAutoTradeStatus Start()
        {
            MainRepo.Events.Add($"start:{row.Id}");
            row.Status = (int)EAutoTradeStatus.Started;
            return EAutoTradeStatus.Started;
        }
        public EAutoTradeStatus Stop()
        {
            MainRepo.Events.Add($"stop:{row.Id}");
            row.Status = (int)EAutoTradeStatus.Stopped;
            return EAutoTradeStatus.Stopped;
        }
        public void WakeUp() { if (IsRunning) MainRepo.Events.Add($"wake:{row.Id}"); }
        public AutoTradeExecStatus? GetExecStatus() => new();
    }
}
