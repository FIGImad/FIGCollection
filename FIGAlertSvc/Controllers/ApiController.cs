using FIGAlertSvc.Services;
using FIGCommon.Controllers;
using FIGCommon.Models;
using FIGCommon.Models.LogMonitor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FIGAlertSvc.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApiController : FIGBaseController
    {
        private readonly AlertProcessingService _alertSvc;
        public ApiController(
              IWebHostEnvironment hostEnvironment
            , IHttpContextAccessor httpContextAccessor
            , ILogger<ApiController> logger
            , IServiceProvider serviceProvider
            , IConfiguration config
            , AlertProcessingService alertSvc
            ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _alertSvc = alertSvc;
        }

        #region Alerts
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin + "," + Role.SVC)]
        [HttpPost("alert/systemrec")]
        public async Task<IActionResult> ReportSystemAlert([FromBody] LogAlertMessage alertRec)
        {
            if (alertRec.SystemAlertId is not > 0 || string.IsNullOrWhiteSpace(alertRec.ServiceId)
                || !FIGCommon.Models.Alert.AlertCategory.IsSystemAlert(alertRec.Source, alertRec.Level))
                return BadRequest("A system alert requires a source service and positive source record ID.");
            try
            {
                int id = await _alertSvc.ProcessAlertAsync(alertRec);
                if (id <= 0) throw new InvalidOperationException("Alert persistence returned no record ID.");
                return Ok(new SystemAlertReceipt { SystemAlertId = alertRec.SystemAlertId.Value, AlertId = id });
            }
            catch (Exception e) { return OnException(e); }
        }

        // POST api/alert/logrec
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin + "," + Role.SVC)]
        [Route("alert/logrec")]
        [HttpPost]
        public async Task<IActionResult> ReportAlert([FromBody] LogAlertMessage alertRec)
        {
            try
            {
                _logger.LogInformation("Received alert: {LogName} - {Message}", alertRec.LogName, alertRec.Message);
                await _alertSvc.ProcessAlertAsync(alertRec);
                return Ok();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }
        #endregion Alerts
    }

}
