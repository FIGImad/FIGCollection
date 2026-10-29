using FIGMarketLive.Adapters.IBKR;
using FIGMarketLive.MarketData;
using Microsoft.Extensions.Logging.Abstractions;

namespace FIGMarketLive;

internal static class ConfigurationUiChecks
{
    public static void Run(string root)
    {
        Directory.CreateDirectory(root);
        using(var branding=new BrandingAssets())
        {
            if(BrandingAssets.SelectState(true,false,false)!=MonitorIconState.Off ||
               BrandingAssets.SelectState(true,true,false)!=MonitorIconState.Live ||
               BrandingAssets.SelectState(true,true,true)!=MonitorIconState.Warning ||
               BrandingAssets.SelectState(false,true,false)!=MonitorIconState.Off)
                throw new Exception("Tray status must require monitoring and connection, with attention taking priority.");
            using var preview=new Bitmap(300,88);
            using(var graphics=Graphics.FromImage(preview))
            {
                graphics.Clear(Color.White);
                foreach(var state in Enum.GetValues<MonitorIconState>())
                {
                    int x=(int)state*100;
                    graphics.DrawIcon(branding.TrayIcon(state),new Rectangle(x+32,8,32,32));
                    graphics.DrawString(state.ToString(),SystemFonts.DefaultFont,Brushes.Black,x+25,50);
                }
            }
            preview.Save(Path.Combine(root,"tray-icons.png"));
        }
        var path=Path.Combine(root,"settings-"+Guid.NewGuid().ToString("N")+".json");
        var settings=new MarketLiveSettings
        {
            ConnectionString="Server=localhost;Database=FIGMarketLive;Integrated Security=true",
            Adapter=new(){ProviderId="IBKR",Parameters=new(){["Host"]="127.0.0.1",["Port"]="4002",["ClientId"]="37"}},
        };
        SettingsFile.Save(path,settings);
        var testFactory=new ConnectionTestFactory();
        using(var form=new ConfigurationForm(path,new MarketDataAdapterCatalog([testFactory]),false))
        {
            form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new(-32000,-32000);
            form.Show();Application.DoEvents();
            form.DisplayDataSets([new(51,12,"NQZ6","NQZ6","FUT","1","1 Min",0)]);
            var tabs=form.Controls.OfType<TabControl>().Single();
            if(tabs.TabCount!=5)throw new Exception("Expected database, adapter, ticker, study and ColType tabs.");
            var studyCatalog=new StudyConfigurationCatalog([new(12,"NQZ6")],
                [new(){Id="1",Name="1 Min",Seq=1,IntervalLen=60},new(){Id="5",Name="5 Min",Seq=5,IntervalLen=300},new(){Id="10",Name="10 Min",Seq=10,IntervalLen=600},new(){Id="D",Name="1 Day",Seq=100,IntervalLen=86400}],
                [new(51,12,"5")],[new("L2","FIG.StudyCollections.L2.dll","L2 collection")],
                [new(201,12,"10","L2","{}",true,"NQZ6","10 Min")]);
            foreach(var panel in Descendants(form).OfType<StudyConfigurationPanel>())panel.ApplyCatalog(studyCatalog,settings.ConnectionString);
            var studyIntervals=Descendants(form).OfType<ComboBox>().Single(c=>c.DataSource is List<CompatibleStudyInterval>);
            var offered=((List<CompatibleStudyInterval>)studyIntervals.DataSource!).Select(c=>c.Interval.Id).ToArray();
            if(offered.Contains("1")||!new[]{"5","10","D"}.All(offered.Contains))throw new Exception("Study editor must offer only compatible target intervals.");
            if(Descendants(form).OfType<ComboBox>().Single(c=>c.DataSource is List<CollectionType>).Items.Count!=1)throw new Exception("Study ColType dropdown must bind to SQL catalog types.");
            tabs.SelectedIndex=2;Application.DoEvents();
            var search=Descendants(form).OfType<Button>().Single(b=>b.Text=="Search contracts");
            if(!search.Enabled)throw new Exception("Search must be available before manual database/adapter tests.");
            if(Descendants(form).OfType<Button>().Any(b=>b.Text=="Load existing DataSets"))throw new Exception("Legacy dataset loading button must be removed.");
            var saveTicker=Descendants(form).OfType<Button>().Single(b=>b.Text=="Save Ticker");
            if(!saveTicker.Parent!.Controls.OfType<ComboBox>().Any())throw new Exception("Save Ticker must sit beside the interval selector.");
            var datasetGrid=Descendants(form).OfType<DataGridView>().Single(g=>g.DataSource is List<ConfiguredDataSet>);
            if(datasetGrid.ColumnCount<6 || datasetGrid.Rows.Count!=1)throw new Exception("Configured datasets must show as a populated multi-column list.");
            form.DisplayDataSets([new(52,13,"ESZ6","ESZ6","FUT","1","1 Min",0),new(51,12,"NQZ6","NQZ6","FUT","1","1 Min",0)]);
            Application.DoEvents();
            if((datasetGrid.CurrentRow?.DataBoundItem as ConfiguredDataSet)?.DataSetId!=51)
                throw new Exception("SQL refresh must preserve dataset selection by ID when row order changes.");
            form.DisplayDataSets([new(52,13,"ESZ6","ESZ6","FUT","1","1 Min",0)]);Application.DoEvents();
            if(datasetGrid.CurrentRow is not null)throw new Exception("Removing the selected dataset must not silently select another delete target.");
            for(int i=0;i<tabs.TabCount;i++)
            {
                tabs.SelectedIndex=i;Application.DoEvents();
                using var bitmap=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));
                bitmap.Save(Path.Combine(root,$"configuration-{i}.png"));
            }
            tabs.SelectedIndex=1;Application.DoEvents();
            var result=Descendants(form).OfType<PictureBox>().Single(p=>p.AccessibleName=="Adapter connection result");
            form.TestBrokerAsync(default).GetAwaiter().GetResult();
            if(result.Image is null || result.AccessibleDescription!="Adapter connection passed")throw new Exception("Successful connection must show the success icon.");
            using(var bitmap=new Bitmap(form.Width,form.Height))
            {form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.Combine(root,"connection-success.png"));}
            testFactory.Fail=true;
            try{form.TestBrokerAsync(default).GetAwaiter().GetResult();throw new Exception("Expected failed connection.");}
            catch(IOException) { }
            if(result.Image is null || result.AccessibleDescription!="Adapter connection failed")throw new Exception("Failed connection must show the failure icon.");
            using(var bitmap=new Bitmap(form.Width,form.Height))
            {form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.Combine(root,"connection-failure.png"));}
            var port=Descendants(form).OfType<TextBox>().Single(t=>t.Text=="4002");port.Text="4003";
            if(result.Image is not null)throw new Exception("Editing connection settings must clear the stale result.");
            port.Text="4002";
            tabs.SelectedIndex=2;Application.DoEvents();
            if(!search.Enabled)throw new Exception("Changing adapter parameters must not lock out search; validate when clicked.");
            var button=form.Controls.OfType<FlowLayoutPanel>().Single().Controls.OfType<Button>().Single(b=>b.Text=="Save configuration");
            button.PerformClick();Application.DoEvents();
            if(form.DialogResult!=DialogResult.OK)throw new Exception("Configuration save did not succeed.");
        }
        var saved=SettingsFile.Load(path);
        if(saved.LegacyMonitors is not null || File.ReadAllText(path).Contains("\"Monitors\"",StringComparison.Ordinal) || saved.LegacyDataSelections is not null || File.ReadAllText(path).Contains("DataSelections",StringComparison.Ordinal) || saved.Adapter.ProviderId!="IBKR" || saved.Adapter.Parameters["Port"]!="4002" || saved.Adapter.Parameters["ClientId"]!="37")
            throw new Exception("Saving configuration lost existing monitors/selections.");
        File.WriteAllText(Path.Combine(root,"result.txt"),"PASS: branded tabs render; tray icons/status checked; connection success/failure icons and invalidation checked; Save preserves monitors/datasets and adapter parameters. No SQL or provider connections were made.");
    }

    private static IEnumerable<Control> Descendants(Control root)
    {foreach(Control child in root.Controls){yield return child;foreach(var nested in Descendants(child))yield return nested;}}

    private sealed class ConnectionTestFactory : IMarketDataAdapterFactory
    {
        private readonly IbkrAdapterFactory real=new(NullLoggerFactory.Instance);
        public bool Fail;
        public AdapterDescriptor Descriptor=>real.Descriptor;
        public void Validate(MarketDataAdapterSettings settings)=>real.Validate(settings);
        public IMarketDataAdapter Create(MarketDataAdapterSettings settings)=>new ConnectionTestAdapter(Fail);
    }
    private sealed class ConnectionTestAdapter(bool fail) : IMarketDataAdapter
    {
        public string ProviderId=>"IBKR";public bool IsConnected{get;private set;}
        public Task ConnectAsync(CancellationToken ct)
        {if(fail)throw new IOException("Test connection refused at 127.0.0.1:4002. Check TWS/Gateway and API socket port.");IsConnected=true;return Task.CompletedTask;}
        public Task DisconnectAsync(CancellationToken ct){IsConnected=false;return Task.CompletedTask;}
        public ValueTask DisposeAsync(){IsConnected=false;return ValueTask.CompletedTask;}
        public Task<IReadOnlyList<MarketInstrument>> SearchAsync(InstrumentQuery query,CancellationToken ct)=>throw new NotSupportedException();
        public Task<MarketInstrument> ResolveAsync(MarketInstrument instrument,CancellationToken ct)=>throw new NotSupportedException();
        public Task<IReadOnlyList<MarketBar>> GetHistoricalBarsAsync(HistoryQuery query,CancellationToken ct)=>throw new NotSupportedException();
        public Task<decimal?> GetSnapshotAsync(MarketInstrument instrument,CancellationToken ct)=>throw new NotSupportedException();
    }
}
