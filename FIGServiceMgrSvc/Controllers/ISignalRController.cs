namespace FIGServiceMgrSvc.Controllers
{
    public interface ISignalRController
    {
        bool CanHandle(string method, string[] segments);
        Task<string?> HandleAsync(string method, string[] segments);
    }
}
