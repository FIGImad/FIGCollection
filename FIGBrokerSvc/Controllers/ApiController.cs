using FIGCommon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FIGCommon.Controllers;
using FIGBrokerSvc.Services;
using FIGCommon.Exceptions;
using FIGCommon.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FIGCommon.Models.FIGBroker;

namespace FIGBrokerSvc.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApiController : FIGBaseController
    {
        protected readonly OrderService _orderInstructionSvc;

        private static object _fileLock = new();
        public ApiController(
              IWebHostEnvironment hostEnvironment
            , IHttpContextAccessor httpContextAccessor
            , ILogger<ApiController> logger
            , IServiceProvider serviceProvider
            , IConfiguration config
            , OrderService orderInstructionSvc
            ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _orderInstructionSvc = orderInstructionSvc ?? throw new ArgumentNullException(nameof(orderInstructionSvc));
        }


        #region Config

        //// GET api/config/load/config
        //[Authorize(Roles = $"{Role.Admin},{Role.SuperAdmin},{Role.Operator}")]
        //[Route("config/load/config")]
        //[HttpGet]
        //public async Task<IActionResult> LoadProvidersConfig()
        //{
        //    try
        //    {
        //        return Ok(_orderInstructionSvc.LoadConfig());
        //    }
        //    catch (Exception e)
        //    {
        //        return await OnException(e);
        //    }
        //}


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