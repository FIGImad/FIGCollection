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
    private readonly TextBox databaseStatus=new(){Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical};
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
    private readonly TextBox exchange=new(){Width=75};
    private readonly TextBox currency=new(){Text="USD",Width=50};
    private readonly TextBox expiry=new(){Width=80};
    private readonly TextBox alias=new(){Width=140};
    private readonly ComboBox interval=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=115};
    private readonly DataGridView contracts=new(){Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=false,AllowUserToAddRows=false,AllowUserToDeleteRows=false,MultiSelect=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect};
    private readonly DataGridView selections=new(){Dock=DockStyle.Fill,Height=150,ReadOnly=true,AutoGenerateColumns=false,AllowUserToAddRows=false,AllowUserToDeleteRows=false,MultiSelect=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect};
    private List<ConfiguredDataSet>? loadedDataSets;
    private readonly Label selectionStatus=new(){AutoSize=true,MaximumSize=new(760,0)};
    private readonly TabControl tabs=new(){Dock=DockStyle.Fill};
    private readonly Button save=new(){Text="Save configuration",AutoSize=true};
    private readonly List<Button> tickerActions=[];
    private readonly CancellationTokenSource lifetime=new();
    private string? checkedDatabase;
    private string? checkedBroker;
    private bool busy;
    private bool closeRequested;
    private bool resourcesDisposed;

    public ConfigurationForm(string path,MarketDataAdapterCatalog adapters,bool autoLoadStudyEditors=true)
    {
        this.path=path;this.adapters=adapters;
        settings=SettingsFile.Load(path);
        Text="FIGMarketLive configuration";Width=980;Height=740;MinimumSize=new(840,620);
        Icon=branding.ApplicationIcon;
        tabImages.Images.Add("database",branding.Image("database.png"));tabImages.Images.Add("chart",branding.Image("chart.png"));tabImages.Images.Add("adapter",branding.Image("plug.png"));tabImages.Images.Add("studies",branding.Image("bar-chart.png"));
        tabImages.Images.Add("collectionTypes",branding.Image("coltype.png"));
        tabs.ImageList=tabImages;
        StartPosition=FormStartPosition.CenterParent;
        connection.Text=settings.ConnectionString;
        type.Items.AddRange(["FUT","STK"]);type.SelectedIndex=0;
        var database=Page("Database");
        database.ImageKey="database";
        var dbLayout=CreateLayout();database.Controls.Add(dbLayout);
        Add(dbLayout,new Label { Text="FIGMarketLive SQL Server connection string",AutoSize=true });Add(dbLayout,connection);
        Add(dbLayout,new Label { Text="Use Database / Initial Catalog to name the database. Connection strings are saved locally; prefer integrated authentication.",AutoSize=true });
        var databaseTestRow=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};
        databaseTestRow.Controls.Add(Button("Test database and schema",()=>RunAsync(TestDatabaseAsync)));
        databaseTestRow.Controls.Add(databaseResult);
        Add(dbLayout,databaseTestRow);Add(dbLayout,databaseStatus,true);
        var broker=Page("Market data adapter");var brokerLayout=CreateLayout();broker.Controls.Add(brokerLayout);
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
        var query=new FlowLayoutPanel { AutoSize=true,Dock=DockStyle.Fill,WrapContents=true };
        foreach(var (label,control) in new (string,Control)[]{("Symbol",symbol),("Type",type),("Exchange",exchange),("Currency",currency),("Expiry",expiry)})
        { query.Controls.Add(new Label { Text=label,AutoSize=true,Margin=new(6,7,3,3) });query.Controls.Add(control); }
        Add(tickerLayout,new Label { Text="Search checks the current database and connects the adapter as needed. Futures require a contract month (YYYYMM) or expiry (YYYYMMDD).",AutoSize=true,MaximumSize=new(850,0) });
        Add(tickerLayout,query);Add(tickerLayout,TickerAction("Search contracts",()=>RunAsync(SearchAsync)));
        foreach(var name in new[]{"Symbol","LocalSymbol","SecurityType","Expiry","Exchange","Currency","Name","ProviderInstrumentId"})
            contracts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName=name,HeaderText=name,AutoSizeMode=DataGridViewAutoSizeColumnMode.AllCells });
        Add(tickerLayout,contracts,true);
        var choose=new FlowLayoutPanel { AutoSize=true,Dock=DockStyle.Fill };
        choose.Controls.Add(new Label { Text="Local database symbol",AutoSize=true });choose.Controls.Add(alias);
        choose.Controls.Add(new Label { Text="Interval",AutoSize=true });choose.Controls.Add(interval);
        choose.Controls.Add(TickerAction("Save Ticker",()=>RunAsync(SaveDataSetAsync)));
        Add(tickerLayout,choose);
        Add(tickerLayout,selectionStatus);
        Add(tickerLayout,new Label { Text="Configured price datasets",AutoSize=true });
        foreach(var (name,title,width) in new[]{("DataSetId","DataSet",65),("Symbol","Symbol",105),("LocalSymbol","Contract",100),("SecurityType","Type",55),
            ("IntervalName","Interval",95),("StudyCollections","StudyCols",75),("Monitors","Monitors",70)})
            selections.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=name,HeaderText=title,Width=width});
        Add(tickerLayout,selections);
        Add(tickerLayout,TickerAction("Delete selected DataSet",()=>RunAsync(DeleteDataSetAsync)));
        var studyTab=Page("Market Data & Studies");studyTab.ImageKey="studies";
        var studyEditor=new StudyConfigurationPanel(false,()=>connection.Text,RunAsync);studyTab.Controls.Add(studyEditor);
        var typeTab=Page("Collection Types");typeTab.ImageKey="collectionTypes";
        var typeEditor=new StudyConfigurationPanel(true,()=>connection.Text,RunAsync);typeTab.Controls.Add(typeEditor);
        tabs.SelectedIndexChanged+=async(_,_)=>
        {
            if(busy||!autoLoadStudyEditors)return;
            if(tabs.SelectedTab==studyTab&&studyEditor.NeedsReload)await RunAsync(studyEditor.ReloadAsync);
            else if(tabs.SelectedTab==typeTab&&typeEditor.NeedsReload)await RunAsync(typeEditor.ReloadAsync);
        };
        var bottom=new FlowLayoutPanel { Dock=DockStyle.Bottom,Height=48,FlowDirection=FlowDirection.RightToLeft,Padding=new(6) };
        var cancel=new Button {Text="Close",AutoSize=true};cancel.Click+=(_,_)=>Close();
        save.Click+=(_,_)=>SaveConfiguration();bottom.Controls.Add(cancel);bottom.Controls.Add(save);
        Controls.Add(tabs);Controls.Add(bottom);
        connection.TextChanged+=(_,_)=> {checkedDatabase=null;SetDatabaseResult(null);databaseStatus.Text="Connection changed. Test the database again.";interval.DataSource=null;loadedDataSets=null;selections.DataSource=null;UpdateReadiness();};
        provider.SelectedIndexChanged+=(_,_)=> {RenderAdapterFields();InvalidateAdapter();};
        foreach(var field in new[]{symbol,exchange,currency,expiry})field.TextChanged+=(_,_)=>contracts.DataSource=null;
        type.SelectedIndexChanged+=(_,_)=>contracts.DataSource=null;
        contracts.SelectionChanged+=(_,_)=>
        {
            if(SelectedContract() is {} c) alias.Text=c.SecurityType=="FUT"?c.LocalSymbol:c.Symbol;
        };
        RefreshSelections();
        if(autoLoadStudyEditors)Shown+=async(_,_)=>await RunAsync(TestDatabaseAsync);
        FormClosing+=(_,e)=>
        {
            if(busy){closeRequested=true;lifetime.Cancel();e.Cancel=true;}
            else if(adapter is not null){e.Cancel=true;closeRequested=true;_=RunAsync(DisconnectAdapterAsync);}
        };
    }

    private TabPage Page(string text){var p=new TabPage(text){Padding=new(12)};tabs.TabPages.Add(p);return p;}
    private static TableLayoutPanel CreateLayout()=>new(){Dock=DockStyle.Fill,ColumnCount=1,AutoScroll=true};
    private static void Add(TableLayoutPanel p,Control c,bool grow=false)
    {int row=p.RowCount++;p.RowStyles.Add(new RowStyle(grow?SizeType.Percent:SizeType.AutoSize,grow?100:0));p.Controls.Add(c,0,row);c.Margin=new(3,5,3,5);}
    private static void Field(TableLayoutPanel p,string label,Control c){Add(p,new Label{Text=label,AutoSize=true});Add(p,c);}
    private static Button Button(string text,Func<Task> action)
    {var b=new Button{Text=text,AutoSize=true};b.Click+=async(_,_)=>await action();return b;}
    private Button TickerAction(string text,Func<Task> action)
    {var b=Button(text,action);tickerActions.Add(b);return b;}
    private void UpdateReadiness(){foreach(var button in tickerActions)button.Enabled=!busy;}
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

    private async Task TestDatabaseAsync(CancellationToken ct)
    {
        checkedDatabase=null;SetDatabaseResult(null);databaseStatus.Text="Testing database accessibility and required schema…";
        try
        {
            await SettingsFile.MigrateProvidersAsync(path,settings,connection.Text,ct);
            await SettingsFile.MigrateMonitorsAsync(path,settings,connection.Text,ct);
            var service=new DatabaseSetupService(connection.Text);var result=await service.CheckAsync(ct);databaseStatus.Text=result.Message;
            if(!result.Ready){SetDatabaseResult(false);return;}
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
            adapter=adapters.Create(AdapterSettings());
            await adapter.ConnectAsync(ct);
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
    {checkedBroker=null;SetAdapterResult(null);if(adapter is not null){var previous=adapter;adapter=null;await previous.DisposeAsync();}brokerStatus.Text="Disconnected.";}

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
        var choice=await new DatabaseSetupService(connection.Text).EnsureDataSetAsync(resolved,alias.Text.Trim(),selectedInterval.Id,ct,adapter!.ProviderId);
        await RefreshDataSetsAsync(ct);selectionStatus.Text=$"Ticker {choice.TickerId}, provider mapping and DataSet {choice.Id} saved in SQL.";
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
        int monitors=preview.DataSet.Monitors;
        string warning=preview.StudyColIds.Count>0||monitors>0
            ?$"WARNING: This dataset is used by {preview.StudyColIds.Count} StudyCol(s) and {monitors} configured monitor(s). ALL of these study collections and monitors will be removed.\n\n":"";
        string message=$"Delete DataSet {selected.DataSetId}: {preview.DataSet.Symbol}, {preview.DataSet.IntervalName}?\n\n"+warning+
            $"This also permanently deletes {preview.PriceRows:N0} price bar(s) and {preview.StudyRows:N0} BarStudy row(s). The ticker itself is kept.\n\nContinue?";
        if(MessageBox.Show(this,message,"Delete DataSet",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
        await store.DeleteConfirmedAsync(preview,ct);
        await RefreshDataSetsAsync(ct);selectionStatus.Text=$"DataSet {selected.DataSetId}, its price/study rows, StudyCols and configured monitors were removed.";
    }

    private void SaveConfiguration()
    {
        try
        {
            // Draft adapter/database settings may be saved before connectivity is available.
            if(settings.LegacyDataSelections?.Count>0&&!new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(settings.ConnectionString).EquivalentTo(new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection.Text)))
                throw new InvalidDataException("Migrate legacy DataSelections using the original database connection before saving a different database.");
            if(settings.LegacyMonitors?.Count>0&&!new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(settings.ConnectionString).EquivalentTo(new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection.Text)))
                throw new InvalidDataException("Validate legacy monitors against the original SQL database before saving a different database.");
            settings.Adapter=AdapterSettings();adapters.Get(settings.Adapter.ProviderId).Validate(settings.Adapter);
            settings.ConnectionString=connection.Text.Trim();settings.Broker=null;
            SettingsFile.Save(path,settings);DialogResult=DialogResult.OK;Close();
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Cannot save configuration");}
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
