using FIGAutoTraderAdminSvc.Models;
using FIGAutoTraderAdminSvc.Services;
using FIGCommon.Controllers;
using FIGCommon.DataAccess;
using FIGCommon.Exceptions;
using FIGCommon.Models;
using FIGCommon.Services;
using FIGCommon.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace FIGAutoTraderAdminSvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutoTradeController : FIGBaseController
    {
        protected readonly ATSApiService _ATSApiSvc;

        public AutoTradeController(
                   IWebHostEnvironment hostEnvironment
                   , IHttpContextAccessor httpContextAccessor
                   , ILogger<AutoTradeController> logger
                   , IServiceProvider serviceProvider
                   , IConfiguration config
                   , ATSApiService ATSApiSvc
                   ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _ATSApiSvc = ATSApiSvc ?? throw new ArgumentNullException(nameof(ATSApiSvc));
        }

        #region AutoTrade

        // GET api/autotrade/all
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("all")]
        [HttpGet]
        public async Task<IActionResult> GetAutoTrades()
        {
            try
            {
                var autotrades = MainRepo.GetAutoTrades();

                var tasks = autotrades.Select(async autoTrade =>
                {
                    autoTrade.ExecStatus = new(autoTrade.Id);
                    autoTrade.ExecStatus.Status = autoTrade.Status;

                    if (autoTrade.Status == (int)EAutoTradeStatus.Started)
                    {
                        AutoTradeExecStatus? status = await _ATSApiSvc.GetExecStatus(autoTrade.Id, 2000);
                        if (status != null)
                        {
                            status.AutoTradeId = autoTrade.Id;
                            autoTrade.ExecStatus = new(status);
                        }
                    }
                });

                try
                {
                    await Task.WhenAll(tasks);
                    return Ok(autotrades);
                }
                catch(AppErrorException e)
                {
                    if (e.errorCode == ErrorCodes.SysError_GatewayTimeout)
                    {
                        return Ok(autotrades); // return auttrades without status
                    }
                    throw;
                }

            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/autotrade/list
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("list")]
        [HttpPost]
        public async Task<IActionResult> GetAutoTradesFromList([FromBody] List<int> ids)
        {
            try
            {
                List<AutoTradeRS> autotrades = new();
                // get autotrades
                ids.ForEach(id =>
                {
                    if (id >= 0)
                    {
                        var autoTrade = MainRepo.GetAutoTrade(id);
                        if (autoTrade != null)
                        {
                            autoTrade.ExecStatus = new(autoTrade.Id);
                            autoTrade.ExecStatus.Status = autoTrade.Status;
                            autotrades.Add(autoTrade);
                        }
                    }
                });
                var tasks = autotrades.Select(async autoTrade =>
                {
                    if (autoTrade.Status == (int)EAutoTradeStatus.Started)
                    {
                        AutoTradeExecStatus? status = await _ATSApiSvc.GetExecStatus(autoTrade.Id, 2000);
                        if (status != null)
                        {
                            status.AutoTradeId = autoTrade.Id;
                            autoTrade.ExecStatus = new(status);
                        }
                    }
                });
                try
                {
                    await Task.WhenAll(tasks);
                    return Ok(autotrades);
                }
                catch (AppErrorException e)
                {
                    if (e.errorCode == ErrorCodes.SysError_GatewayTimeout)
                    {
                        return Ok(autotrades); // return auttrades without status
                    }
                    throw;
                }
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }


        // GET api/autotrade/{id}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("{id}")]
        [HttpGet]
        public async Task<IActionResult> GetAutoTrade(int id)
        {
            try
            {
                var autotrade = MainRepo.GetAutoTrade(id);
                if (autotrade == null)
                {
                    throw new Exception("DataError_NoData");
                }
                autotrade.ExecStatus = new(autotrade.Id);
                autotrade.ExecStatus.Status = autotrade.Status;
                if (autotrade.Status == (int)EAutoTradeStatus.Started)
                {
                    // find real status if it is really getting executed
                    AutoTradeExecStatus? status = await _ATSApiSvc.GetExecStatus(autotrade.Id, 2000);
                    if (status != null)
                    {
                        status.AutoTradeId = autotrade.Id;
                        autotrade.ExecStatus = new(status);
                    }
                }
                return Ok(autotrade);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/autotrade/upsert
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("upsert")]
        [HttpPost]
        public async Task<IActionResult> UpsertAutoTrade([FromBody] Object autoTradeIn)
        {
            try
            {
                // deserialize JObject to AutoTradeRS
                var autoTrade = JsonConvert.DeserializeObject<AutoTradeRS>(autoTradeIn?.ToString()??"");

                if (autoTrade == null)
                {
                    throw new Exception("DataError_NoData");
                }
                //AutoTradeRS autoTrade = (AutoTradeRS)autoTradeIn;
                var autotrade = MainRepo.UpsertAutoTrade(autoTrade);
                return Ok(autotrade);
            }
            //catch (JsonException jsonEx)
            //{
            //    // Enhanced error logging
            //    var rawData = autoTradeIn.ToString();
            //    _logger.LogError(jsonEx, "JSON deserialization failed. Data: {RawData}", rawData);

            //    // Try to identify the problematic property
            //    try
            //    {
            //        var jObj = JObject.Parse(rawData);
            //        var expectedProps = typeof(AutoTradeRS).GetProperties();
            //        foreach (var prop in jObj.Properties())
            //        {
            //            if (!expectedProps.Any(p =>
            //                string.Equals(p.Name, prop.Name, StringComparison.OrdinalIgnoreCase)))
            //            {
            //                return BadRequest($"Unexpected property: {prop.Name}");
            //            }
            //        }
            //    }
            //    catch { }

            //    return BadRequest($"Invalid JSON format: {jsonEx.Message}");
            //}
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/autotrade/upsertdeep
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("upsertdeep")]
        [HttpPost]
        public async Task<IActionResult> UpsertAutoTradeDeep([FromBody] Object autoTradeIn)
        {
            try
            {
                // deserialize JObject to AutoTradeRS
                var autoTrade = JsonConvert.DeserializeObject<AutoTradeRS>(autoTradeIn?.ToString() ?? "");

                if (autoTrade == null)
                {
                    throw new Exception("DataError_NoData");
                }
                //AutoTradeRS autoTrade = (AutoTradeRS)autoTradeIn;
                var autotrade = MainRepo.UpsertAutoTrade(autoTrade);
                return Ok(autotrade);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/autotrade/duplicate
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("duplicate")]
        [HttpPost]
        public async Task<IActionResult> DuplicateAutoTrade([FromBody] AutoTradeRS autoTradeIn)
        {
            try
            {
                int autoTradeId = autoTradeIn.Id;
                autoTradeIn.Id = -1;
                autoTradeIn.Status = 0;

                var autotrade = MainRepo.UpsertAutoTrade(autoTradeIn);

                return Ok(autotrade);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // DELETE api/autotrade/{id}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("{id}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteAutoTrade(int id)
        {
            try
            {
                if (_ATSApiSvc != null)
                {
                    try { await _ATSApiSvc.StopAutoTradeExec(id, 10000); }
                    catch (Exception ex) { _logger.LogWarning(ex, "StopAutoTradeExec failed for id {Id} before delete.", id); }
                }
                MainRepo.DeleteAutoTrade(id);
                return Ok();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/autotrade/deleterange
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("deleterange")]
        [HttpPost]
        public async Task<IActionResult> DeleteAutoTrades([FromBody] List<int> ids)
        {
            try
            {
                ids.ForEach(id =>
                {
                    if (id >= 0)
                    {
                        if (_ATSApiSvc != null)
                        {
                            try { _ATSApiSvc.StopAutoTradeExec(id, 2000).GetAwaiter().GetResult(); }
                            catch (Exception ex) { _logger.LogWarning(ex, "StopAutoTradeExec failed for id {Id} before delete.", id); }
                        }
                        MainRepo.DeleteAutoTrade(id);
                    }
                });
                return Ok();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("execrange")]
        [HttpPost]
        public async Task<IActionResult> ExecAutoTradeRange([FromBody] List<int> autoTradeIds)
        {
            try
            {
                if (autoTradeIds == null || autoTradeIds.Count == 0)
                    return Ok(new List<ExecAutoTradeRangeResultDto>());

                var tasks = autoTradeIds
                    .Where(id => id >= 0)
                    .Select(async id =>
                    {
                        try
                        {
                            var autoTrade = MainRepo.GetAutoTrade(id);
                            if (autoTrade == null)
                            {
                                return new ExecAutoTradeRangeResultDto
                                {
                                    AutoTradeId = id,
                                    Status = -1
                                };
                            }

                            return new ExecAutoTradeRangeResultDto
                            {
                                AutoTradeId = id,
                                Status = (int)await _ATSApiSvc.StartAutoTradeExec(autoTrade.Id, 10000)
                            };
                        }
                        catch
                        {
                            return new ExecAutoTradeRangeResultDto
                            {
                                AutoTradeId = id,
                                Status = -2
                            };
                        }
                    });
                var results = await Task.WhenAll(tasks);

                return Ok(results.Where(x => x is not null).Select(x => x!).ToList());
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/autotrade/stoprange
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("stoprange")]
        [HttpPost]
        public async Task<IActionResult> StopAutoTradeRange([FromBody] List<int> autoTradeIds)
        {
            try
            {
                if (autoTradeIds == null || autoTradeIds.Count == 0)
                    return Ok(new List<ExecAutoTradeRangeResultDto>());

                var tasks = autoTradeIds
                    .Where(id => id >= 0)
                    .Select(async id =>
                    {
                        try
                        {
                            var autoTrade = MainRepo.GetAutoTrade(id);
                            if (autoTrade == null)
                            {
                                return new ExecAutoTradeRangeResultDto
                                {
                                    AutoTradeId = id,
                                    Status = -1
                                };
                            }

                            return new ExecAutoTradeRangeResultDto
                            {
                                AutoTradeId = id,
                                Status = (int)await _ATSApiSvc.StopAutoTradeExec(autoTrade.Id, 10000)
                            };
                        }
                        catch
                        {
                            return new ExecAutoTradeRangeResultDto
                            {
                                AutoTradeId = id,
                                Status = -2
                            };
                        }
                    });
                var results = await Task.WhenAll(tasks);

                return Ok(results.Where(x => x is not null).Select(x => x!).ToList());
            }
            catch (Exception e)
            {
                return OnException(e);
            }

        }


        // Post api/autotrade/exec/status
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("exec/status")]
        [HttpPost]
        public async Task<IActionResult> GetExecAutoTradeStatus([FromBody] List<int> autoTradeIds)
        {
            try
            {
                return Ok(await _ATSApiSvc.GetExecStatusFromList(autoTradeIds, 10000));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        #endregion AutoTrade

        #region AutoTradeSignal
        // GET api/autotrade/signals/{id}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("signals/{id}")]
        [HttpGet]
        public async Task<IActionResult> GetAutoTradeSignals(int id)
        {
            try
            {
                return Ok(MainRepo.GetAutoTradeSignalsFromSingalId(id));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // Post api/autotrade/signal/manualqty
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("signal/manualqty")]
        [HttpPost]
        public async Task<IActionResult> SettAutoTradeSignalManualQty([FromBody] Object content)
        {
            try
            {
                // content is in the format {"signalId": signalId, "manualQty": newQty}
                // extract both signalId and newQty from content
                var jsonObj = JsonConvert.DeserializeObject<dynamic>(content.ToString() ?? "");
                if (jsonObj != null)
                {
                    int signalId = jsonObj.signalId;
                    int newQty = jsonObj.manualQty;
                    return Ok(MainRepo.SetAutoTradeSignalManualQty(signalId, newQty));
                }
                else
                {
                    throw new Exception("DataError_NoData");
                }
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        #endregion AutoTradeSignal

        #region AutoTradeSignalOrder
        // GET api/autotrade/signal/orders/{autotradesignalid}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("signal/orders/{autotradesignalid}")]
        [HttpGet]
        public async Task<IActionResult> GetAutoTradeSignalOrders(int autotradeSignalId)
        {
            try
            {
                return Ok(MainRepo.GetAutoTradeSignalOrdersByAutoTradeSignalId(autotradeSignalId));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }
        #endregion AutoTradeSignalOrder

    }
}