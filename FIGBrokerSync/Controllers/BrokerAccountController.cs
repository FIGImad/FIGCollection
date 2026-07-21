using FIGBrokerSvc.DataAccess;
using FIGCommon.Controllers;
using FIGCommon.Models;
using FIGCommon.Models.FIGBroker;
using IBKRProvider.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Ocsp;

namespace FIGBrokerSvc.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BrokerAccountController : FIGBaseController
    {
        private readonly IServiceProvider _svcProvider;

        public BrokerAccountController(
              IWebHostEnvironment hostEnvironment
            , IHttpContextAccessor httpContextAccessor
            , ILogger<BrokerAccountController> logger
            , IServiceProvider serviceProvider
            , IConfiguration config
            ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _svcProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        #region BrokerAccount

        // GET brokeraccount/{serviceid}/{accountid}
        [Authorize(Roles = $"{Role.Broker},{Role.Admin},{Role.SuperAdmin},{Role.SVC}")]
        //[AllowAnonymous]
        [Route("{serviceid}/{accountid}")]
        [HttpGet]
        public async Task<IActionResult> GetBrokerAccount(string serviceId, string accountId)
        {
            try
            {
                var brokerAccount = BrokerRepo.GetBrokerAccountByAccountId(accountId);
                if (brokerAccount != null && brokerAccount.AccountServiceId.Trim().ToUpper() == serviceId.ToUpper().Trim())
                {
                    return Ok(brokerAccount);
                }
                else
                {
                    return NotFound($"Broker account with account id {accountId} not found for service {serviceId}");
                }
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET brokeraccount/pos/{serviceid}/{accountid}/{tickerid}
        [Authorize(Roles = $"{Role.Broker},{Role.Admin},{Role.SuperAdmin},{Role.SVC}")]
        [Route("pos/{serviceid}/{accountid}/{tickerid}")]
        [HttpGet]
        public async Task<IActionResult> GetBrokerAccountPos(string serviceId, string accountId, int tickerId)
        {
            try
            {
                var brokerAccount = BrokerRepo.GetBrokerAccountByAccountId(accountId);
                if (brokerAccount != null && brokerAccount.AccountServiceId.Trim().ToUpper() == serviceId.ToUpper().Trim())
                {
                    var ticker = BrokerRepo.GetTicker(tickerId);
                    if (ticker == null)
                        return NotFound($"Ticker {tickerId} not found");

                    string providerKey = NormalizeProviderKey(accountId, true);

                    var positionTracker = _svcProvider.GetKeyedService<IPositionTrackerService>(providerKey);
                    var pos = positionTracker?.GetPositionForTicker(ticker.ToTickerInfo());

                    TickerPosDto posDTO = new TickerPosDto
                    {
                        AccountId = accountId,
                        AccountServiceId = serviceId,
                        TickerId = tickerId,
                        Position = (int?) (pos?.Pos),
                        PendingPosition = (int?) (pos?.PendingPos),
                        AvgPrice = (decimal?)(pos?.AvgCost)
                    };

                    return Ok(posDTO);
                }
                else
                {
                    return NotFound($"Broker account with account id {accountId} not found for service {serviceId}");
                }
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/brokeraccount/ticker
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin + ",")]
        [Route("ticker")]
        [HttpPost]
        public async Task<IActionResult> UpsertBrokerAccountTicker([FromBody] BrokerAccountTickerRS tickerPermission)
        {
            try
            {
                // check if account is valid
                var brokerAccount = BrokerRepo.GetBrokerAccount(tickerPermission.BrokerAccountId);
                if (brokerAccount == null)
                {
                    return NotFound("Invalid Broker Account");
                }
                // check if ticker is configured already
                // find if tickerPermission.tickerId is already in brokerAccount.acctTickerPermissions
                var existingPermission = brokerAccount.acctTickerPermissions.FirstOrDefault(t => t.TickerId == tickerPermission.TickerId);
                // if already exists only update Allowed, LongLimit and ShortLimit fields
                if (existingPermission == null)
                {
                    var rec = BrokerRepo.UpsertBrokerAccountTicker(tickerPermission);
                    return Ok(rec);
                }
                else
                {
                    existingPermission.Allowed = tickerPermission.Allowed;
                    existingPermission.LongLimit = tickerPermission.LongLimit;
                    existingPermission.ShortLimit = tickerPermission.ShortLimit;
                    var rec = BrokerRepo.UpsertBrokerAccountTicker(existingPermission);
                    return Ok(rec);
                }
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }


        // POST api/brokeraccount/ticker/{accountId}/{tickerId}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin + ",")]
        [Route("ticker/{accountId}/{tickerId}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteBrokerAccountTicker(int accountId, int tickerId)
        {
            try
            {
                var brokerAccount = BrokerRepo.GetBrokerAccount(accountId);
                if (brokerAccount == null)
                {
                    return NotFound("Invalid Broker Account");
                }
                // check if ticker is configured already
                // find if tickerPermission.tickerId is already in brokerAccount.acctTickerPermissions
                var existingPermission = brokerAccount.acctTickerPermissions.FirstOrDefault(t => t.TickerId == tickerId);
                // if already exists only update Allowed, LongLimit and ShortLimit fields
                if (existingPermission == null)
                {
                    return NotFound("Broker Ticker is not found");
                }
                else
                {
                    BrokerRepo.DeleteBrokerAccountTicker(accountId, tickerId);
                    return Ok(true);
                }
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        #endregion BrokerAccount
        private static string NormalizeProviderKey(string id, bool isTracking)
        {
            return (isTracking ? "TRACKING_" : string.Empty) + id.Trim().ToUpperInvariant();
        }

    }
}