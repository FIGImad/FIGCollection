//using FIGPriceSyncService.Services;
//using FIGPriceSyncSvc.Services;
//using Microsoft.AspNetCore.SignalR;
//using System.Collections.Concurrent;
//using FIGCommon.Interfaces;
//using FIGCommon.Models.FIGProviderAPI;
//using FIGCommon.Models.FIGBroker;

//namespace FIGBrokerSvc.Services
//{
//    public interface IMarketDataTrackerService
//    {
//    }

//    public class MarketDataTrackerService : BackgroundService, IMarketDataTrackerService
//    {
//        private readonly ConcurrentQueue<(IProviderService providerService, int orderId)> _queue = new();
//        private readonly ConcurrentDictionary<int, MarketDataDto> _latestMarketData = new();
//        private readonly ILogger<MarketDataTrackerService> _logger;
//        private readonly IServiceProvider _serviceProvider;
//        private readonly ICallbackQueue _callbackQueue;
//        private readonly PriceSyncService _priceSyncSvc;
//        private readonly ATSAdminAPIService _atsAdminAPISvc;

//        private readonly IHubContext<MarketDataMonitorHub> _hubContext;

//        public MarketDataTrackerService(ILogger<MarketDataTrackerService> logger,
//                                   IServiceProvider serviceProvider,
//                                   ICallbackQueue queue,
//                                   PriceSyncService providerSvc,
//                                   IHubContext<MarketDataMonitorHub> hubContext,
//                                   ATSAdminAPIService atsAdminAPISvc
//                                   )
//        {
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
//            _callbackQueue = queue ?? throw new ArgumentNullException(nameof(queue));
//            _priceSyncSvc = providerSvc ?? throw new ArgumentNullException(nameof(providerSvc));
//            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
//            _atsAdminAPISvc = atsAdminAPISvc ?? throw new ArgumentNullException(nameof(atsAdminAPISvc));
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            try
//            {
//                _logger.LogInformation("MarketDataTrackerService starting");
//                await Task.Delay(2000, stoppingToken);

//                _ = SendMarketDataLoop(stoppingToken);

//                await foreach (var message in _callbackQueue.Subscribe(BrokerCallbackMessageType.MarketData, BrokerCallbackMessageType.Error).WithCancellation(stoppingToken))
//                {
//                    await HandleMessageAsync(message, stoppingToken);
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "MarketDataTrackerService encountered an error: {Message}", ex.Message);
//            }

//            // do startup work here
//            await Task.CompletedTask;
//        }


//        private async Task HandleMessageAsync(BrokerCallbackMessage message, CancellationToken cancellationToken)
//        {
//            switch (message.Type)
//            {
//                case BrokerCallbackMessageType.MarketData:
//                    // handle order status update
//                    await HandleMarketData(message);
//                    break;
//                case BrokerCallbackMessageType.Error:
//                    // handle order status error
//                    ErrorResponseDto? err = message.DataJson?.ToObject<ErrorResponseDto>();
//                    await HandleError(err);
//                    break;
//                default:
//                    break;
//            }
//        }


//        private async Task HandleError(ErrorResponseDto? err)
//        {
//        }

//        private async Task HandleMarketData(BrokerCallbackMessage message)
//        {
//            MarketDataDto? marketData = message.DataJson?.ToObject<MarketDataDto>();

//            if (marketData == null) return;

//            // get Ticker from TickerId
//            FIGCommon.Models.TickerRS? ticker = _priceSyncSvc.GetMarketDataTicker(marketData.TickerId);

//            if (ticker == null) return;

//            marketData.TickerId = ticker.Id;

//            //await _hubContext.Clients.All.SendAsync("marketdata", marketData, null);
//            // send every 5 seconds

//            _latestMarketData[marketData.TickerId] = marketData;
           
//        }

//        private async Task SendMarketDataLoop(CancellationToken ct)
//        {
//            while (!ct.IsCancellationRequested)
//            {
//                try
//                {
//                    var snapshot = _latestMarketData.Values.ToList();

//                    if (snapshot.Count > 0)
//                    {
//                        foreach (var md in snapshot)
//                        {
//                            await _atsAdminAPISvc.SendMarketData(md);
//                            //await _hubContext.Clients.All.SendAsync("marketdata", md, null);
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Error sending market data");
//                }

//                await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
//            }
//        }

//    }
//}
