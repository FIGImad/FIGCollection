using FIGAutoTraderAdminSvc.Services;
using FIGCommon.Controllers;
using FIGCommon.DataAccess;
using FIGCommon.Models;
using FIGCommon.Models.FIGBroker;
using FIGCommon.Models.FIGProviderAPI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FIGAutoTraderAdminSvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BrokerController : FIGBaseController
    {

        protected readonly BrokerApiService _BrokerApiSvc;

        public BrokerController(
                   IWebHostEnvironment hostEnvironment
                   , IHttpContextAccessor httpContextAccessor
                   , ILogger<BrokerController> logger
                   , IServiceProvider serviceProvider
                   , IConfiguration config
                   , BrokerApiService BrokerApiSvc
            ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _BrokerApiSvc = BrokerApiSvc;
        }

        #region StatusCodes

        // GET api/broker/statuscodes
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("statuscodes")]
        [HttpGet]
        public async Task<IActionResult> GetStatusCodes()
        {
            try
            {
                var statusCodes = MainRepo.GetStatusCodes();
                return Ok(statusCodes);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        #endregion StatusCodes

        #region BrokerAccounts

        // GET api/broker/brokeraccount/{serviceId}/{accountId}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("brokeraccount/{serviceId}/{accountId}")]
        [HttpGet]
        public async Task<IActionResult> GetBrokerAccount(string serviceId, string accountId)
        {
            try
            {
                serviceId = serviceId.Trim().ToUpper();
                accountId = accountId.Trim().ToUpper();
                if (!string.IsNullOrEmpty(serviceId) && !string.IsNullOrEmpty(accountId))
                {
                    BrokerAccountRS? acct = await _BrokerApiSvc.GetBrokerAccount(accountId, serviceId, 10000);
                    if (acct != null)
                    {
                        return Ok(acct);
                    }
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/broker/brokeraccountpos/{serviceId}/{accountId}/{tickerId}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("brokeraccountpos/{serviceId}/{accountId}/{tickerId}")]
        [HttpGet]
        public async Task<IActionResult> GetBrokerAccountPos(string serviceId, string accountId, int tickerId)
        {
            try
            {
                serviceId = serviceId.Trim().ToUpper();
                accountId = accountId.Trim().ToUpper();
                if (!string.IsNullOrEmpty(serviceId) && !string.IsNullOrEmpty(accountId))
                {
                    TickerPosDto? pos = await _BrokerApiSvc.GetBrokerAccountPos(accountId, serviceId, tickerId, 10000);
                    if (pos != null)
                    {
                        return Ok(pos);
                    }
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        #endregion BrokerAccounts

        #region BrokerTickers
        // GET api/broker/tickers/{serviceId}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("tickers/{serviceId}")]
        [HttpGet]
        public async Task<IActionResult> GetTickers(string serviceId)
        {
            try
            {
                serviceId = serviceId.Trim().ToUpper();
                if (!string.IsNullOrEmpty(serviceId))
                {
                    List<FIGCommon.Models.FIGBroker.TickerRS>? tickers = await _BrokerApiSvc.GetTickers(serviceId, 10000);
                    if (tickers != null)
                    {
                        return Ok(tickers);
                    }
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/broker/brokeraccountticker/{serviceId}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin + ",")]
        [Route("brokeraccountticker/{serviceId}")]
        [HttpPost]
        public async Task<IActionResult> UpsertBrokerAccountTicker([FromBody] BrokerAccountTickerRS tickerPermission, string serviceId)
        {
            try
            {
                serviceId = serviceId.Trim().ToUpper();
                if (!string.IsNullOrEmpty(serviceId))
                {
                    BrokerAccountTickerRS? acctTickerPermission = await _BrokerApiSvc.UpsertAccountTicker(serviceId, tickerPermission, 10000);
                    if (acctTickerPermission != null)
                    {
                        return Ok(acctTickerPermission);
                    }
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }


        // Delete api/broker/brokeraccountticker/{serviceId}/{brokerAccountId}/{tickerId}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin + ",")]
        [Route("brokeraccountticker/{serviceId}/{brokerAccountId}/{tickerId}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteBrokerAccountTicker(string serviceId, int brokerAccountId, int tickerId)
        {
            try
            {
                serviceId = serviceId.Trim().ToUpper();
                if (!string.IsNullOrEmpty(serviceId))
                {
                    bool? ok = await _BrokerApiSvc.DeleteAccountTicker(serviceId, brokerAccountId, tickerId, 10000);
                    return Ok(ok);
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        #endregion BrokerTickers

        #region BrokerAccountOrders
        // GET api/broker/brokeraccountorder/{serviceId}/{accountId}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("brokeraccountorder/{serviceId}/{accountId}")]
        [HttpGet]
        public async Task<IActionResult> GetBrokerAccountOrders(string serviceId, string accountId)
        {
            try
            {
                serviceId = serviceId.Trim().ToUpper();
                accountId = accountId.Trim().ToUpper();
                if (!string.IsNullOrEmpty(serviceId) && !string.IsNullOrEmpty(accountId))
                {
                    // get records starting from begining of the the day yesterday in EPOCH in seconds
                    int fromTime = (int)new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-1)).ToUnixTimeSeconds();
                    List<OrderRS>? acct = await _BrokerApiSvc.GetBrokerAccountOrders(accountId, serviceId, fromTime, 10000);
                    if (acct != null)
                    {
                        return Ok(acct);
                    }
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }


        #endregion BrokerAccountOrders

        #region Prices
        // GET api/broker/currentprice/{serviceId}/{accountid}/{tickerId}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("currentprice/{serviceId}/{accountid}/{tickerId}")]
        [HttpGet]
        public async Task<IActionResult> GetCurrentTickerPrice(string serviceId, string accountId, int tickerId)
        {
            try
            {
                serviceId = serviceId.Trim().ToUpper();
                if (!string.IsNullOrEmpty(serviceId) && tickerId > 0)
                {
                    decimal? price = await _BrokerApiSvc.GetPriceSnapshot(serviceId, accountId, tickerId, 5000);
                    if (price != null)
                    {
                        return Ok(price);
                    }
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        #endregion Prices

        #region Orders
        // POST api/broker/order/new/{serviceId}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin + ",")]
        [Route("order/new/{serviceId}")]
        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderRequestDto orderInfo, string serviceId)
        {
            try
            {
                serviceId = serviceId.Trim().ToUpper();
                if (!string.IsNullOrEmpty(serviceId))
                {
                    OrderStatusDto? status = await _BrokerApiSvc.PlaceOrder(serviceId, orderInfo, 10000);
                    if (status != null)
                    {
                        return Ok(status);
                    }
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/broker/order/cancel/{serviceId}/{accountId}/{brokerRef}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin + ",")]
        [Route("order/cancel/{serviceId}/{accountId}/{brokerRef}")]
        [HttpGet]
        public async Task<IActionResult> CancelOrder(string serviceId, string accountId, string brokerRef)
        {
            try
            {
                serviceId = serviceId.Trim().ToUpper();
                if (!string.IsNullOrEmpty(serviceId))
                {
                    OrderStatusDto? status = await _BrokerApiSvc.CancelOrder(serviceId, accountId, brokerRef, 10000);
                    if (status != null)
                    {
                        return Ok(status);
                    }
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }




        #endregion Orders

    }
}