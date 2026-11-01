using System.Text.Json;
using FIGCommon.Models;
using FIGCommon.Models.FIGProviderAPI;
using FIGMarketLive.MarketData;

namespace FIGMarketLive;

internal sealed class ConfigurationForm : Form
{
    private readonly string path;
    private readonly BrandingAssets branding=new();
    private readonly ImageList tabImages=new(){ImageSize=new(20,20),ColorDepth=ColorDepth.Depth32Bit};
    private readonly MarketLiveSettings settings;
    private readonly TextBox connection=new(){Dock=DockStyle.Fill,Multiline=true,Height=80,UseSystemPasswordChar=false};
    private readonly NumericUpDown historyMinutes=new(){Minimum=1,Maximum=43200,Width=120,ThousandsSeparator=true};
    private readonly NumericUpDown backupPollSeconds=new(){Name="BackupPollSeconds",Minimum=1,Maximum=86400,Width=120,ThousandsSeparator=true};
    private readonly NumericUpDown studySqlTimeout=new(){Name="StudySqlTimeoutSeconds",Minimum=1,Maximum=86400,Width=120,ThousandsSeparator=true};
    private readonly NumericUpDown checkpointEveryBars=new(){Name="CheckpointEveryBars",Minimum=1,Maximum=int.MaxValue,Width=120,ThousandsSeparator=true};
    private readonly NumericUpDown studyBatchSize=new(){Name="StudyBatchSize",Minimum=1,Maximum=100000,Width=120,ThousandsSeparator=true};
    private readonly TextBox databaseStatus=new(){Name="DatabaseTestResults",MinimumSize=new(0,100),Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical};
    private readonly PictureBox databaseResult=new(){Width=24,Height=24,SizeMode=PictureBoxSizeMode.CenterImage,Margin=new(6,5,3,3),AccessibleName="Database test result"};
    private readonly PictureBox adapterResult=new(){Width=24,Height=24,SizeMode=PictureBoxSizeMode.CenterImage,Margin=new(6,5,3,3),AccessibleName="Adapter connection result"};
    private readonly MarketDataAdapterCatalog adapters;
    private readonly ComboBox provider=new(){Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList};
    private readonly TableLayoutPanel adapterFields=CreateLayout();
    private readonly Label adapterHelp=new(){AutoSize=true,MaximumSize=new(820,0)};
    private readonly Dictionary<string,TextBox> parameters=[];
    private IMarketDataAdapter? adapter;
    private readonly Label brokerStatus=new(){AutoSize=true,MaximumSize=new(740,0)};
    private readonly TextBox symbol=new(){Text="NQ",Width=75};
    private readonly ComboBox type=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=65};
    private readonly ComboBox exchange=new(){Width=95,DropDownStyle=ComboBoxStyle.DropDown,AutoCompleteMode=AutoCompleteMode.SuggestAppend,AutoCompleteSource=AutoCompleteSource.ListItems,AccessibleName="Contract exchange"};
    private readonly TextBox currency=new(){Text="USD",Width=50};
    private readonly TextBox expiry=new(){Width=80,AccessibleName="Contract expiry"};
    private readonly TextBox alias=new(){Width=140};
    private readonly ComboBox interval=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=115};
    private readonly DataGridView contracts=new(){Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=false,AllowUserToAddRows=false,AllowUserToDeleteRows=false,MultiSelect=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect};
    private readonly DataGridView selections=new(){Dock=DockStyle.Fill,Height=150,ReadOnly=true,AutoGenerateColumns=false,AllowUserToAddRows=false,AllowUserToDeleteRows=false,MultiSelect=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect};
    private List<ConfiguredDataSet>? loadedDataSets;
    private readonly Label selectionStatus=new(){AutoSize=true,MaximumSize=new(760,0)};
    private readonly TabControl tabs=new(){Dock=DockStyle.Fill};
    private readonly Button save=new(){Text="Save configuration",AutoSize=true};
    private readonly List<Button> tickerActions=[];
    private readonly ToolStrip dataSetToolbar=new(){Dock=DockStyle.Fill,GripStyle=ToolStripGripStyle.Hidden,ImageScalingSize=new(20,20),AccessibleName="Configured datasets toolbar"};
    private readonly ToolStripButton deleteDataSet=new(){Text="Delete",ToolTipText="Delete Selected DataSet",DisplayStyle=ToolStripItemDisplayStyle.Image,Enabled=false};
    private readonly ToolStripButton editContract=new(){Text="Edit contract / expiry",ToolTipText="Edit selected ticker contract/expiry",DisplayStyle=ToolStripItemDisplayStyle.Image,Enabled=false};
    private readonly ToolStripButton syncDataSet=new(){Text="Price sync",ToolTipText="Enable/disable price sync for selected dataset",DisplayStyle=ToolStripItemDisplayStyle.Image,CheckOnClick=false,Enabled=false};
    private readonly TableLayoutPanel tickerSearchPanel=CreateLayout();
    private RowStyle? searchPanelRow;
    private readonly CancellationTokenSource lifetime=new();
    private string? checkedDatabase;
    private string? checkedBroker;
    private bool busy;
    private bool closeRequested;
    private bool resourcesDisposed;
    private string savedDatabase="",savedAdapter="";
    private string savedPluginPath="";
    private PluginCollectionsPanel? pluginEditor;
    private readonly Func<string,bool>? confirmLocalSave;
    private readonly Func<string,bool>? confirmDataSetDiscard;
    private bool addingDataSet;
    private string newDataSetBaseline="";
    private bool updatingExchangeOptions;
    public bool ConfigurationSaved {get;private set;}
    internal Func<string,Task>? BeforeDataSetChangeAsync;
    internal Func<MarketDataAdapterSettings,IMarketDataAdapter?>? RunningAdapter;
    private bool borrowedAdapter;

    public ConfigurationForm(string path,MarketDataAdapterCatalog adapters,bool autoLoadStudyEditors=true,Func<string,bool>? confirmLocalSave=null,Func<string,bool>? confirmStudyDiscard=null,Func<string,bool>? confirmDataSetDiscard=null)
    {
        this.path=path;this.adapters=adapters;
        this.confirmLocalSave=confirmLocalSave;
        this.confirmDataSetDiscard=confirmDataSetDiscard;
        settings=SettingsFile.Load(path);
        Text="FIGMarketLive configuration";Width=980;Height=740;MinimumSize=new(840,620);
        Icon=branding.ApplicationIcon;
        tabImages.Images.Add("database",branding.Image("database.png"));tabImages.Images.Add("chart",branding.Image("chart.png"));tabImages.Images.Add("adapter",branding.Image("plug.png"));tabImages.Images.Add("studies",branding.Image("bar-chart.png"));
        tabImages.Images.Add("collectionTypes",branding.Image("coltype.png"));
        tabs.ImageList=tabImages;
        StartPosition=FormStartPosition.CenterParent;
        connection.Text=settings.ConnectionString;
        historyMinutes.Value=Math.Clamp(settings.PriceHistoryMinutes,1,43200);
        backupPollSeconds.Value=Math.Clamp(settings.PollSeconds,1,86400);
        studySqlTimeout.Value=Math.Clamp(settings.StudySqlTimeoutSeconds,1,86400);studyBatchSize.Value=Math.Clamp(settings.StudyBatchSize,1,100000);
        checkpointEveryBars.Value=Math.Max(1,settings.CheckpointEveryBars);
        type.Items.AddRange(["FUT","STK"]);type.SelectedIndex=0;
        var database=Page("Database");
        database.ImageKey="database";
        var dbLayout=CreateLayout();database.Controls.Add(dbLayout);
        Add(dbLayout,new Label { Text="FIGMarketLive SQL Server connection string",AutoSize=true });Add(dbLayout,connection);
        Add(dbLayout,new Label { Text="Use Database / Initial Catalog to name the database. Connection strings are saved locally; prefer integrated authentication.",AutoSize=true });
        var databaseTestRow=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};
        databaseTestRow.Controls.Add(Button("Test database and schema",()=>RunAsync(TestDatabaseAsync)));
        databaseTestRow.Controls.Add(Button("Configure SQL notifications…",()=>RunAsync(ConfigureNotificationsAsync)));
        databaseTestRow.Controls.Add(databaseResult);
        Add(dbLayout,databaseTestRow);Add(dbLayout,databaseStatus,true);
        var performance=new GroupBox{Text="Performance",AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Dock=DockStyle.Fill,Padding=new(8,20,8,8)};
        var performanceLayout=new TableLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Dock=DockStyle.Top,ColumnCount=1};performance.Controls.Add(performanceLayout);
        var historyRow=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};
        historyRow.Controls.Add(new Label{Text="Maximum price catch-up (minutes; 7,200 = 5 days)",AutoSize=true,Margin=new(3,6,3,3)});historyRow.Controls.Add(historyMinutes);Add(performanceLayout,historyRow);
        var backupRow=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};
        backupRow.Controls.Add(new Label{Text="Backup study reconciliation poll (seconds; events trigger normal updates)",AutoSize=true,Margin=new(3,6,3,3)});backupRow.Controls.Add(backupPollSeconds);Add(performanceLayout,backupRow);
        var studyLimits=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};
        studyLimits.Controls.Add(new Label{Text="Study SQL timeout (seconds)",AutoSize=true,Margin=new(3,6,3,3)});studyLimits.Controls.Add(studySqlTimeout);
        studyLimits.Controls.Add(new Label{Text="Study batch size (records)",AutoSize=true,Margin=new(12,6,3,3)});studyLimits.Controls.Add(studyBatchSize);Add(performanceLayout,studyLimits);
        var checkpointRow=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};
        checkpointRow.Controls.Add(new Label{Text="Checkpoint interval (bars; batch boundaries also save recovery state)",AutoSize=true,Margin=new(3,6,3,3)});checkpointRow.Controls.Add(checkpointEveryBars);Add(performanceLayout,checkpointRow);
        Add(dbLayout,performance);
        var broker=Page("Market Data Adapter");var brokerLayout=CreateLayout();broker.Controls.Add(brokerLayout);
        broker.ImageKey="adapter";
        provider.BindingContext=new BindingContext();
        provider.DisplayMember=nameof(AdapterDescriptor.DisplayName);provider.ValueMember=nameof(AdapterDescriptor.Id);provider.DataSource=adapters.Descriptors.ToList();
        provider.SelectedValue=settings.Adapter.ProviderId;
        Field(brokerLayout,"Market-data provider",provider);
        adapterFields.Dock=DockStyle.Top;adapterFields.AutoSize=true;Add(brokerLayout,adapterFields);
        Add(brokerLayout,adapterHelp);
        var adapterButtons=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};
        adapterButtons.Controls.Add(Button("Connect / test",()=>RunAsync(TestBrokerAsync)));
        adapterButtons.Controls.Add(adapterResult);
        adapterButtons.Controls.Add(Button("Disconnect",()=>RunAsync(DisconnectAdapterAsync)));Add(brokerLayout,adapterButtons);Add(brokerLayout,brokerStatus);
        RenderAdapterFields();
        var ticker=Page("Ticker and DataSet");var tickerLayout=CreateLayout();ticker.Controls.Add(tickerLayout);
        ticker.ImageKey="chart";
        ToolStripButton DatasetAction(string text,string tooltip,string file,Func<CancellationToken,Task> action)
        {var item=new ToolStripButton(text,branding.Image(file)){ToolTipText=tooltip,DisplayStyle=ToolStripItemDisplayStyle.Image};item.Click+=async(_,_)=>await RunAsync(action);return item;}
        dataSetToolbar.Items.Add(DatasetAction("Add","Add new DataSet","add.png",_=>{ShowNewDataSet();return Task.CompletedTask;}));
        deleteDataSet.Image=branding.Image("trash.png");deleteDataSet.Click+=async(_,_)=>await RunAsync(DeleteDataSetAsync);dataSetToolbar.Items.Add(deleteDataSet);
        editContract.Image=branding.Image("change_expiry.png");editContract.Click+=async(_,_)=>await RunAsync(EditTickerContractAsync);dataSetToolbar.Items.Add(editContract);
        syncDataSet.Image=branding.Image("tick.png");syncDataSet.Click+=async(_,_)=>await RunAsync(TogglePriceSyncAsync);dataSetToolbar.Items.Add(syncDataSet);
        dataSetToolbar.Items.Add(new ToolStripSeparator());dataSetToolbar.Items.Add(DatasetAction("Reload","Reload DataSets","reload.png",async ct=>
        {await EnsureDatabaseAsync(ct);await RefreshDataSetsAsync(ct);selectionStatus.Text=$"Reloaded {loadedDataSets?.Count??0} configured price dataset(s) from SQL.";}));
        BrandingAssets.StyleToolbar(dataSetToolbar);
        Add(tickerLayout,dataSetToolbar);
        foreach(var (name,title,width) in new[]{("DataSetId","DataSet",65),("Symbol","Symbol",105),("LocalSymbol","Contract",100),("Expiry","Expiry",85),("SecurityType","Type",55),
            ("IntervalName","Interval",95),("StudyUsage","StudyCols",85),("SyncStatus","Monitor",85)})
            selections.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=name,HeaderText=title,Width=width});
        Add(tickerLayout,selections,true);tickerLayout.RowStyles[tickerLayout.RowStyles.Count-1].Height=40;
        Add(tickerLayout,selectionStatus);
        tickerSearchPanel.AccessibleName="Add DataSet search panel";
        Add(tickerLayout,tickerSearchPanel,true);searchPanelRow=tickerLayout.RowStyles[tickerLayout.RowStyles.Count-1];SetSearchPanelVisible(false);
        Add(tickerSearchPanel,new Label{Text="New DataSet",AutoSize=true,Font=new Font(Font,FontStyle.Bold)});
        var query=new FlowLayoutPanel { AutoSize=true,Dock=DockStyle.Fill,WrapContents=true };
        foreach(var (label,control) in new (string,Control)[]{("Symbol",symbol),("Type",type),("Exchange",exchange),("Currency",currency),("Expiry",expiry)})
        { query.Controls.Add(new Label { Text=label,AutoSize=true,Margin=new(6,7,3,3) });query.Controls.Add(control); }
        Add(tickerSearchPanel,new Label { Text="Search checks the database and adapter. For futures, leave Expiry blank to list available contracts, or enter YYYYMM / YYYYMMDD.",AutoSize=true,MaximumSize=new(850,0) });
        Add(tickerSearchPanel,query);Add(tickerSearchPanel,TickerAction("Search contracts",()=>RunAsync(SearchAsync)));
        foreach(var name in new[]{"Symbol","LocalSymbol","SecurityType","Expiry","Exchange","Currency","Name","ProviderInstrumentId"})
            contracts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName=name,HeaderText=name,AutoSizeMode=DataGridViewAutoSizeColumnMode.AllCells });
        Add(tickerSearchPanel,contracts,true);
        var choose=new FlowLayoutPanel { AutoSize=true,Dock=DockStyle.Fill };
        choose.Controls.Add(new Label { Text="Local database symbol",AutoSize=true });choose.Controls.Add(alias);
        choose.Controls.Add(new Label { Text="Interval",AutoSize=true });choose.Controls.Add(interval);
        var saveTicker=TickerAction("Save Ticker",()=>RunAsync(SaveDataSetAsync));branding.Apply(saveTicker,"save.png");choose.Controls.Add(saveTicker);
        Add(tickerSearchPanel,choose);
        selections.SelectionChanged+=(_,_)=>UpdateReadiness();selections.CurrentCellChanged+=(_,_)=>UpdateReadiness();
        var studyTab=Page("Study Collections");studyTab.ImageKey="studies";studyTab.Padding=new(4);
        var studyPages=new TabControl{Dock=DockStyle.Fill,ImageList=tabImages,Name="StudyCollectionPages"};studyTab.Controls.Add(studyPages);
        var collectionsTab=new TabPage("Collections"){Padding=new(8)};
        var typeTab=new TabPage("Collection Types"){Padding=new(8)};
        studyPages.TabPages.AddRange([collectionsTab,typeTab]);collectionsTab.ImageKey="studies";typeTab.ImageKey="collectionTypes";
        var studyEditor=new StudyConfigurationPanel(false,()=>connection.Text,RunAsync,confirmStudyDiscard,()=>MarketLivePlugins.Discover(ResolvePluginRoot()));collectionsTab.Controls.Add(studyEditor);
        pluginEditor=new PluginCollectionsPanel(settings.PluginPath,ResolvePluginRoot,()=>connection.Text,_=>TrySaveConfiguration(),RunAsync);typeTab.Controls.Add(pluginEditor);
        async Task ReloadStudyPageAsync()
        {
            if(busy||!autoLoadStudyEditors)return;
            if(studyPages.SelectedTab==collectionsTab&&studyEditor.NeedsReload)await RunAsync(studyEditor.ReloadAsync);
            else if(studyPages.SelectedTab==typeTab&&pluginEditor.NeedsReload)await RunAsync(pluginEditor.ReloadAsync);
        }
        studyPages.SelectedIndexChanged+=async(_,_)=>await ReloadStudyPageAsync();
        tabImages.Images.Add("schedule",branding.Image("sync_sched.png"));
        var scheduleTab=Page("Exchange Schedule");scheduleTab.ImageKey="schedule";
        var scheduleEditor=new ExchangeSchedulePanel(()=>connection.Text,RunAsync,confirmStudyDiscard);scheduleTab.Controls.Add(scheduleEditor);
        tabs.SelectedIndexChanged+=async(_,_)=>
        {
            save.Visible=tabs.SelectedIndex is 0 or 1;
            if(busy||!autoLoadStudyEditors)return;
            if(tabs.SelectedTab==studyTab)await ReloadStudyPageAsync();
            else if(tabs.SelectedTab==scheduleTab&&scheduleEditor.NeedsReload)await RunAsync(scheduleEditor.ReloadAsync);
            else if(tabs.SelectedTab==scheduleTab)await RunAsync(scheduleEditor.RefreshExchangesAsync);
        };
        var bottom=new FlowLayoutPanel { Dock=DockStyle.Bottom,Height=48,FlowDirection=FlowDirection.RightToLeft,Padding=new(6) };
        var cancel=new Button {Text="Close",AutoSize=true};cancel.Click+=(_,_)=>Close();
        save.Click+=(_,_)=>{if(TrySaveConfiguration()){DialogResult=DialogResult.OK;Close();}};bottom.Controls.Add(cancel);bottom.Controls.Add(save);
        Controls.Add(tabs);Controls.Add(bottom);
        connection.TextChanged+=(_,_)=> {checkedDatabase=null;SetDatabaseResult(null);databaseStatus.Text="Connection changed. Test the database again.";interval.DataSource=null;loadedDataSets=null;selections.DataSource=null;UpdateReadiness();};
        provider.SelectedIndexChanged+=(_,_)=> {RenderAdapterFields();InvalidateAdapter();};
        foreach(var field in new Control[]{symbol,exchange,currency,expiry})field.TextChanged+=(_,_)=>contracts.DataSource=null;
        exchange.TextChanged+=(_,_)=>{if(!updatingExchangeOptions)UpdateExpiry();};
        type.SelectedIndexChanged+=(_,_)=>{contracts.DataSource=null;UpdateExchangeOptions();UpdateExpiry();};
        UpdateExchangeOptions();UpdateExpiry();
        contracts.SelectionChanged+=(_,_)=>
        {
            if(SelectedContract() is {} c) alias.Text=c.SecurityType=="FUT"?c.LocalSymbol:c.Symbol;
        };
        RefreshSelections();
        savedDatabase=DatabaseSignature();savedAdapter=BrokerSignature();
        savedPluginPath=settings.PluginPath.Trim();
        studyPages.Deselecting+=(_,e)=>
        {
            if(e.TabPage==collectionsTab&&!studyEditor.DiscardChangesForNavigation("switching study collection pages")){e.Cancel=true;return;}
            if(e.TabPage==typeTab&&pluginEditor.DraftPath()!=savedPluginPath&&(!ConfirmSaveLocal("switching study collection pages")||!TrySaveConfiguration()))e.Cancel=true;
        };
        tabs.Deselecting+=(_,e)=>
        {
            if(e.TabPage==ticker&&!DiscardNewDataSet("changing tabs")){e.Cancel=true;return;}
            if(e.TabPage==studyTab&&!studyEditor.DiscardChangesForNavigation("changing tabs")){e.Cancel=true;return;}
            if(e.TabPage==scheduleTab&&!scheduleEditor.DiscardChangesForNavigation("changing tabs")){e.Cancel=true;return;}
            bool dirty=e.TabPageIndex==0?DatabaseSignature()!=savedDatabase:e.TabPageIndex==1?BrokerSignature()!=savedAdapter:e.TabPage==studyTab&&pluginEditor.DraftPath()!=savedPluginPath;
            if(dirty&&(!ConfirmSaveLocal("switching tabs")||!TrySaveConfiguration()))e.Cancel=true;
        };
        if(autoLoadStudyEditors)Shown+=async(_,_)=>await RunAsync(TestDatabaseAsync);
        FormClosing+=(_,e)=>
        {
            if(!DiscardNewDataSet("closing the configuration dialog")){e.Cancel=true;return;}
            if(!studyEditor.DiscardChangesForNavigation("closing the configuration dialog")){e.Cancel=true;return;}
            if(!scheduleEditor.DiscardChangesForNavigation("closing the configuration dialog")){e.Cancel=true;return;}
            if((DatabaseSignature()!=savedDatabase||BrokerSignature()!=savedAdapter||pluginEditor.DraftPath()!=savedPluginPath)&&(!ConfirmSaveLocal("closing this dialog")||!TrySaveConfiguration())){e.Cancel=true;return;}
            if(busy){closeRequested=true;lifetime.Cancel();e.Cancel=true;}
            else if(adapter is not null){e.Cancel=true;closeRequested=true;_=RunAsync(DisconnectAdapterAsync);}
        };
    }

    private TabPage Page(string text){var p=new TabPage(text){Padding=new(12)};tabs.TabPages.Add(p);return p;}
    private void SetSearchPanelVisible(bool visible)
    {
        addingDataSet=visible;tickerSearchPanel.Visible=visible;
        if(searchPanelRow is not null){searchPanelRow.SizeType=visible?SizeType.Percent:SizeType.Absolute;searchPanelRow.Height=visible?60:0;}
    }
    private string NewDataSetDraft()=>JsonSerializer.Serialize(new{Symbol=symbol.Text,Type=type.Text,Exchange=exchange.Text,Currency=currency.Text,Expiry=expiry.Text,Alias=alias.Text,Interval=(interval.SelectedItem as IntervalRS)?.Id,Contract=SelectedContract()?.ProviderInstrumentId});
    private bool ConfirmNewDataSetDiscard(string reason)
    {
        if(!addingDataSet||NewDataSetDraft()==newDataSetBaseline)return true;
        if(confirmDataSetDiscard is not null)return confirmDataSetDiscard(reason);
        return MessageBox.Show(this,$"The entered new DataSet information has not been saved and will be lost when {reason}.\n\nDiscard it and continue?","Unsaved DataSet",MessageBoxButtons.OKCancel,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)==DialogResult.OK;
    }
    private bool DiscardNewDataSet(string reason)
    {if(!ConfirmNewDataSetDiscard(reason))return false;if(addingDataSet&&NewDataSetDraft()!=newDataSetBaseline)SetSearchPanelVisible(false);return true;}
    private void ShowNewDataSet()
    {
        if(!ConfirmNewDataSetDiscard("starting another DataSet"))return;
        contracts.DataSource=null;symbol.Clear();exchange.Text="";currency.Text="USD";expiry.Clear();alias.Clear();type.SelectedItem="FUT";interval.SelectedIndex=-1;
        selections.ClearSelection();selections.CurrentCell=null;SetSearchPanelVisible(true);newDataSetBaseline=NewDataSetDraft();
        selectionStatus.Text="New DataSet: search for a ticker contract, choose an interval, then Save Ticker.";symbol.Focus();UpdateReadiness();
    }
    internal void CompleteDataSetSave(DataSetChoice choice)
    {SetSearchPanelVisible(false);selectionStatus.Text=$"Ticker {choice.TickerId}, provider mapping and DataSet {choice.Id} saved in SQL.";}
    private void UpdateExpiry()
    {
        bool stocks=exchange.Text.Trim().Equals("NYSE",StringComparison.OrdinalIgnoreCase)||exchange.Text.Trim().Equals("NASDAQ",StringComparison.OrdinalIgnoreCase);
        if(stocks&&type.Text!="STK")type.SelectedItem="STK";
        expiry.Enabled=!stocks&&type.Text=="FUT";
        if(!expiry.Enabled)expiry.Clear();
    }
    private void UpdateExchangeOptions()
    {
        string[] futures=["CME","CBOT","NYMEX","COMEX","EUREX"],stocks=["SMART","NYSE","NASDAQ"];
        string[] choices=type.Text=="STK"?stocks:futures,other=type.Text=="STK"?futures:stocks;
        string current=exchange.Text;
        updatingExchangeOptions=true;
        try
        {
            exchange.Items.Clear();exchange.Items.AddRange(choices);
            exchange.Text=other.Contains(current,StringComparer.OrdinalIgnoreCase)?"":current;
        }
        finally{updatingExchangeOptions=false;}
    }
    private static TableLayoutPanel CreateLayout()=>new(){Dock=DockStyle.Fill,ColumnCount=1,AutoScroll=true};
    private static void Add(TableLayoutPanel p,Control c,bool grow=false)
    {int row=p.RowCount++;p.RowStyles.Add(new RowStyle(grow?SizeType.Percent:SizeType.AutoSize,grow?100:0));p.Controls.Add(c,0,row);c.Margin=new(3,5,3,5);}
    private static void Field(TableLayoutPanel p,string label,Control c){Add(p,new Label{Text=label,AutoSize=true});Add(p,c);}
    private static Button Button(string text,Func<Task> action)
    {var b=new Button{Text=text,AutoSize=true};b.Click+=async(_,_)=>await action();return b;}
    private Button TickerAction(string text,Func<Task> action)
    {var b=Button(text,action);tickerActions.Add(b);return b;}
    private ConfiguredDataSet? SelectedDataSet()=>selections.CurrentRow?.Selected==true?selections.CurrentRow.DataBoundItem as ConfiguredDataSet:null;
    private static bool CanEditExpiry(ConfiguredDataSet row)=>row.SecurityType.Equals("FUT",StringComparison.OrdinalIgnoreCase)&&!row.Exchange.Equals("NYSE",StringComparison.OrdinalIgnoreCase)&&!row.Exchange.Equals("NASDAQ",StringComparison.OrdinalIgnoreCase);
    private void UpdateReadiness()
    {
        foreach(var button in tickerActions)button.Enabled=!busy;
        var selected=SelectedDataSet();dataSetToolbar.Enabled=!busy;
        deleteDataSet.Enabled=!busy&&selected is not null;editContract.Enabled=!busy&&selected is not null&&CanEditExpiry(selected);
        syncDataSet.Enabled=!busy&&selected is not null;syncDataSet.Checked=selected?.Monitor==true;
    }
    private MarketDataAdapterSettings AdapterSettings()=>new(){ProviderId=(provider.SelectedItem as AdapterDescriptor)?.Id??"",
        Parameters=parameters.ToDictionary(p=>p.Key,p=>p.Value.Text.Trim())};
    private string BrokerSignature()=>JsonSerializer.Serialize(AdapterSettings());
    private MarketInstrument? SelectedContract()=>contracts.CurrentRow?.DataBoundItem as MarketInstrument;
    private void InvalidateAdapter(){checkedBroker=null;SetAdapterResult(null);brokerStatus.Text="Adapter settings changed. Connect / test again.";contracts.DataSource=null;UpdateReadiness();}
    private void RenderAdapterFields()
    {
        foreach(Control control in adapterFields.Controls.Cast<Control>().ToArray())control.Dispose();
        adapterFields.Controls.Clear();adapterFields.RowStyles.Clear();adapterFields.RowCount=0;parameters.Clear();
        if(provider.SelectedItem is not AdapterDescriptor descriptor)return;
        adapterHelp.Text=descriptor.HelpText;
        foreach(var field in descriptor.Parameters)
        {
            var text=new TextBox{Dock=DockStyle.Fill,Text=settings.Adapter.ProviderId.Equals(descriptor.Id,StringComparison.OrdinalIgnoreCase)
                && settings.Adapter.Parameters.TryGetValue(field.Key,out var value)?value:field.DefaultValue};
            text.TextChanged+=(_,_)=>InvalidateAdapter();parameters[field.Key]=text;Field(adapterFields,field.Label,text);
        }
    }

    private async Task RunAsync(Func<CancellationToken,Task> action)
    {
        if(busy || lifetime.IsCancellationRequested)return;
        busy=true;tabs.Enabled=false;save.Enabled=false;UseWaitCursor=true;
        try{await action(lifetime.Token);}
        catch(OperationCanceledException){selectionStatus.Text="Operation cancelled or timed out.";}
        catch(Exception ex){MessageBox.Show(this,ex.Message,"FIGMarketLive configuration",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        finally
        {
            if(closeRequested)await DisconnectAdapterAsync(CancellationToken.None);
            busy=false;tabs.Enabled=true;save.Enabled=true;UseWaitCursor=false;UpdateReadiness();if(closeRequested)Close();
        }
    }

    private async Task ConfigureNotificationsAsync(CancellationToken ct)
    {
        var setup=new NotificationAdministratorSetup(connection.Text);
        var target=await setup.TargetAsync(ct);
        using var dialog=new NotificationSetupForm(setup,target,BeforeDataSetChangeAsync);
        if(dialog.ShowDialog(this)==DialogResult.OK)await TestDatabaseAsync(ct);
    }

    private async Task TestDatabaseAsync(CancellationToken ct)
    {
        checkedDatabase=null;SetDatabaseResult(null);databaseStatus.Text="Testing database accessibility and required schema…";
        try
        {
            await new DataSetMonitoringStore(connection.Text).EnsureSchemaAsync(ct);
            var studyConnection=new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection.Text){CommandTimeout=(int)studySqlTimeout.Value}.ConnectionString;
            if(await StudyHistoryBinaryMigration.RequiredAsync(studyConnection,ct)&&BeforeDataSetChangeAsync is {} beforeMigration)await beforeMigration("consolidating binary StudyHistory storage");
            await new MarketLiveCheckpointStore(studyConnection).EnsureSchemaAsync(ct);
            await new StudyRegenerationStore(new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection.Text){CommandTimeout=(int)studySqlTimeout.Value}.ConnectionString).EnsureSchemaAsync(ct);
            var notifications=new PriceEventSetup(connection.Text);await notifications.EnsureSchemaAsync(ct);
            await MarketLivePlugins.SyncAsync(connection.Text,MarketLivePlugins.Discover(ResolvePluginRoot()),ct);
            await SettingsFile.MigrateProvidersAsync(path,settings,connection.Text,ct);
            await SettingsFile.MigrateMonitorsAsync(path,settings,connection.Text,ct);
            var service=new DatabaseSetupService(connection.Text);var result=await service.CheckAsync(ct);databaseStatus.Text=result.Message;
            if(!result.Ready){SetDatabaseResult(false);return;}
            var notificationCheck=await notifications.CheckNotificationsAsync(ct);
            databaseStatus.Text+=Environment.NewLine+notificationCheck.Message;
            if(!notificationCheck.Ready){databaseStatus.Text+=Environment.NewLine+"Use Configure SQL notifications… for explicit one-time administrator setup.";SetDatabaseResult(false);return;}
            var intervals=await service.IntervalsAsync(ct);
            interval.DisplayMember=nameof(IntervalRS.Name);interval.ValueMember=nameof(IntervalRS.Id);interval.DataSource=intervals;
            await RefreshDataSetsAsync(ct);
            checkedDatabase=connection.Text;SetDatabaseResult(true);
        }
        catch(OperationCanceledException){databaseStatus.Text="Database test cancelled.";throw;}
        catch(Exception ex){SetDatabaseResult(false);databaseStatus.Text=ex.Message;throw;}
    }

    private void SetDatabaseResult(bool? success)
    {
        // The supplied success asset is named succes.png in Branding.
        databaseResult.Image=success.HasValue?branding.Image(success.Value?"succes.png":"fail.png"):null;
        databaseResult.AccessibleDescription=success switch{true=>"Database test passed",false=>"Database test failed",_=>"No current test result"};
    }

    internal async Task TestBrokerAsync(CancellationToken ct)
    {
        checkedBroker=null;SetAdapterResult(null);
        try
        {
            await DisconnectAdapterAsync(ct);
            brokerStatus.Text="Connecting to the selected provider and waiting for API readiness…";
            adapter=RunningAdapter?.Invoke(AdapterSettings());borrowedAdapter=adapter is not null;
            adapter??=adapters.Create(AdapterSettings());
            if(!borrowedAdapter)await adapter.ConnectAsync(ct);
            checkedBroker=BrokerSignature();SetAdapterResult(true);brokerStatus.Text="Connected. Provider API handshake passed.";
        }
        catch(OperationCanceledException)
        {await DisconnectAdapterAsync(CancellationToken.None);brokerStatus.Text="Connection test cancelled.";throw;}
        catch(Exception ex)
        {await DisconnectAdapterAsync(CancellationToken.None);SetAdapterResult(false);brokerStatus.Text=ex.Message;throw;}
    }

    private void SetAdapterResult(bool? success)
    {
        adapterResult.Image=success.HasValue?branding.Image(success.Value?"succes.png":"fail.png"):null;
        adapterResult.AccessibleDescription=success switch{true=>"Adapter connection passed",false=>"Adapter connection failed",_=>"No current connection result"};
    }

    private async Task DisconnectAdapterAsync(CancellationToken ct)
    {checkedBroker=null;SetAdapterResult(null);if(adapter is not null){var previous=adapter;adapter=null;if(!borrowedAdapter)await previous.DisposeAsync();borrowedAdapter=false;}brokerStatus.Text="Disconnected.";}

    private async Task EnsureConnectionsAsync(CancellationToken ct)
    {
        await EnsureDatabaseAsync(ct);
        if(checkedBroker!=BrokerSignature() || adapter?.IsConnected!=true)
        {
            try{await TestBrokerAsync(ct);}
            catch(OperationCanceledException){throw;}
            catch(Exception ex){throw new InvalidDataException("Market-data adapter is not ready. Open Market data adapter and use Connect / test. "+ex.Message,ex);}
        }
    }

    private async Task EnsureDatabaseAsync(CancellationToken ct)
    {
        if(checkedDatabase!=connection.Text)
        {
            try
            {
                await TestDatabaseAsync(ct);
                if(checkedDatabase!=connection.Text)throw new InvalidDataException(databaseStatus.Text);
            }
            catch(OperationCanceledException){throw;}
            catch(Exception ex){throw new InvalidDataException("Database is not ready. Open the Database tab and use Test database and schema. "+ex.Message,ex);}
        }
    }

    private async Task SearchAsync(CancellationToken ct)
    {
        await EnsureConnectionsAsync(ct);contracts.DataSource=null;
        var result=await adapter!.SearchAsync(new(symbol.Text.Trim(),type.Text,exchange.Text.Trim(),currency.Text.Trim(),expiry.Text.Trim()),ct);
        contracts.DataSource=result.ToList();selectionStatus.Text=$"{result.Count} match(es). Select the exact contract, interval and local database symbol.";
    }

    private async Task SaveDataSetAsync(CancellationToken ct)
    {
        await EnsureConnectionsAsync(ct);var selected=SelectedContract()??throw new InvalidDataException("Select a broker contract first.");
        var selectedInterval=interval.SelectedItem as IntervalRS??throw new InvalidDataException("Select an interval first.");
        // Symbol search can lack minimum tick/multiplier; resolve before persisting any contract metadata.
        var resolved=await adapter!.ResolveAsync(selected,ct);
        if(BeforeDataSetChangeAsync is {} before)await before("saving a dataset");
        var choice=await new DatabaseSetupService(connection.Text).EnsureDataSetAsync(resolved,alias.Text.Trim(),selectedInterval.Id,ct,adapter!.ProviderId);
        CompleteDataSetSave(choice);await RefreshDataSetsAsync(ct);
    }

    private async Task RefreshDataSetsAsync(CancellationToken ct)
    {DisplayDataSets(await new DataSetDeletionStore(connection.Text).ListAsync(ct));}

    internal void DisplayDataSets(List<ConfiguredDataSet> rows)
    {loadedDataSets=rows;RefreshSelections();}

    private void RefreshSelections()
    {
        int? selectedId=(selections.CurrentRow?.DataBoundItem as ConfiguredDataSet)?.DataSetId;
        var rows=loadedDataSets??[];
        selections.DataSource=rows.ToList();
        if(selectedId.HasValue)
        {
            selections.ClearSelection();selections.CurrentCell=null;
            var row=selections.Rows.Cast<DataGridViewRow>().FirstOrDefault(r=>(r.DataBoundItem as ConfiguredDataSet)?.DataSetId==selectedId.Value);
            if(row is not null){selections.CurrentCell=row.Cells[0];row.Selected=true;}
        }
        UpdateReadiness();
    }

    private async Task TogglePriceSyncAsync(CancellationToken ct)
    {
        var selected=selections.CurrentRow?.DataBoundItem as ConfiguredDataSet??throw new InvalidDataException("Select a configured dataset.");
        await EnsureDatabaseAsync(ct);
        if(BeforeDataSetChangeAsync is {} before)await before("changing dataset price sync");
        await new DataSetMonitoringStore(connection.Text).SetAsync(selected.DataSetId,!selected.Monitor,ct);
        if(loadedDataSets is not null){loadedDataSets=loadedDataSets.Select(d=>d.DataSetId==selected.DataSetId?d with{Monitor=!selected.Monitor}:d).ToList();RefreshSelections();}
        await RefreshDataSetsAsync(ct);
        selectionStatus.Text=$"DataSet {selected.DataSetId}: price sync {(!selected.Monitor?"enabled":"disabled")}. StudyCols are unchanged.";
    }

    private async Task EditTickerContractAsync(CancellationToken ct)
    {
        var selected=selections.CurrentRow?.DataBoundItem as ConfiguredDataSet??throw new InvalidDataException("Select a configured dataset to edit its ticker contract.");
        if(!CanEditExpiry(selected))throw new InvalidDataException("Contract expiry changes are available only for futures datasets, not stocks or NYSE/NASDAQ.");
        await EnsureConnectionsAsync(ct);
        var original=await new TickerProviderStore(connection.Text).ReadInstrumentAsync(selected.TickerId,adapter!.ProviderId,ct);
        using var dialog=new TickerContractForm(selected,original,async(local,expiry,providerId,token)=>
        {
            var candidate=TickerContractStore.Candidate(original,local,expiry);
            if(providerId is not null)candidate=candidate with{ProviderInstrumentId=providerId};
            var resolved=await adapter!.ResolveAsync(candidate,token);
            if(BeforeDataSetChangeAsync is {} before)await before("changing a ticker contract");
            await new TickerContractStore(connection.Text).UpdateAsync(selected,adapter.ProviderId,original,candidate,resolved,token);
        },token=>adapter!.SearchAsync(new(original.Symbol,original.SecurityType,original.Exchange,original.Currency,""),token));
        if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        await RefreshDataSetsAsync(ct);contracts.DataSource=null;
        selectionStatus.Text=$"Ticker {selected.TickerId}: contract and provider mapping updated. Existing datasets, sync flags and price/study history were kept.";
    }

    private async Task DeleteDataSetAsync(CancellationToken ct)
    {
        var selected=selections.CurrentRow?.DataBoundItem as ConfiguredDataSet??throw new InvalidDataException("Select a dataset from the configured price datasets list.");
        if(!new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(settings.ConnectionString).EquivalentTo(new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection.Text)))
            throw new InvalidDataException("Save the changed database connection configuration before deleting datasets.");
        await EnsureDatabaseAsync(ct);
        var store=new DataSetDeletionStore(connection.Text);var preview=await store.PreviewAsync(selected.DataSetId,ct);
        DataSetDeletionStore.ValidateSelectedIdentity(selected,preview.DataSet);
        selected=preview.DataSet;
        int monitors=preview.DataSet.Monitor?1:0;
        string warning=preview.StudyColIds.Count>0||monitors>0
            ?$"WARNING: This dataset is used by {preview.StudyColIds.Count} StudyCol(s) and {monitors} configured monitor(s). ALL of these study collections and monitors will be removed.\n\n":"";
        string message=$"Delete DataSet {selected.DataSetId}: {preview.DataSet.Symbol}, {preview.DataSet.IntervalName}?\n\n"+warning+
            $"This also permanently deletes {preview.PriceRows:N0} price bar(s) and {preview.StudyHistoryRows:N0} StudyHistory output/checkpoint row(s). "+
            (preview.RemoveTicker?"This is the ticker's last dataset. The ticker and its provider mappings will also be removed.":"Other datasets still use the ticker, so the ticker is kept.")+"\n\nContinue?";
        if(MessageBox.Show(this,message,"Delete DataSet",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
        if(BeforeDataSetChangeAsync is {} before)await before("deleting a dataset");
        await store.DeleteConfirmedAsync(preview,ct);
        await RefreshDataSetsAsync(ct);selectionStatus.Text=$"DataSet {selected.DataSetId}, its price/study/checkpoint rows and StudyCols were removed."+(preview.RemoveTicker?$" Ticker {selected.TickerId} and its provider mappings were also removed.":"");
    }

    private string DatabaseSignature()=>JsonSerializer.Serialize(new{Connection=connection.Text.Trim(),History=(int)historyMinutes.Value,BackupPoll=(int)backupPollSeconds.Value,StudyTimeout=(int)studySqlTimeout.Value,StudyBatch=(int)studyBatchSize.Value,CheckpointEvery=(int)checkpointEveryBars.Value});
    private string ResolvePluginRoot()=>Path.GetFullPath(settings.PluginPath,Path.GetDirectoryName(Path.GetFullPath(path))!);

    private bool ConfirmSaveLocal(string reason)
    {
        if(confirmLocalSave is not null)return confirmLocalSave(reason);
        using var prompt=new Form{Text="Unsaved configuration",Width=510,Height=205,FormBorderStyle=FormBorderStyle.FixedDialog,StartPosition=FormStartPosition.CenterParent,MinimizeBox=false,MaximizeBox=false,Icon=branding.ApplicationIcon};
        var message=new Label{Dock=DockStyle.Fill,Padding=new(14),Text=$"Database, Market Data Adapter or plugin-folder settings have unsaved changes.\n\nSave configuration before {reason}? Cancel keeps your changes open for editing."};
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=46,FlowDirection=FlowDirection.RightToLeft,Padding=new(6)};
        var cancel=new Button{Text="Cancel",DialogResult=DialogResult.Cancel,AutoSize=true};var saveChanges=new Button{Text="Save",DialogResult=DialogResult.OK,AutoSize=true};
        buttons.Controls.AddRange([cancel,saveChanges]);prompt.Controls.Add(message);prompt.Controls.Add(buttons);prompt.AcceptButton=saveChanges;prompt.CancelButton=cancel;
        return prompt.ShowDialog(this)==DialogResult.OK;
    }
    private bool TrySaveConfiguration()
    {
        try
        {
            // Draft adapter/database settings may be saved before connectivity is available.
            if(settings.LegacyDataSelections?.Count>0&&!new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(settings.ConnectionString).EquivalentTo(new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection.Text)))
                throw new InvalidDataException("Migrate legacy DataSelections using the original database connection before saving a different database.");
            if(settings.LegacyMonitors?.Count>0&&!new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(settings.ConnectionString).EquivalentTo(new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection.Text)))
                throw new InvalidDataException("Validate legacy monitors against the original SQL database before saving a different database.");
            var draft=AdapterSettings();adapters.Get(draft.ProviderId).Validate(draft);
            string pluginPath=pluginEditor?.DraftPath()??settings.PluginPath;
            if(string.IsNullOrWhiteSpace(pluginPath))throw new InvalidDataException("Specify the plugin folder in Collection Types.");
            _=Path.GetFullPath(pluginPath,Path.GetDirectoryName(Path.GetFullPath(path))!);
            var original=(settings.ConnectionString,settings.Adapter,settings.Broker,settings.PriceHistoryMinutes,settings.PollSeconds,settings.PluginPath,settings.StudySqlTimeoutSeconds,settings.StudyBatchSize,settings.CheckpointEveryBars);
            settings.Adapter=draft;
            settings.ConnectionString=connection.Text.Trim();settings.Broker=null;settings.PriceHistoryMinutes=(int)historyMinutes.Value;
            settings.PollSeconds=(int)backupPollSeconds.Value;
            settings.PluginPath=pluginPath;
            settings.StudySqlTimeoutSeconds=(int)studySqlTimeout.Value;settings.StudyBatchSize=(int)studyBatchSize.Value;settings.CheckpointEveryBars=(int)checkpointEveryBars.Value;
            try{SettingsFile.Save(path,settings);}
            catch{(settings.ConnectionString,settings.Adapter,settings.Broker,settings.PriceHistoryMinutes,settings.PollSeconds,settings.PluginPath,settings.StudySqlTimeoutSeconds,settings.StudyBatchSize,settings.CheckpointEveryBars)=original;throw;}
            savedDatabase=DatabaseSignature();savedAdapter=BrokerSignature();savedPluginPath=settings.PluginPath.Trim();ConfigurationSaved=true;return true;
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Cannot save configuration");return false;}
    }

    protected override void Dispose(bool disposing)
    {
        bool release=disposing&&!resourcesDisposed;
        if(release){resourcesDisposed=true;lifetime.Cancel();lifetime.Dispose();}
        base.Dispose(disposing);
        if(release){tabImages.Dispose();branding.Dispose();}
    }
}

