using FIGCommon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FIGCommon.Controllers;

namespace FIGSignalExSvc.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApiController : FIGBaseController
    {

        public ApiController(
              IWebHostEnvironment hostEnvironment
            , IHttpContextAccessor httpContextAccessor
            , ILogger<ApiController> logger
            , IServiceProvider serviceProvider
            , IConfiguration config
            ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
        }

        #region Study
        // GET api/study/{id}
        [Authorize(Roles = Role.SuperAdmin)]
        [Route("study/{id}")]
        [HttpGet]
        public async Task<IActionResult> SelectStudy(int id)
        {
            try
            {
                return Ok();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }
        #endregion Study

    }
}

