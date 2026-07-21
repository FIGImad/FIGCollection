using FIGCommon.Models;
using FIGCommon.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace RootsIdentity.Providers
{
    public class JwtToken
    {
        private static string ConfigVal(string key, string defVal, IConfiguration? _config)
        {
            string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
            return val == null ? defVal : val;
        }

        public static TokenDescriptor GenerateJwtToken(string email, AspNetUser user, IList<string> roles, string? provider = null, IConfiguration? config = null)
        {
            if (null == provider)
            {
                provider = ConfigVal("Jwt:Provider", "", config);
            }

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            claims.Add(new Claim(ClaimTypes.AuthenticationMethod, provider));
            var configKey = ConfigVal("Jwt:Key", "", config);
            var configIssuer = ConfigVal("Jwt:Issuer", "", config);
            var configAudience = ConfigVal("Jwt:Audience", "", config);
            configKey = ProtectedDataUtil.Unprotect(configKey) ?? configKey;
            configIssuer = ProtectedDataUtil.Unprotect(configIssuer) ?? configIssuer;
            configAudience = ProtectedDataUtil.Unprotect(configAudience) ?? configAudience;

            var expiryMinStr = ConfigVal("Jwt:ExpiryMinutes", "", config);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            int expiryMinutes = Int32.Parse(expiryMinStr);
            var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var token = new JwtSecurityToken(
                configIssuer,
                configAudience,
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new TokenDescriptor()
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                TokenType = "bearer",
                ExpiresIn = expiryMinutes * 60,
                UserName = user.UserName ?? "",
                EmailConfirmed = user.EmailConfirmed,
                IdentityConfirmed = false,      // not used
                Roles = String.Join(",", roles.ToArray()),
                // Tue, 03 Sep 2019 18:45:43 GMT
                Issued = string.Format("{0:ddd, dd MMM yyyy HH:mm:ss}", DateTime.UtcNow),
                Expires = string.Format("{0:ddd, dd MMM yyyy HH:mm:ss}", DateTime.UtcNow.AddMinutes(expiryMinutes))
            };
        }
    }
}
