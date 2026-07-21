using FIGCommon.Controllers;
using FIGCommon.DataAccess;
using FIGCommon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FIGAutoTraderAdminSvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataSetController : FIGBaseController
    {
        public DataSetController(
            IWebHostEnvironment hostEnvironment
            , IHttpContextAccessor httpContextAccessor
            , ILogger<DataSetController> logger
            , IServiceProvider serviceProvider
            , IConfiguration config
            ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
        }

        #region DataSet

        // GET api/dataset/all
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("all")]
        [HttpGet]
        public async Task<IActionResult> GetDataSets()
        {
            try
            {
                var dataSets = MainRepo.GetDataSets();
                return Ok(dataSets);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/dataset/id/{id}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("id/{id}")]
        [HttpGet]
        public async Task<IActionResult> GetDataSet(int id)
        {
            try
            {
                var dataSets = MainRepo.GetDataSet(id);
                return Ok(dataSets);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/dataset/query/{symbol}/{interval}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("query/{symbol}/{interval}")]
        [HttpGet]
        public async Task<IActionResult> QueryDataSet(string symbol, string interval)
        {
            try
            {
                var ticker = MainRepo.GetTickerBySymbol(symbol);
                if (ticker == null)
                {
                    throw new Exception("Invalid Symbol");
                }
                var dataSets = MainRepo.QueryDataSet(ticker.Id, interval);
                if (dataSets == null)
                {
                    throw new Exception("Invalid dataset");
                }
                return Ok(dataSets);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }


        // POST api/dataset/upsert
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("upsert")]
        [HttpPost]
        public async Task<IActionResult> UpsertDataSet([FromBody] DataSetRS dataSetIn)
        {
            try
            {
                var dataSet = MainRepo.UpsertDataSet(dataSetIn);
                return Ok(dataSet);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        #endregion DataSet

    }
}