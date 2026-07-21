//TokenDescriptor
namespace FIGCommon.Models.Identity
{
    public class TokenDescriptorDto
    {
        public string AccessToken { get; set; } = "";
        public string TokenType { get; set; } = "";
        public int ExpiresIn { get; set; } = 0;
        public string UserName { get; set; } = "";
        public bool EmailConfirmed { get; set; } = false;
        public bool IdentityConfirmed { get; set; } = false;
        public string Roles { get; set; } = "";
        public string Issued { get; set; } = "";
        public string Expires { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public string Rts { get; set; } = "";
    }
}
