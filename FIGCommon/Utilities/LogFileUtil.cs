
using FIGCommon.Models.LogMonitor;
using Serilog;
using Serilog.Templates;

namespace FIGCommon.Utilities
{
    public static class LogFileUtil
    {
        // return configuration
        public static LogSpec GetSerilogSpec(IConfiguration configuration)
        {
            string expression = "{@t:HH:mm:ss.fff};{#if AlertType is not null}{AlertType}{#else}{@l:u3}{#end};{MachineName};TID:{ThreadId};{Coalesce(Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1), '<none>')};{@m}\n{@x}";
            string format = "{@time:HH:mm:ss.fff};{@level};{@machine};{@thread};{@source};{@message}";
            string logName = configuration.GetValue<string>("LogFile:AppName", "application");
            string logPath = configuration.GetValue<string>("LogFile:LogPath", "") ?? "";
            string minLevel = configuration.GetValue<string>("LogFile:MinLevel", "Information");
            return new LogSpec(logName, logPath, format, expression, minLevel);
        }


        // Returns logger configuration
        public static LoggerConfiguration GetSerilogConfig(LoggerConfiguration? loggerConfig, IConfiguration configuration)
        {
            LogSpec spec = GetSerilogSpec(configuration);
            var expressionTemplate = new ExpressionTemplate(spec.Expression);
            var minLevel = spec.MinLevel;
            var logPath = Path.Combine(spec.FolderPath, spec.FileFilter);
            var appName = spec.LogName;
            string logDir = spec.FolderPath;
            if (logPath != null && logPath.Length > 0 && logDir.Length > 0)
            {
                // Ensure the directory exists
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
            }
            if (loggerConfig == null)
            {
                loggerConfig = new LoggerConfiguration();
            }
            loggerConfig
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.FromLogContext()
                .Filter.ByExcluding("StartsWith(SourceContext, 'Microsoft.')")
                .Enrich.WithProperty("ApplicationName", appName);

            // Set minimum level
            loggerConfig = minLevel switch
            {
                "Verbose" => loggerConfig.MinimumLevel.Verbose(),
                "Fatal" => loggerConfig.MinimumLevel.Fatal(),
                "Information" => loggerConfig.MinimumLevel.Information(),
                "Warning" => loggerConfig.MinimumLevel.Warning(),
                "Error" => loggerConfig.MinimumLevel.Error(),
                _ => loggerConfig.MinimumLevel.Debug()
            };

            // Configure file sink if path exists
            if (!string.IsNullOrEmpty(logPath))
            {
                loggerConfig.WriteTo.File(
                    formatter: expressionTemplate,
                    path: logPath.Replace("-*.txt", "-.txt"),
                    rollingInterval: RollingInterval.Day,
                    buffered: false,
                    flushToDiskInterval: TimeSpan.FromSeconds(5),
                    shared: true,
                    fileSizeLimitBytes: 2147483648,
                    retainedFileCountLimit: 5
                );
            }

            return loggerConfig;
        }
    }
}