using Microsoft.Extensions.Configuration;

namespace RootsIdentity.Model
{
    public class RootsIdentityOptions
    {
        public IConfiguration? Config { get; set; } = null;
        public string? Key { get; set; } = null;
        public string? Issuer { get; set; } = null;
        public string? Audience { get; set; } = null;
        public string? ConnStr{ get; set; } = null;
    }

}
