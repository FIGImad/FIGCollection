using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using FIGCommon.Exceptions;
using FIGCommon.Utilities;
using FIGCommon.Controller;

namespace FIGCommon.Controllers
{
    public class FIGBaseController : ControllerBase
    {
        protected readonly IWebHostEnvironment _hostEnvironment;
        protected readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly ILogger _logger;
        protected readonly IServiceProvider _serviceProvider;
        protected readonly IConfiguration _config;

        public FIGBaseController(IWebHostEnvironment hostEnvironment
            , IHttpContextAccessor httpContextAccessor
            , ILogger logger
            , IServiceProvider serviceProvider
            , IConfiguration config
            )
        {
            _hostEnvironment = hostEnvironment;
            _httpContextAccessor = httpContextAccessor;
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
                string? val = _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        protected string ContentVirtualFolder
        {
            get
            {
                string key = "ContentVirtualFolder";
                string defVal = "";
                string? val = _config[key] == null ? defVal : _config[key];
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
                string? val = _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        protected bool MessagesErrorsSendNotifications
        {
            get
            {
                string key = "Messages:Errors:SendNotifications";
                string defVal = "false";
                string? val = _config[key] == null ? defVal : _config[key];
                return bool.Parse(val == null ? defVal : val);
            }
        }

        protected string MessagesErrorsSubject
        {
            get
            {
                string key = "Messages:Errors:Subject";
                string defVal = "";
                string? val = _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        protected string MessagesErrorsEmailToAddress
        {
            get
            {
                string key = "Messages:Errors:EmailToAddress";
                string defVal = "";
                string? val = _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        #endregion Properties

        public IActionResult OnException(Exception e)
        {
            StringBuilder builder = new StringBuilder();
            bool needsSeparator = false;
            string lastMessage = "";
            ErrorResponseObj errorObj = new();

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
            if (message != "")
            {
                _logger.LogError(message);
            }

            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            errorObj.Message = e.Message;
            if (e is SqlException)
            {
                errorObj.ErrorCode = ErrorCodes.DBError_General;
                if ((e as SqlException)?.Number == 2601 || (e as SqlException)?.Number == 2627)
                {
                    // Cannot insert duplicate key row in object error
                    errorObj.ErrorCode = ErrorCodes.DBError_Duplicate;
                }
            }
            else if (e is SqlProcException)
            {
                errorObj.ErrorCode = ErrorCodes.DBError_ProcedureExec;
            }
            else if (e is SqlUpdateException)
            {
                errorObj.ErrorCode = ErrorCodes.DBError_Update;
            }
            else if (e is SqlInsertException)
            {
                errorObj.ErrorCode = ErrorCodes.DBError_Insert;
            }
            else if (e is SqlUpsertException)
            {
                errorObj.ErrorCode = ErrorCodes.DBError_Upsert;
            }
            else if (e is AppErrorException)
            {
                AppErrorException? appErrorException = e as AppErrorException;
                errorObj.ErrorCode = appErrorException?.errorCode ?? ErrorCodes.SysError_InternalServerError;
                errorObj.ServiceCode = appErrorException?.serviceCode ?? "";
                statusCode = HttpStatusCodeFromErrorCode(errorObj.ErrorCode);
            }
            else if (e is ArgumentNullException || e is ArgumentException)
            {
                errorObj.ErrorCode = ErrorCodes.DataError_InvalidArgument;
            }
            else if (e is HttpRequestException)
            {
                statusCode = ((HttpRequestException)e).StatusCode ?? System.Net.HttpStatusCode.InternalServerError;

                return StatusCode(
                    (int)statusCode,
                    new
                    {
                        error = e.Message,
                        statusCode = (int)statusCode
                    });
            }
            else
            {
                // check message if an error code name
                try
                {
                    errorObj.ErrorCode = ErrorCodes.ToErrorCode(e.Message);
                }
                catch (Exception)
                {
                    errorObj.ErrorCode = ErrorCodes.SysError_InternalServerError;
                }
            }
            return StatusCode((int)statusCode, errorObj);
        }

        private static HttpStatusCode HttpStatusCodeFromErrorCode(int errorCode)
        {
            return errorCode switch
            {
                ErrorCodes.AuthError_General => HttpStatusCode.Unauthorized,
                ErrorCodes.AuthError_Login => HttpStatusCode.Unauthorized,
                ErrorCodes.AuthError_InvalidCred => HttpStatusCode.Unauthorized,
                ErrorCodes.AuthError_InvalidUser => HttpStatusCode.Unauthorized,
                ErrorCodes.AuthError_Authentication => HttpStatusCode.Unauthorized,
                ErrorCodes.AuthError_EmailConfirm => HttpStatusCode.Forbidden,
                ErrorCodes.AuthError_Unauthorized => HttpStatusCode.Forbidden,
                ErrorCodes.ClientError_NotFound => HttpStatusCode.NotFound,
                ErrorCodes.SysError_BadGateway => HttpStatusCode.BadGateway,
                ErrorCodes.SysError_ServiceUnavailable => HttpStatusCode.ServiceUnavailable,
                ErrorCodes.SysError_GatewayTimeout => HttpStatusCode.GatewayTimeout,
                _ => HttpStatusCode.InternalServerError
            };
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
    }        
}
