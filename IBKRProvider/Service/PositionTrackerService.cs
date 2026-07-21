using FIGCommon.DataAccess;
using FIGCommon.Exceptions;
using FIGCommon.Interfaces;
using FIGCommon.Models.FIGBroker;
using FIGCommon.Models.FIGProviderAPI;
using IBKRProvider.Service;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace IBKRProvider.Services
{
    public class PositionTrackerService : BackgroundService, IPositionTrackerService
    {
        private readonly ConcurrentDictionary<string, PositionUpdate> _tickerPositionsMap = new();
        private readonly ILogger<PositionTrackerService> _logger;
        private readonly ICallbackQueue _callbackQueue;
        private readonly IProviderService _providerSvc;
        private readonly IProviderConnectionSignal _connectionSignal;
        private readonly SemaphoreSlim _subscriptionLock = new(1, 1);

        public PositionTrackerService(ILogger<PositionTrackerService> logger,
                                   ICallbackQueue queue,
                                   IProviderService providerSvc,
                                   IProviderConnectionSignal connectionSignal)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _callbackQueue = queue ?? throw new ArgumentNullException(nameof(queue));
            _providerSvc = providerSvc ?? throw new ArgumentNullException(nameof(providerSvc));
            _connectionSignal = connectionSignal ?? throw new ArgumentNullException(nameof(connectionSignal));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PositionTrackerService starting");

            try
            {
                await Task.Delay(5000, stoppingToken);

                var subscription = _callbackQueue.Subscribe(
                    BrokerCallbackMessageType.PositionUpdate,
                    BrokerCallbackMessageType.PositionUpdateError);

                // Subscribe immediately — provider may already be connected
                await TrySubscribeToPositionUpdates(stoppingToken);

                // Watch for disconnect/reconnect events concurrently with message processing
                _ = WatchReconnectsAsync(stoppingToken);
                _ = RunOpenOrdersTrackingLoop(stoppingToken);

                await foreach (var message in subscription.WithCancellation(stoppingToken))
                {
                    try
                    {
                        await HandleMessageAsync(message, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed handling callback message. Type={Type}", message.Type);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("PositionTrackerService stopping due to cancellation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PositionTrackerService encountered a fatal error");
            }
            

            _logger.LogInformation("PositionTrackerService TERMINATED");
        }

        private async Task TrySubscribeToPositionUpdates(CancellationToken stoppingToken)
        {
            if (!await _subscriptionLock.WaitAsync(0, stoppingToken))
            {
                // Another retry loop is already running — no need to start a duplicate
                return;
            }

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        _providerSvc?.SubscribePositions();
                        return;
                    }
                    catch (BrokerConnectionException ex)
                    {
                        _logger.LogError(ex, "Failed to subscribe to positions on startup due to connection error");
                        await Task.Delay(1000, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to subscribe to positions on startup");
                        await Task.Delay(60000, stoppingToken);
                    }
                }
            }
            finally
            {
                _subscriptionLock.Release();
            }
        }

        private async Task WatchReconnectsAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _connectionSignal.WaitForDisconnectionAsync(stoppingToken);
                _logger.LogInformation("PositionTrackerService detected provider disconnection");

                _ = TrySubscribeToPositionUpdates(stoppingToken);

                // Block here until the provider reconnects so this loop does not spin
                // while the provider is continuously disconnected.
                await _connectionSignal.WaitForConnectionAsync(stoppingToken);
            }
        }

        private async Task HandleMessageAsync(BrokerCallbackMessage message, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Received callback message. Type={message.Type}");

            if (message.Type == BrokerCallbackMessageType.PositionUpdate)
            {
                // currently ignoring market data messages in the position tracker, but can add handling logic here if needed in the future
                PositionUpdate? posUpdate = message.DataJson?.ToObject<PositionUpdate>();
                await HandlePositionUpdate(posUpdate);
            }
            else if (message.Type == BrokerCallbackMessageType.PositionUpdateError)
            {
                ErrorResponseDto? err = message.DataJson?.ToObject<ErrorResponseDto>();
                await HandleError(err);
            }
        }


        private async Task HandlePositionUpdate(PositionUpdate? posUpdate)
        {
            if (posUpdate == null || string.IsNullOrEmpty(posUpdate.AccountId) || posUpdate.Ticker == null)
            {
                _logger.LogWarning("Received invalid position update message. AccountId={AccountId}, Ticker={Ticker}",
                    posUpdate?.AccountId ?? "N/A", posUpdate?.Ticker != null ? $"{posUpdate.Ticker.Symbol}:{posUpdate.Ticker.SecType}" : "N/A");
                return;
            }

            _logger.LogInformation("Received position update. AccountId={AccountId}, Ticker={Ticker}, Position={Position}, AvgCost={AvgCost}",
                posUpdate.AccountId, $"{posUpdate.Ticker.Symbol}:{posUpdate.Ticker.SecType}", posUpdate.Pos, posUpdate.AvgCost);

            string tickerKey = posUpdate.Ticker.GetTickerKey();

            // add or update to internal position map
            _tickerPositionsMap.AddOrUpdate(tickerKey, posUpdate, (key, existing) =>
            {
                if (posUpdate.UpdateTime > existing.UpdateTime)
                {
                    return posUpdate;
                }
                else
                {
                    return existing;
                }
            });
        }

        private async Task HandleError(ErrorResponseDto? err)
        {
            if (err == null) return;

            // todo may be end Position call and call it again after some delay if we get an error, to handle transient issues.
            // Need to analyze error codes/messages to determine which are transient vs critical errors that require manual intervention
        }

        private async Task RunOpenOrdersTrackingLoop(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting to track open orders for provider account {ProviderId}", _providerSvc.Id);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    List<OrderStatusDto>? openOrderStatusList = _providerSvc.QueryAllOpenOrders(5000);
                    if (openOrderStatusList != null)
                    {
                        // reset PendingPos for all tickers before processing the open orders,
                        // since we will recalculate the pending positions based on the current open orders.
                        // A successful empty response means there are no pending orders left.
                        foreach (var key in _tickerPositionsMap.Keys)
                        {
                            if (_tickerPositionsMap.TryGetValue(key, out var pos))
                            {
                                pos.PendingPos = 0;
                            }
                        }

                        openOrderStatusList.ForEach(orderStatus =>
                        {
                            // Process each open order
                            string key = orderStatus.Ticker?.GetTickerKey() ?? "";
                            if (key != "")
                            {
                                // retrieve from map if already exists for this ticker, otherwise create new entry
                                if (_tickerPositionsMap.TryGetValue(key, out var existingPos))
                                {
                                    // update existing entry if needed
                                    existingPos.PendingPos += orderStatus.Qty;
                                }
                                else
                                {
                                    PositionUpdate pendingPos = new PositionUpdate
                                    {
                                        AccountId = "", // not currently tracking pending positions by account, but can add if needed in the future
                                        Ticker = orderStatus.Ticker,
                                        Pos = 0,
                                        PendingPos = orderStatus.Qty,
                                        AvgCost = 0,
                                        UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                                    };
                                    _tickerPositionsMap[key] = pendingPos;
                                }
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to query orders for provider={ProviderId}", _providerSvc.Id);
                }

                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
        }

        public PositionUpdate? GetPositionForTicker(TickerInfo ticker)
        {
            if (string.IsNullOrEmpty(ticker.Symbol) || string.IsNullOrEmpty(ticker.SecType))
            {
                _logger.LogWarning("GetPositionForTicker called with invalid ticker. Symbol={Symbol}, SecType={SecType}",
                    ticker?.Symbol ?? "N/A", ticker?.SecType ?? "N/A");
                return null;
            }
            string tickerKey = ticker.GetTickerKey();
            if (_tickerPositionsMap.TryGetValue(tickerKey, out var pos))
            {
                return new PositionUpdate(pos);
            }
            else
            {
                return null;
            }
        }


    }
}
