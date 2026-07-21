using FIGCommon.Models;
using FIGCommon.Services;

namespace FIGPriceSyncSvc.Services
{
    public class PriceChangeMonitorService : IHostedService, IDisposable
    {
        private readonly PriceSyncService _syncService;
        private readonly ILogger<PriceChangeMonitorService> _logger;
        private readonly EventMonitorService _eventMonitor;


        public PriceChangeMonitorService(
            ILogger<PriceChangeMonitorService> logger,
            PriceSyncService syncService,
            EventMonitorService eventMonitor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventMonitor = eventMonitor ?? throw new ArgumentNullException(nameof(eventMonitor));
            _syncService = syncService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // subscribe to data PriceData deletion only
            _eventMonitor.Subscribe("PRICEDATA_DEL", OnPriceDeletion);
            _logger.LogInformation("Subscribed to PRICEDATA changes");
            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _eventMonitor.Unsubscribe("PRICEDATA_DEL", OnPriceDeletion);
            _logger.LogInformation("Unsubscribed from PRICEDATA changes");
            return Task.CompletedTask;
        }

        private void OnPriceDeletion(EventRS? eventData)
        {
            try
            {
                _logger.LogInformation("Received deletion of records for dataset {0}", eventData?.EventId ?? "-1");
                // notify Price Sync service to refresh
                _syncService.Refresh();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing EVENTA change");
            }
        }

        public void Dispose()
        {
            // already cleaned up... check
            //_eventMonitor.Unsubscribe("PRICEDATA", OnPriceChange);
            _logger.LogInformation("Unsubscribed from PRICEDATA changes");
        }

    }
}