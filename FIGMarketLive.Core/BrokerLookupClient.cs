using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FIGCommon.Models.FIGProviderAPI;

namespace FIGMarketLive;

public sealed record ContractLookup(string AccountId,string Symbol,string SecType,string Exchange,string Currency,string ContractMonth);

public sealed class BrokerLookupClient(HttpClient client)
{
    public static Uri ValidateBaseUrl(string value)
    {
        if(!Uri.TryCreate(value.Trim().TrimEnd('/')+"/",UriKind.Absolute,out var uri) ||
            uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidDataException("Enter an HTTP(S) broker base URL without credentials, query or fragment.");
        return uri;
    }

    public async Task PingAsync(string baseUrl,string token,CancellationToken ct)
    {
        using var request=Create(baseUrl,"net/ping",token);
        using var response=await client.SendAsync(request,ct);
        await EnsureAsync(response,ct);
        var pong=await response.Content.ReadFromJsonAsync<PingResponse>(cancellationToken:ct);
        if(pong?.Pong!="Pong") throw new InvalidDataException("Endpoint did not return a FIGBrokerSvc pong.");
        // Unlike anonymous ping, this establishes permission to use the protected lookup API.
        using var authorized=Create(baseUrl,"ticker/all",token);
        using var check=await client.SendAsync(authorized,ct);
        await EnsureAsync(check,ct);
        var catalogue=await check.Content.ReadFromJsonAsync<JsonElement>(cancellationToken:ct);
        if(catalogue.ValueKind!=JsonValueKind.Array)throw new InvalidDataException("Authorized ticker endpoint did not return a ticker catalogue.");
    }

    public async Task<ContractInfo[]> LookupAsync(string baseUrl,string token,ContractLookup lookup,bool resolve,CancellationToken ct)
    {
        using var request=Create(baseUrl,resolve?"marketlookup/resolve":"marketlookup/search",token);
        request.Method=HttpMethod.Post; request.Content=JsonContent.Create(lookup);
        using var response=await client.SendAsync(request,ct);
        await EnsureAsync(response,ct);
        return await response.Content.ReadFromJsonAsync<ContractInfo[]>(cancellationToken:ct)
            ?? throw new InvalidDataException("Empty contract response.");
    }

    private static HttpRequestMessage Create(string url,string path,string token)
    {
        var request=new HttpRequestMessage(HttpMethod.Get,new Uri(ValidateBaseUrl(url),path));
        if(!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token.Trim());
        return request;
    }

    private static async Task EnsureAsync(HttpResponseMessage response,CancellationToken ct)
    {
        if(response.IsSuccessStatusCode) return;
        // Do not echo remote content or credentials into configuration UI/logs.
        await Task.CompletedTask;
        throw new HttpRequestException($"Broker returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}). Check authentication, account and deployed lookup API.",null,response.StatusCode);
    }
    private sealed class PingResponse { public string Pong { get; set; }=""; }
}
