using FIGAutoTraderAdminSvc.Models;
using FIGAutoTraderAdminSvc.Services;
using FIGCommon.Controllers;
using FIGCommon.DataAccess;
using FIGCommon.Exceptions;
using FIGCommon.Models;
using FIGCommon.Models.FIGController;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FIGAutoTraderAdminSvc.Controllers
{


    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : FIGBaseController
    {
        private readonly ControllerApiService _controllerAPI;
        private readonly ServiceMgrApi _serviceMgrApi;
        private readonly IConnectionStatusStore _connectionStatusStore;
        private readonly IHubContext<ATSMonitorHub> _monitorHub;
        private readonly ATSMonitorService _atsMonitorService;

        public DeviceController(
                   IWebHostEnvironment hostEnvironment
                 , IHttpContextAccessor httpContextAccessor
                 , ILogger<DeviceController> logger
                   , IServiceProvider serviceProvider
                 , IConfiguration config
                 , ControllerApiService controllerAPI
                 , ServiceMgrApi serviceMgrApi
                 , IConnectionStatusStore connectionStatusStore
                 , IHubContext<ATSMonitorHub> monitorHub
                 , ATSMonitorService atsMonitorService
                 ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _controllerAPI = controllerAPI ?? throw new ArgumentNullException(nameof(controllerAPI));
            _serviceMgrApi = serviceMgrApi ?? throw new ArgumentNullException(nameof(serviceMgrApi));
            _connectionStatusStore = connectionStatusStore ?? throw new ArgumentNullException(nameof(connectionStatusStore));
            _monitorHub = monitorHub ?? throw new ArgumentNullException(nameof(monitorHub));
            _atsMonitorService = atsMonitorService ?? throw new ArgumentNullException(nameof(atsMonitorService));
        }


        #region DeviceList

        // GET api/device/list
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("list")]
        [HttpGet]
        public async Task<IActionResult> GetConnectedDevices()
        {
            try
            {
                AdminResponesDto response = new AdminResponesDto()
                {
                    Id = "",
                    Status = AdminResponesDto.UNABLE_TO_RETRIEVE_LIST,
                    Message = "Unable to retrieve the list of connected devices",
                    Obj = null
                };

                // create an array of database names from _dbItems
                List<DeviceRS>? deviceList = MainRepo.GetDevices();
                if (deviceList != null)
                {
                    response = new AdminResponesDto()
                    {
                        Id = "",
                        Status = AdminResponesDto.SUCCESS,
                        Message = "",
                        Obj = deviceList
                    };
                }
                return Ok(response);
            }
            catch (AppErrorException e)
            {
                if (e.errorCode == 500)
                {
                    // server may be down
                }
                return OnException(e);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        #endregion DeviceList

        #region DeviceAdministration

        // POST api/device/{id}/enabled/{enabled}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("{id}/enabled/{enabled:bool}")]
        [HttpPost]
        public async Task<IActionResult> SetDeviceEnabled(string id, bool enabled)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(CreateDeviceActionResponse(id, AdminResponesDto.FAILED_TO_FIX_ISSUE, "Device id is required."));
                }

                int rowsUpdated = MainRepo.SetDeviceEnabled(id, enabled);
                if (rowsUpdated == 0)
                {
                    return NotFound(CreateDeviceActionResponse(id, AdminResponesDto.FAILED_TO_FIX_ISSUE, $"Device '{id}' was not found."));
                }

                await PublishDeviceStatusChange(id);

                return Ok(CreateDeviceActionResponse(
                    id,
                    AdminResponesDto.SUCCESS,
                    enabled ? "Device enabled." : "Device disabled."));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // DELETE api/device/{id}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("{id}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteDevice(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(CreateDeviceActionResponse(id, AdminResponesDto.FAILED_TO_FIX_ISSUE, "Device id is required."));
                }

                int rowsDeleted = MainRepo.DeleteDevice(id);
                await PublishDeviceStatusChange(id);

                if (rowsDeleted == 0)
                {
                    return NotFound(CreateDeviceActionResponse(id, AdminResponesDto.FAILED_TO_FIX_ISSUE, $"Device '{id}' was not found."));
                }

                return Ok(CreateDeviceActionResponse(id, AdminResponesDto.SUCCESS, "Device deleted."));
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        private async Task PublishDeviceStatusChange(string deviceId)
        {
            _atsMonitorService.InvalidateDeviceListCache();
            _connectionStatusStore.Remove(deviceId);

            await _monitorHub.Clients.All.SendAsync(
                "ConnectionStatusChanged",
                _connectionStatusStore.GetAll());
        }

        private static AdminResponesDto CreateDeviceActionResponse(string id, int status, string message)
        {
            return new AdminResponesDto()
            {
                Id = id,
                Status = status,
                Message = message,
                Obj = null
            };
        }

        #endregion DeviceAdministration


        #region Services

        // GET api/device/service/status
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("service/status/{serviceId}/{serviceName}")]
        [HttpGet]
        public async Task<IActionResult> GetServiceStatus(string serviceId, string serviceName)
        {
            try
            {

                ServiceStatusDto? status = await _serviceMgrApi.GetStatusAsync(serviceId, serviceName, 2000);
                if (status == null)
                {
                    throw new Exception("Could not retrieve service status");
                }
                return Ok(status);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/device/service/start/{serviceId}/{serviceName}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("service/start/{serviceId}/{serviceName}")]
        [HttpPost]
        public async Task<IActionResult> StartService(string serviceId, string serviceName)
        {
            try
            {

                ServiceStatusDto? status = await _serviceMgrApi.StartAsync(serviceId, serviceName, 12000);
                if (status == null)
                {
                    throw new Exception("Could not retrieve service status");
                }
                return Ok(status);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/device/service/stop/{serviceId}/{serviceName}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("service/stop/{serviceId}/{serviceName}")]
        [HttpPost]
        public async Task<IActionResult> StopService(string serviceId, string serviceName)
        {
            try
            {

                ServiceStatusDto? status = await _serviceMgrApi.StopAsync(serviceId, serviceName, 12000);
                if (status == null)
                {
                    throw new Exception("Could not retrieve service status");
                }
                return Ok(status);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }


        #endregion Services


    }
}
