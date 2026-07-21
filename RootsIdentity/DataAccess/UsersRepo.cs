using FIGCommon.DataAccess;
using FIGCommon.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace RootsIdentity.DataAccess
{
    public class UsersRepo
    {
        private static ILogger<UsersRepo>? _logger;
        private static readonly Object lockAccess = new();
        private static readonly RepositoryBase RBASE = new RepositoryBase();

        public static void Initialize(IServiceProvider serviceProvider, string connectionTag = "DefaultConnection")
        {
            string connectionString = "";
            lock (lockAccess)
            {
                _logger = serviceProvider.GetRequiredService<ILogger<UsersRepo>>();

                // get connection string from appsettings.json
                var config = serviceProvider.GetRequiredService<IConfiguration>();
                if (config != null)
                {
                    connectionString = config.GetConnectionString(connectionTag) ?? "";
                }
                if (config == null || connectionString == "")
                {
                    _logger?.LogError($"Database {connectionTag} Configuration is invalid");
                    throw new SqlConfigException();
                }

                // Access RBASE directly as it is a static field
                RBASE.Initialize(connectionString, _logger);
            }
        }

        #region Properties
        public static string ConnectionString
        {
            get
            {
                lock (lockAccess)
                {
                    return RBASE.ConnectionString;
                }
            }
        }

        public static string DatabaseName
        {
            get
            {
                lock (lockAccess)
                {
                    return RBASE.DatabaseName;
                }
            }
        }
        #endregion Properties


    }
}

