using FIGCommon.Models;

namespace FIGMarketLive;

/// <summary>Database-backed editors; mutations are explicit Save/Remove actions, independent of adapter connection.</summary>
internal sealed class StudyConfigurationPanel : UserControl
{
    private readonly bool typesOnly;
    private readonly Func<string> connection;
    private readonly Func<Func<CancellationToken,Task>,Task> run;
    private readonly ComboBox ticker=new(){Width=210,DropDownStyle=ComboBoxStyle.DropDownList};
    private readonly ComboBox interval=new(){Width=300,DropDownStyle=ComboBoxStyle.DropDownList};
    private readonly ComboBox collection=new(){Width=200,DropDownStyle=ComboBoxStyle.DropDownList};
    private readonly CheckBox enabled=new(){Text="Enabled",Checked=true,AutoSize=true};
    private readonly TextBox json=new(){Multiline=true,AcceptsReturn=true,AcceptsTab=true,ScrollBars=ScrollBars.Both,WordWrap=false,Dock=DockStyle.Fill,Text="{}",Font=new Font("Consolas",10)};
    private readonly TextBox typeName=new(){Width=180,MaxLength=100};
    private readonly TextBox dll=new(){Width=400,MaxLength=256};
    private readonly TextBox description=new(){Multiline=true,Height=80,Dock=DockStyle.Fill,MaxLength=1000,ScrollBars=ScrollBars.Vertical};
    private readonly Label source=new(){AutoSize=true,MaximumSize=new(850,0)};
    private readonly Label status=new(){AutoSize=true,MaximumSize=new(850,0)};
    private readonly DataGridView rows=new(){Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=false,AllowUserToAddRows=false,AllowUserToDeleteRows=false,MultiSelect=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect};
    private readonly BrandingAssets branding=new();
    private readonly ToolStrip toolbar=new(){Dock=DockStyle.Fill,GripStyle=ToolStripGripStyle.Hidden,ImageScalingSize=new(20,20)};
    private readonly ToolStripButton removeStudy=new(){Text="Remove",ToolTipText="Delete Study Collection",DisplayStyle=ToolStripItemDisplayStyle.Image,Enabled=false};
    private readonly ToolStripButton saveStudy=new(){Text="Save",ToolTipText="Save Study Collection",DisplayStyle=ToolStripItemDisplayStyle.Image,Enabled=false};
    private readonly ToolStripButton moveUp=new(){Text="Move up",ToolTipText="Move Study Collection Up",DisplayStyle=ToolStripItemDisplayStyle.Image,Enabled=false};
    private readonly ToolStripButton moveDown=new(){Text="Move down",ToolTipText="Move Study Collection Down",DisplayStyle=ToolStripItemDisplayStyle.Image,Enabled=false};
    private readonly Label editorTitle=new(){Text="New StudyCol",AutoSize=true,Font=new Font(SystemFonts.DefaultFont,FontStyle.Bold)};
    private readonly PictureBox jsonResult=new(){Width=24,Height=24,SizeMode=PictureBoxSizeMode.CenterImage,Margin=new(8,4,3,3),AccessibleName="JSON validation result"};
    private readonly Func<string,bool>? confirmDiscard;
    private readonly Func<PluginCollectionSnapshot>? plugins;
    private string cleanDraft="";
    private bool resourcesDisposed;
    private StudyConfigurationCatalog? catalog;
    private string? loadedConnection;
    private int studyId;
    private string? originalType;
    private bool binding;
    public bool NeedsReload=>catalog is null||loadedConnection!=connection();
    public bool HasUnsavedChanges=>Draft()!=cleanDraft;

