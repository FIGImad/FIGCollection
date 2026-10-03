using FIGCommon.Models.FIGController;
using FIGCommon.Models.SignalPublication;
using FIGCommon.Services;
using System.Text.Json;

namespace FIGSignalExSvc.Publication;

public interface ISignalPublicationClient
{
    Task<SignalPublicationAck> PublishAsync(SignalPublicationMessage message, CancellationToken ct);
}

public sealed class SignalPublicationClient : ApiServiceBase, ISignalPublicationClient
{
    private readonly SignalPublicationOptions options;
    public SignalPublicationClient(IHttpClientFactory factory, ILogger<SignalPublicationClient> logger,
        IClientSignalRService controller, IServiceProvider services, IHttpContextAccessor accessor,
        IConfiguration config, SignalPublicationOptions options)
        : base(factory.CreateClient(nameof(SignalPublicationClient)), logger, controller, services, accessor, config)
        => this.options = options;

    public async Task<SignalPublicationAck> PublishAsync(SignalPublicationMessage message, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ack = await ProcessRouteRequest<SignalPublicationAck>(new ControllerRouteRequest
        {
            DestinationServiceId = options.DestinationServiceId,
            DestinationRole = (int)ControllerClientRole.ATS,
            Route = "/api/signalpublication",
            Method = "POST",
            Load = JsonSerializer.Serialize(message)
        }, 30000).WaitAsync(ct);
        if (ack == null || !ack.Matches(message))
            throw new InvalidOperationException("Receiver did not acknowledge this exact signal revision and payload.");
        return ack;
    }
}
