using FIGAutoTradeExSvc.SignalIngress;
using FIGCommon.Models;
using FIGCommon.Models.SignalPublication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FIGAutoTradeExSvc.Controllers;

[ApiController]
[Route("api/signalpublication")]
[Authorize(Roles = Role.SVC)]
public sealed class SignalPublicationController(ISignalIngressStore store, SignalIngressOptions options,
    ILogger<SignalPublicationController> logger) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(16384)]
    public async Task<IActionResult> Publish([FromBody] SignalPublicationMessage message, CancellationToken ct)
    {
        if (!options.Enabled) return NotFound();
        if (!options.Allows(User, message)) return Forbid();
        try { return Ok(await store.ReceiveAsync(message, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Rejected signal publication from {Producer}", message.ProducerId);
            return Conflict(new { error = ex.Message });
        }
    }
}
