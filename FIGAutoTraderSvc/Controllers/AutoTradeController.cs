using FIGCommon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FIGAutoTradeExSvc.Services;
using FIGCommon.Controllers;
using FIGCommon.Models.FIGProviderAPI;

namespace FIGAutoTradeExSvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutoTradeController : FIGBaseController
    {
        protected readonly IAutoTradeManagementService? _autoTradeSvc = null;
        private readonly IOrderManagementService _orderMgt;

        public AutoTradeController(
                   IWebHostEnvironment hostEnvironment
                   , IHttpContextAccessor httpContextAccessor
                   , ILogger<AutoTradeController> logger
                   , IServiceProvider serviceProvider
                   , IConfiguration config
                   , IAutoTradeManagementService autoTradeSvc
                   , IOrderManagementService orderMgt
                   ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _orderMgt = orderMgt ?? throw new ArgumentNullException(nameof(orderMgt));
            _autoTradeSvc = autoTradeSvc;
        }


        #region AutoTradeService
        // GET api/autotrade/start/{id}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("start/{id}")]
        [HttpGet]
        public async Task<IActionResult> AutoTradeStart(int id)
        {
            try
            {
                return Ok(_autoTradeSvc?.StartExec(id));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/autotrade/stop/{id}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("stop/{id}")]
        [HttpGet]
        public async Task<IActionResult> AutoTradeStop(int id)
        {
            try
            {
                return Ok(_autoTradeSvc?.StopExec(id));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }


        // GET api/autotrade/status/{id}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("status/{id}")]
        [HttpGet]
        public async Task<IActionResult> AutoTradeServiceStatus(int id)
        {
            try
            {
                return Ok(_autoTradeSvc?.GetExecStatus(id));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // Post api/autotrade/liststatus
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("liststatus")]
        [HttpPost]
        public async Task<IActionResult> AutoTradeServiceStatusList([FromBody] List<int> autoTradeIds)
        {

            try
            {
                return Ok(_autoTradeSvc?.GetExecStatusFromList(autoTradeIds));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // Post api/autotrade/orderstatus
        [Authorize(Roles = Role.Broker + "," + Role.SVC + "," + Role.Admin + "," + Role.SuperAdmin)]
        [Route("orderstatus")]
        [HttpPost]
        public async Task<IActionResult> OrderStatus([FromBody] OrderStatusDto orderStatus)
        {

            try
            {
                return Ok(_orderMgt.UpdateOrderStatus(orderStatus));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }


        

        #endregion AutoTradeService

    }
}