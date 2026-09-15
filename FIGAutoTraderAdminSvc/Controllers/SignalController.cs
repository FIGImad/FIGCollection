using FIGAutoTraderAdminSvc.Services;
using FIGCommon.Controllers;
using FIGCommon.DataAccess;
using FIGCommon.Exceptions;
using FIGCommon.Models;
using FIGCommon.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FIGAutoTraderAdminSvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SignalController : FIGBaseController
    {

        public SignalController(
                   IWebHostEnvironment hostEnvironment
                   , IHttpContextAccessor httpContextAccessor
                   , ILogger<SignalController> logger
                   , IServiceProvider serviceProvider
                   , IConfiguration config
            ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
        }

        #region SIGNAL

        // GET api/signal/last
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("last")]
        [HttpGet]
        public async Task<IActionResult> GetLastSignals()
        {
            try
            {
                //var lastSignals = MainRepo.QueryLastSignals();
                var lastSignals = MainRepo.QueryLastSignals();
                return Ok(lastSignals);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/signal/{id}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("{id}")]
        [HttpGet]
        public async Task<IActionResult> GetSignal(int id)
        {
            try
            {
                return Ok(MainRepo.GetSignal(id));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        #endregion SIGNAL

        #region SIGNAL_ARCHIVE
        // GET api/signal/archived_list
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("archived_list")]
        [HttpGet]
        public async Task<IActionResult> GetArchivedSignals()
        {
            try
            {
                var lastSignals = MainRepo.GetArchivedAutoTradeSignals();
                return Ok(lastSignals);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/signal/archived/{tag}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("archived/{tag}")]
        [HttpGet]
        public async Task<IActionResult> GetArchivedSignal(string tag)
        {
            try
            {
                var lastSignals = MainRepo.GetArchivedAutoTradeSignal(tag);
                return Ok(lastSignals);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }
        #endregion SIGNAL_ARCHIVE

    }
}