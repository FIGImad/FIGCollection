using FIGCommon.Interfaces;
using FIGCommon.Models;
using FIGCommon.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RootsIdentity.Controllers;
using RootsIdentity.DataAccess;
using RootsIdentity.Model;
using RootsIdentity.Providers;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;


namespace RootsIdentity
{
    public static class MainHost
    {
        public static IHostBuilder AddRootsIdentity(this IHostBuilder builder, Action<RootsIdentityOptions> setupAction)
        {
            if (builder == null || setupAction == null) throw new ArgumentNullException(nameof(builder));

            RootsIdentityOptions options = new RootsIdentityOptions();
            setupAction?.Invoke(options);


            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.Config == null)
            {
                throw new ArgumentNullException(nameof(options.Config));
            }
            try
            {
                var connectionString = options.Config.GetConnectionString("UserConnection") ?? 
                    throw new InvalidOperationException("Connection string 'UserConnection' not found.");
                if (!connectionString.ToLower().Contains("data source="))
                {
                    // decrypt connection string
                    connectionString = ProtectedDataUtil.Unprotect(connectionString) ?? "";
                }

                var configKey = options.Config["Jwt:Key"] ?? "";
                var configIssuer = options.Config["Jwt:Issuer"] ?? "";
                var configAudience = options.Config["Jwt:Audience"] ?? "";
                configKey = ProtectedDataUtil.Unprotect(configKey) ?? configKey;
                configIssuer = ProtectedDataUtil.Unprotect(configIssuer) ?? configIssuer;
                configAudience = ProtectedDataUtil.Unprotect(configAudience) ?? configAudience;

                options.Key = options.Key == null ? (configKey == null ? "" : configKey) : options.Key;
                options.Issuer = options.Issuer == null ? (configIssuer == null ? "" : configIssuer) : options.Issuer;
                options.Audience = options.Audience == null ? (configAudience == null ? "" : configAudience) : options.Audience;
                options.ConnStr = options.ConnStr == null ? (connectionString == null ? "" : connectionString) : options.ConnStr;
                return builder.ConfigureServices((IServiceCollection services) =>
                {
                    #region dbContext
                    services.AddDbContext<ApplicationDbContext>(opts =>
                        opts.UseSqlServer(options?.ConnStr));
                    #endregion dbContext

                    #region Identity
                    services.AddIdentity<AspNetUser, IdentityRole>()
                        .AddEntityFrameworkStores<ApplicationDbContext>()
                        .AddRoles<IdentityRole>()
                        .AddRoleManager<RoleManager<IdentityRole>>()
                        .AddDefaultTokenProviders();
                    // User/role managers and message service
                    services.AddScoped<IIdentityService, IdentityService>();
                    #endregion Identity

                    #region JWT_Authentication
                    // remove default claims
                    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

                    services.AddAuthentication(sharedOptions =>
                    {
                        sharedOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        sharedOptions.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                        sharedOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer(cfg =>
                    {
                        string? sConfigStr = options?.Key;
                        if (string.IsNullOrWhiteSpace(options?.Key))
                        {
                            throw new InvalidOperationException("JWT signing key is missing.");
                        }
                        cfg.RequireHttpsMetadata = false;
                        cfg.SaveToken = true;
                        cfg.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = options?.Issuer,
                            ValidAudience = options?.Audience,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sConfigStr == null ? "" : sConfigStr)),
                            ClockSkew = TimeSpan.Zero // remove delay of token when expire
                        };
                        cfg.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = context =>
                            {
                                var logger = context.HttpContext.RequestServices
                                    .GetRequiredService<ILoggerFactory>()
                                    .CreateLogger("JwtBearer");

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
                                //var logger = context.HttpContext.RequestServices
                                //    .GetRequiredService<ILoggerFactory>()
                                //    .CreateLogger("JwtBearer");

                                //var claims = context.Principal?.Claims
                                //    .Select(c => $"{c.Type}={c.Value}")
                                //    .ToList();

                                //logger.LogInformation("JWT validated successfully. Claims: {Claims}",
                                //    claims == null ? "none" : string.Join(", ", claims));

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

                    services.AddTransient<IMessageService, SmtpMessageService>();

                    services.AddControllers().AddApplicationPart(typeof(AccountController).Assembly);

                    #endregion JWT_Authentication

                    #region Authorization
                    services.AddAuthorization();
                    #endregion Authorization


                });
            }
            catch (Exception) 
            {
                throw;
            }
        }


        public static void UseRootsIdentity(this IApplicationBuilder app, Action<RootsAppOptions>? optionsAction = null)
        {
            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(OnApplicationStopping);
            lifetime.ApplicationStopped.Register(OnApplicationStopped);

            if (optionsAction != null)
            {
                RootsAppOptions options = new RootsAppOptions();
                optionsAction?.Invoke(options);
            }

            //app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            // initialize Database 
            // I need to get IServiceProvider from app?
            var serviceProvider = app.ApplicationServices.GetRequiredService<IServiceProvider>();
            AccountsRepo.Initialize(serviceProvider);
        }

        private static void OnApplicationStopping()
        {
            Console.WriteLine("Application is stopping...");
            // Perform any cleanup tasks here
        }

        private static void OnApplicationStopped()
        {
            Console.WriteLine("Application has stopped.");
            // Perform any post-stop tasks here
        }

    }
}
