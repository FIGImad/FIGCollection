using FIGCommon.Models.LogMonitor;
using FIGCommon.Services.LogMonitor;
using Microsoft.AspNetCore.Mvc;

namespace FIGCommon.Controllers
{
    [Route("[controller]")]
    public class LogController : FIGBaseController
    {
        private readonly LogFileViewer _logFileViewer;

        public LogController(
              IWebHostEnvironment hostEnvironment
            , IHttpContextAccessor httpContextAccessor
            , ILogger<LogController> logger
            , IServiceProvider serviceProvider
            , IConfiguration config
            , LogFileViewer logFileViewer
            ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _logFileViewer = logFileViewer;
        }

        #region Operations
        // GET log/current/{startpos}/{numbytes}
        [Route("current/{startpos}/{numbytes}")]
        [HttpGet]
        public async Task<IActionResult> GetCurrentLogContent(long startPos, long numBytes)
        {
            try
            {
                LogContentDto logContent = await _logFileViewer.GetCurrentLogContentAsync(startPos, numBytes);
                return Ok(logContent);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET log/archive/{filename}/{startpos}/{numbytes}
        [Route("archive/{filename}/{startpos}/{numbytes}")]
        [HttpGet]
        public async Task<IActionResult> GetArchivedLogContent(string filename, long startPos, long numBytes)
        {
            try
            {
                LogContentDto logContent = await _logFileViewer.GetArchivedLogContentAsync(filename, startPos, numBytes);
                return Ok(logContent);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET log/list
        [Route("list")]
        [HttpGet]
        public IActionResult GetListOfLogFiles()
        {
            try
            {
                List<string> files = _logFileViewer.GetListOfLogFiles();
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
