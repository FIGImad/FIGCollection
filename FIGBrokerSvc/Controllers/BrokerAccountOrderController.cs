using FIGBrokerSvc.DataAccess;
using FIGCommon.Controllers;
using FIGCommon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FIGBrokerSvc.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BrokerAccountOrderController : FIGBaseController
    {
        private readonly IServiceProvider _svcProvider;

        public BrokerAccountOrderController(
              IWebHostEnvironment hostEnvironment
            , IHttpContextAccessor httpContextAccessor
            , ILogger<BrokerAccountOrderController> logger
            , IServiceProvider serviceProvider
            , IConfiguration config
            ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _svcProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        #region BrokerAccountOrder

        // GET brokeraccountorder/{accountid}/{startOrderTime}
        [Authorize(Roles = $"{Role.Broker},{Role.Admin},{Role.SuperAdmin},{Role.SVC}")]
        [Route("{accountid}/{startOrderTime}")]
        [HttpGet]
        public async Task<IActionResult> GetBrokerAccountOrders(string accountId, int startOrderTime)
        {
            try
            {
                var brokerAccount = BrokerRepo.GetBrokerAccountByAccountId(accountId);
                if (brokerAccount == null)
                {
                    return NotFound($"Broker account with account id {accountId} not found");
                }
                // retrieve orders for the account 
                var orders = BrokerRepo.GetAccountOrders(brokerAccount.Id, startOrderTime);
                return Ok(orders);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        #endregion BrokerAccountOrder
    }
}