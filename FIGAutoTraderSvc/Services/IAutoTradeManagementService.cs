using FIGCommon.Models;

namespace FIGAutoTradeExSvc.Services
{
    public interface IAutoTradeManagementService : IHostedService
    {
        void Wakeup(int autoTradeId);
        void WakeupAll();
        EAutoTradeStatus StartExec(int autoTradeId);
        EAutoTradeStatus StopExec(int autoTradeId);
        AutoTradeExecStatus? GetExecStatus(int autoTradeId);
        List<AutoTradeExecStatus> GetExecStatusFromList(List<int> ids);

    }
}
