using FIGCommon.Controllers;
using FIGCommon.Models;
using FIGCommon.Models.FIGProviderAPI;
using FIGPriceSyncSvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FIGPriceSyncSvc.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles=Role.SVC)]
public sealed class HistoricalDataController : FIGBaseController
{
    protected readonly IHistoricalPriceService _historicalPriceSvc;

    public HistoricalDataController(
            IHistoricalPriceService service
           , IWebHostEnvironment hostEnvironment
           , IHttpContextAccessor httpContextAccessor
           , ILogger<HistoricalDataController> logger
           , IServiceProvider serviceProvider
           , IConfiguration config
    ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
    {
        _historicalPriceSvc = service;
    }

    [HttpPost]
    [RequestSizeLimit(16384)]
    public async Task<IActionResult> HistoricalDataRequest([FromBody] HistoricalDataRequest request,CancellationToken ct)
    {
        try { return Ok(await _historicalPriceSvc.RequestAsync(request,ct)); }
        catch(ArgumentException ex) { return BadRequest(new{error=ex.Message}); }
        catch(HistoricalPriceBusyException) { Response.Headers.RetryAfter="15"; return StatusCode(429,new{error="Historical provider busy; retry this window."}); }
        catch(OperationCanceledException) when(ct.IsCancellationRequested) { throw; }
        catch(Exception ex)
        {
            _logger.LogWarning(ex,"Historical data request failed");
            return StatusCode(503,new{error="Historical data unavailable; retry this window."});
        }
    }
}
