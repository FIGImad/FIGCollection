using FIGAutoTraderAdminSvc.Models;
using FIGCommon.Controllers;
using FIGCommon.DataAccess;
using FIGCommon.Models;
using FIGCommon.Utilities;
using FIGCommon.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RootsIdentity.Controllers;
using RootsIdentity.Models;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace FIGAutoTraderAdminSvc.Controllers
{
    

    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseAdminController : FIGBaseController
    {
        private readonly DatabaseItems _dbItems;
        private readonly SystemUsers _systemUsers;
        private readonly string _usersDB = "FIGUserX";
        private readonly string _tempPass = "";
        private readonly string _certThumbPrint;
        private string pricipalLoginUser;

        public DatabaseAdminController(
                   IWebHostEnvironment hostEnvironment
                   , IHttpContextAccessor httpContextAccessor
                   , ILogger<DatabaseAdminController> logger
                   , IServiceProvider serviceProvider
                   , IConfiguration config
                   ) : base(hostEnvironment, httpContextAccessor, logger, serviceProvider, config)
        {
            _dbItems = new DatabaseItems(config);
            pricipalLoginUser = config.GetValue<string>("DatabaseAdmin:LoginName") ?? "roots";
            _usersDB = config.GetValue<string>("DatabaseAdmin:UsersDB") ?? _usersDB;
            _certThumbPrint = config.GetValue<string>("ControllerConfig:CertThumbPrint") ?? "";
            _systemUsers = new SystemUsers(config);
            _tempPass = config.GetValue<string>("TempUser:Password") ?? "";
        }


        #region UsersAdmin

        // GET api/databaseadmin/validateusers
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("validateusers")]
        [HttpGet]
        public async Task<IActionResult> GetUserValidationData()
        {
            try
            {
                AdminResponesDto response = new AdminResponesDto();

                // create an array of database names from _dbItems
                List<string> dbNames = _dbItems.GetDatabaseNames();
                var userStatus = MainRepo.Master_CheckUserConfigured(pricipalLoginUser);
                if (userStatus == null || !userStatus.LoginExists)
                {
                    // user does not exist in the server
                    response = new AdminResponesDto()
                    {
                        Id = "",
                        Status = AdminResponesDto.USER_NOT_FOUND_IN_DATABASE,
                        Message = "User does not exist in the database server",
                        Obj = null
                    };
                    return Ok(response);
                }
                var userDBValidationList = MainRepo.ValidateUserOnDatabase(pricipalLoginUser, dbNames);
                response = new AdminResponesDto()
                {
                    Id = "",
                    Status = AdminResponesDto.SUCCESS,
                    Message = "",
                    Obj = userDBValidationList
                };
                return Ok(response);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/databaseadmin/fixuser/{issueName}/{databaseName}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("fixuser/{issueName}/{databaseName}")]
        [HttpGet]
        public async Task<IActionResult> FixUserIssueAPI(string issueName, string databaseName)
        {
            try
            {
                AdminResponesDto response = new AdminResponesDto()
                {
                    Id = "",
                    Status = AdminResponesDto.SUCCESS,
                    Message = "",
                    Obj = null
                };

                // create an array of database names from _dbItems
                var userStatus = MainRepo.Master_CheckUserConfigured(pricipalLoginUser);
                if (userStatus == null || !userStatus.LoginExists)
                {
                    // user does not exist in the server
                    response = new AdminResponesDto()
                    {
                        Id = "",
                        Status = AdminResponesDto.USER_NOT_FOUND_IN_DATABASE,
                        Message = "User does not exist in the database server",
                        Obj = null
                    };
                    return Ok(response);
                }
                var userDBValidationList = MainRepo.ValidateUserOnDatabase(pricipalLoginUser, [databaseName]);
                if (userDBValidationList != null && userDBValidationList.Count > 0)
                {
                    // check if error is fixable
                    if (!userDBValidationList[0].DBExists)
                    {
                        // database does not exist
                        response = new AdminResponesDto()
                        {
                            Id = "",
                            Status = AdminResponesDto.DATABASE_MISSING,
                            Message = "Database does not exist, create it first",
                            Obj = null
                        };
                        return Ok(response);
                    }
                    // try to fix the issue
                    if (!FixUserIssue(userDBValidationList[0], issueName))
                    {
                        switch (issueName)
                        {
                            case "userReg":
                                response.Status = AdminResponesDto.FAILED_TO_FIX_ISSUE;
                                response.Message = "Failed to register user to database";
                                break;
                            case "mappedToLoginSID":
                                response.Status = AdminResponesDto.FAILED_TO_FIX_ISSUE;
                                response.Message = "Failed to fix user SID mapping for database";
                                break;
                            case "isOwner":
                                response.Status = AdminResponesDto.FAILED_TO_FIX_ISSUE;
                                response.Message = "Failed to fix register user as db_owner for database";
                                break;
                        }
                    }
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/databaseadmin/fixalluser
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("fixalluser")]
        [HttpGet]
        public async Task<IActionResult> FixAllUser()
        {
            try
            {
                AdminResponesDto response = new AdminResponesDto();
                var userStatus = MainRepo.Master_CheckUserConfigured(pricipalLoginUser);
                if (userStatus == null || !userStatus.LoginExists)
                {
                    // user does not exist in the server
                    response = new AdminResponesDto()
                    {
                        Id = "",
                        Status = AdminResponesDto.USER_NOT_FOUND_IN_DATABASE,
                        Message = "User does not exist in the database server",
                        Obj = null
                    };
                    return Ok(response);
                }

                List<string> dbNames = _dbItems.GetDatabaseNames();
                var userDBValidationList = MainRepo.ValidateUserOnDatabase(pricipalLoginUser, dbNames);

                int errorCount = 0;
                if (userDBValidationList != null && userDBValidationList.Count > 0)
                {
                    userDBValidationList.ForEach((userDBValidation) =>
                    {
                        // check if error is fixable
                        if (!userDBValidation.DBExists)
                        {
                            errorCount++;
                        }
                        else
                        {
                            if (!userDBValidation.UserReg) errorCount += (FixUserIssue(userDBValidation, "userReg") ? 0 : 1);
                            if (!userDBValidation.MappedToLoginSID) errorCount += (FixUserIssue(userDBValidation, "mappedToLoginSID") ? 0 : 1);
                            if (!userDBValidation.IsOwner) errorCount += (FixUserIssue(userDBValidation, "isOwner") ? 0 : 1);
                        }
                    });
                }
                response = new AdminResponesDto()
                {
                    Id = "",
                    Status = errorCount > 0 ? AdminResponesDto.FAILED_TO_FIX_ISSUE : AdminResponesDto.SUCCESS,
                    Message = "",
                    Obj = null
                };
                return Ok(response);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        public bool FixUserIssue(DBUserValidationRS databaseItem, string issueName)
        {
            try
            {
                switch (issueName)
                {
                    case "userReg":
                        if (!databaseItem.UserReg)
                        {
                            if (!MainRepo.Master_RegisterUserToDB(pricipalLoginUser, databaseItem.DatabaseName))
                            {
                                return false;
                            }
                        }
                        break;
                    case "mappedToLoginSID":
                        if (!databaseItem.MappedToLoginSID)
                        {
                            if (!MainRepo.Master_FixUserSIDMapping(pricipalLoginUser, databaseItem.DatabaseName))
                            {
                                return false;
                            }
                        }
                        break;
                    case "isOwner":
                        if (!databaseItem.IsOwner)
                        {
                            if (!MainRepo.Master_SetUserOwnership(pricipalLoginUser, databaseItem.DatabaseName))
                            {
                                return false;
                            }
                        }
                        break;
                }
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error fixing user issue: {IssueName} for database: {DatabaseName}", issueName, databaseItem.DatabaseName);
                return false;
            }
        }


        #endregion UsersAdmin


        #region BrokerAdmin

        // GET api/databaseadmin/validatebroker
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("validatebroker")]
        [HttpGet]
        public async Task<IActionResult> GetBrokerValidationData()
        {
            try
            {
                AdminResponesDto response = new AdminResponesDto();

                var userStatus = MainRepo.Master_CheckUserConfigured(pricipalLoginUser);
                if (userStatus == null || !userStatus.LoginExists)
                {
                    // user does not exist in the server
                    response = new AdminResponesDto()
                    {
                        Id = "",
                        Status = AdminResponesDto.USER_NOT_FOUND_IN_DATABASE,
                        Message = "User does not exist in the database server",
                        Obj = null
                    };
                    return Ok(response);
                }

                List<DBBrokerValidationRS> brokerDBList = new List<DBBrokerValidationRS>();
                List<string> dbNames = _dbItems.GetBrokerDatabaseNames();
                dbNames.ForEach(dbName => {
                    DBBrokerValidationRS? brokerDBItem = MainRepo.ValidateDBBroker(pricipalLoginUser, dbName);
                    if (brokerDBItem != null) brokerDBList.Add(brokerDBItem);
                });

                response = new AdminResponesDto()
                {
                    Id = "",
                    Status = AdminResponesDto.SUCCESS,
                    Message = "",
                    Obj = brokerDBList
                };
                return Ok(response);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/databaseadmin/fixbroker/{databaseName}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("fixbroker/{databaseName}")]
        [HttpGet]
        public async Task<IActionResult> FixBrokerAPI(string databaseName)
        {
            try
            {
                AdminResponesDto response = new AdminResponesDto()
                {
                    Id = "",
                    Status = AdminResponesDto.SUCCESS,
                    Message = "",
                    Obj = null
                };

                // create an array of database names from _dbItems
                var userStatus = MainRepo.Master_CheckUserConfigured(pricipalLoginUser);
                if (userStatus == null || !userStatus.LoginExists)
                {
                    // user does not exist in the server
                    response = new AdminResponesDto()
                    {
                        Id = "",
                        Status = AdminResponesDto.USER_NOT_FOUND_IN_DATABASE,
                        Message = "User does not exist in the database server",
                        Obj = null
                    };
                    return Ok(response);
                }
                var brokerItem = MainRepo.ValidateDBBroker(pricipalLoginUser, databaseName);
                if (brokerItem != null)
                {
                    // check if error is fixable
                    if (!brokerItem.Exists)
                    {
                        // database does not exist
                        response = new AdminResponesDto()
                        {
                            Id = "",
                            Status = AdminResponesDto.DATABASE_MISSING,
                            Message = "Database does not exist, create it first",
                            Obj = null
                        };
                        return Ok(response);
                    }
                    // try to fix the issue
                    if (!FixBrokerIssue(brokerItem))
                    {
                        response.Status = AdminResponesDto.FAILED_TO_FIX_ISSUE;
                        response.Message = "Failed to enable broker and/or grant user permissions";
                    }
                }
                return Ok(response);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/databaseadmin/fixallbroker
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("fixallbroker")]
        [HttpGet]
        public async Task<IActionResult> FixAllBroker()
        {
            try
            {
                AdminResponesDto response = new AdminResponesDto();
                var userStatus = MainRepo.Master_CheckUserConfigured(pricipalLoginUser);
                if (userStatus == null || !userStatus.LoginExists)
                {
                    // user does not exist in the server
                    response = new AdminResponesDto()
                    {
                        Id = "",
                        Status = AdminResponesDto.USER_NOT_FOUND_IN_DATABASE,
                        Message = "User does not exist in the database server",
                        Obj = null
                    };
                    return Ok(response);
                }

                List<string> dbNames = _dbItems.GetBrokerDatabaseNames();
                int errorCount = 0;
                dbNames.ForEach((dbName) =>
                {
                    var brokerItem = MainRepo.ValidateDBBroker(pricipalLoginUser, dbName);
                    if (brokerItem == null)
                    {
                        errorCount++;
                    }
                    else
                    {
                        // check if error is fixable
                        if (!brokerItem.Exists)
                        {
                            errorCount++;
                        }
                        else
                        {
                            errorCount += (FixBrokerIssue(brokerItem) ? 0 : 1);
                        }
                    }

                });
                response = new AdminResponesDto()
                {
                    Id = "",
                    Status = errorCount > 0 ? AdminResponesDto.FAILED_TO_FIX_ISSUE : AdminResponesDto.SUCCESS,
                    Message = "",
                    Obj = null
                };
                return Ok(response);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        public bool FixBrokerIssue(DBBrokerValidationRS item)
        {
            try
            {
                if (!MainRepo.FixDBBroker(pricipalLoginUser, item.DatabaseName))
                {
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error fixing broker issue for database: {DatabaseName}", item.DatabaseName);
                return false;
            }
        }


        #endregion BrokerAdmin

        #region SystemUsers

        // GET api/databaseadmin/validatesystemusers
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("validatesystemusers")]
        [HttpGet]
        public async Task<IActionResult> GetSystemUsersValidationData()
        {
            try
            {
                AdminResponesDto response = new AdminResponesDto();

                var userStatus = MainRepo.Master_CheckUserConfigured(pricipalLoginUser);
                if (userStatus == null || !userStatus.LoginExists)
                {
                    // user does not exist in the server
                    response = new AdminResponesDto()
                    {
                        Id = "",
                        Status = AdminResponesDto.USER_NOT_FOUND_IN_DATABASE,
                        Message = "User does not exist in the database server",
                        Obj = null
                    };
                    return Ok(response);
                }

                List<UserRS> configuredUsers = MainRepo.GetUsers(_usersDB);

                foreach (var item in _systemUsers.Get())
                {
                    // check if item.UserName is in configuredUsers
                    // check if username and password match values in configuredUsers
                    var user = configuredUsers.FirstOrDefault(usr => usr.UserName == item.Username);
                    if (user != null)
                    {
                        item.Exists = true;
                        // attempt to login using credentials provided in config to validate password
                        item.CredentialValid = await TryLogin(item.Username, item.Password);

                        // check if roles are setup properly
                        // user.Roles should contain all roles in item.Roles
                        item.RolesValid = item.Roles != null;
                        if (item.Roles != null)
                        {
                            item.Roles.ForEach(role =>
                            {
                                if (user.Roles == null || !user.Roles.Contains(role))
                                {
                                    item.RolesValid = false;
                                }
                            });
                        }
                        item.RolesConfigured = (user.Roles != null && user.Roles.Count > 0) ? user.Roles : new();
                    }
                }

                response = new AdminResponesDto()
                {
                    Id = "",
                    Status = AdminResponesDto.SUCCESS,
                    Message = "",
                    Obj = _systemUsers.Get()
                };
                return Ok(response);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/databaseadmin/fixsystemuser/{username}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("fixsystemuser/{username}")]
        [HttpGet]
        public async Task<IActionResult> FixSystemUser(string username)
        {
            try
            {
                // find user from systemUsers
                List<SystemUser> systemUsersList = _systemUsers.Get();
                var user = systemUsersList.FirstOrDefault(usr => usr.Username.ToLower() == username.ToLower().Trim());

                if (user == null)
                {
                    throw new Exception($"User: {username} not found in configuration");
                }

                AdminResponesDto response = await FixSystemUserInternal(user);
                return Ok(response);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/databaseadmin/fixallsystemusers
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("fixallsystemusers")]
        [HttpGet]
        public async Task<IActionResult> FixAllSystemUsers()
        {
            try
            {
                int errorCount = 0;
                foreach (var user in _systemUsers.Get())
                {
                    try
                    {
                        AdminResponesDto respUser = await FixSystemUserInternal(user);
                        // convert respUser.Obj to SystemUser if not null and store in usr, use straight cast since it should always be SystemUser
                        SystemUser usr = respUser.Obj as SystemUser ?? user;
                        errorCount += (usr.IsValid ? 1 : 0);
                    }
                    catch
                    {
                        errorCount++;
                    }
                }
                AdminResponesDto response = new AdminResponesDto()
                {
                    Id = "",
                    Status = errorCount > 0 ? AdminResponesDto.FAILED_TO_FIX_ISSUE : AdminResponesDto.SUCCESS,
                    Message = "",
                    Obj = null
                };
                return Ok(response);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        private async Task<AdminResponesDto> FixSystemUserInternal(SystemUser user)
        {
            AdminResponesDto response = new AdminResponesDto()
            {
                Id = "",
                Status = AdminResponesDto.FAILED_TO_FIX_ISSUE,
                Message = "",
                Obj = user
            };

            // retrieve user from database
            UserRS? configuredUser = MainRepo.GetUser(user.Username, _usersDB);

            if (configuredUser == null)
            {
                // add user
                if (!await AddSysUser(user))
                {
                    // fatal
                    throw new Exception($"Failed to add user: {user.Username} to database");
                }
                configuredUser = MainRepo.GetUser(user.Username, _usersDB);
                if (configuredUser == null)
                {
                    // fatal
                    throw new Exception($"Failed to add user: {user.Username} to database");
                }

                user.Exists = true;
                await ForceEmailConfirmation(configuredUser.Id);
                if (!await ForcePasswordChange(configuredUser.UserName, user.Password))
                {
                    user.CredentialValid = false;
                }
                user.CredentialValid = true;
                response.Status = AdminResponesDto.SUCCESS;
                return response;

            }
            else
            {
                user.Exists = true;
                // attempt to login using credentials provided in config to validate password
                user.CredentialValid = await TryLogin(user.Username, user.Password);
                if (!user.CredentialValid)
                {
                    user.CredentialValid = !await ForcePasswordChange(configuredUser.UserName, user.Password);
                }
                // check if roles are setup properly
                // user.Roles should contain all roles in item.Roles
                user.RolesValid = user.Roles != null;
                List<string> missingRoles = new List<string>();
                if (user.Roles != null)
                {
                    user.Roles.ForEach(role =>
                    {
                        if (configuredUser.Roles == null || !configuredUser.Roles.Contains(role))
                        {
                            user.RolesValid = false;
                            missingRoles.Add(role);
                        }
                    });
                }
                if (missingRoles.Count > 0)
                {
                    configuredUser.Roles = configuredUser.Roles != null ? configuredUser.Roles.Union(missingRoles).ToList() : missingRoles;
                    user.RolesValid = await UpdateUser(configuredUser);
                }

                user.RolesConfigured = (configuredUser.Roles != null && configuredUser.Roles.Count > 0) ? configuredUser.Roles : new();
                response.Status = AdminResponesDto.SUCCESS;
                return response;
            }
        }


        public bool FixSystemUserIssue(SystemUser item)
        {
            try
            {
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error fixing system-user issue for user: {item.Username}");
                return false;
            }
        }


        private async Task<bool> TryLogin(string usr, string pass)
        {
            // attempt to login using credentials provided in config to validate password and get token for future requests if needed
            // use AccountController of the RootsIdentity service for login and token generation
            try
            {
                var url = Url.Action("Login", "Account", null, Request.Scheme);
                var loginReq = new LoginRequestDto() { Username = usr, Password = pass };
                var json = JsonSerializer.Serialize(loginReq);

                _logger.LogInformation($"Attempting to login user: {usr} using the URL: {url}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                var httpClient = new HttpClient(handler);

                var response = await httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    // deserialize if needed
                    return true;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error attempting to login user: {usr}");
            }
            // handle error
            return false;
        }

        private async Task<bool> AddSysUser(SystemUser usr)
        {
            var addUserReq = new UserRS()
            {
                Id = "",
                FirstName = usr.FirstName,
                LastName = usr.LastName,
                UserName = usr.Username,
                Email = usr.Email,
                EmailConfirmed = true,
                Roles = new List<string>(usr.Roles)
            };
            string fn = nameof(AccountController.AddUser);
            var user = await SendRequest<UserRS>(HttpMethod.Post, fn, JsonSerializer.Serialize(addUserReq));
            return user != null;

        }

        private async Task<bool> UpdateUser(UserRS user)
        {
            string fn = nameof(AccountController.UpdateUser);
            var newUser = await SendRequest<UserRS>(HttpMethod.Put, fn, JsonSerializer.Serialize(user));
            return newUser != null;
        }

        private async Task<bool> ForceEmailConfirmation(string userId)
        {
            string fn = nameof(AccountController.ForceEmailConfirmation);
            return await SendRequest<bool>(HttpMethod.Post, fn, null, new { id = userId });
        }

        private async Task<bool> ForcePasswordChange(string username, string userPassword)
        {
            var req = new ChangePasswordRequest()
            {
                UserName = username,
                Password = _tempPass,
                NewPassword = userPassword
            };
            string fn = nameof(AccountController.ChangePassword);
            var result = await SendRequest<IdentityResult>(HttpMethod.Post, fn, JsonSerializer.Serialize(req));
            return result != null && result.Succeeded;
        }

        private async Task<T?> SendRequest<T>(HttpMethod method, string fn, string? jsonContent, object? value = null)
        {
            // attempt to login using credentials provided in config to validate password and get token for future requests if needed
            // use AccountController of the RootsIdentity service for login and token generation
            //var url = Url.Action(fn, "Account", value, Request.Scheme);
            var url = Url.Action(fn, "Account", value, Request.Scheme); 
            var thumbPrint = _certThumbPrint;
            var token = GetBearerToken();
            var client = CreateHttpClientWithCert(thumbPrint);
            if (client == null)
            {
                _logger.LogError("Failed to create HttpClient with certificate thumbprint: {ThumbPrint}", thumbPrint);
                return default;
            }

            HttpContent? httpContent = jsonContent == null ? null : new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // add token for JWT authentication
            if (token != "")
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                client.DefaultRequestHeaders.Add("Cache-Control", $"no-cache, no-store, must-revalidate");
                client.DefaultRequestHeaders.Add("Pragma", $"no-cache");
            }
            // Send POST request
            HttpResponseMessage? response = null;
            if (method == HttpMethod.Post)
                response = await client.PostAsync(url, httpContent);
            else if (method == HttpMethod.Get)
                response = await client.GetAsync(url);
            else if (method == HttpMethod.Put)
                response = await client.PutAsync(url, httpContent);
            else if (method == HttpMethod.Delete)
                response = await client.DeleteAsync(url);
            else
            {
                _logger.LogError("Unsupported HTTP method: {Method}", method);
                return default;
            }
            if (response != null && response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                // if T is boolean, return true, otherwise deserialize JSON to object
                if (typeof(T) == typeof(bool))
                {
                    return (T)(object)true;
                }
                var obj = JsonSerializer.Deserialize<T>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return obj;
            }
            else
            {
                // handle error
                return default;
            }
        }

        private string? GetBearerToken()
        {
            var authHeader = HttpContext.Request.Headers.Authorization.ToString();

            if (string.IsNullOrWhiteSpace(authHeader))
                return null;

            const string prefix = "Bearer ";

            return authHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? authHeader.Substring(prefix.Length)
                : authHeader;
        }

        public static HttpClient CreateHttpClientWithCert(string thumbPrint)
        {
            var handler = new HttpClientHandler();
            var cert = CertificateUtil.GetCertificateFromStore(thumbPrint);

            if (cert != null)
            {
                handler.ClientCertificates.Add(cert);
            }

            return new HttpClient(handler);
        }

        #endregion SystemUsers


    }
}