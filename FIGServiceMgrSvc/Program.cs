using FIGCommon.Providers;
using FIGCommon.Services;
using FIGCommon.Services.LogMonitor;
using FIGCommon.Utilities;
using FIGServiceMgrSvc.Controllers;
using FIGServiceMgrSvc.Models;
using FIGServiceMgrSvc.Services;
using Serilog;
using System.Runtime.Versioning;
using System.Text;

namespace FIGServiceMgrSvc
{
    public class Program
    {
        private const string WindowsServiceNameArgument = "--windows-service-name";

        [SupportedOSPlatform("windows")]
        public static async Task Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var builder = Host.CreateApplicationBuilder(args);

            #region WindowsService
            builder.Services.AddWindowsService(c =>
            {
                c.ServiceName = ResolveWindowsServiceName(args, "FIGServiceMgrSvc");
            });
            #endregion WindowsService

            #region Configuration
            string settingsFilename = "service-manager-settings";
            string settingsFile = $"{settingsFilename}.json";

            builder.Configuration.Sources.Clear();
            builder.Configuration
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(settingsFile, optional: false, reloadOnChange: true);

            if (builder.Environment.IsDevelopment())
            {
                string developmentSettingsFile =
                    $"{settingsFilename}.{builder.Environment.EnvironmentName}.json";

                // If the environment-specific file is absent, configuration
                // automatically falls back to controller-settings.json.
                builder.Configuration.AddJsonFile(
                    developmentSettingsFile,
                    optional: true,
                    reloadOnChange: true);
            }

            builder.Configuration
                .AddEnvironmentVariables()
                .AddCommandLine(args);

            // from the section "Global" load the file specified by "VarPath" as another configuration source (if defined)
            string varPath = builder.Configuration["Global:VarPath"] ?? "";
            if (varPath.Length > 0)
            {
                builder.Configuration.AddJsonFile(varPath, optional: true, reloadOnChange: true);
            }

            // This is needed for substitution of variables in appsettings.json
            ((IConfigurationBuilder)builder.Configuration).Add(new ConfigurationSourceWithVars(builder.Configuration));
            #endregion Configuration

            #region Serilog_LogAnalyzer
            // ── LogAnalyzerSink – registered as singleton so DI can inject it
            //    into LogAlertRouter. A temporary service provider is built here
            //    so the same instance is also wired into the Serilog pipeline.
            builder.Services.AddSingleton<LogAnalyzerSink>();

            // ── Serilog ───────────────────────────────────────────────────────
            builder.Services.AddSerilog((services, config) =>
            {
                var sink = services.GetRequiredService<LogAnalyzerSink>();
                LogFileUtil.GetSerilogConfig(config, builder.Configuration).WriteTo.Sink(sink);
            });
            builder.Services.AddSingleton<LogAlertRouter>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<LogAlertRouter>());
            Log.Debug("Logging has been configured.");
            #endregion Serilog_LogAnalyzer

            #region ClientSignalRService
            builder.Services.AddSignalR();

            // ── SignalR background service ────────────────────────────────────
            // Register as singleton first so IClientSignalRService is resolvable
            builder.Services.AddSingleton<ServiceManagerSignalRService>();
            builder.Services.AddSingleton<IClientSignalRService>(sp =>
                sp.GetRequiredService<ServiceManagerSignalRService>());
            builder.Services.AddHostedService(sp =>
                sp.GetRequiredService<ServiceManagerSignalRService>());

            #endregion ClientSignalRService

            #region OtherServices
            // ── TaskSchedulerService ──────────────────────────────────────────
            builder.Services.AddSingleton<ITaskSchedulerService, TaskSchedulerService>();

            // ── ManagedServiceController ──────────────────────────────────────
            var svcMgrConfig = new ServiceManagerConfig();
            builder.Configuration.GetSection("ServiceManagerConfig").Bind(svcMgrConfig);
            builder.Services.AddSingleton(sp =>
                new ManagedServiceController(
                    sp.GetRequiredService<ILogger<ManagedServiceController>>(),
                    svcMgrConfig.ManagedServices));

            #endregion OtherServices

            #region SignalR Services
            // ── SignalR controllers ───────────────────────────────────────────
            builder.Services.AddSingleton<LogFileViewer>();  // used by LogController to serve log file contents over SignalR
            builder.Services.AddSingleton<ISignalRController, PingController>();
            builder.Services.AddSingleton<ISignalRController, ServicesController>();
            builder.Services.AddSingleton<ISignalRController, LogController>();
            builder.Services.AddSingleton<SignalRDispatcher>();

            #endregion SignalR Services


            var host = builder.Build();
            await host.RunAsync();
        }

        private static string ResolveWindowsServiceName(string[] args, string defaultServiceName)
        {
            for (int index = 0; index < args.Length; index++)
            {
                if (string.Equals(args[index], WindowsServiceNameArgument, StringComparison.OrdinalIgnoreCase) &&
                    index + 1 < args.Length &&
                    !string.IsNullOrWhiteSpace(args[index + 1]))
                {
                    return args[index + 1].Trim();
                }

                string prefix = $"{WindowsServiceNameArgument}=";
                if (args[index].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string serviceName = args[index][prefix.Length..].Trim();
                    if (!string.IsNullOrWhiteSpace(serviceName))
                    {
                        return serviceName;
                    }
                }
            }

            return defaultServiceName;
        }

    }
}
