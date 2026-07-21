using FIGAutoTraderAdminSvc.Services;
using FIGCommon.Controllers;
using FIGCommon.DataAccess;
using FIGCommon.Exceptions;
using FIGCommon.Models;
using FIGCommon.Models.Alert;
using FIGCommon.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace FIGAutoTraderAdminSvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlertsController : FIGBaseController
    {

        public AlertsController(
                   IWebHostEnvironment hostEnvironment
                   , IHttpContextAccessor httpContextAccessor
                   , ILogger<AlertsController> logger
                   , IServiceProvider serviceProvider
                   , IConfiguration config
            ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
        }

        #region AlertRules

        // GET api/alerts/rules
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("rules")]
        [HttpGet]
        public async Task<IActionResult> GetAlertRules()
        {
            try
            {
                var lastSignals = AlertRepo.GetAlertRules();
                return Ok(lastSignals);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/alerts/rule
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("rule")]
        [HttpPost]
        public async Task<IActionResult> UpsertAutoTrade([FromBody] AlertRuleRS alertRule)
        {
            try
            {
                var rule = AlertRepo.UpsertAlertRule(alertRule);
                return Ok(rule);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // DELETE api/alerts/rule/{ruleid}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("rule/{ruleid}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteAlertRule(int ruleid)
        {
            try
            {
                AlertRepo.DeleteAlertRule(ruleid);
                return Ok();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }


        #endregion AlertRules

        #region AlertRecipients

        // GET api/alerts/recipients
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("recipients")]
        [HttpGet]
        public async Task<IActionResult> GetAlertRecipients()
        {
            try
            {
                var recipients = AlertRepo.GetRecipients();
                return Ok(recipients);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/alerts/recipient
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("recipient")]
        [HttpPost]
        public async Task<IActionResult> UpsertAlertRecipient([FromBody] AlertRecipientRS alertRecipient)
        {
            try
            {
                var recipient = AlertRepo.UpsertRecipient(alertRecipient);
                // also need to update the subscriptions
                if (alertRecipient.Subscriptions != null)
                {
                    foreach (var sub in alertRecipient.Subscriptions)
                    {
                        sub.RecipientId = recipient.Id;
                        AlertRepo.UpsertRecipientSubscription(sub);
                    }
                }
                // how about the subscriptions that were removed?
                var existingSubs = AlertRepo.GetRecipientSubscriptions(recipient.Id);
                if (existingSubs != null)
                {
                    foreach (var sub in existingSubs)
                    {
                        if (alertRecipient.Subscriptions == null || !alertRecipient.Subscriptions.Any(s => s.RecipientId == sub.RecipientId && s.AlertRuleId == sub.AlertRuleId))
                        {
                            AlertRepo.DeleteRecipientSubscription(sub.RecipientId, sub.AlertRuleId);
                        }
                    }
                }
                return Ok(recipient);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // DELETE api/alerts/recipient/{recipientid}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("recipient/{recipientid}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteAlertRecipient(int recipientid)
        {
            try
            {
                AlertRepo.DeleteRecipient(recipientid);
                return Ok();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }


        #endregion AlertRecipients

        #region Alerts

        // GET api/alerts/{duration}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("{duration}")]
        [HttpGet]
        public async Task<IActionResult> GetAlertsSince(int duration)
        {
            try
            {
                var alerts = AlertRepo.GetAlertsSince(duration);
                return Ok(alerts);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        #endregion Alerts

    }
}