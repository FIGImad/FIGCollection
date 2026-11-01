namespace FIGMarketLive;

internal sealed class ExchangeSchedulePanel : UserControl
{
    private readonly Func<string> connection;
    private readonly DataGridView grid=new(){Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=false,AllowUserToAddRows=false,AllowUserToDeleteRows=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false};
    private readonly ComboBox exchange=new(){Width=180,DropDownStyle=ComboBoxStyle.DropDown,AutoCompleteMode=AutoCompleteMode.SuggestAppend,AutoCompleteSource=AutoCompleteSource.ListItems,AccessibleName="Schedule exchange"};
    private readonly CheckBox enabled=new(){Text="Enable price updates during open hours",AutoSize=true};
    private readonly ComboBox zone=new(){Width=490,DropDownStyle=ComboBoxStyle.DropDownList};
    private readonly ComboBox closeDay=new(){Width=120,DropDownStyle=ComboBoxStyle.DropDownList};
    private readonly ComboBox openDay=new(){Width=120,DropDownStyle=ComboBoxStyle.DropDownList};
    private readonly DateTimePicker weeklyClose=Time(),weeklyOpen=Time(),dailyClose=Time(),dailyOpen=Time();
    private readonly Label status=new(){AutoSize=true,MaximumSize=new(800,0)};
    private readonly Label title=new(){Text="New Exchange Schedule",AutoSize=true,Font=new Font(SystemFonts.DefaultFont,FontStyle.Bold)};
    private readonly BrandingAssets branding=new();
    private readonly ToolStrip toolbar=new(){Dock=DockStyle.Fill,GripStyle=ToolStripGripStyle.Hidden,ImageScalingSize=new(20,20)};
    private readonly ToolStripButton remove=new(){Text="Remove",ToolTipText="Delete Exchange Schedule",DisplayStyle=ToolStripItemDisplayStyle.Image,Enabled=false};
    private readonly ToolStripButton save=new(){Text="Save",ToolTipText="Save Exchange Schedule",DisplayStyle=ToolStripItemDisplayStyle.Image,Enabled=false};
    private readonly Func<string,bool>? confirmDiscard;
    private string? originalExchange;
    private string cleanDraft="";
    private bool binding,resourcesDisposed;
    private string? loadedConnection;
    public bool NeedsReload=>loadedConnection!=connection();
    public bool HasUnsavedChanges=>Draft()!=cleanDraft;
    public ExchangeSchedulePanel(Func<string> connection,Func<Func<CancellationToken,Task>,Task> run,Func<string,bool>? confirmDiscard=null)
    {
        this.connection=connection;Dock=DockStyle.Fill;
        this.confirmDiscard=confirmDiscard;
        foreach(var (property,title,width) in new[]{("Exchange","Exchange",90),("TimeZoneId","Timezone",180),("WeeklyBreak","Weekly closure",260),("DailyBreak","Daily break",130)})
            grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=property,HeaderText=title,Width=width});
        grid.Columns.Insert(1,new DataGridViewCheckBoxColumn{DataPropertyName="Enabled",HeaderText="Enabled",Width=65});
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,AutoScroll=true};
        void Add(Control control,bool grow=false){int row=layout.RowCount++;layout.RowStyles.Add(new(grow?SizeType.Percent:SizeType.AutoSize,grow?100:0));layout.Controls.Add(control,0,row);control.Margin=new(3,5,3,5);}
        ToolStripButton Action(string text,string tooltip,string file,Func<CancellationToken,Task> action)
        {var button=new ToolStripButton(text,branding.Image(file)){ToolTipText=tooltip,DisplayStyle=ToolStripItemDisplayStyle.Image};button.Click+=async(_,_)=>await run(action);return button;}
        toolbar.Items.Add(Action("Add","Add New Exchange Schedule","add.png",_=>{if(ConfirmDiscard("starting a new schedule"))New();return Task.CompletedTask;}));
        remove.Image=branding.Image("trash.png");remove.Click+=async(_,_)=>await run(RemoveAsync);toolbar.Items.Add(remove);
        save.Image=branding.Image("save.png");save.Click+=async(_,_)=>await run(SaveAsync);toolbar.Items.Add(save);
        toolbar.Items.Add(new ToolStripSeparator());toolbar.Items.Add(Action("Refresh","Reload Exchange Schedules","reload.png",async ct=>{if(ConfirmDiscard("reloading schedules"))await ReloadAsync(ct);}));
        BrandingAssets.StyleToolbar(toolbar);Add(toolbar);Add(grid,true);Add(status);Add(title);
        Add(new Label{AutoSize=true,MaximumSize=new(830,0),Text="All times are exchange-local. Weekly and daily breaks block price sync; holiday exceptions are not included. Exchange codes are fixed for existing rows. To stop sync for an exchange used by tickers, disable its schedule."});
        var fields=new TableLayoutPanel{ColumnCount=2,AutoSize=true,Dock=DockStyle.Fill};
        void Field(string label,Control value){int row=fields.RowCount++;fields.Controls.Add(new Label{Text=label,AutoSize=true,Margin=new(3,7,10,3)},0,row);fields.Controls.Add(value,1,row);}
        zone.BindingContext=new BindingContext();zone.DisplayMember=nameof(TimeZoneInfo.DisplayName);zone.ValueMember=nameof(TimeZoneInfo.Id);zone.DataSource=TimeZoneInfo.GetSystemTimeZones().ToList();
        closeDay.Items.AddRange(Enum.GetNames<DayOfWeek>());openDay.Items.AddRange(Enum.GetNames<DayOfWeek>());
        Field("Exchange",exchange);Field("Enabled",enabled);Field("Timezone",zone);
        FlowLayoutPanel Pair(params Control[] values){var pair=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};pair.Controls.AddRange(values);return pair;}
        Field("Weekly break starts",Pair(closeDay,weeklyClose));Field("Weekly break ends",Pair(openDay,weeklyOpen));
        Field("Daily break starts / ends",Pair(dailyClose,new Label{Text="to",AutoSize=true},dailyOpen));Add(fields);Controls.Add(layout);
        void SelectionUpdated(object? sender,EventArgs args)
        {
            if(binding)return;
            if(grid.CurrentRow?.DataBoundItem is ExchangePriceSchedule row&&row.Exchange!=originalExchange)
            {
                if(!ConfirmDiscard("selecting another exchange schedule")){RestoreSelection();return;}Edit(row);
            }
            UpdateActions();
        }
        grid.SelectionChanged+=SelectionUpdated;grid.CurrentCellChanged+=SelectionUpdated;
        exchange.TextChanged+=(_,_)=>UpdateActions();zone.SelectedIndexChanged+=(_,_)=>UpdateActions();enabled.CheckedChanged+=(_,_)=>UpdateActions();
        closeDay.SelectedIndexChanged+=(_,_)=>UpdateActions();openDay.SelectedIndexChanged+=(_,_)=>UpdateActions();
        foreach(var clock in new[]{weeklyClose,weeklyOpen,dailyClose,dailyOpen})clock.ValueChanged+=(_,_)=>UpdateActions();
        New();
    }
    private static DateTimePicker Time()=>new(){Format=DateTimePickerFormat.Custom,CustomFormat="HH:mm",ShowUpDown=true,Width=90};
    private void Edit(ExchangePriceSchedule row,bool isNew=false)
    {
        bool prior=binding;binding=true;
        try
        {
        originalExchange=isNew?null:row.Exchange;title.Text=isNew?"New Exchange Schedule":$"Editing Exchange Schedule {row.Exchange}";
        exchange.Text=row.Exchange;enabled.Checked=row.Enabled;zone.SelectedValue=row.TimeZoneId;
        exchange.Enabled=isNew;
        closeDay.SelectedItem=row.WeeklyBreakCloseDay;openDay.SelectedItem=row.WeeklyBreakOpenDay;
        DateTime Value(string time)=>DateTime.Today+TimeSpan.Parse(time,System.Globalization.CultureInfo.InvariantCulture);
        weeklyClose.Value=Value(row.WeeklyBreakCloseTime);weeklyOpen.Value=Value(row.WeeklyBreakOpenTime);dailyClose.Value=Value(row.DailyBreakCloseTime);dailyOpen.Value=Value(row.DailyBreakOpenTime);
        cleanDraft=Draft();
        }
        finally{binding=prior;UpdateActions();}
    }
    private void New()
    {
        bool prior=binding;binding=true;
        try{grid.ClearSelection();grid.CurrentCell=null;Edit(new("",false,"UTC","Friday","16:00","Sunday","17:00","16:00","17:00"),true);}
        finally{binding=prior;UpdateActions();}
    }
    internal void ApplyRows(List<ExchangePriceSchedule> rows)
    {
        binding=true;
        try
        {
            grid.DataSource=rows;loadedConnection=connection();grid.ClearSelection();grid.CurrentCell=null;
            var selected=rows.SingleOrDefault(r=>r.Exchange==originalExchange);
            if(selected is null)New();
            else
            {var gridRow=grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r=>(r.DataBoundItem as ExchangePriceSchedule)?.Exchange==originalExchange);if(gridRow is not null){grid.CurrentCell=gridRow.Cells[0];gridRow.Selected=true;}Edit(selected);}
            status.Text=$"{rows.Count} exchange schedule(s). Select a row to edit, or use Add.";
        }
        finally{binding=false;UpdateActions();}
    }
    internal void ApplyExchanges(IEnumerable<string> exchanges)
    {
        string current=exchange.Text;exchange.Items.Clear();
        exchange.Items.AddRange(exchanges.Where(e=>!string.IsNullOrWhiteSpace(e)).Select(e=>e.Trim().ToUpperInvariant()).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).Cast<object>().ToArray());
        exchange.Text=current;
    }
    public async Task ReloadAsync(CancellationToken ct)
    {
        var store=new ExchangeScheduleStore(connection());await store.EnsureSchemaAsync(ct);
        ApplyExchanges(await store.ExchangesAsync(ct));ApplyRows(await store.ListAsync(ct));
    }
    public async Task RefreshExchangesAsync(CancellationToken ct)
        =>ApplyExchanges(await new ExchangeScheduleStore(connection()).ExchangesAsync(ct));
    private static string Clock(DateTimePicker value)=>value.Value.ToString("HH:mm",System.Globalization.CultureInfo.InvariantCulture);
    private string Draft()=>System.Text.Json.JsonSerializer.Serialize(new{Exchange=exchange.Text,Enabled=enabled.Checked,Zone=zone.SelectedValue as string,CloseDay=closeDay.Text,Close=Clock(weeklyClose),OpenDay=openDay.Text,Open=Clock(weeklyOpen),DailyClose=Clock(dailyClose),DailyOpen=Clock(dailyOpen)});
    private void UpdateActions()
    {
        if(binding)return;
        remove.Enabled=originalExchange is not null&&grid.CurrentRow?.Selected==true&&(grid.CurrentRow.DataBoundItem as ExchangePriceSchedule)?.Exchange==originalExchange;
        save.Enabled=loadedConnection==connection()&&Draft()!=cleanDraft&&!string.IsNullOrWhiteSpace(exchange.Text)&&zone.SelectedValue is string;
    }
    private bool ConfirmDiscard(string reason)=>!HasUnsavedChanges||(confirmDiscard?.Invoke(reason)??MessageBox.Show(FindForm(),$"The entered exchange schedule information has not been saved and will be lost when {reason}.\n\nDiscard it and continue?","Unsaved Exchange Schedule",MessageBoxButtons.OKCancel,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)==DialogResult.OK);
    public bool DiscardChangesForNavigation(string reason)
    {if(!HasUnsavedChanges)return true;if(!ConfirmDiscard(reason))return false;New();return true;}
    private void RestoreSelection()
    {
        binding=true;
        void Restore(){if(IsDisposed)return;try{grid.ClearSelection();grid.CurrentCell=null;var previous=grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r=>(r.DataBoundItem as ExchangePriceSchedule)?.Exchange==originalExchange);if(previous is not null){grid.CurrentCell=previous.Cells[0];previous.Selected=true;}}finally{binding=false;UpdateActions();}}
        if(IsHandleCreated)BeginInvoke((Action)Restore);else Restore();
    }
    private async Task RemoveAsync(CancellationToken ct)
    {
        if(originalExchange is null||grid.CurrentRow?.Selected!=true)throw new InvalidDataException("Select an exchange schedule first.");
        string selected=originalExchange;
        if(MessageBox.Show(FindForm(),$"Delete exchange schedule '{selected}'?\n\nSchedules used by configured tickers cannot be deleted; disable them instead.","Delete Exchange Schedule",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
        await new ExchangeScheduleStore(connection()).DeleteAsync(selected,ct);originalExchange=null;await ReloadAsync(ct);
    }
    private async Task SaveAsync(CancellationToken ct)
    {
        if(zone.SelectedValue is not string timeZone)throw new InvalidDataException("Select an exchange timezone.");
        var row=new ExchangePriceSchedule(exchange.Text.Trim().ToUpperInvariant(),enabled.Checked,timeZone,closeDay.Text,Clock(weeklyClose),openDay.Text,Clock(weeklyOpen),Clock(dailyClose),Clock(dailyOpen));
        await new ExchangeScheduleStore(connection()).SaveAsync(row,ct,originalExchange is null);originalExchange=row.Exchange;await ReloadAsync(ct);status.Text=$"Saved {row.Exchange}. Price sync reloads schedules before requests and publication.";
    }
    protected override void Dispose(bool disposing)
    {base.Dispose(disposing);if(disposing&&!resourcesDisposed){resourcesDisposed=true;branding.Dispose();}}
}
