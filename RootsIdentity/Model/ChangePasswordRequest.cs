namespace RootsIdentity.Models
{
    public class ChangePasswordRequest
    {
        public string Password { get; set; } = "";
        public string UserName { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}

