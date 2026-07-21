using FIGBrokerSvc.DataAccess;
using FIGBrokerSvc.Services;
using FIGCommon.Extensions;
using FIGCommon.Models.FIGProviderAPI;
using FIGCommon.Providers;
using FIGCommon.Services;
using FIGCommon.Services.LogMonitor;
using FIGCommon.Utilities;
using IBKRProvider;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace FIGBrokerSvc
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var builder = WebApplication.CreateBuilder(args);
            #region WindowsServiceURLS
            builder.Host.UseWindowsService(c =>
            {
                c.ServiceName = "FIGBrokerSvc";
            });
            #endregion WindowsServiceURLS

            #region Configuration
            string settingsFilename = "brokersync-settings";
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

            #region RequestBodySize
            //
            // a number of configurations for IIS and request size limits
            builder.Services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = int.MaxValue;
            });
            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = int.MaxValue; // if don't set default value is: 30 MB
            });
            builder.Services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue;
                x.MultipartHeadersLengthLimit = int.MaxValue;
            });
            #endregion RequestBodySize

            #region JWT_Authentication
            try
            {
                var configKey = builder.Configuration["Jwt:Key"] ?? "";
                var configIssuer = builder.Configuration["Jwt:Issuer"] ?? "";
                var configAudience = builder.Configuration["Jwt:Audience"] ?? "";
                configKey = ProtectedDataUtil.Unprotect(configKey) ?? configKey;
                configIssuer = ProtectedDataUtil.Unprotect(configIssuer) ?? configIssuer;
                configAudience = ProtectedDataUtil.Unprotect(configAudience) ?? configAudience;

                // remove default claims
                JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

                builder.Services.AddAuthentication(sharedOptions =>
                {
                    sharedOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    sharedOptions.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    sharedOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = configIssuer,
                        ValidAudience = configAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configKey)),
                        ClockSkew = TimeSpan.Zero // remove delay of token when expire
                    };
                    cfg.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            //var logger = context.HttpContext.RequestServices
                            //    .GetRequiredService<ILoggerFactory>()
                            //    .CreateLogger("JwtBearer");

                            //logger.LogInformation("JWT received. Authorization header: {AuthHeader}",
                            //    context.Request.Headers.Authorization.ToString());
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) &&
                                path.StartsWithSegments("/hubs/broker-agent"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("JwtBearer");

                            logger.LogError(context.Exception,
                                "JWT authentication failed. Message: {Message}",
                                context.Exception.Message);

                            return Task.CompletedTask;
                        },

                        OnChallenge = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("JwtBearer");

                            logger.LogWarning(
                                "JWT challenge triggered. Error: {Error}, Description: {Description}",
                                context.Error,
                                context.ErrorDescription);

                            return Task.CompletedTask;
                        },

                        OnForbidden = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("JwtBearer");

                            string roles = string.Join(",",
                                context.HttpContext.User.Claims
                                    .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                                    .Select(c => c.Value));

                            logger.LogWarning(
                                "JWT forbidden. Method={Method}, Path={Path}, User={User}, Roles={Roles}",
                                context.HttpContext.Request.Method,
                                context.HttpContext.Request.Path,
                                context.HttpContext.User.Identity?.Name ?? "(unknown)",
                                string.IsNullOrWhiteSpace(roles) ? "(none)" : roles);
                            return Task.CompletedTask;
                        }
                    };
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in JWT Authentication setup");
                throw;
            }
            #endregion JWT_Authentication

            #region Authorization
            builder.Services.AddAuthorization();
            #endregion Authorization

            #region IBKRProvider
            try
            {
                // Bind the configuration section
                builder.Services.Configure<ProviderServicesConfig>(builder.Configuration.GetSection("ProviderServices"));
                builder.Host.UseIBKRProvider();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in UseIBKRProvider");
                throw;
            }
            #endregion IBKRProvider

            #region Controller
            builder.Services.AddControllers()
                            .AddLogFileViewer();
            // Register the IHttpContextAccessor service in the dependency injection (DI) container.
            // This allows you to access the current HTTP context (HttpContext) outside of controllers,
            // such as in services, middleware, or other components.
            builder.Services.AddHttpContextAccessor();
            #endregion Controller

            #region ClientSignalRService
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<IClientSignalRService, ClientSignalRService>();
            builder.Services.AddHostedService(sp => (ClientSignalRService)sp.GetRequiredService<IClientSignalRService>());
            #endregion ClientSignalRService

            #region OtherServices
            // provide scheduling tasks cabability
            builder.Services.AddSingleton<ITaskSchedulerService, TaskSchedulerService>();

            // Create a singleton instance of EventMonitorService
            builder.Services.AddSingleton<EventMonitorService>();

            //Create one singleton instance of OrderInstructionMonitorService
            builder.Services.AddSingleton<OrderService>(); // Concrete registration
            // Reuse the same instance and Add it to the IHostedService pipeline.
            builder.Services.AddHostedService(sp => sp.GetRequiredService<OrderService>());

            builder.Services.AddSingleton<IOrderTrackerService, OrderTrackerService>();
            builder.Services.AddHostedService(sp => (OrderTrackerService)sp.GetRequiredService<IOrderTrackerService>());
            #endregion OtherServices

            #region HttpClient
            builder.Services.AddHttpClient();
            static HttpClientHandler CreateIgnoreNameMismatchHandler() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                    (errors & ~System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch) == System.Net.Security.SslPolicyErrors.None
            };
            // order status notification service
            builder.Services.AddHttpClient(nameof(ATSApi)).ConfigurePrimaryHttpMessageHandler(CreateIgnoreNameMismatchHandler);
            builder.Services.AddSingleton<ATSApi>();
            #endregion HttpClient

            #region KerstrelSetup
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
            #endregion KerstrelSetup

            var app = builder.Build();

            //app.UseSerilogRequestLogging();   // log using Serilog

            //app.UseMiddleware<APIMiddleware>();

            // Initialize Controllers
            app.UseRouting();
            app.UseAuthentication();   // This must come before UseAuthorization
            app.UseAuthorization();
            app.MapControllers();

            #region DBAccess
            // Initialize BrokerRepo
            BrokerRepo.Initialize(app.Services);
            #endregion DBAccess

            if (isService)
            {
                await app.RunAsync();
            }
            else
            {
                app.Run();
            }
        }

        private static void AddAppSettingsWatcher(WebApplication app)
        {
            // Add file watcher for appsettings.json (add this before app.Run())
            //var appSettingsPath = Path.Combine(builder.Environment.ContentRootPath, "appsettings.json");
            string appSettingsPath;

            // First try get path from BaseDirectory
            appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

            // Fallback check
            if (!File.Exists(appSettingsPath))
            {
                // Try content root path as fallback
                // How to get builder Environment.ContentRootPath? from app?

                appSettingsPath = Path.Combine(app.Environment.ContentRootPath, "appsettings.json");

                if (!File.Exists(appSettingsPath))
                {
                    throw new FileNotFoundException("Could not locate appsettings.json in either AppContext.BaseDirectory or ContentRootPath");
                }
            }
            string? appSettingsDirName = Path.GetDirectoryName(appSettingsPath);
            if (appSettingsDirName != null)
            {
                var fileProvider = new PhysicalFileProvider(appSettingsDirName);
                var changeToken = fileProvider.Watch(Path.GetFileName(appSettingsPath));

                changeToken.RegisterChangeCallback(_ =>
                {
                    try
                    {
                        // Delay slightly to ensure the file write is complete
                        Thread.Sleep(500);
                        ((IConfigurationRoot)app.Services.GetRequiredService<IConfiguration>()).Reload();
                        Console.WriteLine("appsettings.json changed - configuration reloaded");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reloading configuration: {ex.Message}");
                    }
                }, null);
            }
        }

    }
}
