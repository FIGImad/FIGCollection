using System.Text;
using System.Text.RegularExpressions;

public static class StringExtensions
{
    public static string ToUrlSlug(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // Convert to lower case
        value = value.ToLowerInvariant();

        // Remove all accents and diacritics
        var bytes = Encoding.GetEncoding("Cyrillic").GetBytes(value);
        value = Encoding.ASCII.GetString(bytes);

        // Replace spaces with hyphens
        value = Regex.Replace(value, @"\s", "-", RegexOptions.Compiled);

        // Remove invalid characters
        value = Regex.Replace(value, @"[^a-z0-9\s-_]", string.Empty, RegexOptions.Compiled);

        // Trim multiple hyphens
        value = Regex.Replace(value, @"-+", "-", RegexOptions.Compiled).Trim('-');

        return value;
    }
}