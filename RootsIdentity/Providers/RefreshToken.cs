using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Text;

namespace RootsIdentity.Providers
{
    public class RefreshToken
    {
        private static readonly int ITERATION_COUNT = 100;
        private readonly string accessToken;

        public RefreshToken(string accessToken)
        {
            this.accessToken = accessToken;
        }

        public Tuple<string, string> Create()
        {
            int random = new Random((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds).Next();
            string salt = $"{random}";
            Tuple<string, string> tuple = new Tuple<string, string>(Hash(accessToken, salt), salt);
            return tuple;
        }

        public static bool Verify(string accessToken, string refreshToken, string salt)
        {
            var hash = Hash(accessToken, salt);
            return refreshToken.Equals(refreshToken);
        }

        static private string Hash(string token, string salt)
        {
            byte[] saltBytes = Encoding.UTF8.GetBytes(salt);

            // derive a 256-bit subkey (use HMACSHA1 with 100 iterations)
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: token,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: ITERATION_COUNT,
                numBytesRequested: 256 / 8));
        }
    }
}
