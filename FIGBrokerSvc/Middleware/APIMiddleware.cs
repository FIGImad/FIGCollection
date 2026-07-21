//namespace FIGBrokerSvc.Middleware
//{
//    public class APIMiddleware
//    {
//        private readonly RequestDelegate _next;
//        ILogger<APIMiddleware> _logger;


//        public APIMiddleware(RequestDelegate next,
//            ILogger<APIMiddleware> logger)
//        {
//            _next = next;
//            _logger = logger;
//        }

//        public async Task Invoke(HttpContext context)
//        {
//            // disable logging
//            //try
//            //{
//            //    var endPoint = context.GetEndpoint();
//            //    _logger.LogDebug("Request to {0} - method({1}) - param({2})",
//            //            (endPoint == null ? "[Unspecified Controller]" : endPoint.DisplayName), context.Request.Method.ToString(), context.Request.Path.Value);
//            //}
//            //catch (Exception ex)
//            //{
//            //    _logger.LogDebug("Exception - {0}", ex.Message);
//            //}
//            // Here you can decide if you want the next step of the pipeline or not, usually you want
//            await _next(context);
//            //_logger.LogDebug("After");
//        }
//    }
//}
