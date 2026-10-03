using FIGCommon.Services;

namespace FIGPriceDataSvc.PriceSync;

public static class PriceDataServices
{
    public static IServiceCollection AddPriceDataServices(this IServiceCollection services,IConfiguration configuration)
    {
        var options=new RemotePriceSyncOptions();
        configuration.GetSection("RemotePriceSync").Bind(options);
        options.Validate();
        services.AddSingleton(options);
        services.AddSignalR();
        services.AddSingleton<IClientSignalRService,ClientSignalRService>();
        services.AddHostedService(sp=>(ClientSignalRService)sp.GetRequiredService<IClientSignalRService>());
        if(options.Enabled)
        {
            services.AddSingleton<IRemotePriceStore,RemotePriceStore>();
            services.AddSingleton<IPriceSyncApiService,PriceSyncApiService>();
            services.AddHostedService<RemotePriceSyncService>();
        }
        return services;
    }
}
