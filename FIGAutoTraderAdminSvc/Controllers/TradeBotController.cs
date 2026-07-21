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
    public class TradeBotController : FIGBaseController
    {
        protected readonly ATSApiService _ATSApiSvc;

        public TradeBotController(
                   IWebHostEnvironment hostEnvironment
                   , IHttpContextAccessor httpContextAccessor
                   , ILogger<TradeBotController> logger
                   , IServiceProvider serviceProvider
                   , IConfiguration config
                   , ATSApiService ATSApiSvc
                   ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _ATSApiSvc = ATSApiSvc;
        }

        #region TradeBot

        // GET api/tradebot/all
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("all")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                return Ok(MainRepo.GetAutotradeBots());
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/tradebot/{id}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("{id}")]
        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                return Ok(MainRepo.GetBot(id));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/tradebot/upsert
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("upsert")]
        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] BotRS tradeBot)
        {
            try
            {
                return Ok(MainRepo.UpsertAutotradeBot(tradeBot));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // DELETE api/tradebot/{id}}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("{id}")]
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                return Ok(MainRepo.DeleteAutotradeBot(id));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        #endregion TradeBot

        //#region TradeBotInstruction

        //// GET api/tradebot/lastinstructions/{botId}/{autoTradeId}/{numRec}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("lastinstructions/{botId}/{autoTradeId}/{numRec}")]
        //[HttpGet]
        //public async Task<IActionResult> GetLastInstructions(int botId, int autoTradeId, int numRec)
        //{
        //    try
        //    {
        //        return Ok(MainRepo.GetLastTradeBotInstructions(botId, autoTradeId, numRec));
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}

        //// POST api/tradebot/instruction/insert
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("instruction/insert")]
        //[HttpPost]
        //public async Task<IActionResult> InsertInstruction([FromBody] TradeBotInstructionRS instruction)
        //{
        //    try
        //    {
        //        var res = MainRepo.InsertTradeBotInstruction(instruction);
        //        _ATSApiSvc?.WakeupBotInstructionSyncProcess();
        //        return Ok(res);
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}

        //// DELETE api/tradebot/instruction/{botId}/{id}
        //[Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        //[Route("instruction/{botId}/{id}")]
        //[HttpDelete]
        //public async Task<IActionResult> DeleteInstructions(int botId, int id)
        //{
        //    try
        //    {
        //        if (id <= 0)
        //        {
        //            return Ok(MainRepo.DeleteTradeBotInstructions(botId));
        //        }
        //        else
        //        {
        //            return Ok(MainRepo.DeleteTradeBotInstruction(botId, id));
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        return OnException(e);
        //    }
        //}

        //#endregion TradeBotInstructions

    }
}