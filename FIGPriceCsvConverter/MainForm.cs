using System.Diagnostics;

namespace FIGPriceCsvConverter;

public sealed class MainForm : Form
{
    readonly TextBox source = new() { Dock = DockStyle.Fill };
    readonly TextBox target = new() { Dock = DockStyle.Fill };
    readonly ComboBox zones = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly ComboBox ambiguity = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly Button convert = new() { Text = "Convert CSV", AutoSize = true };
    readonly Button cancel = new() { Text = "Cancel", AutoSize = true, Enabled = false };
    readonly Button showOutput = new() { Text = "Show output file", AutoSize = true, Enabled = false };
    readonly Label status = new() { AutoSize = true, Dock = DockStyle.Fill, Text = "Choose an eSignal export to begin." };
    readonly ProgressBar progress = new() { Dock = DockStyle.Fill, Visible = false, Style = ProgressBarStyle.Marquee };
    readonly DataGridView preview = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false };
    readonly TableLayoutPanel inputs = new() { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3 };
    CancellationTokenSource? cancellation;
    string? completedPath;
    sealed record ZoneChoice(string Label, TimeZoneInfo Zone) { public override string ToString() => Label; }

    public MainForm()
    {
        Text = "FIG Price CSV Converter";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(850, 620); Size = new Size(1000, 720);
        AutoScaleMode = AutoScaleMode.Dpi;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 8 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(layout);
        layout.Controls.Add(new Label { Text = "eSignal → FIGSignalBacktest", AutoSize = true, Font = new Font(Font.FontFamily, 18, FontStyle.Bold), Margin = new Padding(0, 0, 0, 12) });
        layout.Controls.Add(new Label { Text = "Convert local Date / Time to UTC seconds. Prices are sorted oldest first; extra eSignal columns are ignored.", AutoSize = true, Margin = new Padding(0, 0, 0, 18) });
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        void AddRow(string label, Control value, Button? button = null)
        {
            int row = inputs.RowCount++;
            inputs.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            inputs.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 10, 8, 10) }, 0, row);
            value.Margin = new Padding(0, 6, 8, 6); inputs.Controls.Add(value, 1, row);
            if (button != null) { button.Dock = DockStyle.Fill; inputs.Controls.Add(button, 2, row); }
            else inputs.SetColumnSpan(value, 2);
        }
        var browseSource = new Button { Text = "Browse…" }; browseSource.Click += (_, _) => BrowseSource();
        var browseTarget = new Button { Text = "Save as…" }; browseTarget.Click += (_, _) => BrowseTarget();
        AddRow("Source eSignal CSV", source, browseSource);
        AddRow("Target backtest CSV", target, browseTarget);
        zones.Items.Add(new ZoneChoice($"Local computer — {TimeZoneInfo.Local.DisplayName}", TimeZoneInfo.Local));
        foreach (var zone in TimeZoneInfo.GetSystemTimeZones()) zones.Items.Add(new ZoneChoice(zone.DisplayName + " [" + zone.Id + "]", zone));
        zones.SelectedIndex = 0;
        AddRow("Source time zone", zones);
        ambiguity.Items.AddRange(["Stop and report ambiguous times (recommended)", "Use earlier UTC occurrence", "Use later UTC occurrence"]);
        ambiguity.SelectedIndex = 0;
        AddRow("Fall-back clock overlap", ambiguity);
        layout.Controls.Add(inputs);
        layout.Controls.Add(new Label { AutoSize = true, Text = "Choose the zone used by the eSignal export, which may differ from this computer. Daylight-saving rules are applied for each date.\nNonexistent spring-forward times and duplicate UTC timestamps are rejected.", Margin = new Padding(0, 10, 0, 14) });
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        actions.Controls.AddRange([convert, cancel, showOutput]); layout.Controls.Add(actions);
        layout.Controls.Add(status); layout.Controls.Add(progress); layout.Controls.Add(preview);
        convert.Click += async (_, _) => await ConvertAsync();
        cancel.Click += (_, _) => cancellation?.Cancel();
        showOutput.Click += (_, _) =>
        {
            if (completedPath == null) return;
            var info = new ProcessStartInfo("explorer.exe") { UseShellExecute = true };
            info.Arguments = "/select,\"" + completedPath + "\"";
            try { Process.Start(info); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Cannot show output"); }
        };
        FormClosing += (_, e) => { if (cancellation != null) { cancellation.Cancel(); e.Cancel = true; status.Text = "Cancelling conversion; close the window after it stops."; } };
    }

    void BrowseSource()
    {
        using var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", Title = "Choose eSignal export" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        source.Text = dialog.FileName;
        target.Text = Path.Combine(Path.GetDirectoryName(dialog.FileName)!, Path.GetFileNameWithoutExtension(dialog.FileName) + "-fig.csv");
    }
    void BrowseTarget()
    {
        using var dialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", DefaultExt = "csv", FileName = Path.GetFileName(target.Text), OverwritePrompt = false };
        if (dialog.ShowDialog(this) == DialogResult.OK) target.Text = dialog.FileName;
    }
    async Task ConvertAsync()
    {
        if (string.IsNullOrWhiteSpace(source.Text) || string.IsNullOrWhiteSpace(target.Text)) { MessageBox.Show(this, "Choose source and target CSV files."); return; }
        bool overwrite = File.Exists(target.Text);
        if (overwrite && MessageBox.Show(this, "Replace the existing target CSV after conversion succeeds?", "Replace target", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var sourcePath = source.Text.Trim(); var targetPath = target.Text.Trim();
        var zone = ((ZoneChoice)zones.SelectedItem!).Zone; var policy = (AmbiguousTimePolicy)ambiguity.SelectedIndex;
        using var tokenSource = new CancellationTokenSource(); cancellation = tokenSource;
        inputs.Enabled = convert.Enabled = showOutput.Enabled = false; cancel.Enabled = true; progress.Visible = true; preview.DataSource = null;
        try
        {
            var updates = new Progress<string>(message => status.Text = message);
            var result = await Task.Run(() => Converter.ConvertFile(sourcePath, targetPath, zone, policy, overwrite, updates, tokenSource.Token));
            completedPath = Path.GetFullPath(targetPath); showOutput.Enabled = true;
            status.Text = $"Saved {result.Count:N0} rows. UTC range: {result.FirstUtc:yyyy-MM-dd HH:mm:ss} → {result.LastUtc:yyyy-MM-dd HH:mm:ss}. Preview: first {result.Preview.Count} rows.";
            preview.DataSource = result.Preview.ToList();
        }
        catch (OperationCanceledException) { status.Text = "Conversion cancelled. Target file was not replaced."; }
        catch (Exception ex) { status.Text = "Conversion failed. Target file was not replaced."; MessageBox.Show(this, ex.Message, "Cannot convert CSV", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { cancellation = null; inputs.Enabled = convert.Enabled = true; cancel.Enabled = progress.Visible = false; }
    }
}