    public StudyConfigurationPanel(bool typesOnly,Func<string> connection,Func<Func<CancellationToken,Task>,Task> run,Func<string,bool>? confirmDiscard=null,Func<PluginCollectionSnapshot>? plugins=null)
    {
        this.typesOnly=typesOnly;this.connection=connection;this.run=run;Dock=DockStyle.Fill;
        this.confirmDiscard=confirmDiscard;
        this.plugins=plugins;
        editorTitle.Text=typesOnly?"New Collection Type":"New StudyCol";
        ticker.BindingContext=new BindingContext();interval.BindingContext=new BindingContext();collection.BindingContext=new BindingContext();
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,AutoScroll=true};Controls.Add(layout);
        if(typesOnly)
        {
            ToolStripButton Action(string text,string tooltip,string file,Func<CancellationToken,Task> action)
            {var item=new ToolStripButton(text,branding.Image(file)){ToolTipText=tooltip,DisplayStyle=ToolStripItemDisplayStyle.Image};item.Click+=async(_,_)=>await run(action);return item;}
            toolbar.Items.Add(Action("Add","Add New Collection Type","add.png",_=>{if(ConfirmDiscardChanges("starting a new Collection Type"))New();return Task.CompletedTask;}));
            removeStudy.ToolTipText="Delete Collection Type";removeStudy.Image=branding.Image("trash.png");removeStudy.Click+=async(_,_)=>await run(RemoveTypeAsync);toolbar.Items.Add(removeStudy);
            saveStudy.ToolTipText="Save Collection Type";saveStudy.Image=branding.Image("save.png");saveStudy.Click+=async(_,_)=>await run(SaveAsync);toolbar.Items.Add(saveStudy);
            toolbar.Items.Add(new ToolStripSeparator());toolbar.Items.Add(Action("Refresh","Reload Collection Types","reload.png",async ct=>{if(ConfirmDiscardChanges("reloading Collection Types"))await ReloadAsync(ct);}));
            BrandingAssets.StyleToolbar(toolbar);Add(layout,toolbar);Add(layout,rows,true);Add(layout,status);Add(layout,editorTitle);
            var nameRow=Flow();Field(nameRow,"Type",typeName);Field(nameRow,"DLL name / path",dll);Add(layout,nameRow);
            Add(layout,new Label{Text="Description",AutoSize=true});Add(layout,description,true);
            foreach(var key in new[]{"Type","DLLName","Description"})Column(key,key);
            foreach(var text in new[]{typeName,dll,description})text.TextChanged+=(_,_)=>UpdateStudyActions();
        }
        else
        {
            ToolStripButton Action(string text,string tooltip,string file,Func<CancellationToken,Task> action)
            {var button=new ToolStripButton(text,branding.Image(file)){ToolTipText=tooltip,DisplayStyle=ToolStripItemDisplayStyle.Image};button.Click+=async(_,_)=>await run(action);return button;}
            toolbar.Items.Add(Action("Add","Add New Study Collection","add.png",_=>{if(ConfirmDiscardChanges("starting a new StudyCol"))New();return Task.CompletedTask;}));
            removeStudy.Image=branding.Image("trash.png");removeStudy.Click+=async(_,_)=>await run(RemoveStudyAsync);toolbar.Items.Add(removeStudy);
            saveStudy.Image=branding.Image("save.png");saveStudy.Click+=async(_,_)=>await run(SaveAsync);toolbar.Items.Add(saveStudy);
            moveUp.Image=branding.Image("up-arrow.png");moveDown.Image=branding.Image("dn-arrow.png");
            moveUp.Click+=async(_,_)=>await run(ct=>MoveAsync(-1,ct));moveDown.Click+=async(_,_)=>await run(ct=>MoveAsync(1,ct));
            toolbar.Items.Add(moveUp);toolbar.Items.Add(moveDown);
            toolbar.Items.Add(new ToolStripSeparator());toolbar.Items.Add(Action("Refresh","Reload Study Collections","reload.png",async ct=>{if(ConfirmDiscardChanges("reloading Study Collections"))await ReloadAsync(ct);}));
            BrandingAssets.StyleToolbar(toolbar);
            Add(layout,toolbar);Add(layout,rows,true);Add(layout,status);Add(layout,editorTitle);
            var instrumentRow=Flow();Field(instrumentRow,"Ticker",ticker);Field(instrumentRow,"Interval",interval);Add(layout,instrumentRow);
            var typeRow=Flow();collection.DisplayMember=nameof(CollectionType.Type);collection.ValueMember=nameof(CollectionType.Type);Field(typeRow,"ColType",collection);typeRow.Controls.Add(enabled);Add(layout,typeRow);
            Add(layout,source);
            var jsonHeading=Flow();jsonHeading.Controls.Add(new Label{Text="ColParams (JSON object). Use the toolbar arrows to change processing order.",AutoSize=true,Margin=new(3,7,12,3)});
            jsonHeading.Controls.Add(Button("Format / validate JSON",()=>
            {
                if(jsonResult.Image is not null){ClearJsonResult();return Task.CompletedTask;}
                json.Text=StudyIntervalCompatibility.NormalizeParameters(json.Text);jsonResult.Image=branding.Image("succes.png");jsonResult.AccessibleDescription="JSON validated";return Task.CompletedTask;
            }));jsonHeading.Controls.Add(jsonResult);Add(layout,jsonHeading);Add(layout,json,true);
            foreach(var (key,title) in new[]{("OrderSeq","OrderSeq"),("Id","StudyCol"),("Symbol","Ticker"),("IntervalName","Interval"),("ColType","ColType"),("Enabled","Enabled")})Column(key,title);
            ticker.SelectedIndexChanged+=(_,_)=>{if(!binding){UpdateIntervals();UpdateStudyActions();}};
            interval.SelectedIndexChanged+=(_,_)=>{UpdateSource();UpdateStudyActions();};
            collection.SelectedIndexChanged+=(_,_)=>UpdateStudyActions();enabled.CheckedChanged+=(_,_)=>UpdateStudyActions();json.TextChanged+=(_,_)=>{ClearJsonResult();UpdateStudyActions();};
        }
        void SelectionUpdated(object? sender,EventArgs args)
        {
            if(binding)return;
            if(typesOnly&&rows.CurrentRow?.DataBoundItem is CollectionType type&&type.Type!=originalType)
            {if(!ConfirmDiscardChanges("selecting another Collection Type")){RestoreSelection();return;}EditType(type);}
            else if(!typesOnly&&rows.CurrentRow?.DataBoundItem is StudyConfigurationRow row&&row.Id!=studyId)
            {
                if(!ConfirmDiscardChanges("selecting another StudyCol")){RestoreSelection();return;}
                Edit(row);
            }
            UpdateStudyActions();
        }
        rows.SelectionChanged+=SelectionUpdated;rows.CurrentCellChanged+=SelectionUpdated;
        cleanDraft=Draft();
    }
    private static FlowLayoutPanel Flow()=>new(){AutoSize=true,Dock=DockStyle.Fill,WrapContents=true};
    private static void Field(FlowLayoutPanel row,string label,Control input)
    {row.Controls.Add(new Label{Text=label,AutoSize=true,Margin=new(3,7,3,3)});row.Controls.Add(input);}
    private Button Button(string text,Func<CancellationToken,Task> action)
    {var button=new Button{Text=text,AutoSize=true};button.Click+=async(_,_)=>await run(action);return button;}
    private Button Button(string text,Func<Task> action)=>Button(text,_=>action());
    private static void Add(TableLayoutPanel p,Control c,bool grow=false)
    {int row=p.RowCount++;p.RowStyles.Add(new RowStyle(grow?SizeType.Percent:SizeType.AutoSize,grow?50:0));p.Controls.Add(c,0,row);c.Margin=new(3,5,3,5);}
    private void Column(string property,string title)=>rows.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=property,HeaderText=title,SortMode=typesOnly?DataGridViewColumnSortMode.Automatic:DataGridViewColumnSortMode.NotSortable,AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill});

    internal void ApplyCatalog(StudyConfigurationCatalog data,string currentConnection)
    {
        bool changed=loadedConnection!=currentConnection;
        var previousTicker=(ticker.SelectedItem as StudyTicker)?.Id;
        var previousInterval=(interval.SelectedItem as CompatibleStudyInterval)?.Interval.Id;
        string previousType=collection.SelectedValue as string??"";
        data=data with{Studies=data.Studies.OrderBy(s=>s.OrderSeq).ThenBy(s=>s.Id).ToList()};
        catalog=data;loadedConnection=currentConnection;binding=true;
        try
        {
            rows.DataSource=typesOnly?(object)data.ColTypes:data.Studies;
            ticker.DataSource=data.Tickers;ticker.DisplayMember=nameof(StudyTicker.Symbol);ticker.ValueMember=nameof(StudyTicker.Id);
            collection.DataSource=data.ColTypes;
            if(previousTicker.HasValue)ticker.SelectedValue=previousTicker.Value;
            UpdateIntervals(previousInterval);
            if(!string.IsNullOrEmpty(previousType))collection.SelectedValue=previousType;
            rows.ClearSelection();rows.CurrentCell=null;
            if(typesOnly)
            {
                var selected=data.ColTypes.SingleOrDefault(t=>t.Type==originalType);
                if(!changed&&selected is not null)
                {
                    var gridRow=rows.Rows.Cast<DataGridViewRow>().FirstOrDefault(r=>(r.DataBoundItem as CollectionType)?.Type==originalType);
                    if(gridRow is not null){rows.CurrentCell=gridRow.Cells[0];gridRow.Selected=true;}EditType(selected);
                }
                else New();
            }
            else
            {
                var selected=data.Studies.SingleOrDefault(s=>s.Id==studyId);
                if(!changed&&selected is not null)
                {
                    var gridRow=rows.Rows.Cast<DataGridViewRow>().FirstOrDefault(r=>(r.DataBoundItem as StudyConfigurationRow)?.Id==studyId);
                    if(gridRow is not null){rows.CurrentCell=gridRow.Cells[0];gridRow.Selected=true;}
                    Edit(selected);
                }
                else New();
            }
            status.Text=typesOnly?$"{data.ColTypes.Count} collection type(s). Select a row to edit. Types in use cannot be removed or renamed.":
                $"{data.Studies.Count} StudyCol row(s). Select a row to edit, or use Add.";
        }
        finally{binding=false;UpdateStudyActions();}
    }
    public async Task ReloadAsync(CancellationToken ct=default)
    {
        var cs=connection();
        try
        {
            var discovered=plugins?.Invoke();if(discovered is not null)await MarketLivePlugins.SyncAsync(cs,discovered,ct);
            var data=await new StudyConfigurationStore(cs).ReadAsync(ct);
            if(discovered is not null)data=data with{ColTypes=data.ColTypes.Where(t=>discovered.Registrations.Any(r=>r.ColType.Equals(t.Type,StringComparison.OrdinalIgnoreCase))).ToList()};
            ApplyCatalog(data,cs);
        }
        catch(OperationCanceledException){throw;}
        catch(Exception ex){throw new InvalidDataException("Unable to load study configuration. Use Test database and schema on the Database tab, including the ColType table. "+ex.Message,ex);}
    }
    private void New()
    {
        bool prior=binding;binding=true;
        try
        {
            studyId=0;originalType=null;typeName.Clear();dll.Clear();description.Clear();json.Text="{}";ClearJsonResult();enabled.Checked=true;rows.ClearSelection();rows.CurrentCell=null;
            if(!typesOnly){ticker.SelectedIndex=-1;UpdateIntervals();collection.SelectedIndex=-1;editorTitle.Text="New StudyCol";}
            cleanDraft=Draft();if(typesOnly)editorTitle.Text="New Collection Type";
        }
        finally{binding=prior;UpdateStudyActions();}
    }
    private string Draft()=>typesOnly?System.Text.Json.JsonSerializer.Serialize(new{Name=typeName.Text,Dll=dll.Text,Description=description.Text}):System.Text.Json.JsonSerializer.Serialize(new{Ticker=(ticker.SelectedItem as StudyTicker)?.Id,Interval=(interval.SelectedItem as CompatibleStudyInterval)?.Interval.Id,Type=(collection.SelectedItem as CollectionType)?.Type,Json=json.Text,Enabled=enabled.Checked});
    private void ClearJsonResult(){jsonResult.Image=null;jsonResult.AccessibleDescription="Not validated";}
    private bool ConfirmDiscardChanges(string reason)
    {
        if(!HasUnsavedChanges)return true;
        if(confirmDiscard is not null)return confirmDiscard(reason);
        return MessageBox.Show(FindForm(),$"The entered {(typesOnly?"Collection Type information":studyId==0?"new StudyCol information":"StudyCol changes")} has not been saved and will be lost when {reason}.\n\nDiscard it and continue?", "Unsaved configuration",MessageBoxButtons.OKCancel,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)==DialogResult.OK;
    }
    public bool DiscardChangesForNavigation(string reason)
    {
        if(!HasUnsavedChanges)return true;
        if(!ConfirmDiscardChanges(reason))return false;
        New();return true;
    }
    private void RestoreSelection()
    {
        // Grid selection events cannot safely reset CurrentCell synchronously.
        binding=true;
        void Restore()
        {
            if(IsDisposed)return;
            try
            {
                rows.ClearSelection();rows.CurrentCell=null;
                var previous=rows.Rows.Cast<DataGridViewRow>().FirstOrDefault(r=>typesOnly?(r.DataBoundItem as CollectionType)?.Type==originalType:(r.DataBoundItem as StudyConfigurationRow)?.Id==studyId);
                if(previous is not null){rows.CurrentCell=previous.Cells[0];previous.Selected=true;}
            }
            finally{binding=false;UpdateStudyActions();}
        }
        if(IsHandleCreated)BeginInvoke((Action)Restore);else Restore();
    }
    private void UpdateStudyActions()
    {
        if(binding)return;
        if(typesOnly)
        {
            removeStudy.Enabled=originalType is not null&&rows.CurrentRow?.Selected==true&&(rows.CurrentRow.DataBoundItem as CollectionType)?.Type==originalType;
            saveStudy.Enabled=catalog is not null&&loadedConnection==connection()&&Draft()!=cleanDraft&&!string.IsNullOrWhiteSpace(typeName.Text)&&!string.IsNullOrWhiteSpace(dll.Text);return;
        }
        bool hasSelection=studyId>0&&rows.CurrentRow?.Selected==true&&rows.CurrentRow.DataBoundItem is StudyConfigurationRow selected&&selected.Id==studyId;
        removeStudy.Enabled=hasSelection;
        int position=catalog?.Studies.FindIndex(s=>s.Id==studyId)??-1;
        moveUp.Enabled=hasSelection&&loadedConnection==connection()&&position>0;
        moveDown.Enabled=hasSelection&&loadedConnection==connection()&&position>=0&&position<catalog!.Studies.Count-1;
        saveStudy.Enabled=catalog is not null&&loadedConnection==connection()&&Draft()!=cleanDraft&&ticker.SelectedItem is StudyTicker&&interval.SelectedItem is CompatibleStudyInterval&&collection.SelectedItem is CollectionType;
    }
    private void UpdateIntervals(string? select=null)
    {
        interval.DataSource=catalog is not null&&ticker.SelectedItem is StudyTicker selected
            ?StudyIntervalCompatibility.Choices(selected.Id,catalog.Intervals,catalog.DataSets):[];
        if(select is not null)
        {var choice=((List<CompatibleStudyInterval>)interval.DataSource).Find(i=>i.Interval.Id==select);interval.SelectedItem=choice;}
        UpdateSource();
    }
    private void UpdateSource()=>source.Text=interval.SelectedItem is CompatibleStudyInterval i
        ?$"Source DataSet {i.Source.Id} ({i.Source.IntervalId}); "+(i.Aggregate?"OHLCV aggregation required.":"direct dataset."):
        "No compatible dataset/interval is available for this ticker.";
    private void Edit(StudyConfigurationRow row)
    {
        bool prior=binding;binding=true;
        try{studyId=row.Id;ticker.SelectedValue=row.TickerId;UpdateIntervals(row.IntervalId);collection.SelectedValue=row.ColType;json.Text=row.ColParams;ClearJsonResult();enabled.Checked=row.Enabled;editorTitle.Text=$"Editing StudyCol {row.Id}";cleanDraft=Draft();}
        finally{binding=prior;UpdateStudyActions();}
    }
    private void EditType(CollectionType type)
    {
        bool prior=binding;binding=true;
        try{originalType=type.Type;typeName.Text=type.Type;dll.Text=type.DLLName;description.Text=type.Description;editorTitle.Text=$"Editing Collection Type {type.Type}";cleanDraft=Draft();}
        finally{binding=prior;UpdateStudyActions();}
    }
    private void RequireLoaded()
    {if(catalog is null||loadedConnection!=connection())throw new InvalidDataException("Reload this editor for the current database before saving.");}
    private async Task SaveAsync(CancellationToken ct)
    {
        RequireLoaded();var store=new StudyConfigurationStore(connection());
        if(typesOnly)
        {
            await store.SaveTypeAsync(originalType,new(typeName.Text.Trim(),dll.Text.Trim(),description.Text),ct);
            originalType=typeName.Text.Trim();await ReloadAsync(ct);status.Text="ColType saved.";
        }
        else
        {
            var selected=ticker.SelectedItem as StudyTicker??throw new InvalidDataException("Select a ticker with a configured dataset.");
            var target=interval.SelectedItem as CompatibleStudyInterval??throw new InvalidDataException("Select a compatible interval.");
            var type=collection.SelectedItem as CollectionType??throw new InvalidDataException("Select a loaded collection. Check Collection Types for plugin load errors.");
            studyId=await store.SaveStudyAsync(new(studyId,selected.Id,target.Interval.Id,type.Type,json.Text,enabled.Checked),ct);
            await ReloadAsync(ct);status.Text=$"StudyCol {studyId} saved. OrderSeq = {catalog!.Studies.Single(s=>s.Id==studyId).OrderSeq}.";
        }
    }
    private async Task MoveAsync(int direction,CancellationToken ct)
    {
        RequireLoaded();
        // RunAsync disables the parent tab while executing; control Enabled is not selection state.
        if(studyId<1||rows.CurrentRow?.Selected!=true||rows.CurrentRow.DataBoundItem is not StudyConfigurationRow selected||selected.Id!=studyId)return;
        if(!ConfirmDiscardChanges("changing study processing order"))return;
        await new StudyOrderStore(connection()).MoveAsync(studyId,direction,catalog!.Studies,ct);
        await ReloadAsync(ct);status.Text="Study processing order saved. Running studies use it on their next pass; restart studies to apply immediately.";
    }
    private async Task RemoveTypeAsync(CancellationToken ct)
    {
        RequireLoaded();if(originalType is null)throw new InvalidDataException("Select a saved ColType first.");
        if(MessageBox.Show(FindForm(),$"Remove ColType '{originalType}'? Removal will be refused if any StudyCol uses it.","Remove ColType",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
        await new StudyConfigurationStore(connection()).DeleteTypeAsync(originalType,ct);New();await ReloadAsync(ct);status.Text="ColType removed.";
    }
    private async Task RemoveStudyAsync(CancellationToken ct)
    {
        RequireLoaded();if(studyId<1||rows.CurrentRow?.Selected!=true||rows.CurrentRow.DataBoundItem is not StudyConfigurationRow selected||selected.Id!=studyId)throw new InvalidDataException("Select a StudyCol first.");
        var store=new StudyCollectionDeletionStore(connection());var preview=await store.PreviewAsync(studyId,ct);
        string message=$"Delete StudyCol {studyId}?\n\nThis collection is {(preview.Study.Enabled?"ENABLED":"disabled")}.\n"+
            $"All {preview.StudyHistoryRows:N0} StudyHistory output/checkpoint row(s) for this collection will be permanently deleted, along with its StudyCol definition.\n\nPrice datasets are kept. Continue?";
        if(MessageBox.Show(FindForm(),message,"Delete Study Collection",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
        await store.DeleteConfirmedAsync(preview,ct);studyId=0;await ReloadAsync(ct);status.Text="StudyCol and its study history deleted.";
    }
    protected override void Dispose(bool disposing)
    {base.Dispose(disposing);if(disposing&&!resourcesDisposed){resourcesDisposed=true;branding.Dispose();}}
}
