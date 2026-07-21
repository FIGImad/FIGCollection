using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FIGCommon.Models;

namespace FIGAutoTraderAdminSvc.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NetController : ControllerBase
    {
        private readonly ILogger<NetController> _logger;
        //private readonly SyncTradeBotInstructionsService _syncSvc;


        public NetController(
                ILogger<NetController> logger
              //, SyncTradeBotInstructionsService syncSvc
            )
        {
            _logger = logger;
            //_syncSvc = syncSvc;
        }


        // GET net/ping
        [AllowAnonymous]
        [Route("ping")]
        [HttpGet]
        public PongDto Ping()
        {
            //_logger.LogDebug("Received a Ping");
            return new PongDto();
        }


        // GET net/sping
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin + "," + Role.SVC)]
        [Route("sping")]
        [HttpGet]
        public PongDto SecuredPing()
        {
            //_logger.LogDebug("Received a Ping");
            return new PongDto();
        }

        //// GET net/enqueue
        //[AllowAnonymous]
        //[Route("enqueue")]
        //[HttpGet]
        //public Task<IActionResult> TestQueueEvent()
        //{
        //    TaskEvent evt = new TaskEvent("TestEvent", new Newtonsoft.Json.Linq.JObject());
        //    _syncSvc.Enqueue(0, evt);
        //    return Task.FromResult<IActionResult>(Ok(true));
        //}

        // GET net/error
        [AllowAnonymous]
        [Route("error")]
        [HttpGet]
        public IActionResult Error()
        {
            _logger.LogError("Global error handler invoked");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An error occurred while processing your request."
            );
        }

    }

}