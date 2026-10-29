using System.Text.Json;
using Microsoft.Extensions.Logging;
using FIGMarketLive.Adapters.IBKR;
using FIGMarketLive.MarketData;

namespace FIGMarketLive;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if(args.FirstOrDefault() is "--migrate-provider-settings" or "--migrate-sql-monitors")
        {
            try
            {
                string file=Path.GetFullPath(args.ElementAtOrDefault(1)??Path.Combine(AppContext.BaseDirectory,"marketlive.json"));
                var settings=SettingsFile.Load(file);
                int count=SettingsFile.MigrateProvidersAsync(file,settings,settings.ConnectionString,default).GetAwaiter().GetResult();
                Console.WriteLine($"TickerProvider schema ready; {count} provider mapping(s) migrated. DataSelections removed after SQL commit.");
                if(args[0]=="--migrate-sql-monitors")
                {
                    int monitors=SettingsFile.MigrateMonitorsAsync(file,settings,settings.ConnectionString,default).GetAwaiter().GetResult();
                    Console.WriteLine($"SQL monitoring configuration validated; {monitors} enabled monitor(s) loaded. Legacy Monitors section removed.");
                }
            }
            catch(Exception ex){Console.Error.WriteLine(ex.Message);Environment.ExitCode=1;}
            return;
        }
        if(args.FirstOrDefault()=="--configuration-self-test")
        {
            try{ConfigurationUiChecks.Run(Path.GetFullPath(args.ElementAtOrDefault(1)??".test-temp/marketlive-ui"));}
            catch(Exception ex){System.IO.File.WriteAllText(Path.GetFullPath(".test-temp/marketlive-ui-error.txt"),ex.ToString());Environment.ExitCode=1;}
            return;
        }
        using var mutex = new Mutex(true, @"Local\FIGMarketLive", out bool first);
        if (!first) return;
        var path = Path.GetFullPath(args.FirstOrDefault() ?? Path.Combine(AppContext.BaseDirectory,"marketlive.json"));
        using var context = new TrayContext(path);
        Application.Run(context);
    }
}

internal sealed class TrayContext : ApplicationContext
{
    private readonly NotifyIcon tray;
    private readonly BrandingAssets branding=new();
    private readonly System.Windows.Forms.Timer healthTimer=new(){Interval=1000};
    private readonly HashSet<int> attention=[];
    private IMarketDataAdapter? liveAdapter;
    private bool monitoring;
    private bool connectedOnce;
    private bool resourcesDisposed;
    private readonly Form window = new() { Text="FIGMarketLive — Study monitor", Width=780, Height=440 };
    private readonly TextBox status = new() { Multiline=true, ReadOnly=true, Dock=DockStyle.Fill, ScrollBars=ScrollBars.Vertical };
    private readonly Button start = new() { Text="Start studies", AutoSize=true };
    private readonly Button pause = new() { Text="Pause", AutoSize=true, Enabled=false };
    private readonly string settingsPath;
    private CancellationTokenSource? cancellation;
    private Task? worker;
    private readonly ILoggerFactory logs;
    private bool exiting;

    public TrayContext(string settingsPath)
    {
        this.settingsPath=settingsPath;
        var logRoot=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FIGMarketLive");
        Directory.CreateDirectory(logRoot);
        logs=LoggerFactory.Create(builder=>builder.AddProvider(new FileLoggerProvider(Path.Combine(logRoot,"marketlive.log"))));
        var buttons=new FlowLayoutPanel { Dock=DockStyle.Top, AutoSize=true };
        var settings=new Button { Text="Configuration", AutoSize=true };
        window.Icon=branding.ApplicationIcon;
        branding.Apply(settings,"settings.png");branding.Apply(start,"play.png");branding.Apply(pause,"pause.png");
        settings.Click+=async (_,_)=>await OpenSettingsAsync(); start.Click+=async (_,_)=>await StartAsync(); pause.Click+=async (_,_)=>await StopAsync();
        buttons.Controls.AddRange([start,pause,settings]);
        window.Controls.Add(status); window.Controls.Add(buttons);
        window.FormClosing+=(_,e)=> { if (!exiting) { e.Cancel=true; window.Hide(); } };
        var menu=new ContextMenuStrip();
        menu.Items.Add("Open",null,(_,_)=>Show());
        menu.Items.Add("Start studies",branding.Image("play.png",16),async (_,_)=>await StartAsync());
        menu.Items.Add("Pause",branding.Image("pause.png",16),async (_,_)=>await StopAsync());
        menu.Items.Add("Configuration",branding.Image("settings.png",16),async (_,_)=>await OpenSettingsAsync());
        menu.Items.Add("Exit",null,async (_,_)=> { exiting=true; await StopAsync(); ExitThread(); });
        tray=new NotifyIcon { Text="FIGMarketLive — idle", Icon=branding.TrayIcon(MonitorIconState.Off), Visible=true, ContextMenuStrip=menu };
        healthTimer.Tick+=(_,_)=>UpdateTray();healthTimer.Start();
        tray.DoubleClick+=(_,_)=>Show();
        _=window.Handle;
        Report("Prices currently come from SQL; automatic price acquisition is not implemented yet.");
        Report($"Configuration: {settingsPath}");
        Show();
    }

    private void Show() { window.Show(); window.WindowState=FormWindowState.Normal; window.Activate(); }

    private async Task OpenSettingsAsync()
    {
        await StopAsync();
        try
        {
            using var dialog=new ConfigurationForm(settingsPath,new MarketDataAdapterCatalog([new IbkrAdapterFactory(logs)]));
            if(dialog.ShowDialog(window)==DialogResult.OK)Report("Configuration saved. Start studies to apply it.");
        }
        catch(Exception ex){MessageBox.Show(window,ex.Message,"Configuration error");}
    }

