using FIGCommon.Models;

namespace FIGAutoTradeExSvc.Services
{
    public interface IAutoTradeService
    {
        // AutoTrade Control functions
        void Init(AutoTradeRS? autoTrade);
        EAutoTradeStatus Start();
        EAutoTradeStatus Stop();
        void WakeUp();

        // AutoTrade Status and flags
        bool IsRunning { get; }

        // APIs
        AutoTradeExecStatus? GetExecStatus();

    }
}