using FIGAutoTradeExSvc.Services;
using FIGAutoTraderAdminSvc.Services;
using FIGCommon.DataAccess;
using FIGCommon.Extensions;
using FIGCommon.Providers;
using FIGCommon.Services;
using FIGCommon.Services.LogMonitor;
using FIGCommon.Utilities;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.IdentityModel.Logging;
using RootsIdentity;
using Serilog;
using System.Diagnostics;
using System.Text;

namespace FIGAutoTraderAdminSvc
{
    public class Program
    {
        private const string WindowsServiceNameArgument = "--windows-service-name";

        public static async Task Main(string[] args)
        {
            var isIIS = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APP_POOL_ID"));
            var isService = !isIIS && !(Debugger.IsAttached || args.Contains("--console"));
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            IdentityModelEventSource.ShowPII = true; //Add this line

            var builder = WebApplication.CreateBuilder(args);

            #region WindowsServiceURLS
            builder.Host.UseWindowsService(c =>
            {
                c.ServiceName = ResolveWindowsServiceName(args, "FIGAutoTraderAdminSvcSvc");
            });
            #endregion WindowsServiceURLS

            #region Configuration
            string settingsFilename = "appsettings";
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

            #region SeriLog
            // create bootstrap logger
            Log.Logger = LogFileUtil.GetSerilogConfig(null, builder.Configuration).CreateLogger();
            // LogAnalyzerSink must be registered before Serilog and LogAlertRouter
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
            #endregion SeriLog

            #region RootsIdentity
            builder.Host.AddRootsIdentity(c =>
            {
                c.Config = builder.Configuration;
            });
            #endregion RootsIdentity

            #region Controller
            builder.Services.AddControllers().AddLogFileViewer();  // will add LogFileViewer service
            // Register the IHttpContextAccessor service in the dependency injection (DI) container.
            // This allows you to access the current HTTP context (HttpContext) outside of controllers,
            // such as in services, middleware, or other components.
            builder.Services.AddHttpContextAccessor();
            #endregion Controller

            #region HttpClient
            //builder.Services.AddHttpClient();
            static HttpClientHandler CreateIgnoreNameMismatchHandler() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                    (errors & ~System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch) == System.Net.Security.SslPolicyErrors.None
            };
            builder.Services.AddHttpClient(nameof(ATSApiService)).ConfigurePrimaryHttpMessageHandler(CreateIgnoreNameMismatchHandler);
            builder.Services.AddSingleton<ATSApiService>();
            builder.Services.AddHttpClient(nameof(ServiceMgrApi)).ConfigurePrimaryHttpMessageHandler(CreateIgnoreNameMismatchHandler);
            builder.Services.AddSingleton<ServiceMgrApi>();
            builder.Services.AddHttpClient(nameof(BrokerApiService)).ConfigurePrimaryHttpMessageHandler(CreateIgnoreNameMismatchHandler);
            builder.Services.AddSingleton<BrokerApiService>();
            //builder.Services.AddHttpClient(nameof(MonitorApiService)).ConfigurePrimaryHttpMessageHandler(CreateIgnoreNameMismatchHandler);
            //builder.Services.AddSingleton<MonitorApiService>();
            builder.Services.AddHttpClient(nameof(ControllerApiService)).ConfigurePrimaryHttpMessageHandler(CreateIgnoreNameMismatchHandler);
            builder.Services.AddSingleton<ControllerApiService>();
            //builder.Services.AddHttpClient(nameof(PriceSyncApiService)).ConfigurePrimaryHttpMessageHandler(CreateIgnoreNameMismatchHandler);
            //builder.Services.AddSingleton<PriceSyncApiService>();
            builder.Services.AddHttpClient(nameof(LogViewerApi)).ConfigurePrimaryHttpMessageHandler(CreateIgnoreNameMismatchHandler);
            builder.Services.AddSingleton<LogViewerApi>();
            #endregion HttpClient

            #region monitorServices
            builder.Services.AddSignalR();

            builder.Services.AddSingleton<IConnectionStatusStore, ConnectionStatusStore>();
            // Register one ATSMonitorService instance so it can run as a hosted
            // service and also be injected into DeviceMonitorService.
            builder.Services.AddSingleton<ATSMonitorService>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<ATSMonitorService>());
            #endregion monitorServices

            #region KerstrelSetup
            if (!isIIS)
            {
                try
                {
                    //Load the certificate from the file system
                    var cert = HttpUtils.GetCertificate(builder.Configuration);
                    builder.WebHost.ConfigureKestrel((context, options) =>
                    {
                        // This loads settings from config files (appsettings.json) into `options`
                        context.Configuration.GetSection("Kestrel").Bind(options);

                        if (cert != null)
                        {
                            // ConfigureEndpointDefaults must come BEFORE ConfigureHttpsDefaults
                            // otherwise it overrides and wipes the HTTPS/certificate settings
                            options.ConfigureEndpointDefaults(listenOptions =>
                            {
                                listenOptions.UseConnectionLogging();
                            });

                            options.ConfigureHttpsDefaults(httpsOptions =>
                            {
                                httpsOptions.ServerCertificate = cert;
                                httpsOptions.ClientCertificateMode = ClientCertificateMode.NoCertificate;
                                httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                                          System.Security.Authentication.SslProtocols.Tls13;
                            });
                        }
                        else
                        {
                            options.ConfigureEndpointDefaults(listenOptions =>
                            {
                                listenOptions.UseConnectionLogging();
                            });
                        }
                    });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error in KerstrelSetup");
                    throw;
                }
            }
            #endregion KerstrelSetup

            #region ClientSignalRService
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<IClientSignalRService, ClientSignalRService>();
            builder.Services.AddHostedService(sp => (ClientSignalRService)sp.GetRequiredService<IClientSignalRService>());
            #endregion ClientSignalRService

            #region otherServices
            // provide scheduling tasks cabability
            builder.Services.AddSingleton<ITaskSchedulerService, TaskSchedulerService>();

            // Create a singleton instance of EventMonitorService
            builder.Services.AddSingleton<EventMonitorService>();
            
            builder.Services.AddHostedService<DeviceMonitorService>();
            #endregion otherServices

            var app = builder.Build();

            app.UseExceptionHandler("/net/error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();

            app.UseDefaultFiles();
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // log using Serilog
            // app.UseSerilogRequestLogging();

            //app.UseRouting();
            //app.UseMiddleware<APIMiddleware>();

            // Roots identity includes authentication and authorization
            app.UseRootsIdentity();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapHub<ATSMonitorHub>("/hubs/ATS_Monitor");
            //app.MapHub<MarketDataMonitorHub>("/hubs/marketdata");

            // initialize services and DataAccess Repos
            MainRepo.Initialize(app.Services);
            AlertRepo.Initialize(app.Services);

            app.Services.GetRequiredService<ATSApiService>();
            app.Services.GetRequiredService<ServiceMgrApi>();
            app.Services.GetServices<LogViewerApi>().First();

            try
            {
                if (isService)
                {
                    await app.RunAsync();
                }
                else
                {
                    app.Run();
                }
            }
            finally
            {
                // Ensure all buffered log events are written to disk before the
                // process exits (critical when IIS recycles the app pool).
                Log.CloseAndFlush();
            }
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
