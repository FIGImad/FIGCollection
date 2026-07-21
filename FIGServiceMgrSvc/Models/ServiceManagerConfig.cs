namespace FIGServiceMgrSvc.Models
{
    /// <summary>
    /// Bound from the "ServiceManagerConfig" section of appsettings.json.
    /// </summary>
    public class ServiceManagerConfig
    {
        /// <summary>
        /// The Windows service names (as seen in services.msc) that this agent is
        /// allowed to start, stop, or query.
        /// </summary>
        public List<string> ManagedServices { get; set; } = new();
    }
}
