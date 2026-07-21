using FIGCommon.Models.FIGProviderAPI;

namespace FIGAutoTradeExSvc.Services
{
    public interface IOrderManagementService : IHostedService
    {
        Task CancelOrderAsync(int? id, int attempt = 0);
        Task <bool> PlaceOrderAsync(int? id, int attempt = 0);

        bool UpdateOrderStatus(OrderStatusDto orderStatus);
    }
}
