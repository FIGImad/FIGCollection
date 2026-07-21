using FIGCommon.Controllers;
using FIGCommon.DataAccess;
using FIGCommon.Models;
using FIGCommon.Models.FIGController;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FIGControllerSvc.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApiController : FIGBaseController
    {
        private readonly ControllerConfig _controllerConfig;

        public ApiController(
              IWebHostEnvironment hostEnvironment
            , IHttpContextAccessor httpContextAccessor
            , ILogger<ApiController> logger
            , IServiceProvider serviceProvider
            , IConfiguration config
            ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _controllerConfig = new();
            _config.GetSection("ControllerConfig").Bind(_controllerConfig);
            _controllerConfig.Validate();
        }

        // GET api/device/list
        [Authorize(Roles = $"{Role.SVC},{Role.Admin},{Role.SuperAdmin}")]
        [Route("device/list")]
        [HttpGet]
        public async Task<IActionResult> GetDeviceList()
        {
            try
            {
                return Ok(MainRepo.GetActiveDevices());
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }


        //// POST api/register/service
        //[Authorize(Roles = $"{Role.SVC}")]
        ////[AllowAnonymous]
        //[Route("register/service")]
        //[HttpPost]
        //public async Task<IActionResult> RegisterService([FromBody] ControllerConfig regInfo)
        //{
        //    try
        //    {
        //        string localIPAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList
        //            .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        //            ?.ToString() ?? "127.0.0.1";

        //        var remoteIp = HttpContext.Connection.RemoteIpAddress;
        //        bool isLoopback = remoteIp != null && System.Net.IPAddress.IsLoopback(remoteIp);
        //        string? resolvedIp = isLoopback ? localIPAddress : remoteIp?.MapToIPv4().ToString();

        //        regInfo.HostAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
        //            ?? resolvedIp
        //            ?? regInfo.HostAddress;
        //        return Ok(_regSvc.RegisterService(regInfo));
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}

        //// POST api/route
        //[Authorize(Roles = $"{Role.SVC}")]
        //[Route("route")]
        //[HttpPost]
        //public async Task<IActionResult> RouteRequest([FromBody] ControllerRouteRequest req)
        //{
        //    if (req is null)
        //    {
        //        throw new ArgumentNullException(nameof(req));
        //    }
        //    try
        //    {
        //        return Ok(await _regSvc.RouteAsync(req));
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}

    }
}
