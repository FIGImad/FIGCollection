using System.Security.Cryptography;

namespace FIGCommon.Utilities
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0063:Use simple 'using' statement", Justification = "<Pending>")]
    public class AESCryptoUtil
    {
        private static readonly int RSA_SIZE = 32;
        private static readonly int RSA_IV_SIZE = 16;

        public static byte[] LoadRSAPrivateKey(string keyPath, string password)
        {
            byte[] rawData = ReadFile(keyPath);
            var rsa = RSA.Create();
            rsa.ImportFromEncryptedPem(System.Text.Encoding.UTF8.GetString(rawData).ToCharArray(), password);
            return rsa.ExportRSAPrivateKey();
        }

        public static string EncryptString(string str, byte[] pemKey)
        {
            try
            {
                byte[]? encryptedData = EncryptRaw(str, pemKey);
                if (encryptedData != null)
                {
                    return Base64Encode(encryptedData);
                }
            }
            catch (Exception)
            {
                return "";
            }
            return "";
        }

        public static string DecryptString(string base64Str, byte[] pemKey)
        {
            try
            {
                byte[] encryptedData = Base64Decode(base64Str);
                if (encryptedData != null)
                {
                    return DecryptRaw(encryptedData, pemKey);
                }
            }
            catch (Exception)
            {
                return "";
            }
            return "";
        }

        private static byte[]? EncryptRaw(string str, byte[] pemKey)
        {
            if (pemKey == null || pemKey.Length < 48)
            {
                return null;
            }
            try
            {
                byte[] rsaKey = new byte[RSA_SIZE];
                byte[] rsaIV = new byte[RSA_IV_SIZE];
                Buffer.BlockCopy(pemKey, 0, rsaKey, 0, RSA_SIZE);
                Buffer.BlockCopy(pemKey, RSA_SIZE, rsaIV, 0, RSA_IV_SIZE);
                using (Aes myAes = Aes.Create())
                {
                    myAes.KeySize = RSA_SIZE * 8;
                    myAes.Key = rsaKey;
                    myAes.IV = rsaIV;

                    // Encrypt the string to an array of bytes.
                    return EncryptStringToBytes_Aes(str, myAes.Key, myAes.IV);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string DecryptRaw(byte[] encryptedData, byte[] pemKey)
        {
            if (pemKey == null || pemKey.Length < 48)
            {
                return "";
            }
            try
            {
                byte[] rsaKey = new byte[RSA_SIZE];
                byte[] rsaIV = new byte[RSA_IV_SIZE];
                Buffer.BlockCopy(pemKey, 0, rsaKey, 0, RSA_SIZE);
                Buffer.BlockCopy(pemKey, RSA_SIZE, rsaIV, 0, RSA_IV_SIZE);
                using (Aes myAes = Aes.Create())
                {
                    myAes.KeySize = RSA_SIZE * 8;
                    myAes.Key = rsaKey;
                    myAes.IV = rsaIV;
                    return DecryptStringFromBytes_Aes(encryptedData, myAes.Key, myAes.IV);
                }
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static string Base64Encode(byte[] data)
        {
            return System.Convert.ToBase64String(data, Base64FormattingOptions.None);
        }

        public static byte[] Base64Decode(string base64EncodedData)
        {
            return System.Convert.FromBase64String(base64EncodedData);
        }

        static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException(nameof(plainText));
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException(nameof(Key));
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException(nameof(IV));
            byte[] encrypted;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new())
                {
                    using (CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException(nameof(cipherText));
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException(nameof(Key));
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException(nameof(IV));

            // Declare the string used to hold
            // the decrypted text.
            string? plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new(cipherText))
                {
                    using (CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }
        //Reads a file.
        internal static byte[] ReadFile(string fileName)
        {
            FileStream f = new(fileName, FileMode.Open, FileAccess.Read);
            int size = (int)f.Length;
            byte[] data = new byte[size];
            f.ReadExactly(data, 0, size);
            f.Close();
            return data;
        }


        //private static X509Certificate2 LoadPrivateKey(string keyPath, string password)
        //{
        //    //Create X509Certificate2 object from .cer file.
        //    byte[] rawData = ReadFile(keyPath);

        //    var rsa = RSA.Create();
        //    rsa.ImportFromPem(Encoding.Unicode.GetChars(rawData));

        //    var decryptedBytes = rsa.Decrypt(Convert.FromBase64String("{ base64-encoded encrypted string }"), RSAEncryptionPadding.Pkcs1);

        //    return new X509Certificate2(rawData, password);
        //}

    }
}

