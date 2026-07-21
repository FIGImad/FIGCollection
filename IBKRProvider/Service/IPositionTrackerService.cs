using FIGCommon.Models.FIGProviderAPI;

namespace IBKRProvider.Service
{
    public interface IPositionTrackerService
    {
        PositionUpdate? GetPositionForTicker(TickerInfo ticker);
    }
}
