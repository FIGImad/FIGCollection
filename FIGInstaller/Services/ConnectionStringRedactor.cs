using System.Data.Common;

namespace FIGInstaller.Services;

public static class ConnectionStringRedactor
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Access Token",
        "Password",
        "Pwd",
        "Secret",
        "Shared Access Key"
    };

    public static string Redact(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "";
        }

        try
        {
            var source = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };
            var redacted = new DbConnectionStringBuilder();

            foreach (string key in source.Keys)
            {
                redacted[key] = SensitiveKeys.Contains(key) ? "***" : source[key];
            }

            return redacted.ConnectionString;
        }
        catch (ArgumentException)
        {
            return "(connection string was provided, but could not be safely displayed)";
        }
    }
}
