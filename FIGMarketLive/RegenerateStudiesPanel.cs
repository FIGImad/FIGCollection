using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace FIGMarketLive;

internal sealed class RegenerateStudiesPanel:UserControl
{
    private readonly DataGridView rows=new(){Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,MultiSelect=true,SelectionMode=DataGridViewSelectionMode.FullRowSelect,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells};
    private readonly Label status=new(){Dock=DockStyle.Bottom,AutoSize=true,MaximumSize=new(880,0)};
    private readonly ProgressBar progress=new(){Dock=DockStyle.Bottom,Height=22};
    private readonly BindingList<RegenerationRow> data=[];
    private readonly StudyRegenerationService service;
    private readonly Func<string,Task>? beforeChange;
    private bool loaded;
    private readonly CheckBox full=new(){Text="Full",AutoSize=true,Margin=new(12,8,3,3)};
    private readonly CheckBox overridesOnly=new(){Text="L2 overrides (full last 10)",AutoSize=true,Margin=new(12,8,3,3)};
    public RegenerateStudiesPanel(MarketLiveSettings settings,ILoggerFactory logs,BrandingAssets branding,Func<Func<CancellationToken,Task>,Task> run,Func<string,Task>? beforeChange)
    {
        this.beforeChange=beforeChange;service=new(settings,logs);Dock=DockStyle.Fill;
        var actions=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true};var refresh=new Button{Text="Refresh Collections",AutoSize=true};var start=new Button{Text="Regenerate selected collections",AutoSize=true};
        branding.Apply(refresh,"reload.png");branding.Apply(start,"regenerate.png");refresh.Click+=async(_,_)=>await run(RefreshAsync);start.Click+=async(_,_)=>await run(RegenerateAsync);
        full.CheckedChanged+=(_,_)=>{if(full.Checked)overridesOnly.Checked=false;};
        overridesOnly.CheckedChanged+=(_,_)=>{if(overridesOnly.Checked)full.Checked=false;};
        actions.Controls.AddRange([refresh,start,full,overridesOnly]);
        var hint=new Label{Dock=DockStyle.Top,AutoSize=true,MaximumSize=new(880,0),Text=$"Select multiple ColTypes using Ctrl/Shift. Only enabled StudyCols are recalculated; progress uses the enabled count. Each {settings.StudyBatchSize:N0} bars is committed with progress/checkpoints. SQL timeout: {settings.StudySqlTimeoutSeconds:N0} seconds. Interrupted runs resume when their inputs and DLL are compatible."};
        rows.DataSource=data;Controls.Add(rows);Controls.Add(progress);Controls.Add(status);Controls.Add(hint);Controls.Add(actions);
    }
    internal async Task EnsureLoadedAsync(CancellationToken ct){if(!loaded)await RefreshAsync(ct);}
    private async Task RefreshAsync(CancellationToken ct)
    {
        var choices=await service.ListAsync(ct);data.Clear();foreach(var choice in choices)data.Add(new(){ColType=choice.ColType,StudyCols=choice.StudyCols,Enabled=choice.Enabled,Status=choice.Available?"Ready":"Unavailable",Message=choice.Available?"":string.IsNullOrWhiteSpace(choice.Problem)?"Plugin not loaded; check Collection Types.":choice.Problem,Available=choice.Available});
        if(rows.Columns[nameof(RegenerationRow.Available)] is {} column)column.Visible=false;
        loaded=true;status.Text="Select collection types, then Regenerate. Use Cancel operation to retain committed batches and stop.";
    }
    private async Task RegenerateAsync(CancellationToken ct)
    {
        var selected=rows.SelectedRows.Cast<DataGridViewRow>().Select(r=>r.DataBoundItem).OfType<RegenerationRow>().ToList();
        if(selected.Count==0)throw new InvalidDataException("Select one or more collection types.");
        if(selected.Any(r=>!r.Available||r.Enabled==0))throw new InvalidDataException("Select loaded collection types with at least one enabled StudyCol.");
        bool fullRun=full.Checked;
        bool overrideRun=overridesOnly.Checked;
        if(overrideRun&&selected.Any(r=>!StudyLayers.Applies(r.ColType)))throw new InvalidDataException("L2 overrides only requires selecting the L2 collection type only.");
        string mode=fullRun?"FULL: All StudyHistory records and checkpoints for each selected StudyCol will be deleted when its first batch commits, then replaced with direct bulk inserts. Other collections and prices are retained. To resume an interrupted run, uncheck Full; keeping Full checked starts over.":"Existing results are replaced batch by batch. Committed batches remain if interrupted; compatible checkpoints/progress are used when rerunning. Completed runs start fresh.";
        if(overrideRun)mode="L2 OVERRIDES + FULL LAST 10: Earlier bars use stored Studies and recalculate only Study_L2. The final 10 bars recalculate all studies and update OHLCV, Studies and StudiesOverride together. Earlier base rows must match current prices. A compatible full checkpoint prepares the tail; if unavailable, an automatic warm-up rebuilds runtime state without rewriting earlier base rows. Base algorithm/parameter changes still require Full. Keep this option selected to resume interrupted runs.";
        if(MessageBox.Show(this,$"Regenerate {string.Join(", ",selected.Select(r=>r.ColType))}?\n\nPrice sync and live studies will stop. Only enabled StudyCols of these types are included. Disabled collections and their history are retained.\n\n{mode}","Regenerate Studies",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
        if(beforeChange is not null)await beforeChange("regenerating studies");
        foreach(var row in selected){row.Status="Queued";row.Progress="0%";row.Message="";}data.ResetBindings();
        var updates=new Progress<RegenerationProgress>(p=>
        {
            var row=selected.FirstOrDefault(r=>r.ColType.Equals(p.ColType,StringComparison.OrdinalIgnoreCase));if(row is null||IsDisposed)return;
            row.Status=p.Status;row.Message=p.Message;
            if(p.StudyColId!=0){row.StudyCol=p.StudyColId;row.Bars=$"{p.Completed:N0} / {p.Total:N0}";}
            double fraction=p.StudyColId==0&&p.Status=="Completed"?1:((Math.Max(0,p.CollectionIndex-1))+(p.Total>0?(double)p.Completed/p.Total:0))/Math.Max(1,p.CollectionCount);
            if(p.Status!="Completed")fraction=Math.Min(fraction,0.999);
            row.Progress=p.Status=="Failed"?"Failed":$"{fraction:P1}";progress.Value=p.Status=="Failed"?0:Math.Clamp((int)(fraction*100),0,100);data.ResetBindings();status.Text=$"{p.ColType}: {p.Message}";
        });
        try{await Task.Run(()=>service.RunAsync(selected.Select(r=>r.ColType).ToArray(),updates,ct,fullRun,overrideRun),ct);}
        catch(OperationCanceledException)when(ct.IsCancellationRequested)
        {foreach(var row in selected.Where(r=>r.Status=="Queued"))row.Status="Not started";data.ResetBindings();status.Text=overrideRun?"Cancelled. Keep L2 override mode selected to resume.":"Cancelled. Committed batches remain; uncheck Full to resume compatible checkpoints.";throw;}
        // Progress<T> callbacks can still be queued on the UI thread. Let the final
        // callback retain the actual completion/failure message instead of replacing it.
    }
    internal sealed class RegenerationRow
    {
        public string ColType{get;set;}="";public int StudyCols{get;set;}public int Enabled{get;set;}public int StudyCol{get;set;}
        public string Bars{get;set;}="";public string Progress{get;set;}="";public string Status{get;set;}="";public string Message{get;set;}="";public bool Available{get;set;}
    }
}
