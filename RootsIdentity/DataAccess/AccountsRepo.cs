using FIGCommon.DataAccess;
using FIGCommon.Exceptions;
using FIGCommon.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;

namespace RootsIdentity.DataAccess
{
    public class AccountsRepo
    {
        private static ILogger<AccountsRepo>? _logger;
        private static readonly Object lockAccess = new();
        private static readonly RepositoryBase RBASE = new RepositoryBase();

        public static void Initialize(IServiceProvider serviceProvider, string connectionTag = "UserConnection")
        {
            string connectionString = "";
            lock (lockAccess)
            {
                _logger = serviceProvider.GetRequiredService<ILogger<AccountsRepo>>();

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
       
        #region ValidationToken
        public static ValidationTokenRS? SelectValidationToken(long validationTokenId)
        {
            return RBASE.Select<ValidationTokenRS, long>(new ValidationTokenRS(), "usp_ValidationToken_Select", "@ValidationTokenId", validationTokenId);
        }

        public static int InsertValidationToken(ValidationTokenRS validationToken)
        {
            return RBASE.Insert<ValidationTokenRS>(validationToken, "usp_ValidationToken_Insert");
        }

        public static int DeleteValidationToken(long validationTokenId)
        {
            return RBASE.Delete<long, long, long>("usp_ValidationToken_Delete", "@ValidationTokenId", validationTokenId);
        }

        public static int DeleteValidationTokens(string userId)
        {
            return RBASE.Delete<string, string, string>("usp_ValidationToken_DeleteAll", "@UserId", userId);
        }

        public static int PurgeValidationToken(int hours)
        {
            return RBASE.Delete<int, int, int>("usp_ValidationToken_Purge", "@Hours", hours);
        }
        #endregion ValidationToken

        #region Users

        public static string GetUsersWithRolesJson()
        {
            var procName = "usp_user_select_all_json";
            using (SqlConnection connection = new SqlConnection(RBASE.ConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = procName;
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var result = reader.GetString(0);
                            return result;
                            //entities.Add(prototype.CreateFromSqlDataReader(reader));
                        }
                    }
                }
            }
            return "";

            //return SelectMulti<AspNetUser>(new AspNetUser(), "usp_user_role_select_all");
        }

        public static List<UserRS> GetUsers()
        {
            return RBASE.SelectMulti<UserRS>(new UserRS(), "usp_user_select_all");
        }

        public static UserRS? SelectUser(string Id)
        {
            return RBASE.Select<UserRS, string>(new UserRS(), "usp_user_select", "@Id", Id);
        }

        public static void UpdateUser(AspNetUser user)
        {
            RBASE.Update<AspNetUser>(user, "usp_user_update");
        }

        public static void ConfirmUserEmail(AspNetUser user)
        {
            if (user.EmailConfirmed) return;
            user.EmailConfirmed = true;
            RBASE.Update<AspNetUser>(user, "usp_user_update");
        }
        #endregion Users

        #region Roles

        public static string GetUsersRoles()
        {
            var procName = "usp_role_select_all_json";
            using (SqlConnection connection = new SqlConnection(RBASE.ConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = procName;
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var result = reader.GetString(0);
                            return result;
                            //entities.Add(prototype.CreateFromSqlDataReader(reader));
                        }
                    }
                }
            }
            return "";

            //return SelectMulti<AspNetUser>(new AspNetUser(), "usp_user_role_select_all");
        }

        #endregion Roles
    }
}

