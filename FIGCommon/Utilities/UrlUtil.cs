using System;


namespace FIGCommon.Utilities
{
    public static class UrlUtil
    {
        public static string ReplaceHost(string url, string newHost)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be empty.", nameof(url));

            if (string.IsNullOrWhiteSpace(newHost))
                throw new ArgumentException("New host cannot be empty.", nameof(newHost));

            var originalHadTrailingSlash =
                url.EndsWith("/", StringComparison.Ordinal);

            var uri = new Uri(url, UriKind.Absolute);

            var builder = new UriBuilder(uri)
            {
                Host = newHost
            };

            var result = builder.Uri.GetComponents(
                UriComponents.SchemeAndServer |
                UriComponents.Path |
                UriComponents.Query |
                UriComponents.Fragment,
                UriFormat.UriEscaped
            );

            if (!originalHadTrailingSlash && builder.Path == "/")
            {
                result = result.TrimEnd('/');
            }

            return result;
        }
    }

}
