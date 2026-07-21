using Alert_DDNS.Services;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Security.Authentication;
using System.Text;

namespace Alert_DDNS
{
    public class Program
    {
        [SupportedOSPlatform("windows")]
        public static async Task Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var builder = Host.CreateDefaultBuilder(args)
                .UseWindowsService(options =>
                {
                    options.ServiceName = "FIG_DDNS_Job";
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(AppContext.BaseDirectory)
                          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                          .AddEnvironmentVariables()
                          .AddCommandLine(args);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient("DDNS", client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);
                    })
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                    {
                        SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                    });

                    services.AddHostedService<AlertDDNSService>();
                });


            var app = builder.Build();
            if (isService)
            {
                await app.RunAsync();
            }
            else
            {
                app.Run();
            }
        }
    }
}
