using FIGCommon.Interfaces;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Utilities.FIGProviderAPI;
using IBKRProvider.Service;
using IBKRProvider.Services;
using IBKRProvider.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IBKRProvider
{
    public static class IBKRProviderExtensions
    {
        public static IHostBuilder UseIBKRProvider(this IHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices((context, services) =>
            {
                var config = context.Configuration
                    .GetSection("ProviderServices")
                    .Get<ProviderServicesConfig>();

                if (config == null)
                    return;

                foreach (var providerConfig in config.Providers)
                {
                    if (providerConfig.Type?.Equals("IBKR", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        if (providerConfig.ClientId != -1)
                        {
                            services.AddIBKRProvider(providerConfig, false);
                        }
                        if (providerConfig.TrackingClientId != -1)
                        {
                            ProviderOptions trackingProviderConfig = new ProviderOptions
                            {
                                Id = providerConfig.Id + "_TRACKING",
                                Type = providerConfig.Type,
                                Host = providerConfig.Host,
                                Port = providerConfig.Port,
                                ClientId = providerConfig.TrackingClientId,
                                TrackingClientId = providerConfig.TrackingClientId,
                                EnableFrozenData = providerConfig.EnableFrozenData,
                                CallbackQueueCapacity = providerConfig.CallbackQueueCapacity,
                            };
                            services.AddIBKRProvider(trackingProviderConfig, true);
                        }
                    }
                }
            });
        }

        public static IHostBuilder UseIBKRProvider(
            this IHostBuilder hostBuilder,
            Func<IServiceProvider, IEnumerable<ProviderOptions>> optionsFactory)
        {
            return hostBuilder.ConfigureServices((context, services) =>
            {
                using var tempProvider = services.BuildServiceProvider();

                List<ProviderOptions> providers = optionsFactory(tempProvider).ToList();

                foreach (var providerConfig in providers)
                {
                    if (providerConfig.Type?.Equals("IBKR", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        if (providerConfig.ClientId != -1)
                        {
                            services.AddIBKRProvider(providerConfig, false);
                        }
                        if (providerConfig.TrackingClientId != -1)
                        {
                            ProviderOptions trackingProviderConfig = new ProviderOptions
                            {
                                Id = providerConfig.Id,
                                Type = providerConfig.Type,
                                Host = providerConfig.Host,
                                Port = providerConfig.Port,
                                ClientId = providerConfig.TrackingClientId,
                                TrackingClientId = providerConfig.TrackingClientId,
                                EnableFrozenData = providerConfig.EnableFrozenData,
                                CallbackQueueCapacity = providerConfig.CallbackQueueCapacity,
                            };
                            services.AddIBKRProvider(trackingProviderConfig, true);
                        }
                    }
                }
            });
        }

        public static IServiceCollection AddIBKRProvider(
            this IServiceCollection services,
            ProviderOptions options, bool isTracking)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(options.Id))
                throw new ArgumentException("ProviderOptions.Id is required.", nameof(options));

            string providerKey = NormalizeProviderKey(options.Id, isTracking);

            /*
             * ProviderTask can stay transient.
             * If ProviderTask itself needs provider-specific services later,
             * then you may also make it keyed.
             */
            services.AddTransient<ProviderTask>();

            /*
             * Fully isolated callback queue per provider/account.
             *
             * Key:
             *   providerKey
             */
            services.AddKeyedSingleton<ICallbackQueue>(providerKey, (sp, _) =>
                new CallbackQueue(capacity: options.CallbackQueueCapacity));

            /*
             * Fully isolated connection signal per provider/account.
             *
             * This prevents one provider reconnect/disconnect signal from waking
             * another provider's tracker.
             */
            services.AddKeyedSingleton<IProviderConnectionSignal>(providerKey, (sp, _) =>
                new ProviderConnectionSignal());

            /*
             * Register Provider as keyed singleton.
             *
             * Important:
             * We explicitly pass the keyed queue and keyed connection signal into Provider.
             *
             * This requires Provider to have constructor parameters compatible with:
             *   ProviderOptions options
             *   ICallbackQueue callbackQueue
             *   IProviderConnectionSignal connectionSignal
             *
             * If Provider does not currently accept these, update its constructor.
             */
            services.AddKeyedSingleton<IProviderService>(providerKey, (sp, _) =>
            {
                var logger = sp.GetRequiredService<ILogger<Provider>>();

                try
                {
                    var callbackQueue =
                        sp.GetRequiredKeyedService<ICallbackQueue>(providerKey);

                    var connectionSignal =
                        sp.GetRequiredKeyedService<IProviderConnectionSignal>(providerKey);

                    return ActivatorUtilities.CreateInstance<Provider>(
                        sp,
                        options,
                        callbackQueue,
                        connectionSignal);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Failed to create IBKR Provider. Id={Id}, Type={Type}",
                        providerKey,
                        options.Type);

                    throw;
                }
            });

            /*
             * Register the position tracker keyed by the same providerKey.
             *
             * This tracker gets:
             *   - the keyed callback queue
             *   - the keyed provider
             *   - the keyed connection signal
             */
            /*
             * Ensure Provider (IProviderService) is resolved and connected at startup.
             * Provider does not implement IHostedService, so we use a lightweight
             * hosted-service shim that calls CreateConnection on start and Disconnect on stop.
             */
            services.AddSingleton<IHostedService>(sp =>
                new ProviderHostedServiceShim(
                    sp.GetRequiredKeyedService<IProviderService>(providerKey)));

            if (isTracking)
            {
                services.AddKeyedSingleton<PositionTrackerService>(providerKey, (sp, _) =>
                    new PositionTrackerService(
                        sp.GetRequiredService<ILogger<PositionTrackerService>>(),
                        sp.GetRequiredKeyedService<ICallbackQueue>(providerKey),
                        sp.GetRequiredKeyedService<IProviderService>(providerKey),
                    sp.GetRequiredKeyedService<IProviderConnectionSignal>(providerKey)
                ));

                /*
                 * Also expose it as keyed IPositionTrackerService.
                 *
                 * Other services can now resolve:
                 *
                 *   sp.GetRequiredKeyedService<IPositionTrackerService>("ACCOUNTID")
                 */
                services.AddKeyedSingleton<IPositionTrackerService>(providerKey, (sp, _) =>
                    sp.GetRequiredKeyedService<PositionTrackerService>(providerKey));

                /*
                 * Register the keyed PositionTrackerService as hosted service.
                 *
                 * We cannot use normal AddHostedService<T>() here because that resolves
                 * unkeyed services.
                 */
                services.AddSingleton<IHostedService>(sp =>
                    new KeyedBackgroundServiceHost<PositionTrackerService>(
                        sp.GetRequiredKeyedService<PositionTrackerService>(providerKey)));
            }

            return services;
        }

        private static string NormalizeProviderKey(string id, bool isTracking)
        {
            return (isTracking ? "TRACKING_" : string.Empty) + id.Trim().ToUpperInvariant() ;
        }
    }

    internal sealed class ProviderHostedServiceShim : IHostedService
    {
        private readonly IProviderService _provider;

        public ProviderHostedServiceShim(IProviderService provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _provider.CreateConnection();
            }
            catch { }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _provider.Disconnect();
            }
            catch { }
            return Task.CompletedTask;
        }
    }

    internal sealed class KeyedBackgroundServiceHost<TService> : IHostedService
        where TService : IHostedService
    {
        private readonly TService _service;

        public KeyedBackgroundServiceHost(TService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _service.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _service.StopAsync(cancellationToken);
        }
    }
}