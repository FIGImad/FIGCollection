namespace FIGMarketLive;

internal sealed class PluginCollectionsPanel:UserControl
{
    private readonly TextBox path=new(){Width=500,AccessibleName="Plugin folder"};
    private readonly DataGridView rows=new(){Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false};
    private readonly TextBox details=new(){Dock=DockStyle.Bottom,Height=100,ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Both};
    private readonly Label status=new(){Dock=DockStyle.Bottom,AutoSize=true,MaximumSize=new(850,0)};
    private readonly Func<string> connection,savedRoot;
    private readonly Func<string,bool> savePath;
    private readonly BrandingAssets branding=new();
    private string? loadedConnection;
    private string? loadedRoot;
    private bool loaded;
    internal string DraftPath()=>path.Text.Trim();
    internal bool NeedsReload=>!loaded||loadedConnection!=connection()||loadedRoot!=savedRoot();
    public PluginCollectionsPanel(string initialPath,Func<string> savedRoot,Func<string> connection,Func<string,bool> savePath,Func<Func<CancellationToken,Task>,Task> run)
    {
        this.savedRoot=savedRoot;this.connection=connection;this.savePath=savePath;Dock=DockStyle.Fill;path.Text=initialPath;
        var toolbar=new ToolStrip{Dock=DockStyle.Top,GripStyle=ToolStripGripStyle.Hidden};
        var refresh=new ToolStripButton("Refresh plugins",branding.Image("reload.png")){ToolTipText="Discover plugins and synchronize Collection Types"};
        refresh.Click+=async(_,_)=>await run(ReloadAsync);toolbar.Items.Add(refresh);BrandingAssets.StyleToolbar(toolbar);
        var folder=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true};var browse=new Button{Text="Browse…",AutoSize=true};
        browse.Click+=(_,_)=>{using var picker=new FolderBrowserDialog{SelectedPath=path.Text};if(picker.ShowDialog(this)==DialogResult.OK)path.Text=picker.SelectedPath;};
        var save=new Button{Text="Save plugin folder",AutoSize=true};branding.Apply(save,"save.png");
        save.Click+=async(_,_)=>{if(savePath(DraftPath()))await run(ReloadAsync);};
        folder.Controls.AddRange([new Label{Text="Plugin folder",AutoSize=true,Padding=new Padding(0,7,0,0)},path,browse,save]);
        Controls.Add(rows);Controls.Add(details);Controls.Add(status);Controls.Add(toolbar);Controls.Add(folder);
        void ShowDetails(object? sender,EventArgs args)
        {if(rows.CurrentRow?.DataBoundItem is PluginCollectionRow row)details.Text=$"{row.Type} — {row.Status}\r\n{row.Manifest}\r\n{row.Error}";}
        rows.SelectionChanged+=ShowDetails;rows.CurrentCellChanged+=ShowDetails;
        status.Text="Collection Types are discovered from plugin.json. Install complete packages, save the folder, then refresh. Restart studies to load changed plugins.";
    }
    internal void ApplyRows(IEnumerable<PluginCollectionRow> entries)
    {
        rows.DataSource=entries.ToList();
        if(rows.Columns[nameof(PluginCollectionRow.Manifest)] is {} column)column.HeaderText="Plugin / manifest path";
    }
    internal async Task ReloadAsync(CancellationToken ct)
    {
        var snapshot=MarketLivePlugins.Discover(savedRoot());ApplyRows(snapshot.Rows);
        await MarketLivePlugins.SyncAsync(connection(),snapshot,ct);
        var catalog=await new StudyConfigurationStore(connection()).ReadAsync(ct);
        var result=snapshot.Rows.ToList();
        foreach(var type in catalog.ColTypes.Where(t=>!result.Any(r=>r.Type.Equals(t.Type,StringComparison.OrdinalIgnoreCase))))
            result.Add(new(type.Type,type.DLLName,"","","Unavailable","No successfully loaded plugin was discovered for this SQL collection type. Existing StudyCol references are retained."));
        ApplyRows(result);loaded=true;loadedConnection=connection();loadedRoot=snapshot.Root;
        status.Text=$"Plugin folder: {snapshot.Root}\r\n{snapshot.Registrations.Count} loaded; {result.Count(r=>r.Status!="Loaded")} error/unavailable entries. DLL names come from plugin.json. Restart studies after plugin changes.";
    }
    protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing)branding.Dispose();}
}