internal static class SettingsFile
{
    private static readonly JsonSerializerOptions Options=new(){WriteIndented=true,PropertyNameCaseInsensitive=true,ReadCommentHandling=JsonCommentHandling.Skip,
        UnmappedMemberHandling=System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow};
    public static MarketLiveSettings Load(string path)=>File.Exists(path)?JsonSerializer.Deserialize<MarketLiveSettings>(File.ReadAllText(path),Options)
        ??throw new InvalidDataException("Empty configuration."):new();
    public static void Save(string path,MarketLiveSettings settings)
    {
        string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllText(temporary,JsonSerializer.Serialize(settings,Options));File.Move(temporary,path,true);}
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }

    public static async Task<int> MigrateProvidersAsync(string path,MarketLiveSettings settings,string targetConnection,CancellationToken ct)
    {
        var legacy=settings.LegacyDataSelections;
        if(legacy is null){await new TickerProviderStore(targetConnection).EnsureSchemaAsync(ct);return 0;}
        if(legacy.Count>0&&!new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(settings.ConnectionString).EquivalentTo(new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(targetConnection)))
            throw new InvalidDataException("Migrate legacy provider settings to their original database before changing the database connection.");
        string? original=File.Exists(path)?await File.ReadAllTextAsync(path,ct):null;
        if(original is not null&&JsonSerializer.Serialize(Load(path),Options)!=JsonSerializer.Serialize(settings,Options))
            throw new IOException("Settings changed since this dialog was opened. Close and reopen configuration before migration.");
        int count=await new TickerProviderStore(targetConnection).MigrateAsync(legacy,ct);
        if(original is not null&&await File.ReadAllTextAsync(path,ct)!=original)
            throw new IOException("Provider mappings committed, but settings changed during migration. JSON was retained; retry migration.");
        if(original is not null)await File.WriteAllTextAsync(path+".before-ticker-provider-"+Guid.NewGuid().ToString("N")+".bak",original,ct);
        settings.LegacyDataSelections=null;
        try{Save(path,settings);}catch{settings.LegacyDataSelections=legacy;throw;}
        return count;
    }

    public static async Task<int> MigrateMonitorsAsync(string path,MarketLiveSettings settings,string targetConnection,CancellationToken ct)
    {
        var legacy=settings.LegacyMonitors;
        if(legacy is null)return (await new SqlMonitorLoader(targetConnection).LoadAsync(ct)).Monitors.Count;
        if(legacy.Count>0&&!new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(settings.ConnectionString).EquivalentTo(new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(targetConnection)))
            throw new InvalidDataException("Validate legacy monitors against their original SQL database before changing connections.");
        string? original=File.Exists(path)?await File.ReadAllTextAsync(path,ct):null;
        if(original is not null&&JsonSerializer.Serialize(Load(path),Options)!=JsonSerializer.Serialize(settings,Options))
            throw new IOException("Settings changed since loading. Reopen configuration before removing legacy monitors.");
        var catalog=await new StudyConfigurationStore(targetConnection).ReadAsync(ct);
        if(legacy.Any(m=>m.StudyCol.Id<1||!catalog.Studies.Any(s=>s.Id==m.StudyCol.Id)))
            throw new InvalidDataException("Legacy monitors reference StudyCol rows missing from SQL. Configure those studies in SQL before removing the JSON section.");
        var loaded=await new SqlMonitorLoader(targetConnection).LoadAsync(ct);
        if(loaded.Issues.Count>0)throw new InvalidDataException("SQL monitor configuration is incomplete: "+string.Join("; ",loaded.Issues.Select(i=>$"StudyCol {i.StudyColId}: {i.Message}")));
        if(original is not null&&await File.ReadAllTextAsync(path,ct)!=original)throw new IOException("Settings changed during SQL validation. Legacy Monitors retained; retry.");
        if(original is not null)await File.WriteAllTextAsync(path+".before-sql-monitors-"+Guid.NewGuid().ToString("N")+".bak",original,ct);
        settings.LegacyMonitors=null;
        try{Save(path,settings);}catch{settings.LegacyMonitors=legacy;throw;}
        return loaded.Monitors.Count;
    }
}
