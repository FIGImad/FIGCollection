using FIGCommon.Models.FIGController;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Services;
using System.Text.Json;

namespace FIGPriceDataSvc.PriceSync;

public interface IPriceSyncApiService
{
    Task<HistoricalPricesResponse> HistoricalDataRequestAsync(HistoricalDataRequest request,CancellationToken ct);
}
public sealed class PriceSyncApiService(IHttpClientFactory factory,ILogger<PriceSyncApiService> logger,
    IClientSignalRService controller,IServiceProvider services,IHttpContextAccessor accessor,IConfiguration config,
    RemotePriceSyncOptions options)
    :ApiServiceBase(factory.CreateClient(nameof(PriceSyncApiService)),logger,controller,services,accessor,config),IPriceSyncApiService
{
    public async Task<HistoricalPricesResponse> HistoricalDataRequestAsync(HistoricalDataRequest request,CancellationToken ct)
    {
        HistoricalPrices.Validate(request);
        ct.ThrowIfCancellationRequested();
        return await ProcessRouteRequest<HistoricalPricesResponse>(new ControllerRouteRequest
        {
            DestinationRole=(int)ControllerClientRole.PriceSync,DestinationServiceId=options.DestinationServiceId,
            Method="POST",Route=HistoricalPrices.Route,Load=JsonSerializer.Serialize(request),
            TimeoutMs=RemotePriceSyncService.ControllerRouteTimeoutMs
        },RemotePriceSyncService.ResponseTimeoutMs).WaitAsync(ct) ?? throw new InvalidOperationException("Historical service did not return a response.");
    }
}
