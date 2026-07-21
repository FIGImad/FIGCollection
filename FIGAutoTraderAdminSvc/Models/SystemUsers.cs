using FIGCommon.Utilities;

namespace FIGAutoTraderAdminSvc.Models
{
    public class SystemUser
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public List<string> Roles { get; set; } = new();

        // Exists if username maches the username of this system user
        public bool Exists { get; set; } = false;
        public bool CredentialValid { get; set; } = false;

        public List<string> RolesConfigured { get; set; } = new();
        public bool RolesValid { get; set; } = false;

        public bool IsValid => Exists && CredentialValid && RolesValid;

        public void Validate()
        {
            Password = Password != "" ? ProtectedDataUtil.Unprotect(Password) ?? "" : Password;
        }

    }

    public interface ISystemUsers
    {
        List<SystemUser> Get();
        void Set(List<SystemUser> items);
    }

    public class SystemUsers : ISystemUsers
    {
        private List<SystemUser> _items;

        public SystemUsers(IConfiguration config)
        {
            try
            {
                _items = config.GetSection("DatabaseAdmin:SystemUsers").Get<List<SystemUser>>() ?? new List<SystemUser>();
                foreach (var item in _items)
                {
                    item.Validate();
                }
            }
            catch
            {
                _items = new();
            }
        }

        public List<SystemUser> Get() => _items;

        public void Set(List<SystemUser> items)
        {
            _items = items;
        }

        public List<string> GetSystemUsers()
        {
            return _items.Select(x => x.Username).ToList();
        }
    }
}


