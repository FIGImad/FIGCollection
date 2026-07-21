using FIGCommon.DataAccess;
using FIGCommon.Extensions;
using FIGCommon.Providers;
using FIGCommon.Services;
using FIGCommon.Services.LogMonitor;
using FIGCommon.Utilities;
using FIGSignalExSvc.Plugins;
using FIGSignalExSvc.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.Versioning;
using System.Text;

namespace FIGSignalExSvc
{
    [SupportedOSPlatform("windows")]
    public class Program
    {
        private const string WindowsServiceNameArgument = "--windows-service-name";

        static bool CompatabilityMode = false;
        public static async Task Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var builder = WebApplication.CreateBuilder(args);

            #region WindowsServiceURLS
            builder.Host.UseWindowsService(c =>
            {
                c.ServiceName = ResolveWindowsServiceName(args, "FIGSignalExSvc");
            });
            #endregion WindowsServiceURLS

            #region Configuration
            string settingsFilename = "signal-settings";
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
            // â”€â”€ Serilog â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            Log.Logger = LogFileUtil.GetSerilogConfig(null, builder.Configuration).CreateLogger();
            // LogAnalyzerSink must be registered before Serilog and LogAlertRouter
            if (!CompatabilityMode)
            {
                builder.Services.AddSingleton<LogAnalyzerSink>();
            }
            builder.Services.AddSerilog((services, config) =>
            {
                if (CompatabilityMode)
                {
                    LogFileUtil.GetSerilogConfig(config, builder.Configuration);
                }
                else
                {
                    var sink = services.GetRequiredService<LogAnalyzerSink>();
                    LogFileUtil.GetSerilogConfig(config, builder.Configuration).WriteTo.Sink(sink);
                }
            });
            //builder.Services.AddSingleton<LogAlertRouter>();
            //builder.Services.AddHostedService(sp => sp.GetRequiredService<LogAlertRouter>());
            Log.Debug("Logging has been configured.");
            #endregion Serilog_LogAnalyzer

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

                            logger.LogWarning("JWT forbidden triggered.");
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

            #region FIGControllerClient
            builder.Services.AddKeyedSingleton<ITaskSchedulerService, TaskSchedulerService>("FIGController");
            #endregion FIGControllerClient

            #region Controller
            if (CompatabilityMode)
            {
                builder.Services.AddControllers();
            }
            else
            {
                builder.Services.AddControllers().AddLogFileViewer();
            }
            //builder.Services.AddControllers().AddLogFileViewer();
            builder.Services.AddControllers();
            // Register the IHttpContextAccessor service in the dependency injection (DI) container.
            // This allows you to access the current HTTP context (HttpContext) outside of controllers,
            // such as in services, middleware, or other components.
            builder.Services.AddHttpContextAccessor();
            #endregion Controller

            #region HttpClient
            builder.Services.AddHttpClient();
            #endregion HttpClient

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

            // Discover collection plugins once. Collection instances are created per StudyCol.
            builder.Services.AddSingleton<IStudyPluginCatalog, StudyPluginCatalog>();

            builder.Services.AddHostedService<PriceMonitorService>();
            #endregion OtherServices

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

            try
            {
                var app = builder.Build();
                var pluginCatalog = app.Services.GetRequiredService<IStudyPluginCatalog>();
                var pluginColTypes = pluginCatalog.Registrations.Count == 0
                    ? "none"
                    : string.Join(", ", pluginCatalog.Registrations.Select(item => item.ColType).OrderBy(item => item));

                app.Logger.LogInformation(
                    "Study plugin catalog initialized before service start with {PluginCount} plugin(s): {ColTypes}",
                    pluginCatalog.Registrations.Count,
                    pluginColTypes);

                if (args.Contains("--list-plugins", StringComparer.OrdinalIgnoreCase))
                {
                    foreach (var plugin in pluginCatalog.Registrations.OrderBy(item => item.ColType))
                    {
                        Console.WriteLine($"{plugin.ColType}|{plugin.Version}|{plugin.AssemblyPath}");
                    }
                    return;
                }

                //app.UseSerilogRequestLogging();   // log using Serilog

                // Initialize Controllers
                app.UseRouting();
                app.UseAuthentication();   // This must come before UseAuthorization
                app.UseAuthorization();
                app.MapControllers();

                // Initialize MainRepo
                MainRepo.Initialize(app.Services);

                if (isService)
                {
                    await app.RunAsync();
                }
                else
                {
                    app.Run();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not build app");
                throw;
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
