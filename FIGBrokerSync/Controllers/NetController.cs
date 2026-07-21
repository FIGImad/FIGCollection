using FIGCommon.Controllers;
using FIGCommon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;


namespace FIGBrokerSvc.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class NetController : FIGBaseController
    {
        public NetController(
              IWebHostEnvironment hostEnvironment
            , IHttpContextAccessor httpContextAccessor
            , ILogger<NetController> logger
            , IServiceProvider serviceProvider
            , IConfiguration config
            ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
        }


        // GET net/ping
        [AllowAnonymous]
        [Route("ping")]
        [HttpGet]
        public PongDto Ping()
        {
            _logger.LogDebug("Received a Ping");
            return new PongDto();
        }


        // GET net/sping
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin + "," + Role.SVC)]
        [Route("sping")]
        [HttpGet]
        public PongDto SecuredPing()
        {
            _logger.LogDebug("Received a Ping");
            //var callerIp = GetCallerIpAddress();

            return new PongDto();
        }

        protected string GetCallerIpAddress()
        {
            var ip = HttpContext?.Connection?.RemoteIpAddress;

            if (ip is null)
                return "UNKNOWN";

            if (IPAddress.IsLoopback(ip))
                return "localhost";

            if (ip.IsIPv4MappedToIPv6)
                ip = ip.MapToIPv4();

            return ip.ToString();
        }

    }

}