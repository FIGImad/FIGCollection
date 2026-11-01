namespace FIGMarketLive;

/// <summary>Tools are independent panels so additional import/maintenance tools can be added later.</summary>
internal sealed class ImportPricesPanel : UserControl
{
    private readonly TextBox file=new(){Width=550,ReadOnly=true};
    private readonly ComboBox ticker=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=300};
    private readonly ComboBox zone=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=550};
    private readonly TextBox preview=new(){Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical};
    private readonly Button import=new(){Text="Import reviewed prices…",AutoSize=true,Enabled=false};
    private readonly Func<string> connection;
    private readonly Func<Func<CancellationToken,Task>,Task> run;
    private readonly Func<CancellationToken,Task<List<StudyTicker>>> loadTickers;
    private PriceImportPlan? plan;
    private string? plannedConnection;
    internal Func<string,Task>? BeforeImportAsync;

    public ImportPricesPanel(Func<string> connection,Func<Func<CancellationToken,Task>,Task> run,
        Func<CancellationToken,Task<List<StudyTicker>>>? loadTickers=null)
    {
        this.connection=connection;this.run=run;Dock=DockStyle.Fill;
        this.loadTickers=loadTickers??(ct=>new ESignalImportService(connection()).TickersAsync(ct));
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=7};
        layout.RowStyles.Add(new(SizeType.AutoSize));layout.RowStyles.Add(new(SizeType.AutoSize));layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.AutoSize));layout.RowStyles.Add(new(SizeType.AutoSize));layout.RowStyles.Add(new(SizeType.AutoSize));layout.RowStyles.Add(new(SizeType.Percent,100));
        layout.Controls.Add(new Label{Text="Import e-Signal Prices",AutoSize=true,Font=new Font(Font,FontStyle.Bold)});
        var browse=new Button{Text="Select e-Signal file…",AutoSize=true};
        browse.Click+=(_,_)=>{using var dialog=new OpenFileDialog{Filter="e-Signal CSV/text (*.csv;*.txt)|*.csv;*.txt|All files (*.*)|*.*"};if(dialog.ShowDialog(this)==DialogResult.OK){file.Text=dialog.FileName;InvalidatePreview();}};
        layout.Controls.Add(Row(browse,file));
        var load=new Button{Text="Refresh Tickers",AutoSize=true};
        load.Click+=async(_,_)=>await run(RefreshTickersAsync);
        layout.Controls.Add(Row(new Label{Text="Ticker",AutoSize=true},ticker,load));
        zone.BindingContext=new BindingContext();zone.DisplayMember=nameof(TimeZoneInfo.DisplayName);
        var zones=TimeZoneInfo.GetSystemTimeZones().ToList();
        if(!zones.Any(z=>z.Id==TimeZoneInfo.Local.Id))zones.Add(TimeZoneInfo.Local);
        zone.DataSource=zones;zone.SelectedItem=zones.Single(z=>z.Id==TimeZoneInfo.Local.Id);
        layout.Controls.Add(Row(new Label{Text="Source timezone",AutoSize=true},zone));
        layout.Controls.Add(new Label{AutoSize=true,MaximumSize=new(820,0),Text="Times are converted to UTC. Local timezone is selected by default. Ambiguous/nonexistent daylight-saving times are rejected. Files are sorted oldest first; interval detection uses the smallest spacing, allowing multiples for gaps."});
        var inspect=new Button{Text="Read file / preview import",AutoSize=true};inspect.Click+=async(_,_)=>await run(PreviewAsync);
        import.Click+=async(_,_)=>await run(ImportAsync);
        layout.Controls.Add(Row(inspect,import));layout.Controls.Add(preview);Controls.Add(layout);
        ticker.SelectedIndexChanged+=(_,_)=>InvalidatePreview();zone.SelectedIndexChanged+=(_,_)=>InvalidatePreview();
    }

    private static FlowLayoutPanel Row(params Control[] controls)
    {var row=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,WrapContents=true};row.Controls.AddRange(controls);return row;}
    private void InvalidatePreview(){plan=null;import.Enabled=false;preview.Text="Select a file and ticker, then preview the import.";}
    internal async Task RefreshTickersAsync(CancellationToken ct)
    {
        int? selected=(ticker.SelectedItem as StudyTicker)?.Id;
        InvalidatePreview();preview.Text="Loading tickers…";
        ticker.DataSource=null;
        var rows=await loadTickers(ct);ticker.DataSource=rows;
        if(selected is not null&&rows.FirstOrDefault(t=>t.Id==selected) is {} previous)ticker.SelectedItem=previous;
        preview.Text=rows.Count==0?"No tickers configured. Add a ticker in Configuration first.":"Select a file and ticker, then preview the import.";
    }

    private async Task PreviewAsync(CancellationToken ct)
    {
        InvalidatePreview();
        if(ticker.SelectedItem is not StudyTicker selected)throw new InvalidDataException("Refresh tickers and select the ticker for this file.");
        if(zone.SelectedItem is not TimeZoneInfo timezone)throw new InvalidDataException("Select the source timezone.");
        string source=file.Text;plannedConnection=connection();preview.Text="Reading and validating file…";
        var prices=await Task.Run(()=>ESignalImportService.ReadFile(source,timezone,ct),ct);
        plan=await Task.Run(()=>new ESignalImportService(plannedConnection).PreviewAsync(selected.Id,prices,ct),ct);
        preview.Text=Describe(plan);import.Enabled=plan.Action!=PriceImportAction.NoChange;
    }

    internal static string Describe(PriceImportPlan p)
    {
        string Utc(long? raw)=>raw is null?"none":DateTimeOffset.FromUnixTimeSeconds(raw.Value).ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
        string action=p.Action switch
        {
            PriceImportAction.Replace=>$"REPLACE: delete all {p.State.Count:N0} existing PriceData bars for this dataset and import the entire file.",
            PriceImportAction.Upsert=>"UPDATE/INSERT: refresh matching file timestamps and insert missing bars. Prices outside the file are kept.",
            PriceImportAction.Insert=>"INSERT: import the entire file into an empty dataset.",
            _=>"NO CHANGE: file does not extend existing coverage; import will not run."
        };
        return $"Ticker: {p.Symbol} (ID {p.TickerId})\r\nDetected interval: {p.IntervalName} ({p.File.IntervalSeconds} seconds)\r\n"+
            (p.DataSetId is null?"No dataset exists: your approval is required to create it.\r\n":$"DataSet: {p.DataSetId}\r\n")+
            $"File: {p.File.Rows.Count:N0} bars, {Utc(p.File.Rows[0].RawTime)} through {Utc(p.File.Rows[^1].RawTime)}\r\n"+
            $"Existing: {p.State.Count:N0} bars, {Utc(p.State.First)} through {Utc(p.State.Last)}\r\n\r\n{action}\r\n\r\n"+
            (p.File.HasVolume?"":"WARNING: Vol/Volume is missing. Volume will not be imported; imported rows will have Volume = 0, including refreshed existing bars.\r\n\r\n")+
            "Interval inference cannot distinguish missing bars from a larger export interval. Verify the detected interval before importing.\r\nExisting StudyHistory outputs are kept; restart Live Studies to recalculate. Replacing prices with shorter coverage may require a separate study-history rebuild.";
    }

    private async Task ImportAsync(CancellationToken ct)
    {
        var reviewed=plan??throw new InvalidDataException("Preview the file first.");
        if(connection()!=plannedConnection){InvalidatePreview();throw new InvalidDataException("Database connection changed. Preview again.");}
        string question=Describe(reviewed)+(reviewed.DataSetId is null?"\r\n\r\nCreate this dataset and import prices?":"\r\n\r\nContinue with this import?");
        if(MessageBox.Show(this,question,"Confirm e-Signal import",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
        if(BeforeImportAsync is {} before)await before("importing e-Signal prices");
        import.Enabled=false;
        int id=await Task.Run(()=>new ESignalImportService(plannedConnection!).ImportConfirmedAsync(reviewed,true,ct),ct);
        plan=null;preview.Text=$"Imported {reviewed.File.Rows.Count:N0} bars into DataSet {id}.\r\nRestart price sync and Live Studies when ready.\r\n"+(reviewed.File.HasVolume?"":"WARNING: volume was unavailable; imported volumes are zero.");
    }
}
