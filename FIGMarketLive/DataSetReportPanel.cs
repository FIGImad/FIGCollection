namespace FIGMarketLive;

internal sealed class DataSetReportPanel : UserControl
{
    private readonly ComboBox datasets=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=500};
    private readonly Button analyze=new(){Text="Analyze",AutoSize=true,Enabled=false};
    private readonly TextBox summary=new(){Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical};
    private readonly DataGridView first=Grid(),last=Grid();
    private readonly Func<string> connection;
    private bool loaded;
    public DataSetReportPanel(Func<string> connection,Func<Func<CancellationToken,Task>,Task> run,BrandingAssets branding)
    {
        this.connection=connection;Dock=DockStyle.Fill;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6};
        layout.ColumnStyles.Add(new(SizeType.Percent,100));
        layout.RowStyles.Add(new(SizeType.AutoSize));layout.RowStyles.Add(new(SizeType.Percent,40));
        layout.RowStyles.Add(new(SizeType.AutoSize));layout.RowStyles.Add(new(SizeType.Percent,30));
        layout.RowStyles.Add(new(SizeType.AutoSize));layout.RowStyles.Add(new(SizeType.Percent,30));
        var row=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true};
        var refresh=new Button{Text="Refresh DataSets",AutoSize=true};branding.Apply(refresh,"reload.png");branding.Apply(analyze,"research.png");
        row.Controls.AddRange([new Label{Text="DataSet",AutoSize=true,Padding=new Padding(0,8,0,0)},datasets,analyze,refresh]);
        layout.Controls.Add(row);layout.Controls.Add(summary);
        layout.Controls.Add(new Label{Text="First 3 bars (oldest first)",AutoSize=true});layout.Controls.Add(first);
        layout.Controls.Add(new Label{Text="Last 3 bars (oldest first)",AutoSize=true});layout.Controls.Add(last);Controls.Add(layout);
        datasets.SelectedIndexChanged+=(_,_)=>{Clear();analyze.Enabled=datasets.SelectedItem is DataSetReportChoice;};
        refresh.Click+=async(_,_)=>await run(RefreshAsync);
        analyze.Click+=async(_,_)=>await run(AnalyzeAsync);
        summary.Text="Select a dataset, then Analyze. All dates are displayed in the computer's local timezone.";
    }
    private static DataGridView Grid()=>new(){Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,AutoGenerateColumns=true};
    private void Clear(){summary.Clear();first.DataSource=null;last.DataSource=null;}
    internal async Task EnsureLoadedAsync(CancellationToken ct){if(!loaded)await RefreshAsync(ct);}
    private async Task RefreshAsync(CancellationToken ct)
    {
        int? selected=(datasets.SelectedItem as DataSetReportChoice)?.Id;Clear();
        var rows=await new DataSetReportStore(connection()).ListAsync(ct);datasets.DataSource=rows;
        if(rows.FirstOrDefault(d=>d.Id==selected) is {} previous)datasets.SelectedItem=previous;
        loaded=true;analyze.Enabled=rows.Count>0;
        summary.Text=rows.Count==0?"No datasets configured. Add one in Configuration → Ticker and DataSet.":"Select a dataset, then Analyze.";
    }
    private async Task AnalyzeAsync(CancellationToken ct)
    {
        Clear();if(datasets.SelectedItem is not DataSetReportChoice selected)throw new InvalidDataException("Select a dataset.");
        var report=await new DataSetReportStore(connection()).AnalyzeAsync(selected.Id,ct);var t=report.Ticker;
        string Time(long raw)=>DateTimeOffset.FromUnixTimeSeconds(raw).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz");
        summary.Text=$"DataSet: {report.DataSetId}    Bars: {report.BarCount:N0}\r\nInterval: {report.IntervalName} (ID {report.IntervalId}; {report.IntervalSeconds:N0} seconds)    Price Sync: {(report.Monitor?"Enabled":"Disabled")}    Study collections using source: {report.StudyCollections}\r\n"+
            $"Ticker: {t.Symbol} (ID {t.Id})    Local symbol: {t.LocalSymbol}\r\nName: {t.Name}\r\nType: {t.SecurityType}    Exchange: {t.Exchange}    Currency: {t.Currency}    Expiry: {(t.SecurityType=="STK"||t.ExpiryDate<=0?"n/a":t.ExpiryDate.ToString())}\r\nMinimum tick: {t.MinTick}    Contract size: {t.ContractSize}\r\n"+
            $"Local timezone: {TimeZoneInfo.Local.DisplayName}\r\n"+
            (report.BarCount==0?"No PriceData bars stored.":$"First bar: {Time(report.FirstBars[0].RawTime)}\r\nLast bar: {Time(report.LastBars[^1].RawTime)}");
        // Bind a typed list so the grid retains columns even for an empty dataset.
        first.DataSource=report.FirstBars.Select(b=>new ReportRow(b.RawTime,Time(b.RawTime),b.Open,b.High,b.Low,b.Close,b.Volume)).ToList();
        last.DataSource=report.LastBars.Select(b=>new ReportRow(b.RawTime,Time(b.RawTime),b.Open,b.High,b.Low,b.Close,b.Volume)).ToList();
        foreach(var grid in new[]{first,last})
        {
            if(grid.Columns[nameof(ReportRow.LocalTime)] is {} timeColumn)timeColumn.FillWeight=210;
            foreach(string column in new[]{"Open","High","Low","Close"})if(grid.Columns[column] is {} priceColumn)priceColumn.DefaultCellStyle.Format="0.##########";
        }
    }
    private sealed record ReportRow(long RawTime,string LocalTime,decimal Open,decimal High,decimal Low,decimal Close,long Volume);
}
