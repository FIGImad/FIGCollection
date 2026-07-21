using FIGCommon.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RootsIdentity.Providers
{
    public class TokenDescriptor
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

        public static TokenDescriptor Create(string accessToken, AspNetUser appUser, IList<string> roles, IConfiguration? _config = null)
        {
            string key = "Jwt:ExpiryMinutes";
            string defVal = "0";
            string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
            int expiryMinutes = Int32.Parse(val == null ? defVal : val);
            var tuple = new RefreshToken(accessToken).Create();

            return new TokenDescriptor()
            {
                AccessToken = accessToken,
                TokenType = "bearer",
                ExpiresIn = expiryMinutes * 60,
                UserName = appUser.UserName == null ? "" : appUser.UserName,
                EmailConfirmed = appUser.EmailConfirmed,
                IdentityConfirmed = false,      // not used
                Roles = String.Join(",", roles.ToArray()),
                Issued = string.Format("{0:ddd; dd MMM yyyy HH:mm:ss GMT}", DateTime.UtcNow),
                Expires = string.Format("{0:ddd; dd MMM yyyy HH:mm:ss GMT}", DateTime.UtcNow.AddMinutes(expiryMinutes)),
                RefreshToken = tuple.Item1,
                Rts = tuple.Item2
            }; 
        }
    }
}
