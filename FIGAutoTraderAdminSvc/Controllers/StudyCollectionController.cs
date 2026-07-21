using FIGCommon.Controllers;
using FIGCommon.DataAccess;
using FIGCommon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FIGAutoTraderAdminSvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudyCollectionController : FIGBaseController
    {

        public StudyCollectionController(
                   IWebHostEnvironment hostEnvironment
                   , IHttpContextAccessor httpContextAccessor
                   , ILogger<StudyCollectionController> logger
                   , IServiceProvider serviceProvider
                   , IConfiguration config
                   ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
        }

        #region StudyCollection

        // GET api/studycollection/all
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("all")]
        [HttpGet]
        public async Task<IActionResult> GetStudyCollections()
        {
            try
            {
                return Ok(MainRepo.GetStudyColList());
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }
        #endregion StudyCollection

    }
}