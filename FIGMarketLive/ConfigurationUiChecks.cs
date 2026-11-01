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
        using(var form=new ConfigurationForm(path,new MarketDataAdapterCatalog([testFactory]),false,confirmStudyDiscard:_=>true,confirmDataSetDiscard:_=>true))
        {
            form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new(-32000,-32000);
            form.Show();Application.DoEvents();
            form.DisplayDataSets([new(51,12,"NQZ6","NQZ6","FUT","1","1 Min",0)]);
            var tabs=form.Controls.OfType<TabControl>().Single();
            if(tabs.TabCount!=5||tabs.TabPages[4].Text!="Exchange Schedule"||tabs.TabPages[4].ImageKey!="schedule")throw new Exception("Configuration must contain the branded Exchange Schedule tab; Tools has its own dialog.");
            Descendants(form).OfType<ExchangeSchedulePanel>().Single().ApplyRows([new("CME",true,"Central Standard Time","Friday","16:00","Sunday","17:00","16:00","17:00")]);
            var scheduleEditor=Descendants(form).OfType<ExchangeSchedulePanel>().Single();
            var scheduleExchange=Descendants(scheduleEditor).OfType<ComboBox>().Single(c=>c.AccessibleName=="Schedule exchange");
            scheduleExchange.Text="CME";scheduleEditor.ApplyExchanges(["CME","NYSE"," CME ","COMEX"]);
            if(scheduleExchange.DropDownStyle!=ComboBoxStyle.DropDown||!scheduleExchange.Items.Cast<string>().SequenceEqual(new[]{"CME","COMEX","NYSE"})||scheduleExchange.Text!="CME")throw new Exception("Schedule exchange must list distinct configured ticker exchanges and retain the selected exchange.");
            if(tabs.TabPages[3].Text!="Study Collections")throw new Exception("Study tab must be named Study Collections.");
            var performance=Descendants(tabs.TabPages[0]).OfType<GroupBox>().Single(g=>g.Text=="Performance");
            if(Descendants(performance).OfType<NumericUpDown>().Count()!=5)throw new Exception("All five runtime settings belong in Performance.");
            var databaseLayout=(TableLayoutPanel)performance.Parent!;var testResults=Descendants(tabs.TabPages[0]).Single(c=>c.Name=="DatabaseTestResults");
            if(databaseLayout.GetRow(testResults)>=databaseLayout.GetRow(performance))throw new Exception("Database results must appear above Performance.");
            var studyCatalog=new StudyConfigurationCatalog([new(12,"NQZ6")],
                [new(){Id="1",Name="1 Min",Seq=1,IntervalLen=60},new(){Id="5",Name="5 Min",Seq=5,IntervalLen=300},new(){Id="10",Name="10 Min",Seq=10,IntervalLen=600},new(){Id="D",Name="1 Day",Seq=100,IntervalLen=86400}],
                [new(51,12,"5")],[new("L2","FIG.StudyCollections.L2.dll","L2 collection")],
                [new(202,12,"10","L2","{}",false,"NQZ6","10 Min",2),new(201,12,"10","L2","{}",true,"NQZ6","10 Min",1)]);
            foreach(var panel in Descendants(form).OfType<StudyConfigurationPanel>())panel.ApplyCatalog(studyCatalog,settings.ConnectionString);
            var studyTicker=Descendants(form).OfType<ComboBox>().Single(c=>c.DataSource is List<StudyTicker>);studyTicker.SelectedIndex=0;
            var studyIntervals=Descendants(form).OfType<ComboBox>().Single(c=>c.DataSource is List<CompatibleStudyInterval>);
            var offered=((List<CompatibleStudyInterval>)studyIntervals.DataSource!).Select(c=>c.Interval.Id).ToArray();
            if(offered.Contains("1")||!new[]{"5","10","D"}.All(offered.Contains))throw new Exception("Study editor must offer only compatible target intervals.");
            if(Descendants(form).OfType<ComboBox>().Single(c=>c.DataSource is List<CollectionType>).Items.Count!=1)throw new Exception("Study ColType dropdown must bind to SQL catalog types.");
            tabs.SelectedIndex=3;Application.DoEvents();
            var studyGrid=Descendants(form).OfType<DataGridView>().Single(g=>g.DataSource is List<StudyConfigurationRow>);
            var studyPages=tabs.TabPages[3].Controls.OfType<TabControl>().Single();
            if(studyPages.TabCount!=2||studyPages.TabPages[0].Text!="Collections"||studyPages.TabPages[1].Text!="Collection Types"||studyPages.TabPages[1].ImageKey!="collectionTypes")throw new Exception("Study Collections must group collections and plugin types with icons.");
            var studyToolbar=Descendants(studyPages.TabPages[0]).OfType<ToolStrip>().Single();
            var up=studyToolbar.Items.OfType<ToolStripButton>().Single(b=>b.ToolTipText=="Move Study Collection Up");
            var down=studyToolbar.Items.OfType<ToolStripButton>().Single(b=>b.ToolTipText=="Move Study Collection Down");
            if(up.Image is null||down.Image is null||!studyGrid.Columns.Cast<DataGridViewColumn>().Any(c=>c.DataPropertyName=="OrderSeq"))throw new Exception("Study order column and arrow icons are required.");
            if(studyGrid.Rows[0].DataBoundItem is not StudyConfigurationRow{Id:201})throw new Exception("Live Studies table must display OrderSeq order.");
            var studyAdd=studyToolbar.Items.OfType<ToolStripButton>().Single(b=>b.ToolTipText=="Add New Study Collection");
            var studyRemove=studyToolbar.Items.OfType<ToolStripButton>().Single(b=>b.ToolTipText=="Delete Study Collection");
            var studySave=studyToolbar.Items.OfType<ToolStripButton>().Single(b=>b.ToolTipText=="Save Study Collection");
            if(studyToolbar.Items.OfType<ToolStripButton>().Any(b=>b.Margin.Left!=4||b.Margin.Right!=4))throw new Exception("Configuration toolbar icons must use compact, consistent spacing.");
            studyAdd.PerformClick();Application.DoEvents();
            if(studyRemove.Enabled||studySave.Enabled)throw new Exception("New empty study must not enable Delete or Save.");
            studyGrid.CurrentCell=studyGrid.Rows[0].Cells[0];studyGrid.Rows[0].Selected=true;Application.DoEvents();
            if(!studyRemove.Enabled||studySave.Enabled||!Descendants(tabs.TabPages[3]).OfType<Label>().Any(l=>l.Text=="Editing StudyCol 201"))throw new Exception($"Study selection must show the editing label, enable Delete and keep clean Save disabled. Delete={studyRemove.Enabled}, Save={studySave.Enabled}; {string.Join(" | ",Descendants(tabs.TabPages[3]).OfType<Label>().Select(l=>l.Text))}");
            var orderPanel=Descendants(studyPages.TabPages[0]).OfType<StudyConfigurationPanel>().Single();
            tabs.Enabled=false;orderPanel.ApplyCatalog(studyCatalog with{Studies=studyCatalog.Studies.Select(s=>s with{OrderSeq=s.Id==201?2:1}).ToList()},settings.ConnectionString);tabs.Enabled=true;Application.DoEvents();
            if(studyGrid.CurrentRow?.DataBoundItem is not StudyConfigurationRow{Id:201,OrderSeq:2}||studyGrid.CurrentRow.Index!=1||!up.Enabled||down.Enabled)throw new Exception("Busy reorder refresh must move and retain the selected row and restore arrow availability.");
            if(studyGrid.Columns.Cast<DataGridViewColumn>().Any(c=>c.SortMode!=DataGridViewColumnSortMode.NotSortable))throw new Exception("Collection grid must retain OrderSeq/Id sorting.");
            orderPanel.ApplyCatalog(studyCatalog,settings.ConnectionString);Application.DoEvents();
            var studyJson=Descendants(tabs.TabPages[3]).OfType<TextBox>().Single(t=>t.AcceptsTab);studyJson.Text="{\"period\":5}";
            var formatJson=Descendants(tabs.TabPages[3]).OfType<Button>().Single(b=>b.Text=="Format / validate JSON");
            var jsonResult=Descendants(tabs.TabPages[3]).OfType<PictureBox>().Single(p=>p.AccessibleName=="JSON validation result");
            formatJson.PerformClick();Application.DoEvents();if(jsonResult.Image is null)throw new Exception("Successful JSON validation must display succes.png.");
            formatJson.PerformClick();Application.DoEvents();if(jsonResult.Image is not null)throw new Exception("Pressing JSON validation again must clear its success icon.");
            formatJson.PerformClick();Application.DoEvents();studyJson.Text="{\"period\":6}";if(jsonResult.Image is not null)throw new Exception("Editing JSON must clear stale validation success.");
            if(!studySave.Enabled)throw new Exception("Editing study parameters must enable Save.");studyJson.Text="{}";
            if(studySave.Enabled)throw new Exception("Reverting study edits must disable Save.");
            studyAdd.PerformClick();Application.DoEvents();
            if(studySave.Enabled||studyRemove.Enabled||!Descendants(tabs.TabPages[3]).OfType<Label>().Any(l=>l.Text=="New StudyCol"))throw new Exception("Add must reset the work area and selection.");
            studyTicker.SelectedIndex=0;Descendants(tabs.TabPages[3]).OfType<ComboBox>().Single(c=>c.DataSource is List<CollectionType>).SelectedIndex=0;
            if(!studySave.Enabled||studyRemove.Enabled)throw new Exception("A populated new study must enable Save while keeping Delete disabled.");
            studyAdd.PerformClick();Application.DoEvents();
            studyGrid.CurrentCell=studyGrid.Rows[0].Cells[0];studyGrid.Rows[0].Selected=true;Application.DoEvents();
            studyPages.SelectedIndex=1;Application.DoEvents();
            var typePanel=Descendants(studyPages.TabPages[1]).OfType<PluginCollectionsPanel>().Single();
            var typeToolbar=Descendants(typePanel).OfType<ToolStrip>().Single();
            typePanel.ApplyRows([new("L2","L2.dll","1","plugin.json","Loaded",""),new("Broken","Missing.dll","1","broken/plugin.json","Load error","Plugin assembly was not found.")]);
            var typeGrid=Descendants(typePanel).OfType<DataGridView>().Single();
            if(!typeGrid.ReadOnly||typeGrid.AllowUserToAddRows||typeToolbar.Items.Count!=1)throw new Exception("Collection Types must be a read-only plugin list with Refresh only.");
            typeGrid.CurrentCell=typeGrid.Rows[1].Cells[0];Application.DoEvents();
            if(!Descendants(typePanel).OfType<TextBox>().Any(t=>t.ReadOnly&&t.Text.Contains("Plugin assembly was not found.")))throw new Exception("Plugin load errors must be visible in Collection Types.");
            using(var pluginBitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(pluginBitmap,new Rectangle(Point.Empty,form.Size));pluginBitmap.Save(Path.Combine(root,"collection-plugins.png"));}
            tabs.SelectedIndex=4;Application.DoEvents();
            var scheduleToolbar=Descendants(scheduleEditor).OfType<ToolStrip>().Single();
            var scheduleAdd=scheduleToolbar.Items.OfType<ToolStripButton>().Single(b=>b.ToolTipText=="Add New Exchange Schedule");
            var scheduleSave=scheduleToolbar.Items.OfType<ToolStripButton>().Single(b=>b.ToolTipText=="Save Exchange Schedule");
            var scheduleDelete=scheduleToolbar.Items.OfType<ToolStripButton>().Single(b=>b.ToolTipText=="Delete Exchange Schedule");
            var scheduleGrid=Descendants(scheduleEditor).OfType<DataGridView>().Single();scheduleGrid.CurrentCell=scheduleGrid.Rows[0].Cells[0];scheduleGrid.Rows[0].Selected=true;Application.DoEvents();
            if(!scheduleDelete.Enabled||scheduleSave.Enabled)throw new Exception("Exchange Schedule toolbar must track selection and clean state.");
            scheduleAdd.PerformClick();Application.DoEvents();
            if(scheduleExchange.Text!=""||!scheduleExchange.Enabled||scheduleSave.Enabled||scheduleDelete.Enabled)throw new Exception("Exchange Schedule Add must clear and unlock the exchange entry.");
            scheduleExchange.Text="TEST";if(!scheduleSave.Enabled)throw new Exception("New exchange schedule data must enable Save.");
            scheduleAdd.PerformClick();Application.DoEvents();scheduleGrid.CurrentCell=scheduleGrid.Rows[0].Cells[0];scheduleGrid.Rows[0].Selected=true;Application.DoEvents();
            tabs.SelectedIndex=2;Application.DoEvents();
            var dataToolbar=Descendants(tabs.TabPages[2]).OfType<ToolStrip>().Single();
            var dataAdd=dataToolbar.Items.OfType<ToolStripButton>().Single(b=>b.ToolTipText=="Add new DataSet");
            var dataRemove=dataToolbar.Items.OfType<ToolStripButton>().Single(b=>b.ToolTipText=="Delete Selected DataSet");
            var dataEdit=dataToolbar.Items.OfType<ToolStripButton>().Single(b=>b.ToolTipText=="Edit selected ticker contract/expiry");
            var dataSync=dataToolbar.Items.OfType<ToolStripButton>().Single(b=>b.ToolTipText=="Enable/disable price sync for selected dataset");
            var addPanel=Descendants(tabs.TabPages[2]).OfType<TableLayoutPanel>().Single(p=>p.AccessibleName=="Add DataSet search panel");
            if(addPanel.Visible)throw new Exception("Dataset search panel must be hidden until Add.");
            dataAdd.PerformClick();Application.DoEvents();
            if(!addPanel.Visible||dataRemove.Enabled||dataEdit.Enabled||dataSync.Enabled)throw new Exception("Add must show the cleared search panel and clear the dataset selection.");
            var search=Descendants(form).OfType<Button>().Single(b=>b.Text=="Search contracts");
            if(!search.Enabled)throw new Exception("Search must be available before manual database/adapter tests.");
            if(Descendants(form).OfType<Button>().Any(b=>b.Text=="Load existing DataSets"))throw new Exception("Legacy dataset loading button must be removed.");
            var saveTicker=Descendants(form).OfType<Button>().Single(b=>b.Text=="Save Ticker");
            if(!saveTicker.Parent!.Controls.OfType<ComboBox>().Any()||saveTicker.Image is null)throw new Exception("Save Ticker must sit beside the interval selector and use save.png.");
            if(!dataToolbar.Items.OfType<ToolStripButton>().Any(b=>b.ToolTipText=="Reload DataSets"))throw new Exception("Dataset toolbar must have Reload DataSets.");
            var exchangeInput=Descendants(form).OfType<ComboBox>().Single(c=>c.AccessibleName=="Contract exchange");
            if(exchangeInput.DropDownStyle!=ComboBoxStyle.DropDown||!exchangeInput.Items.Cast<string>().SequenceEqual(new[]{"CME","CBOT","NYMEX","COMEX","EUREX"}))throw new Exception("FUT must show only futures exchange presets while accepting custom text.");
            exchangeInput.Text="CUSTOM-EXCHANGE";if(exchangeInput.Text!="CUSTOM-EXCHANGE")throw new Exception("Exchange must accept values outside its suggestions.");exchangeInput.Text="CME";
            var expiryInput=Descendants(form).OfType<TextBox>().Single(c=>c.AccessibleName=="Contract expiry");
            var securityType=Descendants(form).OfType<ComboBox>().Single(c=>c.Items.Cast<object>().SequenceEqual(new object[]{"FUT","STK"}));
            foreach(string stockExchange in new[]{"NYSE","NASDAQ"})
            {
                expiryInput.Text="202612";exchangeInput.Text=stockExchange;
                if(expiryInput.Enabled||expiryInput.Text!=""||securityType.Text!="STK")throw new Exception("Stock exchange must select STK and disable/clear expiry.");
                if(!exchangeInput.Items.Cast<string>().SequenceEqual(new[]{"SMART","NYSE","NASDAQ"}))throw new Exception("STK must show only stock routing/listing choices.");
            }
            exchangeInput.Text="CME";securityType.SelectedItem="FUT";
            if(!expiryInput.Enabled)throw new Exception("Futures expiry must become available again for a futures exchange.");
            if(!exchangeInput.Items.Cast<string>().SequenceEqual(new[]{"CME","CBOT","NYMEX","COMEX","EUREX"}))throw new Exception("Returning to FUT must restore futures-only exchange choices.");
            var datasetGrid=Descendants(form).OfType<DataGridView>().Single(g=>g.DataSource is List<ConfiguredDataSet>);
            if(datasetGrid.ColumnCount<6 || datasetGrid.Rows.Count!=1)throw new Exception("Configured datasets must show as a populated multi-column list.");
            if(!datasetGrid.Columns.Cast<DataGridViewColumn>().Any(c=>c.DataPropertyName==nameof(ConfiguredDataSet.StudyUsage))||
                !datasetGrid.Columns.Cast<DataGridViewColumn>().Any(c=>c.DataPropertyName==nameof(ConfiguredDataSet.SyncStatus)))throw new Exception("Dataset grid must separate study usage and price sync flags.");
            datasetGrid.CurrentCell=datasetGrid.Rows[0].Cells[0];datasetGrid.Rows[0].Selected=true;Application.DoEvents();
            if(!dataRemove.Enabled||!dataEdit.Enabled||!dataSync.Enabled||dataSync.Checked||dataSync.CheckOnClick)throw new Exception("Dataset toolbar must follow selection and use a persisted pressed-state sync button.");
            form.DisplayDataSets([new(51,12,"NQ#F","NQZ6","FUT","1","1 Min",1,true,20261218,"CME"),new(52,13,"ABC","ABC","STK","1","1 Min",0,false,0,"NYSE"),new(53,14,"DEF","DEF","STK","1","1 Min",0,false,0,"NASDAQ")]);Application.DoEvents();
            if(!dataSync.Checked)throw new Exception("Enabled SQL Monitor must show the sync button pressed.");
            foreach(int stockRow in new[]{1,2})
            {datasetGrid.CurrentCell=datasetGrid.Rows[stockRow].Cells[0];datasetGrid.Rows[stockRow].Selected=true;Application.DoEvents();if(dataEdit.Enabled||!dataRemove.Enabled||!dataSync.Enabled||dataSync.Checked)throw new Exception("Stock datasets must disable expiry editing while keeping delete/sync available.");}
            datasetGrid.CurrentCell=datasetGrid.Rows[0].Cells[0];datasetGrid.Rows[0].Selected=true;Application.DoEvents();
            form.DisplayDataSets([new(52,13,"ESZ6","ESZ6","FUT","1","1 Min",0),new(51,12,"NQZ6","NQZ6","FUT","1","1 Min",0)]);
            Application.DoEvents();
            if((datasetGrid.CurrentRow?.DataBoundItem as ConfiguredDataSet)?.DataSetId!=51)
                throw new Exception("SQL refresh must preserve dataset selection by ID when row order changes.");
            form.DisplayDataSets([new(52,13,"ESZ6","ESZ6","FUT","1","1 Min",0)]);Application.DoEvents();
            if(datasetGrid.CurrentRow is not null)throw new Exception("Removing the selected dataset must not silently select another delete target.");
            form.DisplayDataSets([new(51,12,"NQZ6","NQZ6","FUT","1","1 Min",1,false),new(52,13,"ESZ6","ESZ6","FUT","1","1 Min",0,true)]);
            using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.Combine(root,"ticker-add-panel.png"));}
            form.CompleteDataSetSave(new(51,12,"1"));Application.DoEvents();if(addPanel.Visible)throw new Exception("Successful ticker save must hide the Add search panel.");
            for(int i=0;i<tabs.TabCount;i++)
            {
                tabs.SelectedIndex=i;if(i==3)studyPages.SelectedIndex=0;Application.DoEvents();
                if(Descendants(form).OfType<Button>().Single(b=>b.Text=="Save configuration").Visible!=(i<2))throw new Exception("Save configuration must only appear on Database and Market Data Adapter.");
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
            dataAdd.PerformClick();Application.DoEvents();
            if(!search.Enabled)throw new Exception("Changing adapter parameters must not lock out search; validate when clicked.");
            tabs.SelectedIndex=0;Application.DoEvents();
            var button=form.Controls.OfType<FlowLayoutPanel>().Single().Controls.OfType<Button>().Single(b=>b.Text=="Save configuration");
            button.PerformClick();Application.DoEvents();
            if(form.DialogResult!=DialogResult.OK)throw new Exception("Configuration save did not succeed.");
        }
        var runningAdapter=new ConnectionTestAdapter(false);runningAdapter.ConnectAsync(default).GetAwaiter().GetResult();
        int mutationStops=0;
        using(var browsing=new ConfigurationForm(path,new MarketDataAdapterCatalog([new ConnectionTestFactory{Fail=true}]),false))
        {
            browsing.BeforeDataSetChangeAsync=_=>{mutationStops++;return Task.CompletedTask;};
            browsing.RunningAdapter=_=>runningAdapter;
            browsing.ShowInTaskbar=false;browsing.StartPosition=FormStartPosition.Manual;browsing.Location=new(-32000,-32000);browsing.Show();Application.DoEvents();
            browsing.TestBrokerAsync(default).GetAwaiter().GetResult();
            if(!runningAdapter.IsConnected||mutationStops!=0)throw new Exception("Browsing/testing configuration must preserve running sync and borrow the active adapter.");
            browsing.Close();Application.DoEvents();
            if(!runningAdapter.IsConnected||mutationStops!=0)throw new Exception("Closing configuration must not disconnect its borrowed runtime adapter.");
        }
        runningAdapter.DisposeAsync().GetAwaiter().GetResult();
        bool discardStudy=false;int discardPrompts=0;
        using(var guarded=new ConfigurationForm(path,new MarketDataAdapterCatalog([new ConnectionTestFactory()]),false,confirmStudyDiscard:_=>{discardPrompts++;return discardStudy;}))
        {
            guarded.ShowInTaskbar=false;guarded.StartPosition=FormStartPosition.Manual;guarded.Location=new(-32000,-32000);guarded.Show();Application.DoEvents();
            var pages=guarded.Controls.OfType<TabControl>().Single();
            var panel=Descendants(pages.TabPages[3]).OfType<StudyConfigurationPanel>().Single();
            panel.ApplyCatalog(new([new(12,"NQZ6")],[new(){Id="5",Name="5 Min",Seq=5,IntervalLen=300}],[new(51,12,"5")],[new("L2","L2.dll","")],[new(201,12,"5","L2","{}",true,OrderSeq:1),new(202,12,"5","L2","{}",true,OrderSeq:2)]),settings.ConnectionString);
            pages.SelectedIndex=3;Application.DoEvents();
            var toolbar=Descendants(panel).OfType<ToolStrip>().Single();var add=toolbar.Items.OfType<ToolStripButton>().Single(b=>b.Text=="Add");add.PerformClick();Application.DoEvents();
            var draftJson=Descendants(panel).OfType<TextBox>().Single(t=>t.AcceptsTab);draftJson.Text="{\"draft\":true}";
            var grid=Descendants(panel).OfType<DataGridView>().Single();grid.CurrentCell=grid.Rows[0].Cells[0];grid.Rows[0].Selected=true;Application.DoEvents();
            if(draftJson.Text!="{\"draft\":true}"||grid.CurrentRow is not null||discardPrompts==0)throw new Exception("Cancelling row selection must preserve the new draft and restore no selection.");
            var innerPages=pages.TabPages[3].Controls.OfType<TabControl>().Single();innerPages.SelectedIndex=1;Application.DoEvents();if(innerPages.SelectedIndex!=0)throw new Exception("Unsaved collection edits must block inner-page navigation when discard is cancelled.");
            pages.SelectedIndex=2;Application.DoEvents();if(pages.SelectedIndex!=3)throw new Exception("Cancelling draft loss must block tab changes.");
            guarded.Close();Application.DoEvents();if(!guarded.Visible)throw new Exception("Cancelling draft loss must keep configuration open.");
            discardStudy=true;add.PerformClick();Application.DoEvents();
            if(draftJson.Text!="{}"||panel.HasUnsavedChanges||Descendants(panel).OfType<ComboBox>().Any(c=>c.SelectedIndex!=-1))throw new Exception("Confirmed Add must clear the entire work area.");
            grid.CurrentCell=grid.Rows[0].Cells[0];grid.Rows[0].Selected=true;Application.DoEvents();
            discardStudy=false;draftJson.Text="{\"moveDraft\":true}";int beforeMovePrompt=discardPrompts;
            var moveDown=toolbar.Items.OfType<ToolStripButton>().Single(b=>b.Text=="Move down");
            if(!moveDown.Enabled)throw new Exception("Selected first row must enable Move Down.");
            moveDown.PerformClick();Application.DoEvents();
            if(discardPrompts!=beforeMovePrompt+1||!panel.HasUnsavedChanges||grid.CurrentRow?.Index!=0)throw new Exception("Move must execute under the busy parent and honor cancelled draft confirmation without SQL writes.");
            discardStudy=true;add.PerformClick();Application.DoEvents();
            draftJson.Text="{\"draft\":true}";pages.SelectedIndex=2;Application.DoEvents();
            if(pages.SelectedIndex!=2||panel.HasUnsavedChanges||draftJson.Text!="{}")throw new Exception("Confirming tab-change discard must clear the abandoned study draft.");
            guarded.Close();Application.DoEvents();
        }
        bool allowLocalSave=false;int prompts=0;
        var promptPath=Path.Combine(root,"prompt-settings.json");SettingsFile.Save(promptPath,settings);
        using(var draftForm=new ConfigurationForm(promptPath,new MarketDataAdapterCatalog([new ConnectionTestFactory()]),false,reason=>{prompts++;return allowLocalSave;}))
        {
            draftForm.ShowInTaskbar=false;draftForm.StartPosition=FormStartPosition.Manual;draftForm.Location=new(-32000,-32000);draftForm.Show();Application.DoEvents();
            var pages=draftForm.Controls.OfType<TabControl>().Single();
            if(pages.TabPages[1].Text!="Market Data Adapter")throw new Exception("Adapter tab capitalization must match other titles.");
            pages.SelectedIndex=1;var draftPort=Descendants(pages.TabPages[1]).OfType<TextBox>().Single(t=>t.Text=="4002");draftPort.Text="4003";
            pages.SelectedIndex=2;Application.DoEvents();
            if(pages.SelectedIndex!=1||prompts!=1)throw new Exception("Cancel must block navigation away from unsaved adapter configuration.");
            allowLocalSave=true;pages.SelectedIndex=2;Application.DoEvents();
            if(pages.SelectedIndex!=2||SettingsFile.Load(promptPath).Adapter.Parameters["Port"]!="4003"||!draftForm.ConfigurationSaved)throw new Exception("Saving the tab-change prompt must persist configuration and continue without closing.");
            pages.SelectedIndex=3;var pluginPages=pages.TabPages[3].Controls.OfType<TabControl>().Single();pluginPages.SelectedIndex=1;
            var pluginFolder=Descendants(pluginPages.TabPages[1]).OfType<TextBox>().Single(t=>t.AccessibleName=="Plugin folder");pluginFolder.Text=Path.Combine(root,"Plugins");
            allowLocalSave=false;pluginPages.SelectedIndex=0;Application.DoEvents();if(pluginPages.SelectedIndex!=1)throw new Exception("Cancel must preserve unsaved plugin settings when switching inner pages.");
            allowLocalSave=true;pluginPages.SelectedIndex=0;Application.DoEvents();if(pluginPages.SelectedIndex!=0||SettingsFile.Load(promptPath).PluginPath!=pluginFolder.Text)throw new Exception("Inner-page navigation must save plugin settings when confirmed.");
            pluginPages.SelectedIndex=1;pluginFolder.Text=Path.Combine(root,"OtherPlugins");
            allowLocalSave=false;pages.SelectedIndex=2;Application.DoEvents();if(pages.SelectedIndex!=3)throw new Exception("Cancelling plugin-folder save must preserve the draft and tab.");
            allowLocalSave=true;pages.SelectedIndex=2;Application.DoEvents();if(SettingsFile.Load(promptPath).PluginPath!=pluginFolder.Text)throw new Exception("Plugin-folder configuration must persist through the navigation save prompt.");
            pages.SelectedIndex=0;var history=Descendants(pages.TabPages[0]).OfType<NumericUpDown>().Single(n=>n.Name=="");history.Value=123;
            Descendants(pages.TabPages[0]).OfType<NumericUpDown>().Single(n=>n.Name=="StudySqlTimeoutSeconds").Value=480;
            Descendants(pages.TabPages[0]).OfType<NumericUpDown>().Single(n=>n.Name=="StudyBatchSize").Value=2500;
            Descendants(pages.TabPages[0]).OfType<NumericUpDown>().Single(n=>n.Name=="CheckpointEveryBars").Value=7500;
            var backupPoll=Descendants(pages.TabPages[0]).OfType<NumericUpDown>().Single(n=>n.Name=="BackupPollSeconds");backupPoll.Value=75;
            allowLocalSave=false;draftForm.Close();Application.DoEvents();
            if(draftForm.IsDisposed||!draftForm.Visible)throw new Exception("Cancel must keep a dialog with unsaved database settings open.");
            allowLocalSave=true;draftForm.Close();Application.DoEvents();
            if(SettingsFile.Load(promptPath).PriceHistoryMinutes!=123||SettingsFile.Load(promptPath).PollSeconds!=75||SettingsFile.Load(promptPath).StudySqlTimeoutSeconds!=480||SettingsFile.Load(promptPath).StudyBatchSize!=2500||SettingsFile.Load(promptPath).CheckpointEveryBars!=7500)throw new Exception("Save on close must persist database catch-up, backup reconciliation and study limits.");
        }
        bool contractSaved=false;
        using(var edit=new TickerContractForm(new(51,12,"NQ#F","NQZ6","FUT","1","1 Min",1),new(){Symbol="NQ",LocalSymbol="NQZ6",SecurityType="FUT",Expiry="202612"},(local,expiry,id,ct)=>{contractSaved=local=="NQH7"&&expiry=="202703"&&id is null;return Task.CompletedTask;}))
        {
            edit.ShowInTaskbar=false;edit.StartPosition=FormStartPosition.Manual;edit.Location=new(-32000,-32000);edit.Show();Application.DoEvents();
            Descendants(edit).OfType<TextBox>().Single(t=>t.Text=="NQZ6").Text="NQH7";Descendants(edit).OfType<TextBox>().Single(t=>t.Text=="202612").Text="202703";
            using(var bitmap=new Bitmap(edit.Width,edit.Height)){edit.DrawToBitmap(bitmap,new Rectangle(Point.Empty,edit.Size));bitmap.Save(Path.Combine(root,"edit-ticker-contract.png"));}
            Descendants(edit).OfType<Button>().Single(b=>b.Text=="Resolve and save contract").PerformClick();Application.DoEvents();
            if(!contractSaved||edit.DialogResult!=DialogResult.OK)throw new Exception("Ticker editor must save edited fields and close successfully.");
        }
        var oldContract=new MarketInstrument{Symbol="NQ",LocalSymbol="NQZ6",SecurityType="FUT",Expiry="20261218",ExpirationDate="20261218",Exchange="CME",Currency="USD",MinimumTick=.25m,ContractMultiplier=20,ProviderInstrumentId="16"};
        var nextContract=oldContract with{LocalSymbol="NQH7",Expiry="20270319",ExpirationDate="20270319",ProviderInstrumentId="17"};
        bool pickedSaved=false;int chainLoads=0;
        using(var edit=new TickerContractForm(new(51,12,"NQ#F","NQZ6","FUT","1","1 Min",1),oldContract,(local,expiry,id,ct)=>{pickedSaved=local=="NQH7"&&expiry=="20270319"&&id=="17";return Task.CompletedTask;},ct=>{chainLoads++;return Task.FromResult<IReadOnlyList<MarketInstrument>>([oldContract,nextContract]);}))
        {
            edit.ShowInTaskbar=false;edit.StartPosition=FormStartPosition.Manual;edit.Location=new(-32000,-32000);edit.Show();Application.DoEvents();
            var picker=Descendants(edit).OfType<ComboBox>().Single();
            if(chainLoads!=1||picker.Items.Count!=2||picker.SelectedIndex!=0)throw new Exception("Contract editor must load available expiries and retain the current contract.");
            picker.SelectedIndex=1;Application.DoEvents();
            using(var bitmap=new Bitmap(edit.Width,edit.Height)){edit.DrawToBitmap(bitmap,new Rectangle(Point.Empty,edit.Size));bitmap.Save(Path.Combine(root,"contract-dropdown.png"));}
            Descendants(edit).OfType<Button>().Single(b=>b.Text=="Resolve and save contract").PerformClick();Application.DoEvents();
            if(!pickedSaved||edit.DialogResult!=DialogResult.OK)throw new Exception("Contract dropdown must save the selected expiry and exact provider identity.");
        }
        int administratorChanges=0;
        using(var adminDialog=new NotificationSetupForm(new NotificationAdministratorSetup(settings.ConnectionString),new("TestDatabase","mapped_application_user","application_login",false),_=>{administratorChanges++;return Task.CompletedTask;}))
        {
            adminDialog.ShowInTaskbar=false;adminDialog.StartPosition=FormStartPosition.Manual;adminDialog.Location=new(-32000,-32000);adminDialog.Show();Application.DoEvents();
            var auth=Descendants(adminDialog).OfType<ComboBox>().Single();auth.SelectedIndex=1;
            var secret=Descendants(adminDialog).OfType<TextBox>().Single(t=>t.UseSystemPasswordChar);secret.Text="temporary-test-value";
            if(!secret.Enabled||!Descendants(adminDialog).OfType<Label>().Any(l=>l.Text.Contains("mapped_application_user")))throw new Exception("Administrator setup must identify the mapped user and mask one-time credentials.");
            adminDialog.Close();Application.DoEvents();
            if(administratorChanges!=0||secret.Text.Length!=0)throw new Exception("Cancelling administrator setup must clear credentials without stopping workers or executing changes.");
        }
        int tickerLoads=0;
        using(var tools=new ToolsForm(settings.ConnectionString,ct=>{tickerLoads++;return Task.FromResult(new List<StudyTicker>{new(12,"NQZ6"),new(13,"ESZ6")});},_=>{mutationStops++;return Task.CompletedTask;}))
        {
            tools.ShowInTaskbar=false;tools.StartPosition=FormStartPosition.Manual;tools.Location=new(-32000,-32000);tools.Show();Application.DoEvents();
            var tabs=tools.Controls.OfType<TabControl>().Single();
            if(tabs.TabCount!=3||tabs.TabPages[0].Text!="Import e-Signal Prices"||tabs.TabPages[0].ImageKey!="import"||tabs.TabPages[1].Text!="DataSet Report"||tabs.TabPages[1].ImageKey!="report"||tabs.TabPages[2].Text!="Regenerate Studies"||tabs.TabPages[2].ImageKey!="regenerate")throw new Exception("Tools must provide import, dataset report and regeneration tabs with their icons.");
            var combos=Descendants(tools).OfType<ComboBox>().ToList();
            if(tickerLoads!=1||combos.Single(c=>c.DataSource is List<StudyTicker>).Items.Count!=2)throw new Exception("Tools must load tickers as soon as it opens.");
            var sourceZone=combos.Single(c=>c.DataSource is List<TimeZoneInfo>);
            if(sourceZone.SelectedItem is not TimeZoneInfo selectedZone||selectedZone.Id!=TimeZoneInfo.Local.Id)throw new Exception("Importer must default to local timezone.");
            Descendants(tools).OfType<Button>().Single(b=>b.Text=="Refresh Tickers").PerformClick();Application.DoEvents();
            if(tickerLoads!=2)throw new Exception("Refresh Tickers must reload the ticker list.");
            using var bitmap=new Bitmap(tools.Width,tools.Height);tools.DrawToBitmap(bitmap,new Rectangle(Point.Empty,tools.Size));bitmap.Save(Path.Combine(root,"tools-import.png"));
            tools.Close();
            if(mutationStops!=0)throw new Exception("Opening, browsing and closing Tools must not invoke the import stop hook.");
        }
        using(var regenerationBranding=new BrandingAssets())
        using(var regenerationWindow=new Form{Width=980,Height=740,ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Location=new(-32000,-32000),Text="Regenerate Studies"})
        {
            var panel=new RegenerateStudiesPanel(settings,NullLoggerFactory.Instance,regenerationBranding,_=>Task.CompletedTask,null);regenerationWindow.Controls.Add(panel);regenerationWindow.Show();Application.DoEvents();
            var grid=Descendants(panel).OfType<DataGridView>().Single();
            if(!grid.MultiSelect||!grid.ReadOnly)throw new Exception("Regeneration must offer multiple collection selection in a read-only table.");
            grid.DataSource=new List<RegenerateStudiesPanel.RegenerationRow>{new(){ColType="L2",StudyCols=2,Enabled=1,StudyCol=1,Bars="1,000 / 5,000",Progress="20%",Status="Calculating",Message="Committed batch; checkpoints and progress saved."}};
            using var bitmap=new Bitmap(regenerationWindow.Width,regenerationWindow.Height);regenerationWindow.DrawToBitmap(bitmap,new Rectangle(Point.Empty,regenerationWindow.Size));bitmap.Save(Path.Combine(root,"tools-regenerate.png"));regenerationWindow.Close();
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
