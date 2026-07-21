using FIGCommon.Controllers;
using FIGCommon.DataAccess;
using FIGCommon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FIGAutoTraderAdminSvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TickerController : FIGBaseController
    {
        public TickerController(
                   IWebHostEnvironment hostEnvironment
                   , IHttpContextAccessor httpContextAccessor
                   , ILogger<TickerController> logger
                   , IServiceProvider serviceProvider
                   , IConfiguration config
                   ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
        }

        #region Tickers

        // GET api/ticker/all
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("all")]
        [HttpGet]
        public async Task<IActionResult> GetTickers()
        {
            try
            {
                return Ok(MainRepo.GetTickers());
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/ticker/{id}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("{id}")]
        [HttpGet]
        public async Task<IActionResult> GetTicker(int id)
        {
            try
            {
                return Ok(MainRepo.GetTicker(id));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/ticker/upsert
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("upsert")]
        [HttpPost]
        public async Task<IActionResult> UpsertTicker([FromBody] TickerRS ticker)
        {
            try
            {
                return Ok(MainRepo.UpsertTicker(ticker));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }
        #endregion Tickers


    }
}