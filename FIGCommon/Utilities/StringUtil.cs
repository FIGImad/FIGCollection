using System.Security.Cryptography;
using System.Text;

namespace FIGCommon.Utilities
{
    public class StringUtil
    {

        public static string ComputeChecksum(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Convert the input string to a byte array and compute the hash
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Convert the byte array to a hex string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        //public static (string, string) ExtractKey(string label, char delim)
        //{
        //    string[] parts = label.Split(new[] { delim }, 2); // Split into two parts

        //    if (parts.Length >= 2)
        //    {
        //        return (parts[0], parts[1]);
        //    }
        //    else if (parts.Length >= 1)
        //    {
        //        return (parts[0], "");
        //    }
        //    return (label, "");
        //}
        public static (string, string) ExtractFirstKey(string label, char separator)
        {
            string[] parts = label.Split(new[] { separator }, 2); // Split into two parts

            if (parts.Length > 1)
            {
                return (parts[0], parts[1]);
            }
            if (parts.Length > 0)
            {
                return (parts[0], "");
            }
            return (label, "");
        }

        public static (string Host, int Port) ParseHostPort(string input, int defaultPort = -1)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("URL is empty.", nameof(input));

            // Uri requires a scheme for reliable parsing (especially for host/port).
            var normalized = input.Contains("://", StringComparison.Ordinal)
                ? input
                : "http://" + input;

            var uri = new Uri(normalized, UriKind.Absolute);

            // If no port was specified, Uri.Port becomes the scheme default (80/443).
            // If you want "no port specified" to remain as defaultPort, detect it:
            var portSpecified = uri.IsDefaultPort == false;

            return (uri.Host, portSpecified ? uri.Port : defaultPort);
        }
    }
}
