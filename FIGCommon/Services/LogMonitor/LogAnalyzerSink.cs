using FIGCommon.Models.FIGController;
using FIGCommon.Models.LogMonitor;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Concurrent;

namespace FIGCommon.Services.LogMonitor
{
    /// <summary>
    /// Serilog <see cref="ILogEventSink"/> that intercepts every log event in-process,
    /// evaluates it against a set of <see cref="LogAlertRule"/>s, and enqueues
    /// matching events so <see cref="LogAlertRouter"/> can forward them to the host.
    ///
    /// Thread-safe: Serilog may call <see cref="Emit"/> from any thread.
    /// </summary>
    public sealed class LogAnalyzerSink : ILogEventSink
    {
        private static readonly AsyncLocal<int> ForwardingSuppressionDepth = new();

        // ── Internal alert record ─────────────────────────────────────────────
        //public sealed record LogAlertEvent(
        //    LogEventLevel Level,
        //    string RenderedMessage,
        //    string? SourceContext,
        //    DateTimeOffset Timestamp,
        //    string? ExceptionText);

        // ── State ─────────────────────────────────────────────────────────────
        private readonly BlockingCollection<LogAlertMessage> _queue;
        private readonly int _MaxQueueSize = 50;

        // ── Construction ──────────────────────────────────────────────────────
        private readonly ControllerConfig _controllerConfig;
        private readonly object _lockAccess = new object();

        public LogAnalyzerSink(IConfiguration config)
        {
            _queue = new BlockingCollection<LogAlertMessage>(_MaxQueueSize);
            _controllerConfig = new ControllerConfig();
            config.GetSection("ControllerConfig").Bind(_controllerConfig);
            _controllerConfig.Validate();
        }

        // ── ILogEventSink ─────────────────────────────────────────────────────

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null || ForwardingSuppressionDepth.Value > 0) return;

            string rendered = logEvent.RenderMessage().Replace("\"\"", "\"");
            string? sourceContext = null;
            if (logEvent.Properties.TryGetValue("SourceContext", out var scProp))
                sourceContext = scProp.ToString().Trim('"');

            bool isAlert = logEvent.Properties.ContainsKey("AlertType");

            if (logEvent.Level == LogEventLevel.Error
                || logEvent.Level == LogEventLevel.Fatal
                || isAlert)
            {
                var alert = new LogAlertMessage
                {
                    LogName = _controllerConfig.Name,
                    ServiceId = _controllerConfig.Id,
                    Level = isAlert ? "ALT" : logEvent.Level.ToString(),
                    Message = rendered,
                    Source = sourceContext ?? "",
                    RawTime = logEvent.Timestamp.ToUnixTimeSeconds(),
                    Milliseconds = logEvent.Timestamp.Millisecond,
                    ExceptionText = logEvent.Exception?.ToString() ?? ""
                };

                // Drop oldest if full (non-blocking) rather than blocking the caller.
                if (_queue.Count >= _MaxQueueSize)
                    _queue.TryTake(out _);
                _queue.TryAdd(alert);
            }
        }

        /// <summary>Exposes the queue so <see cref="LogAlertRouter"/> can consume it.</summary>
        internal BlockingCollection<LogAlertMessage> Queue => _queue;

        /// <summary>
        /// Prevents logs produced while forwarding an alert from being enqueued as
        /// new alerts. The scope flows across awaits through <see cref="AsyncLocal{T}"/>.
        /// </summary>
        internal static IDisposable SuppressForwarding()
        {
            ForwardingSuppressionDepth.Value++;
            return new ForwardingSuppressionScope();
        }

        private sealed class ForwardingSuppressionScope : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed) return;

                ForwardingSuppressionDepth.Value =
                    Math.Max(0, ForwardingSuppressionDepth.Value - 1);
                _disposed = true;
            }
        }
    }
}
