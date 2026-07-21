using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RootsIdentity.Models;
using RootsIdentity.DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using System;
using FIGCommon.Utilities;
using FIGCommon.Exceptions;
using Microsoft.Extensions.Configuration;
using FIGCommon.Models;
using FIGCommon.Interfaces;

namespace RootsIdentity.Controllers
{
    public class AppControllerBase : ControllerBase
    {
        protected readonly IWebHostEnvironment _hostEnvironment;
        protected readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly IIdentityService _identityService;
        protected readonly ILogger _logger;
        protected readonly IServiceProvider _serviceProvider;
        protected readonly IConfiguration? _config;

        public AppControllerBase(IWebHostEnvironment hostEnvironment
            , IHttpContextAccessor httpContextAccessor
            , IIdentityService identityService
            , ILogger logger
            , IServiceProvider serviceProvider
            , IConfiguration config
            )
        {
            _hostEnvironment = hostEnvironment;
            _httpContextAccessor = httpContextAccessor;
            _identityService = identityService;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _config = config;
        }

        #region Properties
        protected IWebHostEnvironment HostEnvironment { get { return _hostEnvironment; } }

        protected string BaseUrl
        {
            get
            {
                string key = "Redirect:BaseUrl";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        protected string ContentVirtualFolder
        {
            get
            {
                string key = "ContentVirtualFolder";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        protected string ContentPhysicalFolder
        {
            // WebRootPath is an absolute path to the directory that contains the web-servable application content files
            get { return Path.Combine(_hostEnvironment.WebRootPath, ContentVirtualFolder); }
        }

        protected string MessagesHtmlTemplatesFolder
        {
            get
            {
                string key = "Messages:HtmlTemplatesFolder";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        protected bool MessagesErrorsSendNotifications
        {
            get
            {
                string key = "Messages:Errors:SendNotifications";
                string defVal = "false";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return bool.Parse(val == null ? defVal : val);
            }
        }

        protected string MessagesErrorsSubject
        {
            get
            {
                string key = "Messages:Errors:Subject";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        protected string MessagesErrorsEmailToAddress
        {
            get
            {
                string key = "Messages:Errors:EmailToAddress";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        #endregion Properties

        public IActionResult OnException(Exception e, int reason = ErrorCodes.UnSpecified)
        {
            StringBuilder builder = new StringBuilder();
            bool needsSeparator = false;
            string lastMessage = "";

            var next = e;
            do
            {
                string sanitized = next.Message.Replace('\n', ' ');
                if (0 != sanitized.CompareTo(lastMessage))
                {
                    if (needsSeparator)
                    {
                        builder.Append("  ");
                    }
                    builder.Append(sanitized);
                    lastMessage = sanitized;
                }
                needsSeparator = true;
                next = next.InnerException;
            } while (null != next);

            string message = builder.ToString();
            _logger.LogError(message);

            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;

            if (e is NotifyException && MessagesErrorsSendNotifications)
            {
                SendErrorNotificationEmailAsync(message).Wait();
            }
            else if (e is SqlException)
            {
                if (reason == ErrorCodes.UnSpecified)
                {
                    reason = ErrorCodes.DBError_General;
                    if ((e as SqlException)?.Number == 2601 || (e as SqlException)?.Number == 2627)
                    {
                        // Cannot insert duplicate key row in object error
                        reason = ErrorCodes.DBError_Duplicate;
                    }
                }
            }
            else if (e is SqlProcException)
            {
                if (reason == ErrorCodes.UnSpecified)
                {
                    reason = ErrorCodes.DBError_ProcedureExec;
                }
            }
            else if (e is SqlUpdateException)
            {
                if (reason == ErrorCodes.UnSpecified)
                {
                    reason = ErrorCodes.DBError_Update;
                }
            }
            else if (e is SqlInsertException)
            {
                if (reason == ErrorCodes.UnSpecified)
                {
                    reason = ErrorCodes.DBError_Insert;
                }
            }
            else if (e is SqlUpsertException)
            {
                if (reason == ErrorCodes.UnSpecified)
                {
                    reason = ErrorCodes.DBError_Upsert;
                }
            }
            else
            {
                if (reason == ErrorCodes.UnSpecified)
                {
                    reason = ErrorCodes.SysError_InternalServerError;
                    // check message if an error code name
                    try
                    {
                        reason = ErrorCodes.ToErrorCode(e.Message);
                    }
                    catch (Exception)
                    {
                        reason = ErrorCodes.SysError_InternalServerError;
                    }
                }
            }
            return StatusCode((int)statusCode, reason);
        }

        public Task<IActionResult> OnHttpError(HttpStatusCode statusCode, string msg)
        {
            //reason = (ErrorCodes)Enum.Parse(typeof(ErrorCodes), "ClientError_NotFound", true);
            return Task.FromResult<IActionResult>(StatusCode((int)statusCode, msg));
        }

        public IActionResult OnBadRequest(int reason = ErrorCodes.UnSpecified)
        {
            return StatusCode((int)HttpStatusCode.BadRequest, reason);
        }

        public async Task SendConfirmationEmailAsync(AspNetUser user, bool validatePassword, bool newUser)
        {
            var validationToken = new ValidationTokenRS()
            {
                ValidationTokenId = ValidationTokenRS.GenerateValidationTokenId(),
                Token = await _identityService.UserManager.GenerateEmailConfirmationTokenAsync(user),
                UserId = user.Id
            };

            AccountsRepo.InsertValidationToken(validationToken);
            var uri = new Uri(new Uri(BaseUrl), $"api/account/confirmemail/{(validatePassword ? 1 : newUser ? 2 : 0)}/{user.Id}/{validationToken.ValidationTokenId}");

            var message = new EmailConfirmationMessage(
                uri.ToString(),
                MessagesHtmlTemplatesFolder,
                _config
                );
            message.Build();


            try
            {
                if (user.Email == null)
                {
                    throw new Exception("Invalid Email Address");
                }
                _identityService.MessageService.Send(user.Email, message.Subject, message.Body).Wait();
                _logger.LogInformation($"Confirmation e-mail has been sent to {user.Email}");
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to send confirmation e-mail to {user.Email}. Exception={e.Message}");
            }
        }

        public async Task ForceEmailConfirmation(AspNetUser user)
        {
            try
            {
                AccountsRepo.ConfirmUserEmail(user);
                _logger.LogInformation($"User's e-mail {user.Email} has been confirmed for {user.UserName}");
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to confirm user's e-mail of {user.UserName}. Exception={e.Message}");
            }
        }

        private async Task SendErrorNotificationEmailAsync(string message)
        {
            try
            {
                await _identityService.MessageService.Send(MessagesErrorsEmailToAddress, MessagesErrorsSubject, message);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }
    }
}