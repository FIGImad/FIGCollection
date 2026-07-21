using FIGCommon.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using RootsIdentity.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace RootsIdentity.DataAccess
{
    public class Seeder
    {
        #region Roles
        // these will stay hard-coded until we start supporting dynamic authorization
        public static readonly string SUPER_ADMIN_ROLE = "SuperAdmin";
        public static readonly string ADMIN_ROLE = "Administrator";
        public static readonly string OPERATOR_ROLE = "Operator";
        public static readonly string BOT_ROLE = "Bot";
        public static readonly string LINK_ROLE = "LinkAPI";
        public static readonly string ATSAPI_ROLE = "ATSAPI";
        public static string SuperAdminRole
        {
            get
            {
                return SUPER_ADMIN_ROLE;
            }
        }

        private static readonly IdentityRole[] ROLES = new IdentityRole[]
        {
            new IdentityRole(SUPER_ADMIN_ROLE),
            new IdentityRole(ADMIN_ROLE),
            new IdentityRole(OPERATOR_ROLE),
            new IdentityRole(BOT_ROLE),
            new IdentityRole(LINK_ROLE),
            new IdentityRole(ATSAPI_ROLE)
        };

        private static readonly IdentityRole[] ADMIN_DEFAULT_ROLES = new IdentityRole[]
        {
            ROLES[0],   // SUPER_ADMIN_ROLE
            ROLES[1]    // ADMIN_ROLE
        };

        private static readonly IdentityRole[] BOT_DEFAULT_ROLES = new IdentityRole[]
        {
            ROLES[3]    // BOT_ROLE
        };

        private static readonly IdentityRole[] LINK_DEFAULT_ROLES = new IdentityRole[]
        {
            ROLES[4]    // LINK_ROLE
        };

        private static readonly IdentityRole[] ATSAPI_DEFAULT_ROLES = new IdentityRole[]
{
            ROLES[5]    // LINK_ROLE
        };


        public static IdentityRole[] Roles
        {
            get { return ROLES; }
        }

        private static IdentityRole[] AdminDefaultRoles
        {
            get { return ADMIN_DEFAULT_ROLES; }
        }

        private static IdentityRole[] BotDefaultRoles
        {
            get { return BOT_DEFAULT_ROLES; }
        }

        private static IdentityRole[] LinkDefaultRoles
        {
            get { return LINK_DEFAULT_ROLES; }
        }
        private static IdentityRole[] ATSAPIDefaultRoles
        {
            get { return ATSAPI_DEFAULT_ROLES; }
        }
        #endregion Roles


        private UserManager<AspNetUser> _userManager;
        private RoleManager<IdentityRole> _roleManager;
        private IConfiguration? _config;

        public Seeder(UserManager<AspNetUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration? config)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _config = config;
        }

        #region AdminUser
        private string AdminUserName
        {
            get
            {
                string key = "AdminUser:Name";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        private string AdminUserPassword
        {
            get
            {
                string key = "AdminUser:Password";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }
        private string AdminUserEmail
        {
            get
            {
                string key = "AdminUser:Email";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }
        private string AdminUserPhoneNumber
        {
            get
            {
                string key = "AdminUser:PhoneNumber";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }
        private bool AdminUserEmailConfirmed
        {
            get
            {
                string key = "AdminUser:EmailConfirmed";
                string defVal = "false";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return bool.Parse(val == null ? defVal : val);
            }
        }
        private string AdminUserFirstName
        {
            get
            {
                string key = "AdminUser:FirstName";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }
        private string AdminUserLastName
        {
            get
            {
                string key = "AdminUser:LastName";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }
        #endregion AdminUser

        public async Task<bool> SeedSite()
        {
            int numRoles = _roleManager.Roles.Count();
            if (_roleManager.Roles.Count() > 0)
            {
                return false;
            }

            // create roles
            foreach (var role in Roles)
            {
                await _roleManager.CreateAsync(new IdentityRole { Name = role.Name });
            }

            // create admin user
            var adminUser = new AspNetUser()
            {
                UserName = AdminUserName,
                Email = AdminUserEmail,
                PhoneNumber = AdminUserPhoneNumber,
                EmailConfirmed = AdminUserEmailConfirmed,
                FirstName = AdminUserFirstName,
                LastName = AdminUserLastName
            };
            var user = await CreateUser(adminUser, AdminUserPassword, AdminDefaultRoles);

            return true;
        }

        public async Task<AspNetUser> CreateUser(AspNetUser user, string password, IdentityRole[] roles)
        {
            if (null != await _userManager.FindByNameAsync(user.UserName??""))
            {
                throw new Exception($"User '{user.UserName}' user already exists!");
            }

            IdentityResult result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new Exception($"Failed to create user '{user.UserName}'");
            }
            var identityUser = await _userManager.FindByNameAsync(user.UserName??"");

            var roleNames = new List<string>();
            foreach (var role in roles)
            {
                if (role.Name != null)
                {
                    roleNames.Add(role.Name);
                }
            }

            result = await _userManager.AddToRolesAsync(user, roleNames.ToArray());
            if (!result.Succeeded)
            {
                // user cascade deleted when tenant is deleted
                throw new Exception($"Failed to add roles to user '{user.UserName}'");
            }

            return user;
        }

        public async Task CreateRole(int role)
        {
            await _roleManager.CreateAsync(Roles[role]);
        }


        public static string GetRandomPassword(int length)
        {
            if (length < 8)
            {
                throw new ArgumentException("length is below minimum", "length");
            }
            if (length > 1024)
            {
                throw new ArgumentException("length is too long", "length");
            }
            const string alphanumericCharacters =
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                "abcdefghijklmnopqrstuvwxyz" +
                "0123456789" +
                "!@#$%^&*";
            string capString = GetRandomString(1, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            string numString = GetRandomString(1, "0123456789");
            string symbolString = GetRandomString(1, "!@#$%^&*");
            string randString = GetRandomString(length - 3, alphanumericCharacters);
            return capString + numString + randString + symbolString;
        }

        public static string GetRandomString(int length, IEnumerable<char> characterSet)
        {
            var characterArray = characterSet.Distinct().ToArray();
            if (characterArray.Length == 0)
            {
                throw new ArgumentException("characterSet must not be empty", "characterSet");
            }

            using (var generator = RandomNumberGenerator.Create())
            {
                var bytes = new byte[length * 8];
                generator.GetBytes(bytes);
                var result = new char[length];
                for (int i = 0; i < length; i++)
                {
                    ulong value = BitConverter.ToUInt64(bytes, i * 8);
                    result[i] = characterArray[value % (uint)characterArray.Length];
                }

                return new string(result);
            }
        }
    }
}
