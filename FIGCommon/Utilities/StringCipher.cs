using System.Security.Cryptography;
using System.Text;

namespace FIGCommon.Utilities
{
    public static class StringCipher
    {
        // This size of the IV (in bytes) must = (keysize / 8). Default keysize is 256, so IV is 32 bytes
        private static readonly int BlockSize = 128;
        private static readonly int KeySize = 256;

        // Preconfigured Encryption Parameters
        // Should be at least 8 bytes
        private static readonly byte[] SaltBytes = Encoding.UTF8.GetBytes("CdFIOC3J3SgXxIITmpCg48ymPrZfWWRHef3kBW896bSAwMNJ2o1302cJPAZAigO695RM9GEzlYHUVkU84GxYVnu+");

        public static string Encrypt(string plainText, string passPhrase)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            byte[] keyBytes = Rfc2898DeriveBytes.Pbkdf2(passPhrase, SaltBytes, 10000, HashAlgorithmName.SHA256, KeySize / 8);

            using var aes = Aes.Create();
            aes.BlockSize = BlockSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = keyBytes;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            byte[] result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public static string Decrypt(string cipherText, string passPhrase)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            byte[] keyBytes = Rfc2898DeriveBytes.Pbkdf2(passPhrase, SaltBytes, 10000, HashAlgorithmName.SHA256, KeySize / 8);

            using var aes = Aes.Create();
            aes.BlockSize = BlockSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = keyBytes;

            // Extract IV from the first 16 bytes of cipherText
            byte[] iv = new byte[aes.BlockSize / 8];
            byte[] cipherTextBytes = new byte[cipherBytes.Length - iv.Length];
            Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, iv.Length, cipherTextBytes, 0, cipherTextBytes.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherTextBytes, 0, cipherTextBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
