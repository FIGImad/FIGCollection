namespace FIGAutoTradeExSvc.Middleware
{
    public class AuthenticationMiddleware // : IMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            Console.WriteLine("Before");
            // Here you can decide if you want the next step of the pipeline or not, usually you want
            await _next(context);
        }

        //public Task InvokeAsync(HttpContext context, RequestDelegate next)
        //{
        //    Console.WriteLine("Before");
        //    // Here you can decide if you want the next step of the pipeline or not, usually you want
        //    return _next(context);
        //}
    }
}