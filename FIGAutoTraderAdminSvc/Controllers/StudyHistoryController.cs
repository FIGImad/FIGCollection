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
    public class StudyHistoryController : FIGBaseController
    {
        protected readonly ATSApiService _ATSApiSvc;

        public StudyHistoryController(
                   IWebHostEnvironment hostEnvironment
                   , IHttpContextAccessor httpContextAccessor
                   , ILogger<StudyHistoryController> logger
                   , IServiceProvider serviceProvider
                   , IConfiguration config
                   , ATSApiService ATSApiSvc
                   ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _ATSApiSvc = ATSApiSvc;
        }

        #region StudyCollection

        // GET api/studyhistory/studycols
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("studycols")]
        [HttpGet]
        public async Task<IActionResult> GetStudycols()
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

        #region StudyHistory

        // GET api/studyhistory/data/{id}/{startRawTime}/{numRecords}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("data/{id}/{startRawTime}/{numRecords}")]
        [HttpGet]
        public async Task<IActionResult> GetStudyHistoryData(int id, long startRawTime, int numRecords)
        {
            try
            {
                var studyHist = MainRepo.GetStudyHistory(id, startRawTime, numRecords);
                return Ok(studyHist);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }
        #endregion StudyHistory

    }
}
