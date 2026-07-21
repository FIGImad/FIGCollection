using FIGCommon.Services;
using FIGCommon.Utilities;
using System.Reflection;
using System.Text.Json.Serialization;

namespace FIGCommon.Models.FIGController
{
    public enum ControllerClientRole
    {
        None = 0,
        Bus = 1,
        Interface = 2,
        PriceSync = 4,
        Signal = 8,
        ATS = 16,
        Broker = 32,
        //Monitor = 64,
        ServiceManager = 128,
        Alert= 256
    }

    public class ControllerConfig
    {
        // General Settings
        //------------------------------
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public string CertThumbPrint { get; set; } = string.Empty;
        public string VersionNumber { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;

        // ControllerClient Settings
        //-----------------------------
        public string HostAddress { get; set; } = string.Empty;
        public string AuthUsername { get; set; } = string.Empty;
        public string AuthPassword { get; set; } = string.Empty;

        // Authorization URL
        public string AuthURL { get; set; } = string.Empty;

        // API request definitions
        public string ReqLogin { get; set; } = "/api/account/login";
        public string ReqRegister { get; set; } = "/api/register/service";
        public string ReqStatusCheck { get; set; } = "/api/status/notify";
        public string ReqRouteMsg { get; set; } = "/api/route";

        protected string authPassword = string.Empty;
        protected int role = 0;

        [System.Text.Json.Serialization.JsonConstructor]
        public ControllerConfig()
        {
            this.Id = "";
            this.Name = "";
            this.ServiceName = "";
            this.ServiceType = "";
            this.DeviceName = Environment.MachineName;
            this.Roles = new();
            this.CertThumbPrint = "";
            this.VersionNumber = "";
            this.HostAddress = "";
            this.AuthUsername = "";
            this.AuthPassword = "";
            this.ReqLogin = "/api/account/login";
            this.ReqRegister = "/api/register/service";
            this.ReqStatusCheck = "/api/status/notify";
            this.authPassword = string.Empty;
            this.role = 0;
            this.AuthURL = "";
        }

        public ControllerConfig(ControllerConfig rec)
        {
            this.Id = rec.Id;
            this.Name = rec.Name;
            this.ServiceName = rec.ServiceName;
            this.ServiceType = rec.ServiceType;
            this.DeviceName = rec.DeviceName;
            this.Roles = new List<string>(rec.Roles);
            this.CertThumbPrint = rec.CertThumbPrint;
            this.VersionNumber = rec.VersionNumber;
            this.HostAddress = rec.HostAddress;
            this.AuthUsername = rec.AuthUsername;
            this.AuthPassword = rec.AuthPassword;
            this.ReqLogin = rec.ReqLogin;
            this.ReqRegister = rec.ReqRegister;
            this.ReqStatusCheck = rec.ReqStatusCheck;
            this.authPassword = rec.authPassword;
            this.role = rec.role;
            this.AuthURL = rec.AuthURL;
        }

        [JsonIgnore]
        public string AuthPasswordFlat
        {
            get => authPassword;
        }

        [JsonIgnore]
        public int Role
        {
            get
            {
                if (role == 0)
                {
                    role = CompileRole(Roles);
                }
                return role;
            }
        }

        [JsonIgnore]
        public bool IsBus
        {
            get
            {
                return (Role & (int)ControllerClientRole.Bus) != 0;
            }
        }

        public void Validate()
        {
            // validate HostAddress
            if (HostAddress.EndsWith("/"))
            {
                HostAddress = HostAddress.Remove(HostAddress.Length - 1);
            }
            // try decrypt password
            if (authPassword == string.Empty && AuthPassword != string.Empty)
            {
                authPassword = ProtectedDataUtil.Unprotect(AuthPassword) ?? AuthPassword;
            }
            // populate version number if empty
            if (VersionNumber == string.Empty)
            {
                VersionNumber = GetAppVersion();
            }
            // Determine Role
            if (Role == 0)
            {
                role = CompileRole(Roles);
            }
            DeviceName = Environment.MachineName;
        }

        public static string GetAppVersion()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        }

        public static int CompileRole(List<string> roles)
        {
            int role = 0;
            foreach (string roleName in roles)
            {
                if (Enum.TryParse(roleName, out ControllerClientRole roleValue))
                {
                    role |= (int)roleValue;
                }
            }
            return role;
        }

        public static string GetRoleNamesCsv(int role)
        {
            var roles = Enum.GetValues<ControllerClientRole>()
                .Where(r => r != ControllerClientRole.None && (role & (int)r) == (int)r)
                .Select(r => r.ToString());

            return string.Join(",", roles);
        }

    }
}
