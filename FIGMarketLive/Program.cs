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
        if(args.FirstOrDefault()=="--reset-study-storage")
        {
            try{StudyHistoryBinaryMigration.ResetAsync(SettingsFile.Load(Path.GetFullPath(args[1])).StudyConnectionString(),default).GetAwaiter().GetResult();Console.WriteLine("StudyHistory truncated; regeneration progress cleared; Studies is varbinary(max); BarStudy removed. PriceData and study definitions retained.");}
            catch(Exception ex){Console.Error.WriteLine(ex.Message);Environment.ExitCode=1;}
            return;
        }
        if(args.FirstOrDefault()=="--setup-price-events")
        {
            try
            {
                var setup=new PriceEventSetup(SettingsFile.Load(Path.GetFullPath(args[1])).ConnectionString);
                setup.EnsureSchemaAsync(default).GetAwaiter().GetResult();
                var check=setup.CheckNotificationsAsync(default).GetAwaiter().GetResult();Console.WriteLine(check.Message);
                if(!check.Ready)Environment.ExitCode=1;
            }
            catch(Exception ex){Console.Error.WriteLine(ex.Message);Environment.ExitCode=1;}
            return;
        }
        if(args.FirstOrDefault() is "--migrate-study-checkpoints" or "--migrate-study-storage")
        {
            try{new MarketLiveCheckpointStore(SettingsFile.Load(Path.GetFullPath(args[1])).StudyConnectionString()).EnsureSchemaAsync(default).GetAwaiter().GetResult();Console.WriteLine("StudyHistory binary outputs and checkpoint schema ready. Legacy BarStudy was consolidated and removed if present.");}
            catch(Exception ex){Console.Error.WriteLine(ex.Message);Environment.ExitCode=1;}
            return;
        }
        if(args.FirstOrDefault()=="--migrate-price-schema")
        {
            try
            {
                var settings=SettingsFile.Load(Path.GetFullPath(args[1]));
                PriceSchemaMigration.MigrateAsync(settings.ConnectionString,default).GetAwaiter().GetResult();
                Console.WriteLine(PriceSchemaMigration.VerifyAsync(settings.ConnectionString,default).GetAwaiter().GetResult());
            }
            catch(Exception ex){Console.Error.WriteLine(ex.Message);Environment.ExitCode=1;}
            return;
        }
        if(args.FirstOrDefault()=="--audit-price-schema")
        {
            try{PriceSchemaMigration.AuditAsync(SettingsFile.Load(Path.GetFullPath(args[1])).ConnectionString,Path.GetFullPath(args[2]),default).GetAwaiter().GetResult();}
            catch(Exception ex){Console.Error.WriteLine(ex.Message);Environment.ExitCode=1;}
            return;
        }
        if(args.FirstOrDefault() is "--migrate-provider-settings" or "--migrate-sql-monitors")
        {
            try
            {
                string file=Path.GetFullPath(args.ElementAtOrDefault(1)??Path.Combine(AppContext.BaseDirectory,"marketlive.json"));
                var settings=SettingsFile.Load(file);
                new DataSetMonitoringStore(settings.ConnectionString).EnsureSchemaAsync(default).GetAwaiter().GetResult();
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
    private readonly Button priceSync = new() { Text="Start Price Sync", AutoSize=true };
    private readonly Label priceSyncState=new(){Text="Price Sync stopped.",Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(8),MaximumSize=new Size(740,0)};
    private CancellationTokenSource? priceCancellation;
    private Task? priceWorker;
    private bool priceRunning;
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
        var tools=new Button { Text="Tools",AutoSize=true };
        window.Icon=branding.ApplicationIcon;
        branding.Apply(settings,"settings.png");branding.Apply(start,"play.png");branding.Apply(pause,"pause.png");
        branding.Apply(tools,"tools.png");tools.Click+=async(_,_)=>await OpenToolsAsync();
        settings.Click+=async (_,_)=>await OpenSettingsAsync(); start.Click+=async (_,_)=>await StartAsync(); pause.Click+=async (_,_)=>await StopAsync();
        branding.Apply(priceSync,"stock-sync.png");priceSync.Image=branding.PriceSyncImage(false);
        priceSync.Click+=async (_,_)=>{if(priceCancellation is not null)await StopPriceSyncAsync();else await StartPriceSyncAsync();};
        buttons.Controls.AddRange([priceSync,start,pause,settings,tools]);
        window.Controls.Add(status);window.Controls.Add(priceSyncState); window.Controls.Add(buttons);
        window.FormClosing+=(_,e)=> { if (!exiting) { e.Cancel=true; window.Hide(); } };
        var menu=new ContextMenuStrip();
        menu.Items.Add("Open",null,(_,_)=>Show());
        menu.Items.Add("Start studies",branding.Image("play.png",16),async (_,_)=>await StartAsync());
        menu.Items.Add("Pause",branding.Image("pause.png",16),async (_,_)=>await StopAsync());
        menu.Items.Add("Configuration",branding.Image("settings.png",16),async (_,_)=>await OpenSettingsAsync());
        menu.Items.Add("Tools",branding.Image("tools.png",16),async(_,_)=>await OpenToolsAsync());
        menu.Items.Add("Exit",null,async (_,_)=> { exiting=true; await StopPriceSyncAsync();await StopAsync(); ExitThread(); });
        tray=new NotifyIcon { Text="FIGMarketLive — idle", Icon=branding.TrayIcon(MonitorIconState.Off), Visible=true, ContextMenuStrip=menu };
        healthTimer.Tick+=(_,_)=>UpdateTray();healthTimer.Start();
        tray.DoubleClick+=(_,_)=>Show();
        _=window.Handle;
        Report("Start Price Sync captures configured price datasets. Start studies calculates enabled SQL Live Studies independently.");
        Report($"Configuration: {settingsPath}");
        Show();
    }

    private void Show() { window.Show(); window.WindowState=FormWindowState.Normal; window.Activate(); }

    private async Task OpenToolsAsync()
    {
        try
        {
            var toolSettings=SettingsFile.Load(settingsPath);toolSettings.PluginPath=Path.GetFullPath(toolSettings.PluginPath,Path.GetDirectoryName(settingsPath)!);
            using var dialog=new ToolsForm(toolSettings.ConnectionString,beforeImport:StopForDataChangeAsync,studySettings:toolSettings,logs:logs);
            dialog.ShowDialog(window);
        }
        catch(Exception ex){MessageBox.Show(window,ex.Message,"Tools error");}
    }

    private async Task OpenSettingsAsync()
    {
        try
        {
            var previous=SettingsFile.Load(settingsPath);
            using var dialog=new ConfigurationForm(settingsPath,new MarketDataAdapterCatalog([new IbkrAdapterFactory(logs)]));
            dialog.BeforeDataSetChangeAsync=StopForDataChangeAsync;
            dialog.RunningAdapter=draft=>
            {
                if(liveAdapter?.IsConnected!=true)return null;
                if(draft.ProviderId!=previous.Adapter.ProviderId||draft.Parameters.Count!=previous.Adapter.Parameters.Count||draft.Parameters.Any(p=>!previous.Adapter.Parameters.TryGetValue(p.Key,out var value)||value!=p.Value))
                    throw new InvalidOperationException("Price Sync is using the active adapter. Stop Price Sync before testing different adapter settings.");
                return liveAdapter;
            };
            dialog.ShowDialog(window);
            if(dialog.ConfigurationSaved)
            {
                if(JsonSerializer.Serialize(previous)!=JsonSerializer.Serialize(SettingsFile.Load(settingsPath)))
                    await StopForDataChangeAsync("applying saved database or adapter settings");
                Report("Configuration saved. Start Price Sync / studies to apply it.");
            }
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
            cancellation?.Dispose(); cancellation=new(); var token=cancellation.Token;
            monitoring=true;
            start.Enabled=false; pause.Enabled=true;UpdateTray();Report($"Starting SQL Live Studies… Batch size: {settings.StudyBatchSize:N0} bars; checkpoint interval: {settings.CheckpointEveryBars:N0} bars.");
            worker=Task.Run(()=>RunMonitoringAsync(engine,settings,token));
        }
        catch(Exception ex) { SetAttention(-2,true);Report(ex.Message); logs.CreateLogger("Host").LogError(ex,"Start failed"); }
        return Task.CompletedTask;
    }

    private async Task RunMonitoringAsync(MarketLiveEngine engine,MarketLiveSettings settings,CancellationToken token)
    {
        try
        {
            await SettingsFile.MigrateProvidersAsync(settingsPath,settings,settings.ConnectionString,token);
            await new DataSetMonitoringStore(settings.ConnectionString).EnsureSchemaAsync(token);
            await SettingsFile.MigrateMonitorsAsync(settingsPath,settings,settings.ConnectionString,token);
            Report("SQL Live Studies started.");
            await engine.RunAsync(token);
        }
        catch(OperationCanceledException) when(token.IsCancellationRequested) { }
        catch(Exception ex)
        {SetAttention(-2,true);Report(ex.Message);logs.CreateLogger("Host").LogError(ex,"Monitoring failed");}
        finally
        {
            OnUi(()=>{monitoring=false;start.Enabled=true;pause.Enabled=false;UpdateTray();});
        }
    }

    private Task StartPriceSyncAsync()
    {
        if(priceWorker is {IsCompleted:false})return Task.CompletedTask;
        try
        {
            var settings=SettingsFile.Load(settingsPath);settings.Validate();
            var adapter=new MarketDataAdapterCatalog([new IbkrAdapterFactory(logs)]).Create(settings.Adapter);
            priceCancellation?.Dispose();priceCancellation=new();var token=priceCancellation.Token;
            liveAdapter=adapter;connectedOnce=false;priceSync.Enabled=false;
            Report("Connecting adapter for price sync…");
            priceSyncState.Text="Connecting adapter for price sync…";
            priceWorker=Task.Run(async ()=>
            {
                try
                {
                    await SettingsFile.MigrateProvidersAsync(settingsPath,settings,settings.ConnectionString,token);
                    await new DataSetMonitoringStore(settings.ConnectionString).EnsureSchemaAsync(token);
                    await adapter.ConnectAsync(token);
                    OnUi(()=>{priceRunning=true;connectedOnce=true;priceSync.Enabled=true;UpdatePriceButton();UpdateTray();});
                    var sync=new PriceSyncEngine(settings,adapter);sync.Status+=Report;
                    sync.StateChanged+=message=>OnUi(()=>priceSyncState.Text=message);
                    sync.AttentionChanged+=(id,warning)=>SetAttention(-10000-id,warning);
                    await sync.RunAsync(token);
                }
                catch(OperationCanceledException) when(token.IsCancellationRequested){}
                catch(Exception ex){SetAttention(-1,true);Report($"Price sync: {ex.Message}");OnUi(()=>priceSyncState.Text=$"Price Sync stopped: {ex.Message}");logs.CreateLogger("PriceSync").LogError(ex,"Price sync failed");}
                finally
                {
                    try{await adapter.DisposeAsync();}catch(Exception ex){Report($"Price adapter shutdown: {ex.Message}");}
                    OnUi(()=>{liveAdapter=null;priceRunning=false;priceCancellation?.Dispose();priceCancellation=null;priceSync.Enabled=true;UpdatePriceButton();UpdateTray();});
                }
            });
        }
        catch(Exception ex){Report($"Price sync: {ex.Message}");SetAttention(-1,true);}
        return Task.CompletedTask;
    }

    private async Task StopForDataChangeAsync(string reason)
    {await StopPriceSyncAsync(reason);await StopAsync();}

    private async Task StopPriceSyncAsync(string reason="requested by user or application exit")
    {
        if(priceCancellation is null)return;
        priceSync.Enabled=false;priceCancellation.Cancel();
        if(priceWorker is not null)await priceWorker;
        priceRunning=false;attention.RemoveWhere(id=>id<=-10000||id==-1);
        UpdatePriceButton();UpdateTray();Report($"Price sync stopped: {reason}.");
        priceSyncState.Text="Price Sync stopped.";
    }

    private void UpdatePriceButton()
    {
        bool running=priceRunning&&liveAdapter?.IsConnected==true;
        priceSync.Text=priceRunning?"Stop Price Sync":"Start Price Sync";
        priceSync.Image=branding.PriceSyncImage(running);
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
        if(priceRunning&&connectedOnce&&!connected)attention.Add(-1);
        UpdatePriceButton();
        var state=BrandingAssets.SelectState(priceRunning,connected,attention.Count>0);
        tray.Icon=branding.TrayIcon(state);
        tray.Text=state switch{MonitorIconState.Warning=>"FIGMarketLive — attention needed",MonitorIconState.Live=>"FIGMarketLive — monitoring / adapter connected",
            _=>monitoring?"FIGMarketLive — connecting":"FIGMarketLive — idle / paused"};
    }

    private async Task StopAsync()
    {
        monitoring=false;
        if (cancellation is null){attention.RemoveWhere(id=>id>=0||id is -2 or -3);UpdateTray();return;}
        cancellation.Cancel();
        try { if(worker is not null) await worker; }
        catch(OperationCanceledException) { }
        catch(Exception ex) { Report(ex.Message); }
        attention.RemoveWhere(id=>id>=0||id is -2 or -3);start.Enabled=true; pause.Enabled=false;UpdateTray();Report("Live Studies paused.");
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
        {resourcesDisposed=true;healthTimer.Stop();healthTimer.Dispose();tray.Visible=false;tray.ContextMenuStrip?.Dispose();tray.Dispose();window.Dispose();cancellation?.Dispose();priceCancellation?.Dispose();logs.Dispose();branding.Dispose();}
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