    private Task StartAsync()
    {
        if (worker is { IsCompleted:false }) return Task.CompletedTask;
        try
        {
            var settings=JsonSerializer.Deserialize<MarketLiveSettings>(File.ReadAllText(settingsPath),new JsonSerializerOptions
                { PropertyNameCaseInsensitive=true, ReadCommentHandling=JsonCommentHandling.Skip, UnmappedMemberHandling=System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow })
                ?? throw new InvalidDataException("Empty configuration.");
            settings.PluginPath=Path.GetFullPath(settings.PluginPath,Path.GetDirectoryName(settingsPath)!);
            var engine=new MarketLiveEngine(settings,logs);
            engine.Status+=Report;
            engine.AttentionChanged+=(id,warning)=>SetAttention(id,warning);
            var session=new MarketDataAdapterCatalog([new IbkrAdapterFactory(logs)]).Create(settings.Adapter);
            cancellation?.Dispose(); cancellation=new(); var token=cancellation.Token;
            liveAdapter=session;monitoring=true;connectedOnce=false;attention.Clear();
            start.Enabled=false; pause.Enabled=true;UpdateTray();Report("Connecting market-data adapter…");
            worker=Task.Run(()=>RunMonitoringAsync(session,engine,settings,token));
        }
        catch(Exception ex) { SetAttention(-1,true);Report(ex.Message); logs.CreateLogger("Host").LogError(ex,"Start failed"); }
        return Task.CompletedTask;
    }

    private async Task RunMonitoringAsync(IMarketDataAdapter session,MarketLiveEngine engine,MarketLiveSettings settings,CancellationToken token)
    {
        try
        {
            await SettingsFile.MigrateProvidersAsync(settingsPath,settings,settings.ConnectionString,token);
            await SettingsFile.MigrateMonitorsAsync(settingsPath,settings,settings.ConnectionString,token);
            await session.ConnectAsync(token);
            OnUi(()=>{connectedOnce=true;UpdateTray();});
            Report("Adapter connected. Study monitoring started; automatic SQL price acquisition remains pending.");
            await engine.RunAsync(token);
        }
        catch(OperationCanceledException) when(token.IsCancellationRequested) { }
        catch(Exception ex)
        {SetAttention(-1,true);Report(ex.Message);logs.CreateLogger("Host").LogError(ex,"Monitoring failed");}
        finally
        {
            try{await session.DisposeAsync();}
            catch(Exception ex){SetAttention(-1,true);Report($"Adapter shutdown: {ex.Message}");}
            OnUi(()=>{if(ReferenceEquals(liveAdapter,session))liveAdapter=null;monitoring=false;start.Enabled=true;pause.Enabled=false;UpdateTray();});
        }
    }

    private void OnUi(Action action)
    {
        if(window.IsDisposed)return;
        if(window.InvokeRequired){window.BeginInvoke(action);return;}
        action();
    }
    private void SetAttention(int id,bool warning)=>OnUi(()=>{if(warning)attention.Add(id);else attention.Remove(id);UpdateTray();});
    private void UpdateTray()
    {
        bool connected=liveAdapter?.IsConnected==true;
        if(monitoring&&connectedOnce&&!connected)attention.Add(-1);
        var state=BrandingAssets.SelectState(monitoring,connected,attention.Count>0);
        tray.Icon=branding.TrayIcon(state);
        tray.Text=state switch{MonitorIconState.Warning=>"FIGMarketLive — attention needed",MonitorIconState.Live=>"FIGMarketLive — monitoring / adapter connected",
            _=>monitoring?"FIGMarketLive — connecting":"FIGMarketLive — idle / paused"};
    }

    private async Task StopAsync()
    {
        monitoring=false;
        if (cancellation is null){attention.Clear();UpdateTray();return;}
        cancellation.Cancel();
        try { if(worker is not null) await worker; }
        catch(OperationCanceledException) { }
        catch(Exception ex) { Report(ex.Message); }
        attention.Clear();start.Enabled=true; pause.Enabled=false;UpdateTray();Report("Paused.");
    }

    private void Report(string message)
    {
        if (window.IsDisposed) return;
        if (window.InvokeRequired) { window.BeginInvoke(()=>Report(message)); return; }
        if (status.TextLength > 100_000) status.Clear();
        status.AppendText($"{DateTimeOffset.Now:T}  {message}{Environment.NewLine}");
    }

    protected override void Dispose(bool disposing)
    {
        if(disposing&&!resourcesDisposed)
        {resourcesDisposed=true;healthTimer.Stop();healthTimer.Dispose();tray.Visible=false;tray.ContextMenuStrip?.Dispose();tray.Dispose();window.Dispose();cancellation?.Dispose();logs.Dispose();branding.Dispose();}
        base.Dispose(disposing);
    }
}

internal sealed class FileLoggerProvider(string path) : ILoggerProvider
{
    private readonly object gate=new();
    private readonly string filePath=path;
    public ILogger CreateLogger(string categoryName)=>new FileLogger(this,categoryName);
    public void Dispose() { }
    private sealed class FileLogger(FileLoggerProvider owner,string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState:notnull=>null;
        public bool IsEnabled(LogLevel level)=>level>=LogLevel.Information;
        public void Log<TState>(LogLevel level,EventId id,TState state,Exception? exception,Func<TState,Exception?,string> formatter)
        {
            if(!IsEnabled(level)) return;
            lock(owner.gate)
                try { File.AppendAllText(owner.filePath,$"{DateTimeOffset.Now:O} {level} {category}: {formatter(state,exception)} {exception}{Environment.NewLine}"); }
                catch(IOException) { } // A log failure must not terminate the monitor.
        }
    }
}
