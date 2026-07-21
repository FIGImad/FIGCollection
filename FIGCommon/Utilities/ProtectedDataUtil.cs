using System.Security.Cryptography;
using System.Text;

namespace FIGCommon.Utilities
{
    /// <summary>
    /// Provides two protection strategies:
    ///
    /// 1. Windows-only (ProtectedData / DPAPI):
    ///    StorePassPhrase / GetPassPhrase  — tied to a Windows user or machine scope.
    ///    Cannot be decrypted by a different user or on a non-Windows OS.
    ///
    /// 2. Cross-platform (AES-256-GCM):
    ///    Protect / Unprotect  — works on any OS and any user account.
    ///    Requires a 32-byte master key supplied via the environment variable
    ///    FIG_MASTER_KEY (Base64-encoded).  Set it once per machine/service:
    ///
    ///      $key = [Convert]::ToBase64String((1..32 | ForEach-Object { [byte](Get-Random -Max 256) }))
    ///      [System.Environment]::SetEnvironmentVariable("FIG_MASTER_KEY", $key, "Machine")
    ///
    ///    Any service account that can read that machine-level env var can decrypt.
    ///    To restrict to a group, set it as a User-scope variable for each account instead.
    /// </summary>
    public class ProtectedDataUtil
    {
        private const string MasterKeyEnvVar = "FIG_MASTER_KEY";
        // AES-GCM constants
        private const int NonceSize = 12;   // 96-bit nonce  (NIST recommended)
        private const int TagSize   = 16;   // 128-bit authentication tag

        ////remember to clear password after usage using 
        ////CryptographicOperations.ZeroMemory(passphraseBytes);
        //public static string? StorePassPhrase(DataProtectionScope scope, string passPhrase)
        //{
        //    if (passPhrase == "")
        //    {
        //        return "";
        //    }
        //    byte[] plainBytes = Encoding.UTF8.GetBytes(passPhrase);
        //    try
        //    {
        //        byte[] encrypted = ProtectedData.Protect(plainBytes, null, scope);
        //        try
        //        {
        //            return Convert.ToBase64String(encrypted);
        //        }
        //        finally
        //        {
        //            CryptographicOperations.ZeroMemory(encrypted);
        //        }
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //    finally
        //    {
        //        CryptographicOperations.ZeroMemory(plainBytes);
        //    }
        //}

        ////remember to clear passPhrase after finishing using it by using:
        ////CryptographicOperations.ZeroMemory(passphraseBytes);
        //public static string? GetPassPhrase(DataProtectionScope scope, string encryptedPassPhrase)
        //{
        //    byte[]? protectedBytes = null;
        //    byte[]? passphraseBytes = null;
        //    if (encryptedPassPhrase == "")
        //    {
        //        return "";
        //    }
        //    try
        //    {
        //        protectedBytes = Convert.FromBase64String(encryptedPassPhrase);
        //        passphraseBytes = ProtectedData.Unprotect(protectedBytes, null, scope);
        //        return Encoding.UTF8.GetString(passphraseBytes);
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //    finally
        //    {
        //        if (passphraseBytes != null)
        //        {
        //            CryptographicOperations.ZeroMemory(passphraseBytes.AsSpan());
        //        }
        //    }
        //}


        /// <summary>
        /// Cross-platform encryption using AES-256-GCM.
        /// Encrypts <paramref name="plainText"/> using the master key from the
        /// FIG_MASTER_KEY environment variable.
        /// Returns a Base64 string in the format: nonce(12) + ciphertext + tag(16)
        /// Returns null on failure.
        /// </summary>
        public static string? Protect(string plainText, string masterKeyLoc = MasterKeyEnvVar)
        {
            if (plainText == "") return "";
            byte[]? masterKey = GetMasterKey(masterKeyLoc);
            if (masterKey == null) return null;

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] nonce = new byte[NonceSize];
            byte[] ciphertext = new byte[plainBytes.Length];
            byte[] tag = new byte[TagSize];

            try
            {
                RandomNumberGenerator.Fill(nonce);
                using var aesGcm = new AesGcm(masterKey, TagSize);
                aesGcm.Encrypt(nonce, plainBytes, ciphertext, tag);

                // output = nonce + ciphertext + tag
                byte[] output = new byte[NonceSize + ciphertext.Length + TagSize];
                nonce.CopyTo(output, 0);
                ciphertext.CopyTo(output, NonceSize);
                tag.CopyTo(output, NonceSize + ciphertext.Length);
                return Convert.ToBase64String(output);
            }
            catch
            {
                return null;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plainBytes);
                CryptographicOperations.ZeroMemory(masterKey);
            }
        }

        /// <summary>
        /// Cross-platform decryption using AES-256-GCM.
        /// Decrypts a Base64 string produced by <see cref="Protect"/>.
        /// Returns null on failure (wrong key, tampered data, etc.).
        /// </summary>
        public static string? Unprotect(string encryptedBase64, string masterKeyLoc = MasterKeyEnvVar)
        {
            if (encryptedBase64 == "") return "";
            byte[]? masterKey = GetMasterKey(masterKeyLoc);
            if (masterKey == null) return null;

            byte[]? plainBytes = null;
            try
            {
                byte[] blob = Convert.FromBase64String(encryptedBase64);
                if (blob.Length < NonceSize + TagSize) return null;

                int ciphertextLength = blob.Length - NonceSize - TagSize;
                byte[] nonce = blob[..NonceSize];
                byte[] ciphertext = blob[NonceSize..(NonceSize + ciphertextLength)];
                byte[] tag = blob[(NonceSize + ciphertextLength)..];

                plainBytes = new byte[ciphertextLength];
                using var aesGcm = new AesGcm(masterKey, TagSize);
                aesGcm.Decrypt(nonce, ciphertext, tag, plainBytes);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return null;
            }
            finally
            {
                if (plainBytes != null) CryptographicOperations.ZeroMemory(plainBytes);
                CryptographicOperations.ZeroMemory(masterKey);
            }
        }

        // Reads and validates the master key from (in priority order):
        //   1. Docker secret file  : /run/secrets/FIG_MASTER_KEY  (Docker Swarm / Kubernetes)
        //   2. Machine env var     : FIG_MASTER_KEY               (Windows service / Linux systemd)
        //   3. User env var        : FIG_MASTER_KEY               (dev/test per-user)
        //   4. Process env var     : FIG_MASTER_KEY               (injected at launch, e.g. CI/CD)
        private static byte[]? GetMasterKey(string masterKeyLoc)
        {
            string? b64 = null;

            // 1. Docker / Kubernetes secret file
            string secretFile = Path.Combine("/run/secrets", masterKeyLoc);
            if (File.Exists(secretFile))
            {
                b64 = File.ReadAllText(secretFile).Trim();
            }

            // 2-4. Environment variable (Machine → User → Process)
            if (string.IsNullOrWhiteSpace(b64))
            {
                b64 = Environment.GetEnvironmentVariable(masterKeyLoc, EnvironmentVariableTarget.Machine)
                   ?? Environment.GetEnvironmentVariable(masterKeyLoc, EnvironmentVariableTarget.User)
                   ?? Environment.GetEnvironmentVariable(masterKeyLoc, EnvironmentVariableTarget.Process);
            }

            if (string.IsNullOrWhiteSpace(b64)) return null;
            try
            {
                byte[] key = Convert.FromBase64String(b64);
                return key.Length == 32 ? key : null;   // must be exactly 256-bit
            }
            catch
            {
                return null;
            }
        }
    }
}
