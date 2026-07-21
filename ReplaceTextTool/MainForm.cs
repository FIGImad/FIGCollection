using System.Text.Json;

namespace ReplaceTextTool;

internal sealed class MainForm : Form
{
    private readonly RichTextBox configurationBox = CreateTextBox();
    private readonly RichTextBox workingTextBox = CreateTextBox();
    private readonly RichTextBox processedTextBox = CreateTextBox(readOnly: true);
    private readonly ToolStripStatusLabel statusLabel = new("Ready");
    private string configurationPath = Path.Combine(AppContext.BaseDirectory, "ReplacementConfig.json");

    public MainForm()
    {
        Text = "ReplaceTextTool";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1050, 560);
        ClientSize = new Size(1400, 720);

        Controls.Add(BuildLayout());
        Load += (_, _) => LoadInitialConfiguration();
    }

    private Control BuildLayout()
    {
        var openButton = new Button { Text = "Open Config...", AutoSize = true };
        var saveButton = new Button { Text = "Save Config", AutoSize = true };
        var processButton = new Button { Text = "Process", AutoSize = true };
        var copyButton = new Button { Text = "Copy Result", AutoSize = true };

        openButton.Click += (_, _) => OpenConfiguration();
        saveButton.Click += (_, _) => SaveConfiguration();
        processButton.Click += (_, _) => ProcessText();
        copyButton.Click += (_, _) => CopyProcessedText();

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(8, 7, 8, 5),
            WrapContents = false
        };
        toolbar.Controls.AddRange([openButton, saveButton, processButton, copyButton]);

        var editors = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(8, 0, 8, 8)
        };
        editors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        editors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        editors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
        editors.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        editors.Controls.Add(BuildEditorPanel("Replacement configuration (* = identifier, % = one char)", configurationBox), 0, 0);
        editors.Controls.Add(BuildEditorPanel("Original text (paste here)", workingTextBox), 1, 0);
        editors.Controls.Add(BuildEditorPanel("Processed text", processedTextBox), 2, 0);

        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(statusLabel);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(toolbar, 0, 0);
        layout.Controls.Add(editors, 0, 1);
        layout.Controls.Add(statusStrip, 0, 2);
        return layout;
    }

    private static Control BuildEditorPanel(string title, Control editor)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Padding = new Padding(0, 0, 0, 6)
        }, 0, 0);
        panel.Controls.Add(editor, 0, 1);
        return panel;
    }

    private static RichTextBox CreateTextBox(bool readOnly = false) => new()
    {
        Dock = DockStyle.Fill,
        AcceptsTab = true,
        DetectUrls = false,
        Font = new Font("Consolas", 10F),
        WordWrap = false,
        ReadOnly = readOnly,
        BackColor = SystemColors.Window
    };

    private void LoadInitialConfiguration()
    {
        if (File.Exists(configurationPath))
        {
            LoadConfiguration(configurationPath);
        }
        else
        {
            SetStatus("Default ReplacementConfig.json was not found.", true);
        }
    }

    private void OpenConfiguration()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open replacement configuration",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = Path.GetFileName(configurationPath),
            InitialDirectory = Path.GetDirectoryName(configurationPath)
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            LoadConfiguration(dialog.FileName);
        }
    }

    private void LoadConfiguration(string path)
    {
        try
        {
            configurationBox.Text = File.ReadAllText(path);
            configurationPath = path;
            SetStatus($"Loaded {path}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowError("The configuration file could not be opened.", exception);
        }
    }

    private void SaveConfiguration()
    {
        try
        {
            _ = TextReplacementProcessor.Process(string.Empty, configurationBox.Text);
            File.WriteAllText(configurationPath, configurationBox.Text);
            SetStatus($"Saved {configurationPath}");
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            ShowError("The configuration could not be saved.", exception);
        }
    }

    private void ProcessText()
    {
        try
        {
            ReplacementResult result = TextReplacementProcessor.Process(workingTextBox.Text, configurationBox.Text);
            processedTextBox.Text = result.Text;
            SetStatus($"Applied {result.RuleCount} rules; made {result.ReplacementCount} replacements.");
        }
        catch (JsonException exception)
        {
            ShowError("The replacement configuration is invalid.", exception);
        }
    }

    private void CopyProcessedText()
    {
        if (processedTextBox.TextLength == 0)
        {
            SetStatus("There is no processed text to copy.", true);
            return;
        }

        Clipboard.SetText(processedTextBox.Text);
        SetStatus("Processed text copied to the clipboard.");
    }

    private void ShowError(string message, Exception exception)
    {
        SetStatus(message, true);
        MessageBox.Show(this, $"{message}\n\n{exception.Message}", "ReplaceTextTool",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void SetStatus(string message, bool isError = false)
    {
        statusLabel.Text = message;
        statusLabel.ForeColor = isError ? Color.Firebrick : SystemColors.ControlText;
    }
}
