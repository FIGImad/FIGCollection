using FIGCommon.Controllers;
using FIGCommon.DataAccess;
using FIGCommon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FIGAutoTraderAdminSvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IntervalController : FIGBaseController
    {

        public IntervalController(
                   IWebHostEnvironment hostEnvironment
                   , IHttpContextAccessor httpContextAccessor
                   , ILogger<IntervalController> logger
                   , IServiceProvider serviceProvider
                   , IConfiguration config
                   ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
        }

        #region Intervals

        // GET api/interval/all
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("all")]
        [HttpGet]
        public async Task<IActionResult> GetIntervals()
        {
            try
            {
                return Ok(MainRepo.GetIntervals());
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }
        #endregion Intervals

    }
}