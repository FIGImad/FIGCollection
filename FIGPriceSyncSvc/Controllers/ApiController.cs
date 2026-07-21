using FIGCommon.Controllers;
using FIGCommon.DataAccess;
using FIGCommon.Exceptions;
using FIGCommon.Models;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Utilities;
using FIGPriceSyncSvc.Model;
using FIGPriceSyncSvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace FIGPriceSyncSvc.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApiController : FIGBaseController
    {
        protected readonly PriceSyncService _priceSyncSvc;

        private static object _fileLock = new();
        public ApiController(
              IWebHostEnvironment hostEnvironment
            , IHttpContextAccessor httpContextAccessor
            , ILogger<ApiController> logger
            , IServiceProvider serviceProvider
            , IConfiguration config
            , PriceSyncService providerSvc
            ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _priceSyncSvc = providerSvc ?? throw new ArgumentNullException(nameof(providerSvc));
        }

        #region Lookup

        // GET api/lookup/symbol/{symbol}
        [Authorize(Roles = $"{Role.Admin},{Role.SuperAdmin},{Role.Operator}")]
        //[AllowAnonymous]
        [Route("lookup/symbol/{symbol}")]
        [HttpGet]
        public async Task<IActionResult> LookupSymbol(string symbol)
        {
            try
            {
                return Ok(_priceSyncSvc.LookupSymbol(symbol));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET provider/lookup/future/{symbol}/{expiry}/{exchange}/{currency}
        [Authorize(Roles = $"{Role.Admin},{Role.SuperAdmin},{Role.Operator}")]
        [Route("lookup/future/{symbol}")]
        [HttpGet]
        public async Task<IActionResult> LookupFuture(string symbol,
                                                      [FromQuery] string exchange = "",
                                                      [FromQuery] string expiry = "",
                                                      [FromQuery] string currency = "")
        {
            try
            {
                InstrumentLookupRequest lookupContract = new InstrumentLookupRequest()
                {
                    Symbol = symbol,
                    SecType = "FUT",
                    Exchange = exchange,
                    Currency = currency,
                    LastTradeDateOrContractMonth = expiry
                };
                return Ok(_priceSyncSvc.FutureContractDetails(lookupContract));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        #endregion Lookup

        #region MarketData

        // GET api/marketdata/{tickerId}
        [Authorize(Roles = $"{Role.Admin},{Role.SuperAdmin},{Role.Operator}")]
        //[AllowAnonymous]
        [Route("marketdata/{tickerId}")]
        [HttpGet]
        public async Task<IActionResult> MarketDataRequest(int tickerId)
        {
            try
            {
                //todo: revise
                throw new NotImplementedException();

                TickerRS? ticker = MainRepo.GetTicker(tickerId);
                if (ticker == null) return NotFound($"Ticker with id {tickerId} not found");
                var setting = MainRepo.GetSetting("MarketData", "Subscription");
                if (setting != null)
                {
                    // convert setting.Value from string to List<int>
                    List<int>? subscribedTickers = JsonConvert.DeserializeObject<List<int>>(setting.Value);
                    // find tickerId in subscribedTickers, if not found add it and update setting.Value
                    if (subscribedTickers != null && !subscribedTickers.Contains(tickerId))
                    {
                        subscribedTickers.Add(tickerId);
                        setting.Value = JsonConvert.SerializeObject(subscribedTickers);
                        MainRepo.UpsertSetting(setting);
                        _priceSyncSvc.SubscribeMarketData(ticker);
                    }
                }
                else
                {
                    // create new setting
                    List<int> subscribedTickers = new List<int>() { tickerId };
                    setting = new SettingRS()
                    {
                        Group = "MarketData",
                        Tag = "Subscription",
                        Value = JsonConvert.SerializeObject(subscribedTickers)
                    };
                    MainRepo.UpsertSetting(setting);
                    _priceSyncSvc.SubscribeMarketData(ticker);
                }
                return Ok();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        #endregion MarketData


        #region Config

        // GET api/config/load/providers
        [Authorize(Roles = $"{Role.Admin},{Role.SuperAdmin},{Role.Operator}")]
        [Route("config/load/providers")]
        [HttpGet]
        public async Task<IActionResult> LoadProvidersConfig()
        {
            try
            {
                return Ok(_priceSyncSvc.LoadConfig());
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }


        // GET api/config/load/loglevel
        [Authorize(Roles = $"{Role.Admin},{Role.SuperAdmin},{Role.Operator}")]
        [Route("config/load/loglevel")]
        [HttpGet]
        public async Task<IActionResult> LoadLogLevelConfig()
        {
            try
            {
                return Ok(_config["Serilog:MinimumLevel"] ?? "");
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/config/apply/loglevel
        [Authorize(Roles = $"{Role.Admin},{Role.SuperAdmin},{Role.Operator}")]
        [Route("config/apply/loglevel")]
        [HttpPost]
        public async Task<IActionResult> ApplyConfig([FromBody] LogLevelRequestDto logLevel)
        {
            try
            {
                // validate logLevel is one of Verbose, Debug, Information, Warning, Error, Fatal
                string[] validLogLevels = { "Verbose", "Debug", "Information", "Warning", "Error", "Fatal" };
                if (!validLogLevels.Contains(logLevel.LogLevel))
                {
                    throw new AppErrorException(ErrorCodes.DataError_InvalidData, "", $"Invalid log level: {logLevel.LogLevel}. Valid log levels are: {string.Join(", ", validLogLevels)}");
                }
                // update config
                //_config["Serilog:MinimumLevel"] = logLevel.LogLevel;
                UpdateAppSettings("Serilog:MinimumLevel", logLevel.LogLevel);
                // 6. Optional: Reload configuration
                //((IConfigurationRoot)_config).Reload();

                return Ok();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }


        public static void UpdateAppSettings(string key, string value)
        {
            lock (_fileLock) // Ensure thread safety
            {
                // 1. Locate appsettings.json
                string appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

                // 2. Read and parse JSON
                var jsonContent = System.IO.File.ReadAllText(appSettingsPath);
                var json = JObject.Parse(jsonContent); // Automatically ignores comments

                // 3. Navigate to the target key
                var keys = key.Split(':');
                JToken? current = json;

                for (int i = 0; i < keys.Length - 1; i++)
                {
                    if (current is JObject jObj)
                    {
                        if (jObj[keys[i]] == null)
                        {
                            jObj.Add(keys[i], new JObject());
                        }
                        current = jObj[keys[i]];
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected JSON structure at key: {keys[i]}");
                    }
                }

                // 4. Update the value
                if (current is JObject finalObj)
                {
                    finalObj[keys[^1]] = value;
                }
                else
                {
                    throw new InvalidOperationException($"Unable to set value for key: {key}");
                }

                // 5. Write back with formatting
                System.IO.File.WriteAllText(
                    appSettingsPath,
                    json.ToString(Formatting.Indented) // Newtonsoft.Json's formatter
                );
            }
        }
    }
    #endregion Config
}