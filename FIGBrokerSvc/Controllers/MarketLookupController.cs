using FIGBrokerSvc.DataAccess;
using FIGCommon.Interfaces;
using FIGCommon.Models;
using FIGCommon.Models.FIGProviderAPI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FIGBrokerSvc.Controllers;

[ApiController]
[Route("marketlookup")]
[Authorize(Roles=$"{Role.Broker},{Role.Admin},{Role.SuperAdmin},{Role.SVC}")]
public sealed class MarketLookupController(IServiceProvider services, IConfiguration configuration) : ControllerBase
{
    public sealed class LookupRequest
    {
        public string AccountId { get; set; } = "";
        public string Symbol { get; set; } = "";
        public string SecType { get; set; } = "";
        public string Exchange { get; set; } = "";
        public string Currency { get; set; } = "";
        public string ContractMonth { get; set; } = "";
    }

    [HttpPost("search")]
    public Task<IActionResult> Search(LookupRequest request, CancellationToken ct)=>Lookup(request,false,ct);

    [HttpPost("resolve")]
    public Task<IActionResult> Resolve(LookupRequest request, CancellationToken ct)=>Lookup(request,true,ct);

    private async Task<IActionResult> Lookup(LookupRequest r,bool resolve,CancellationToken ct)
    {
        r.Symbol=r.Symbol.Trim().ToUpperInvariant(); r.SecType=r.SecType.Trim().ToUpperInvariant();
        r.AccountId=r.AccountId.Trim(); r.ContractMonth=r.ContractMonth.Trim();
        if(r.Symbol.Length is <1 or >50 || r.AccountId.Length is <1 or >100 || r.SecType is not ("FUT" or "STK"))
            return BadRequest("Account, symbol and FUT/STK security type are required.");
        if(r.SecType=="FUT" && (r.ContractMonth.Length is not (6 or 8) || !r.ContractMonth.All(char.IsAsciiDigit)))
            return BadRequest("Futures lookup requires contract month YYYYMM or expiry YYYYMMDD.");
        var account=BrokerRepo.GetBrokerAccountByAccountId(r.AccountId);
        if(account is null || !account.Enable || !account.AccountServiceId.Trim().Equals(configuration["ControllerConfig:Id"]?.Trim(),StringComparison.OrdinalIgnoreCase))
            return NotFound("Account is unavailable on this broker service.");
        var provider=services.GetKeyedService<IProviderService>(account.AccountId.Trim().ToUpperInvariant());
        if(provider is null || !provider.IsConnected) return StatusCode(503,"The account's provider is not connected.");
        try
        {
            if(!resolve && r.SecType=="STK")
            {
                var matches=await Task.Run(()=>provider.SymbolLookup(r.Symbol,15000),ct).WaitAsync(TimeSpan.FromSeconds(20),ct);
                if(matches is null) return StatusCode(503,"Symbol search failed; retry the request.");
                return Ok(matches.Where(c=>c.SecType.Equals("STK",StringComparison.OrdinalIgnoreCase)
                    && (string.IsNullOrWhiteSpace(r.Currency) || c.Currency.Equals(r.Currency,StringComparison.OrdinalIgnoreCase))
                    && (string.IsNullOrWhiteSpace(r.Exchange) || c.Exchange.Equals(r.Exchange,StringComparison.OrdinalIgnoreCase)
                        || c.PrimaryExch.Equals(r.Exchange,StringComparison.OrdinalIgnoreCase))).ToArray());
            }
            var contract=await Task.Run(()=>provider.ContractInfo(new InstrumentLookupRequest
            {
                Symbol=r.Symbol,SecType=r.SecType,Exchange=r.Exchange.Trim(),Currency=r.Currency.Trim(),
                LastTradeDateOrContractMonth=r.ContractMonth
            },15000),ct).WaitAsync(TimeSpan.FromSeconds(20),ct);
            if(contract is null) return NotFound("No unambiguous contract was resolved. Check exchange, currency and expiry.");
            return Ok(new[]{contract});
        }
        catch(OperationCanceledException) when(ct.IsCancellationRequested) { throw; }
        catch(TimeoutException) { return StatusCode(504,"Contract lookup timed out."); }
        catch(Exception) { return StatusCode(503,"Provider lookup failed."); }
    }
}
