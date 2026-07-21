using FIGCommon.Controllers;
using FIGCommon.DataAccess;
using FIGCommon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FIGAutoTraderAdminSvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingController : FIGBaseController
    {

        public SettingController(
                   IWebHostEnvironment hostEnvironment
                   , IHttpContextAccessor httpContextAccessor
                   , ILogger<SettingController> logger
                   , IServiceProvider serviceProvider
                   , IConfiguration config
                   ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
        }

        #region Setting

        // GET api/setting/{group}/{tag}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("{group}/{tag}")]
        [HttpGet]
        public async Task<IActionResult> GetSetting(string group, string tag)
        {
            try
            {
                return Ok(MainRepo.GetSetting(group, tag));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/setting/all/{group}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("all/{group}")]
        [HttpGet]
        public async Task<IActionResult> GetSettings(string group)
        {
            try
            {
                return Ok(MainRepo.GetSettings(group));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/setting/upsert
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin + ",")]
        [Route("upsert")]
        [HttpPost]
        public async Task<IActionResult> Upsertsetting([FromBody] SettingRS setting)
        {
            try
            {
                return Ok(MainRepo.UpsertSetting(setting));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // DELETE api/data/setting/{group}/{tag}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("{group}/{tag}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteSetting(string group, string tag)
        {
            try
            {
                MainRepo.DeleteSetting(group, tag);
                return Ok();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }
        #endregion Setting

    }
}