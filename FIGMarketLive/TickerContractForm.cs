using FIGMarketLive.MarketData;

namespace FIGMarketLive;

internal sealed class TickerContractForm : Form
{
    private readonly BrandingAssets branding=new();
    private readonly TextBox local=new(){Width=220};
    private readonly TextBox expiry=new(){Width=160};
    private readonly Button save=new(){Text="Resolve and save contract",AutoSize=true};
    private readonly Label status=new(){AutoSize=true,MaximumSize=new(560,0)};
    private readonly ComboBox available=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=760,DropDownWidth=1000,MaxDropDownItems=15};
    private readonly Button refresh=new(){Text="Refresh available contracts",AutoSize=true};
    private MarketInstrument? picked;
    private bool applying;
    private sealed record ContractChoice(MarketInstrument Instrument)
    {public string Label=>$"{Instrument.LocalSymbol}  |  Last trade / month: {Instrument.Expiry}  |  Expiration: {(string.IsNullOrWhiteSpace(Instrument.ExpirationDate)?"not supplied":Instrument.ExpirationDate)}  |  Provider ID: {Instrument.ProviderInstrumentId}";}
    private readonly CancellationTokenSource lifetime=new();
    private bool busy,closeRequested,resourcesDisposed;

    public TickerContractForm(ConfiguredDataSet selected,MarketInstrument original,Func<string,string,string?,CancellationToken,Task> persist,
        Func<CancellationToken,Task<IReadOnlyList<MarketInstrument>>>? loadContracts=null)
    {
        Text="Edit ticker contract / expiry";Icon=branding.ApplicationIcon;Width=840;Height=460;MinimumSize=new(840,460);StartPosition=FormStartPosition.CenterParent;
        local.Text=original.LocalSymbol;expiry.Text=original.Expiry;expiry.Enabled=original.SecurityType=="FUT";
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,Padding=new(12),AutoScroll=true};
        void Add(Control control){int row=layout.RowCount++;layout.RowStyles.Add(new(SizeType.AutoSize));layout.Controls.Add(control,0,row);}
        Add(new Label{Text=$"Ticker {selected.TickerId}: {selected.Symbol}  |  Provider root: {original.Symbol}",AutoSize=true});
        Add(new Label{Text="Available non-expired contracts (select a contract, or enter details below)",AutoSize=true});
        available.BindingContext=new BindingContext();available.DisplayMember=nameof(ContractChoice.Label);Add(available);Add(refresh);
        refresh.Enabled=loadContracts is not null&&original.SecurityType=="FUT";
        void ApplyChoice()
        {
            if(applying||available.SelectedItem is not ContractChoice choice)return;
            applying=true;local.Text=choice.Instrument.LocalSymbol;expiry.Text=choice.Instrument.Expiry;picked=choice.Instrument;applying=false;
            status.Text=$"Selected provider instrument ID: {picked.ProviderInstrumentId}. Save to apply this contract.";
        }
        available.SelectedIndexChanged+=(_,_)=>ApplyChoice();
        void ManualEdit(){if(applying)return;applying=true;picked=null;available.SelectedIndex=-1;applying=false;}
        local.TextChanged+=(_,_)=>ManualEdit();expiry.TextChanged+=(_,_)=>ManualEdit();
        async Task LoadAsync()
        {
            if(busy||loadContracts is null||original.SecurityType!="FUT")return;
            busy=true;save.Enabled=false;refresh.Enabled=false;available.Enabled=false;local.Enabled=false;expiry.Enabled=false;UseWaitCursor=true;status.Text="Querying available futures contracts…";
            try
            {
                var rows=await loadContracts(lifetime.Token);var choices=new List<ContractChoice>();
                foreach(var row in rows)
                {
                    try{TickerContractStore.ValidateResolved(original,TickerContractStore.Candidate(original,row.LocalSymbol,row.Expiry),row);choices.Add(new(row));}
                    catch(InvalidDataException){ }
                }
                var unique=choices.DistinctBy(c=>c.Instrument.ProviderInstrumentId).OrderBy(c=>c.Instrument.Expiry,StringComparer.Ordinal).ToList();
                string? desired=picked?.ProviderInstrumentId??(local.Text==original.LocalSymbol&&expiry.Text==original.Expiry?original.ProviderInstrumentId:null);
                applying=true;available.DataSource=unique;
                available.SelectedIndex=unique.FindIndex(c=>desired is not null&&c.Instrument.ProviderInstrumentId==desired);
                if(available.SelectedIndex<0)picked=null;applying=false;
                ApplyChoice();status.Text=$"{unique.Count} compatible non-expired contract(s). IDs differ between expiries. Select a contract or enter details manually.";
            }
            catch(OperationCanceledException){status.Text="Contract lookup cancelled.";}
            catch(Exception ex){status.Text=$"Contract lookup: {ex.Message} You can refresh or enter the contract manually.";}
            finally{busy=false;refresh.Enabled=true;available.Enabled=true;save.Enabled=true;local.Enabled=true;expiry.Enabled=original.SecurityType=="FUT";UseWaitCursor=false;if(closeRequested)Close();}
        }
        refresh.Click+=async(_,_)=>await LoadAsync();
        if(loadContracts is not null)Shown+=async(_,_)=>await LoadAsync();
        Add(new Label{Text="Contract local name",AutoSize=true});Add(local);Add(new Label{Text="Expiry YYYYMM or YYYYMMDD",AutoSize=true});Add(expiry);
        Add(new Label{AutoSize=true,MaximumSize=new(570,0),Text="Updates this ticker across ALL its datasets and StudyCols. The database symbol, IDs and existing price history are kept. New-contract capture starts with the first bar opening after the roll is saved; older bars are preserved. No back-adjustment is performed."});
        var buttons=new FlowLayoutPanel{AutoSize=true};var cancel=new Button{Text="Cancel",AutoSize=true};cancel.Click+=(_,_)=>Close();buttons.Controls.AddRange([save,cancel]);Add(buttons);Add(status);Controls.Add(layout);
        save.Click+=async(_,_)=>
        {
            if(busy)return;
            bool saved=false;
            busy=true;save.Enabled=false;refresh.Enabled=false;available.Enabled=false;local.Enabled=false;expiry.Enabled=false;UseWaitCursor=true;status.Text="Resolving the updated contract and saving…";
            try{await persist(local.Text,expiry.Text,picked?.ProviderInstrumentId,lifetime.Token);saved=true;}
            catch(OperationCanceledException){status.Text="Contract update cancelled.";}
            catch(Exception ex){status.Text=ex.Message;}
            finally{busy=false;save.Enabled=true;refresh.Enabled=loadContracts is not null&&original.SecurityType=="FUT";available.Enabled=true;local.Enabled=true;expiry.Enabled=original.SecurityType=="FUT";UseWaitCursor=false;if(saved)DialogResult=DialogResult.OK;if(saved||closeRequested)Close();}
        };
        FormClosing+=(_,e)=>{if(busy){closeRequested=true;lifetime.Cancel();e.Cancel=true;}};
    }
    protected override void Dispose(bool disposing)
    {base.Dispose(disposing);if(disposing&&!resourcesDisposed){resourcesDisposed=true;lifetime.Dispose();branding.Dispose();}}
}
