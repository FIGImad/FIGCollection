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
    private StudyConfigurationCatalog? catalog;
    private string? loadedConnection;
    private int studyId;
    private string? originalType;
    private bool binding;
    public bool NeedsReload=>catalog is null||loadedConnection!=connection();

    public StudyConfigurationPanel(bool typesOnly,Func<string> connection,Func<Func<CancellationToken,Task>,Task> run)
    {
        this.typesOnly=typesOnly;this.connection=connection;this.run=run;Dock=DockStyle.Fill;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,AutoScroll=true};Controls.Add(layout);
        var actions=Flow();actions.Controls.Add(Button("Reload",()=>ReloadAsync()));actions.Controls.Add(Button("New",()=>{New();return Task.CompletedTask;}));
        actions.Controls.Add(Button(typesOnly?"Save ColType":"Save StudyCol",SaveAsync));
        if(typesOnly)actions.Controls.Add(Button("Remove ColType",RemoveTypeAsync));
        else actions.Controls.Add(Button("Format / validate JSON",()=>{json.Text=StudyIntervalCompatibility.NormalizeParameters(json.Text);status.Text="JSON parameters are valid.";return Task.CompletedTask;}));
        Add(layout,actions);
        if(typesOnly)
        {
            var nameRow=Flow();Field(nameRow,"Type",typeName);Field(nameRow,"DLL name / path",dll);Add(layout,nameRow);
            Add(layout,new Label{Text="Description",AutoSize=true});Add(layout,description);
            foreach(var key in new[]{"Type","DLLName","Description"})Column(key,key);
        }
        else
        {
            var instrumentRow=Flow();Field(instrumentRow,"Ticker",ticker);Field(instrumentRow,"Interval",interval);Add(layout,instrumentRow);
            var typeRow=Flow();collection.DisplayMember=nameof(CollectionType.Type);collection.ValueMember=nameof(CollectionType.Type);Field(typeRow,"ColType",collection);typeRow.Controls.Add(enabled);Add(layout,typeRow);
            Add(layout,source);Add(layout,new Label{Text="ColParams (JSON object). OrderSeq is always 1.",AutoSize=true});Add(layout,json,true);
            foreach(var (key,title) in new[]{("Id","StudyCol"),("Symbol","Ticker"),("IntervalName","Interval"),("ColType","ColType"),("Enabled","Enabled")})Column(key,title);
            ticker.SelectedIndexChanged+=(_,_)=>{if(!binding)UpdateIntervals();};
            interval.SelectedIndexChanged+=(_,_)=>UpdateSource();
        }
        Add(layout,status);Add(layout,rows,true);
        rows.SelectionChanged+=(_,_)=>
        {
            if(binding)return;
            if(typesOnly&&rows.CurrentRow?.DataBoundItem is CollectionType type)
            {originalType=type.Type;typeName.Text=type.Type;dll.Text=type.DLLName;description.Text=type.Description;}
            else if(!typesOnly&&rows.CurrentRow?.DataBoundItem is StudyConfigurationRow row)Edit(row);
        };
    }
    private static FlowLayoutPanel Flow()=>new(){AutoSize=true,Dock=DockStyle.Fill,WrapContents=true};
    private static void Field(FlowLayoutPanel row,string label,Control input)
    {row.Controls.Add(new Label{Text=label,AutoSize=true,Margin=new(3,7,3,3)});row.Controls.Add(input);}
    private Button Button(string text,Func<CancellationToken,Task> action)
    {var button=new Button{Text=text,AutoSize=true};button.Click+=async(_,_)=>await run(action);return button;}
    private Button Button(string text,Func<Task> action)=>Button(text,_=>action());
    private static void Add(TableLayoutPanel p,Control c,bool grow=false)
    {int row=p.RowCount++;p.RowStyles.Add(new RowStyle(grow?SizeType.Percent:SizeType.AutoSize,grow?50:0));p.Controls.Add(c,0,row);c.Margin=new(3,5,3,5);}
    private void Column(string property,string title)=>rows.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=property,HeaderText=title,AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill});

    internal void ApplyCatalog(StudyConfigurationCatalog data,string currentConnection)
    {
        bool changed=loadedConnection!=currentConnection;
        var previousTicker=(ticker.SelectedItem as StudyTicker)?.Id;
        var previousInterval=(interval.SelectedItem as CompatibleStudyInterval)?.Interval.Id;
        string previousType=collection.SelectedValue as string??"";
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
            if(changed)New();
            status.Text=typesOnly?$"{data.ColTypes.Count} collection type(s). Select a row to edit. Types in use cannot be removed or renamed.":
                $"{data.Studies.Count} StudyCol row(s). Select a row to edit, or use New.";
        }
        finally{binding=false;}
    }
    public async Task ReloadAsync(CancellationToken ct=default)
    {
        var cs=connection();
        try{ApplyCatalog(await new StudyConfigurationStore(cs).ReadAsync(ct),cs);}
        catch(OperationCanceledException){throw;}
        catch(Exception ex){throw new InvalidDataException("Unable to load study configuration. Use Test database and schema on the Database tab, including the ColType table. "+ex.Message,ex);}
    }
    private void New()
    {studyId=0;originalType=null;typeName.Clear();dll.Clear();description.Clear();json.Text="{}";enabled.Checked=true;rows.ClearSelection();rows.CurrentCell=null;status.Text=typesOnly?"New ColType":"New StudyCol";}
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
        binding=true;
        try{studyId=row.Id;ticker.SelectedValue=row.TickerId;UpdateIntervals(row.IntervalId);collection.SelectedValue=row.ColType;json.Text=row.ColParams;enabled.Checked=row.Enabled;status.Text=$"Editing StudyCol {row.Id}";}
        finally{binding=false;}
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
            var type=collection.SelectedItem as CollectionType??throw new InvalidDataException("Select a ColType. Add it in the ColType tab if needed.");
            studyId=await store.SaveStudyAsync(new(studyId,selected.Id,target.Interval.Id,type.Type,json.Text,enabled.Checked),ct);
            await ReloadAsync(ct);status.Text=$"StudyCol {studyId} saved. OrderSeq = 1.";
        }
    }
    private async Task RemoveTypeAsync(CancellationToken ct)
    {
        RequireLoaded();if(originalType is null)throw new InvalidDataException("Select a saved ColType first.");
        if(MessageBox.Show(FindForm(),$"Remove ColType '{originalType}'? Removal will be refused if any StudyCol uses it.","Remove ColType",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
        await new StudyConfigurationStore(connection()).DeleteTypeAsync(originalType,ct);New();await ReloadAsync(ct);status.Text="ColType removed.";
    }
}
