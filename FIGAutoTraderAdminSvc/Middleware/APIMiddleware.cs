
//using System.Net;

//namespace FIGAutoTraderAdminSvc.Middleware
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
//            //Endpoint? endPoint = null;
//            try
//            {
//                //// disablee logging for now
//                //endPoint = context.GetEndpoint();
//                //_logger.LogDebug("Request to {0} - method({1}) - param({2})",
//                //        (endPoint == null ? "[Unspecified Controller]" : endPoint.DisplayName), context.Request.Method.ToString(), context.Request.Path.Value);
//                // Here you can decide if you want the next step of the pipeline or not, usually you want
//                await _next(context);
//                //_logger.LogDebug("After");
//            }
//            catch (Exception _ex)
//            {
//                //_logger.LogError(ex, "Unhandled exception in request to {Endpoint}",
//                //    endPoint?.DisplayName ?? "[Unknown]");

//                if (context.Response.HasStarted)
//                {
//                    // Headers already sent — cannot modify the response; just log and bail
//                    return;
//                }

//                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
//                context.Response.ContentType = "application/json";

//                var errorResponse = new
//                {
//                    error = "An error occurred processing your request",
//                    requestId = context.TraceIdentifier
//                };

//                await context.Response.WriteAsJsonAsync(errorResponse);
//            }

//            //catch (Exception ex)
//            //{
//            //    _logger.LogDebug("Exception - {0}", ex.Message);
//            //}

//        }
//    }
//}
