using FIGCommon.Services.LogMonitor;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Reflection;

namespace FIGCommon.Extensions
{
    public static class LogFileViewerExtensions
    {
        /// <summary>
        /// Registers <see cref="LogFileViewer"/> and exposes the shared
        /// <see cref="Controllers.LogController"/> to the host application.
        ///
        /// Usage:
        /// <code>
        ///   builder.Services.AddControllers()
        ///                   .AddLogFileViewer();
        /// </code>
        /// The controller will be reachable at GET /log/current/{startpos}/{numbytes},
        /// GET /log/archive/{filename}/{startpos}/{numbytes} and GET /log/list
        /// without any additional code in the host.
        /// </summary>
        public static IMvcBuilder AddLogFileViewer(this IMvcBuilder mvcBuilder)
        {
            mvcBuilder.Services.AddSingleton<LogFileViewer>();

            // Make ASP.NET Core discover controllers that live in FIGCommon.dll
            mvcBuilder.ConfigureApplicationPartManager(apm =>
            {
                var assembly = Assembly.GetAssembly(typeof(LogFileViewer))!;
                if (!apm.ApplicationParts.OfType<AssemblyPart>().Any(p => p.Assembly == assembly))
                {
                    apm.ApplicationParts.Add(new AssemblyPart(assembly));
                }
            });

            return mvcBuilder;
        }
    }
}
