
namespace FIGAutoTradeExSvc.Middleware
{
    public class APIMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<APIMiddleware> _logger;
        private readonly IConfiguration _config;


        public APIMiddleware(RequestDelegate next,
            ILogger<APIMiddleware> logger,
            IConfiguration config)
        {
            _next = next;
            _logger = logger;
            _config = config;
        }

        protected bool LogAPICalls
        {
            get
            {
                string key = "LogFile:LogAPICalls";
                string defVal = "false";
                string? val = _config[key] == null ? defVal : _config[key];
                return bool.Parse(val == null ? defVal : val);
            }
        }

        public async Task Invoke(HttpContext context)
        {
            if (LogAPICalls)
            {
                try
                {
                    var endPoint = context.GetEndpoint();
                    _logger.LogDebug("Request to {0} - method({1}) - param({2})",
                            (endPoint == null ? "[Unspecified Controller]" : endPoint.DisplayName), context.Request.Method.ToString(), context.Request.Path.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Exception - {0}", ex.Message);
                }
            }
            // Here you can decide if you want the next step of the pipeline or not, usually you want
            await _next(context);
            //_logger.LogDebug("After");
        }
    }
}
