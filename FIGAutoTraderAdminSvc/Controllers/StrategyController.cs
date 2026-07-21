using FIGCommon.Controllers;
using FIGCommon.DataAccess;
using FIGCommon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FIGAutoTraderAdminSvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StrategyController : FIGBaseController
    {

        public StrategyController(
                   IWebHostEnvironment hostEnvironment
                   , IHttpContextAccessor httpContextAccessor
                   , ILogger<StrategyController> logger
                   , IServiceProvider serviceProvider
                   , IConfiguration config
                   ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
        }

        #region Strategies

        // GET api/strategy/all
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("all")]
        [HttpGet]
        public async Task<IActionResult> GetStrategies()
        {
            try
            {
                return Ok(MainRepo.GetStrategyConfigs());
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        #endregion Strategies


    }
}