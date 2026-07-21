using FIGCommon.DataAccess;
using FIGCommon.Extensions;
using FIGCommon.Models;
using FIGCommon.Models.FIGController;
using FIGCommon.Providers;
using FIGCommon.Services;
using FIGCommon.Services.LogMonitor;
using FIGCommon.Utilities;
using FIGControllerSvc.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;

namespace FIGControllerSvc
{
    public class Program
    {
        private const string WindowsServiceNameArgument = "--windows-service-name";

        public static async Task Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var builder = WebApplication.CreateBuilder(args);

            #region WindowsServiceURLS
            builder.Host.UseWindowsService(c =>
            {
                c.ServiceName = ResolveWindowsServiceName(args, "FIGControllerSvc");
            });
            #endregion WindowsServiceURLS

            #region Configuration
            string settingsFilename = "controller-settings";
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

            #region Hubs
            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10 MB
            });
            #endregion Hubs

            #region FIGControllerHost
            try
            {
                builder.Services.AddKeyedSingleton<ITaskSchedulerService, TaskSchedulerService>("FIGController");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in FIGController");
                throw;
            }
            #endregion FIGControllerHost

            #region Controller
            builder.Services.AddControllers().AddLogFileViewer();  // will add LogFileViewer service
            // Register the IHttpContextAccessor service in the dependency injection (DI) container.
            // This allows you to access the current HTTP context (HttpContext) outside of controllers,
            // such as in services, middleware, or other components.
            builder.Services.AddHttpContextAccessor();
            #endregion Controller

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

            WebApplication? app = null;
            #region BuildApp
            try
            {
                app = builder.Build();

                //app.UseSerilogRequestLogging();   // log using Serilog

                // logging/debugging purposes
                // ---------------------------
                app.Lifetime.ApplicationStarted.Register(() =>
                {
                    var addressFeature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
                    foreach (var addr in (addressFeature?.Addresses ?? []))
                    {
                        Log.Information("Listening on: {Address}", addr);
                    }
                });

                app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        var ex = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
                        Log.Error(ex, "Unhandled exception");
                        context.Response.StatusCode = 500;
                    });
                });
                // ---------------------------

                // Roots identity includes authentication and authorization
                app.UseAuthentication();
                app.UseAuthorization();

                // add hub addresses
                app.MapHub<ClientAgentHub>("/hubs/broker-agent");
                app.MapControllers(); // Maps controller actions
                
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in building the app");
                throw;
            }
            #endregion BuildApp

            // Initialize MainRepo
            MainRepo.Initialize(app.Services, "ControllerConnection");

            var config = new ControllerConfig();
            app.Configuration.GetSection("ControllerConfig").Bind(config);
            config.Validate();

            app.Lifetime.ApplicationStarted.Register(() =>
            {
                MainRepo.UpsertDevice(new DeviceRS
                {
                    Id = config.Id,
                    Name = config.Name,
                    ServiceName = config.ServiceName,
                    ServiceType = config.ServiceType,
                    MachineName = config.DeviceName,
                    IPAddress = config.HostAddress,
                    Role = config.Role,
                    Version = config.VersionNumber,
                    ConnectionId = "", // Controller is the hub, not a hub client
                    Enabled = true
                });
            });

            app.Lifetime.ApplicationStopping.Register(() =>
            {
                MainRepo.InvalidateDevice(config.Id, "");
            });

            var endpoint = app.Configuration["Kestrel:Endpoints:Https:Url"];

            Log.Information("Resolved Kestrel endpoint: {Endpoint}", endpoint);

            if (string.IsNullOrWhiteSpace(endpoint) || endpoint.Contains("{{"))
            {
                throw new InvalidOperationException(
                    $"Kestrel endpoint contains an unresolved variable: {endpoint}");
            }

            if (isService)
            {
                try
                {
                    Log.Information("Starting FIGControllerSvc host...");
                    await app.StartAsync();
                    Log.Information("FIGControllerSvc host started successfully.");
                    await app.WaitForShutdownAsync();
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "FIGControllerSvc failed during startup. Endpoint={Endpoint}", endpoint);
                    throw; throw;
                }
                finally
                {
                    await Log.CloseAndFlushAsync();
                }
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
