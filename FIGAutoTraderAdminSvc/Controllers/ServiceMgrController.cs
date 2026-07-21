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
    public class ServiceMgrConroller : FIGBaseController
    {
        private readonly ServiceMgrApi _serviceMgrApiService;

        public ServiceMgrConroller(
                   IWebHostEnvironment hostEnvironment
                   , IHttpContextAccessor httpContextAccessor
                   , ILogger<ServiceMgrConroller> logger
                   , IServiceProvider serviceProvider
                   , IConfiguration config
                   , ServiceMgrApi serviceMgrApiService
            ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _serviceMgrApiService = serviceMgrApiService;
        }

        #region Services

        // GET api/servicemgr/status/{serviceId}/{serviceName}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("status/{serviceId}/{serviceName}")]
        [HttpGet]
        public async Task<IActionResult> GetStatus(string serviceId, string serviceName)
        {
            try
            {
                var status = await _serviceMgrApiService.GetStatusAsync(serviceId, serviceName, 10000);
                return Ok(status);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/servicemgr/start/{serviceId}/{serviceName}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("start/{serviceId}/{serviceName}")]
        [HttpGet]
        public async Task<IActionResult> StartService(string serviceId, string serviceName)
        {
            try
            {
                var status = await _serviceMgrApiService.StartAsync(serviceId, serviceName, 10000);
                return Ok(status);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/servicemgr/stop/{serviceId}/{serviceName}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("stop/{serviceId}/{serviceName}")]
        [HttpGet]
        public async Task<IActionResult> StopService(string serviceId, string serviceName)
        {
            try
            {
                var status = await _serviceMgrApiService.StopAsync(serviceId, serviceName, 10000);
                return Ok(status);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }


        #endregion Services

    }
}