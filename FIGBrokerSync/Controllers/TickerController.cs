using FIGBrokerSvc.DataAccess;
using FIGCommon.Controllers;
using FIGCommon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FIGBrokerSvc.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TickerController : FIGBaseController
    {
        private readonly IServiceProvider _svcProvider;

        public TickerController(
              IWebHostEnvironment hostEnvironment
            , IHttpContextAccessor httpContextAccessor
            , ILogger<TickerController> logger
            , IServiceProvider serviceProvider
            , IConfiguration config
            ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _svcProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        #region Tickers

        // GET ticker/all
        [Authorize(Roles = $"{Role.Broker},{Role.Admin},{Role.SuperAdmin},{Role.SVC}")]
        [Route("all")]
        [HttpGet]
        public async Task<IActionResult> GetTickers()
        {
            try
            {
                var tickers= BrokerRepo.GetTickers();
                return Ok(tickers);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        #endregion Tickers
    }
}