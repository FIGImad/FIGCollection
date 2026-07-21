using FIGAutoTraderAdminSvc.Services;
using FIGCommon.Controllers;
using FIGCommon.Models;
using FIGCommon.Models.LogMonitor;
using FIGCommon.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FIGAutoTraderAdminSvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogViewerController : FIGBaseController
    {
        private readonly LogViewerApi _logViewerApi;

        public LogViewerController(
                   IWebHostEnvironment hostEnvironment
                   , IHttpContextAccessor httpContextAccessor
                   , ILogger<LogViewerController> logger
                   , IServiceProvider serviceProvider
                   , IConfiguration config
                   , LogViewerApi logViewerApi
                   , IClientSignalRService controllerClient
                   ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _logViewerApi = logViewerApi ?? throw new ArgumentNullException(nameof(logViewerApi ));
        }

        #region Operations
        // GET logviewer/current/{serviceId}/{startpos}/{numbytes}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("current/{serviceId}/{startpos}/{numbytes}")]
        [HttpGet]
        public async Task<IActionResult> GetCurrentLogContent(string serviceId, long startPos, long numBytes)
        {
            try
            {
                LogContentDto? logContent = await _logViewerApi.GetCurrentLogContentAsync(serviceId, startPos, numBytes, 10000);
                return Ok(logContent);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET logviewer/archive/{serviceId}/{filename}/{startpos}/{numbytes}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("archive/{serviceId}/{filename}/{startpos}/{numbytes}")]
        [HttpGet]
        public async Task<IActionResult> GetArchivedLogContent(string serviceId, string filename, long startPos, long numBytes)
        {
            try
            {
                LogContentDto? logContent = await _logViewerApi.GetArchivedLogContentAsync(serviceId, filename, startPos, numBytes, 10000);
                return Ok(logContent);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET logviewer/list/{serviceId}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("list/{serviceId}")]
        [HttpGet]
        public async Task<IActionResult> GetListOfLogFiles(string serviceId)
        {
            try
            {
                List<string>? files = await _logViewerApi.GetListOfLogFilesAsync(serviceId, 10000);
                return Ok(files);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }
        #endregion Operations
    }
}