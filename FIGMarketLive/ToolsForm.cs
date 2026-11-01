namespace FIGMarketLive;

internal sealed class ToolsForm : Form
{
    private readonly BrandingAssets branding=new();
    private readonly ImageList tabImages=new(){ImageSize=new(20,20),ColorDepth=ColorDepth.Depth32Bit};
    private readonly TabControl tabs=new(){Dock=DockStyle.Fill};
    private readonly CancellationTokenSource lifetime=new();
    private readonly Label status=new(){AutoSize=true,Dock=DockStyle.Bottom};
    private readonly Button cancelOperation=new(){Text="Cancel operation",AutoSize=true,Enabled=false};
    private CancellationTokenSource? operation;
    private bool busy,closeRequested;
    private bool resourcesDisposed;

    public ToolsForm(string connectionString,Func<CancellationToken,Task<List<StudyTicker>>>? loadTickers=null,Func<string,Task>? beforeImport=null,MarketLiveSettings? studySettings=null,Microsoft.Extensions.Logging.ILoggerFactory? logs=null)
    {
        Text="FIGMarketLive Tools";Icon=branding.ApplicationIcon;Width=980;Height=740;MinimumSize=new(840,620);
        StartPosition=FormStartPosition.CenterParent;
        tabImages.Images.Add("import",branding.Image("import.png"));tabs.ImageList=tabImages;
        var importTab=new TabPage("Import e-Signal Prices"){Padding=new(12)};
        var importer=new ImportPricesPanel(()=>connectionString,RunAsync,loadTickers);importTab.Controls.Add(importer);tabs.TabPages.Add(importTab);importTab.ImageKey="import";
        importer.BeforeImportAsync=beforeImport;
        tabImages.Images.Add("report",branding.Image("chart.png"));
        var reportTab=new TabPage("DataSet Report"){Padding=new(12)};
        var report=new DataSetReportPanel(()=>connectionString,RunAsync,branding);reportTab.Controls.Add(report);tabs.TabPages.Add(reportTab);
        // Assign after parenting so WinForms resolves the key against the tab control's ImageList.
        reportTab.ImageKey="report";
        tabImages.Images.Add("regenerate",branding.Image("regenerate.png"));
        var regenerateTab=new TabPage("Regenerate Studies"){Padding=new(12)};
        var regenerate=new RegenerateStudiesPanel(studySettings??new(){ConnectionString=connectionString,PluginPath=Path.Combine(AppContext.BaseDirectory,"Plugins")},logs??Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance,branding,RunAsync,beforeImport);
        regenerateTab.Controls.Add(regenerate);tabs.TabPages.Add(regenerateTab);regenerateTab.ImageKey="regenerate";
        tabs.SelectedIndexChanged+=async(_,_)=>{if(tabs.SelectedTab==reportTab)await RunAsync(report.EnsureLoadedAsync);};
        tabs.SelectedIndexChanged+=async(_,_)=>{if(tabs.SelectedTab==regenerateTab)await RunAsync(regenerate.EnsureLoadedAsync);};
        var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=48,FlowDirection=FlowDirection.RightToLeft,Padding=new(6)};
        var close=new Button{Text="Close",AutoSize=true};close.Click+=(_,_)=>Close();bottom.Controls.Add(close);bottom.Controls.Add(cancelOperation);cancelOperation.Click+=(_,_)=>operation?.Cancel();
        Controls.Add(tabs);Controls.Add(status);Controls.Add(bottom);
        Shown+=async(_,_)=>await RunAsync(importer.RefreshTickersAsync);
        FormClosing+=(_,e)=>{if(busy){closeRequested=true;lifetime.Cancel();e.Cancel=true;}};
    }

    private async Task RunAsync(Func<CancellationToken,Task> action)
    {
        if(busy||lifetime.IsCancellationRequested)return;
        busy=true;tabs.Enabled=false;UseWaitCursor=true;status.Text="";operation=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);cancelOperation.Enabled=true;
        try{await action(operation.Token);}
        catch(OperationCanceledException){status.Text="Operation cancelled.";}
        catch(Exception ex)
        {status.Text=ex.Message;if(!closeRequested)MessageBox.Show(this,ex.Message,"FIGMarketLive Tools",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        finally{operation.Dispose();operation=null;cancelOperation.Enabled=false;busy=false;tabs.Enabled=true;UseWaitCursor=false;if(closeRequested)Close();}
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if(disposing&&!resourcesDisposed){resourcesDisposed=true;lifetime.Dispose();tabImages.Dispose();branding.Dispose();}
    }
}
