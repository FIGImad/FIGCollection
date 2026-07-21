using Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Authentication;

namespace Alert_DDNS.Services
{
    public class AlertDDNSService : IHostedService, IDisposable
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IConfiguration _configuration;
        //private readonly IHttpClientFactory _httpClientFactory;
        private readonly List<Timer> _timers = new List<Timer>();
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        private readonly Object _lockAccess = new Object();
        private bool _disposed;

        public AlertDDNSService(
            IHostApplicationLifetime hostApplicationLifetime,
            IConfiguration configuration)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _configuration = configuration;
            //_httpClientFactory = httpClientFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _hostApplicationLifetime.ApplicationStopping.Register(OnServiceStopping);

            var ddnsServers = _configuration.GetSection("DdnsServers").Get<List<DdnsServerConfig>>();
            if (ddnsServers == null || !ddnsServers.Any())
            {
                return Task.CompletedTask;
            }

            foreach (var server in ddnsServers.Where(s => s.Enable))
            {
                var timer = new Timer(
                    async _ => await SendAlert(server),
                    null,
                    TimeSpan.Zero,  // Start immediately
                    TimeSpan.FromMinutes(server.SendEvery)
                );
                _timers.Add(timer);
            }

            return Task.CompletedTask;
        }

        private async Task SendAlert(DdnsServerConfig server)
        {
            try
            {
                if (_stoppingCts.IsCancellationRequested)
                    return;

                var handler = new HttpClientHandler
                {
                    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                using var httpClient = new HttpClient(handler);
                var response = await httpClient.GetAsync(server.AlertURL, _stoppingCts.Token);
                string content = await response.Content.ReadAsStringAsync();

                server.Status = response.StatusCode.ToString();
                server.LastMessage = content ?? string.Empty;
                //WriteConfig<string>(server.Name, "Status", server.Status);
                //WriteConfig<string>(server.Name, "LastMessage", server.LastMessage);
                WriteServerConfig(server);
            }
            catch (OperationCanceledException)
            {
                // Service is stopping, ignore
            }
            catch (Exception ex)
            {
                server.Status = "Exception";
                server.LastMessage = ex.Message;
                //WriteConfig<string>(server.Name, "Status", server.Status);
                //WriteConfig<string>(server.Name, "LastMessage", server.LastMessage);
                WriteServerConfig(server);
            }
        }

        public void OnServiceStopping()
        {
            _stoppingCts.Cancel();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            OnServiceStopping();

            foreach (var timer in _timers)
            {
                timer?.Change(Timeout.Infinite, 0);
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _stoppingCts.Dispose();
                foreach (var timer in _timers)
                {
                    timer?.Dispose();
                }
            }

            _disposed = true;
        }

        public void WriteConfig<T>(string serverName, string property, T value)
        {
            lock (_lockAccess)
            {
                var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                var json = File.ReadAllText(appSettingsPath);
                var jsonObj = JObject.Parse(json);

                // Find the server by name
                var servers = jsonObj["DdnsServers"] as JArray;
                if (servers == null)
                {
                    throw new ArgumentException("DdnsServers section not found in appsettings.json");
                }

                var server = servers.FirstOrDefault(s => s["Name"]?.ToString() == serverName);
                if (server == null)
                {
                    throw new ArgumentException($"Server '{serverName}' not found in appsettings.json");
                }

                // Validate property exists
                if (server[property] == null)
                {
                    throw new ArgumentException($"Property '{property}' not found for server '{serverName}'");
                }

                // Type-specific handling
                if (typeof(T) == typeof(int) && !int.TryParse(value?.ToString(), out _))
                {
                    throw new ArgumentException($"Value for {property} must be an integer");
                }

                // Update the value
                server[property] = JToken.FromObject(value);

                // Pretty-format the JSON when saving
                File.WriteAllText(appSettingsPath, jsonObj.ToString(Formatting.Indented));
            }
        }

        private void WriteServerConfig(DdnsServerConfig serverInfo)
        {
            lock (_lockAccess)
            {
                var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                var json = File.ReadAllText(appSettingsPath);
                var jsonObj = JObject.Parse(json);

                // Find the server by name
                var servers = jsonObj["DdnsServers"] as JArray;
                if (servers == null)
                {
                    throw new ArgumentException("DdnsServers section not found in appsettings.json");
                }

                var server = servers.FirstOrDefault(s => s["Name"]?.ToString() == serverInfo.Name);
                if (server == null)
                {
                    throw new ArgumentException($"Server '{serverInfo.Name}' not found in appsettings.json");
                }

                // Status Update
                server["Status"] = JToken.FromObject(serverInfo.Status);
                server["LastMessage"] = JToken.FromObject(serverInfo.LastMessage);
                server["LastUpdateTime"] = JToken.FromObject(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                // Pretty-format the JSON when saving
                File.WriteAllText(appSettingsPath, jsonObj.ToString(Formatting.Indented));
            }
        }


        private class DdnsServerConfig
        {
            public string Name { get; set; } = "";
            public string AlertURL { get; set; } = "";
            public bool Enable { get; set; } = false;
            public int SendEvery { get; set; } = 30;
            public string Status { get; set; } = "";
            public string LastMessage { get; set; } = "";
        }
    }
}