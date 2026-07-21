using FIGAlertSvc.Services;
using FIGCommon.DataAccess;
using FIGCommon.Extensions;
using FIGCommon.Providers;
using FIGCommon.Services;
using FIGCommon.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.Versioning;
using System.Text;

namespace FIGPriceSyncSvc
{
    public class Program
    {
        private const string WindowsServiceNameArgument = "--windows-service-name";

        [SupportedOSPlatform("windows")]
        public static async Task Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var builder = WebApplication.CreateBuilder(args);

            #region Configuration
            string settingsFilename = "alerts-settings";
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

            #region WindowsServiceURLS
            builder.Host.UseWindowsService(c =>
            {
                c.ServiceName = ResolveWindowsServiceName(args, "FIGAlertSvc");
            });
            #endregion WindowsServiceURLS

            #region Serilog_LogAnalyzer
            // ── Serilog ───────────────────────────────────────────────────────
            // Do not report error.. this app trigger alerts and should not report log errors
            Log.Logger = LogFileUtil.GetSerilogConfig(null, builder.Configuration).CreateLogger();
            builder.Services.AddSerilog((services, config) =>
            {
                LogFileUtil.GetSerilogConfig(config, builder.Configuration);
            });
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

            #region AlertProcessingService
            //Create one singleton instance of AlertProcessingService
            builder.Services.AddSingleton<AlertProcessingService>(); // Concrete registration
            // Reuse the same instance and Add it to the IHostedService pipeline.
            builder.Services.AddHostedService(sp => sp.GetRequiredService<AlertProcessingService>());
            #endregion AlertProcessingService

            #region AlertDispatchService
            builder.Services.AddSingleton<SmtpMessageService>();
            builder.Services.AddSingleton<PushOverMessageService>();
            builder.Services.AddHostedService<AlertDispatchService>();
            #endregion AlertDispatchService

            #region Controller
            builder.Services.AddControllers().AddLogFileViewer();
            // Register the IHttpContextAccessor service in the dependency injection (DI) container.
            // This allows you to access the current HTTP context (HttpContext) outside of controllers,
            // such as in services, middleware, or other components.
            builder.Services.AddHttpContextAccessor();
            #endregion Controller

            #region HttpClient
            builder.Services.AddHttpClient();
            static HttpClientHandler CreateIgnoreNameMismatchHandler() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                    (errors & ~System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch) == System.Net.Security.SslPolicyErrors.None
            };
            builder.Services.AddHttpClient(nameof(ControllerApiService)).ConfigurePrimaryHttpMessageHandler(CreateIgnoreNameMismatchHandler);
            builder.Services.AddSingleton<ControllerApiService>();
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
                        // Your manual overrides (will take precedence)
                        options.ConfigureHttpsDefaults(httpsOptions =>
                        {
                            httpsOptions.ServerCertificate = cert;
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

            // Initialize Controllers
            app.UseRouting();
            app.UseAuthentication();   // This must come before UseAuthorization
            app.UseAuthorization();
            app.MapControllers();

            // Device information is stored in the AutoTrader database, while
            // alert rules, recipients and pending alerts are in the Alert database.
            MainRepo.Initialize(app.Services, "AutoTraderConnection");
            AlertRepo.Initialize(app.Services, "DefaultConnection");

            if (isService)
            {
                await app.RunAsync();
            }
            else
            {
                app.Run();
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
