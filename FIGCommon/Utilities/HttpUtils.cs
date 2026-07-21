using Serilog;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
namespace FIGCommon.Utilities
{
    public class HttpUtils
    {
        private static readonly ConcurrentDictionary<string, HttpClient> _clients = new();

        public HttpUtils()
        {
        }


        public static async Task<string> GetJsonAsync(string url, string token, string thumbPrint,
                 int millisecondsTimeout = 500000, CancellationToken cancellationToken = default)
        {
            HttpClient client = GetHttpClientWithCert(thumbPrint);

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(millisecondsTimeout));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token);

            // add token for JWT authentication
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Send GET request
            using HttpResponseMessage response = await client.SendAsync(request, linkedCts.Token);
            string responseBody = await response.Content.ReadAsStringAsync(linkedCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}\n{responseBody}",
                    inner: null,
                    statusCode: response.StatusCode);
            }

            return await response.Content.ReadAsStringAsync(linkedCts.Token);
        }


        public static async Task<string> PostJsonAsync<T>(string url, T content, string token, string thumbPrint,
                        int millisecondsTimeout = 500000, CancellationToken cancellationToken = default)
        {
            HttpClient client = GetHttpClientWithCert(thumbPrint);

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(millisecondsTimeout));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token);

            StringContent? httpContent = null;
            if (content is StringContent)
            {
                httpContent = content as StringContent;
            }
            else
            {
                string jsonContent = JsonSerializer.Serialize(content);
                httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = httpContent;

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using HttpResponseMessage response = await client.SendAsync(request, linkedCts.Token);
            string responseBody = await response.Content.ReadAsStringAsync(linkedCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}\n{responseBody}",
                    inner: null,
                    statusCode: response.StatusCode);
            }
            //response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(linkedCts.Token);
        }

        public static async Task<string> PutJsonAsync<T>(string url, T content, string token, string thumbPrint,
                int millisecondsTimeout = 500000, CancellationToken cancellationToken = default)
        {
            HttpClient client = GetHttpClientWithCert(thumbPrint);

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(millisecondsTimeout));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token);

            string jsonContent = JsonSerializer.Serialize(content);

            using var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using HttpResponseMessage response = await client.SendAsync(request, linkedCts.Token);
            string responseBody = await response.Content.ReadAsStringAsync(linkedCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}\n{responseBody}",
                    inner: null,
                    statusCode: response.StatusCode);
            }            //response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(linkedCts.Token);
        }

        public static async Task<string> DeleteJsonAsync(string url, string token, string thumbPrint,
                        int millisecondsTimeout = 500000, CancellationToken cancellationToken = default)
        {
            HttpClient client = GetHttpClientWithCert(thumbPrint);

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(millisecondsTimeout));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token);

            // add token for JWT authentication
            using var request = new HttpRequestMessage(HttpMethod.Delete, url);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Send DELETE request
            using HttpResponseMessage response = await client.SendAsync(request, linkedCts.Token);
            string responseBody = await response.Content.ReadAsStringAsync(linkedCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}\n{responseBody}",
                    inner: null,
                    statusCode: response.StatusCode);
            }            //response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(linkedCts.Token);
        }

        private static readonly ConcurrentDictionary<string, SocketsHttpHandler> _handlers = new();

        /// <summary>
        /// Creates a new <see cref="SocketsHttpHandler"/> for the given thumbprint without caching.
        /// The caller owns the handler's lifetime and is responsible for disposing it.
        /// Use this when handing a handler to a component (e.g. SignalR) that will dispose it.
        /// </summary>
        public static SocketsHttpHandler CreateFreshHttpHandlerWithCert(string thumbPrint)
        {
            var cert = CertificateUtil.GetCertificateFromStore(thumbPrint);
            var normalizedThumbprint = thumbPrint.Replace(" ", "").ToUpperInvariant();
            var sslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
                                   | System.Security.Authentication.SslProtocols.Tls13,
                RemoteCertificateValidationCallback = (sender, serverCert, chain, errors) =>
                {
                    if (errors == System.Net.Security.SslPolicyErrors.None)
                        return true;
                    if (!string.IsNullOrEmpty(normalizedThumbprint) && serverCert is System.Security.Cryptography.X509Certificates.X509Certificate2 cert2)
                    {
                        var serverThumbprint = cert2.Thumbprint?.Replace(" ", "").ToUpperInvariant();
                        if (serverThumbprint == normalizedThumbprint)
                            return true;
                    }
                    return (errors & ~System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch) == System.Net.Security.SslPolicyErrors.None;
                }
            };
            if (cert != null)
            {
                sslOptions.ClientCertificates = new System.Security.Cryptography.X509Certificates.X509CertificateCollection { cert };
            }
            return new SocketsHttpHandler { SslOptions = sslOptions };
        }

        /// <summary>
        /// Returns a long-lived cached <see cref="SocketsHttpHandler"/> for the given thumbprint.
        /// This handler is shared across all callers for the same thumbprint and must never be
        /// disposed externally. Use <see cref="CreateFreshHttpHandlerWithCert"/> when the consumer
        /// takes ownership of the handler's lifetime (e.g. SignalR's HttpMessageHandlerFactory).
        /// </summary>
        public static SocketsHttpHandler GetHttpHandlerWithCert(string thumbPrint)
        {
            return _handlers.GetOrAdd(thumbPrint, tp => CreateFreshHttpHandlerWithCert(tp));
        }

        public static HttpClient GetHttpClientWithCert(string thumbPrint)
        {
            return _clients.GetOrAdd(thumbPrint, tp =>
                new HttpClient(GetHttpHandlerWithCert(tp)));
        }


        public static X509Certificate2? GetCertificate(IConfiguration config)
        {
            // load the certificate programmatically.. for some reason loading it from appsettings.json is not successful
            // so this section is to force it load the certificate from LocalMachine/My store
            // Check if Kerstel server running as https first
            var httpsURL = config.GetValue<string>("Kestrel:Endpoints:Https:Url");
            if (string.IsNullOrEmpty(httpsURL))
            {
                return null;
            }

            // get thumbprint — prefer Kestrel:Certificate block, fall back to ControllerConfig:CertThumbPrint
            var thumbprint = config.GetValue<string>("Kestrel:Endpoints:Https:Certificate:Thumbprint");
            if (string.IsNullOrEmpty(thumbprint))
            {
                thumbprint = config.GetValue<string>("ControllerConfig:CertThumbPrint");
            }
            if (string.IsNullOrEmpty(thumbprint))
            {
                throw new Exception("Kestrel HTTPS URL or Certificate Thumbprint is not configured");
            }
            // get Location/Store — prefer Kestrel:Certificate block, default to LocalMachine/My
            var location = config.GetValue<string>("Kestrel:Endpoints:Https:Certificate:Location") ?? "LocalMachine";
            var storeKey = config.GetValue<string>("Kestrel:Endpoints:Https:Certificate:Store") ?? "My";

            // get storeLocation from location string
            StoreLocation storeLocation = location.ToLower() == "currentuser" ? StoreLocation.CurrentUser : StoreLocation.LocalMachine;
            StoreName storeName = storeKey.ToLower() == "my" ? StoreName.My
                : storeKey.ToLower() == "root" ? StoreName.Root
                : storeKey.ToLower() == "certificateauthority" ? StoreName.CertificateAuthority
                : storeKey.ToLower() == "authroot" ? StoreName.AuthRoot
                : storeKey.ToLower() == "trustedpublisher" ? StoreName.TrustedPublisher
                : StoreName.My; // default to Personal store

            // Strip spaces and invisible characters (e.g. U+200E) that Windows cert viewer adds when copying thumbprints
            thumbprint = thumbprint.Replace(" ", "").Trim().TrimStart('\u200e', '\u200f', '\u200b', '\u200c', '\u200d', '\uFEFF');

            Log.Debug("Looking for certificate - Thumbprint: {Thumbprint}, Store: {Store}, Location: {Location}", thumbprint, storeName, storeLocation);

            var store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
            if (certs.Count == 0)
            {
                throw new Exception("Certificate not found");
            }
            var cert = certs[0];
            Log.Information("Certificate loaded - Subject: {Subject}, Thumbprint: {Thumbprint}, NotBefore: {NotBefore}, NotAfter: {NotAfter}",
                cert.Subject, cert.Thumbprint, cert.NotBefore, cert.NotAfter);
            if (!cert.HasPrivateKey)
            {
                throw new Exception("Certificate has no private key");
            }
            return cert;
        }
    }
}