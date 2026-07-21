using FIGCommon.DataAccess;
using FIGCommon.Models;
using FIGAutoTraderAdminSvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FIGCommon.Controllers;


namespace FIGAutoTraderAdminSvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : FIGBaseController
    {
        protected readonly ReportCompilerService _reportCompilerService;
        public ReportsController(
                   IWebHostEnvironment hostEnvironment
                   , IHttpContextAccessor httpContextAccessor
                   , ILogger<ReportsController> logger
                   , IServiceProvider serviceProvider
                   , IConfiguration config
                   , ReportCompilerService reportCompilerService
                   ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _reportCompilerService = reportCompilerService;
        }

        //#region Reports

        //// GET api/reports/net/{autoTradeId}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("net/{autoTradeId}")]
        //[HttpGet]
        //public async Task<IActionResult> GetNetReport(int autoTradeId)
        //{
        //    try
        //    {
        //        return Ok(_reportCompilerService.NetReport(autoTradeId));
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}

        //#endregion Reports

    }
}