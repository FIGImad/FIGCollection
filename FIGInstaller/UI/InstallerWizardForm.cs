using FIGInstaller.Models;
using FIGInstaller.Services;
using FIGCommon.Utilities;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Text.Encodings.Web;
using System.Data.Common;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

namespace FIGInstaller.UI;

public sealed class InstallerWizardForm : Form
{
    private static readonly Color AccentColor = Color.FromArgb(34, 96, 173);
    private static readonly Color ShellBackColor = Color.FromArgb(244, 246, 249);
    private static readonly Color SuccessColor = Color.FromArgb(28, 128, 75);
    private static readonly Color ErrorColor = Color.FromArgb(176, 40, 40);
    private static readonly JsonSerializerOptions InstallerJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private static readonly string[] PreviousInstallationDatabaseNames = ["FIGAutoTrader", "FIGUser", "FIGAlert"];
    private static readonly Guid AutoTraderAdminSslBindingAppId = new("DA1B9AB3-54CF-4B56-9F92-746C89D4B58B");
    private const string ControllerServiceKey = "FIGControllerSvc";
    private const string AutoTraderServiceKey = "FIGAutoTraderSvc";
    private const string AlertServiceKey = "FIGAlertSvc";
    private const string AutoTraderAdminServiceKey = "FIGAutoTraderAdminSvc";
    private const string PriceSyncServiceKey = "FIGPriceSyncSvc";
    private const string SignalServiceKey = "FIGSignalSvc";
    private const string BrokerServiceKey = "FIGBrokerSvc";
    private const string ServiceManagerServiceKey = "FIGServiceMgrSvc";
    private static readonly IReadOnlyDictionary<string, ServiceDescriptor> ServiceDescriptors = new Dictionary<string, ServiceDescriptor>(StringComparer.OrdinalIgnoreCase)
    {
        [ServiceManagerServiceKey] = new(ServiceManagerServiceKey, "service-manager-settings.json", "FIGServiceMgrSvc.exe", 0, InstallerComponent.None, ServiceInstallKind.WindowsService, IncludeInServiceManager: false, RequiresCustomServiceAccount: true, WritesManagedServices: true),
        [ControllerServiceKey] = new(ControllerServiceKey, "controller-settings.json", "FIGControllerSvc.exe", 10, InstallerComponent.AutoTradeSystem, ServiceInstallKind.WindowsService, IncludeInServiceManager: false),
        [AutoTraderServiceKey] = new(AutoTraderServiceKey, "autotrade-settings.json", "FIGAutoTraderSvc.exe", 20, InstallerComponent.AutoTradeSystem, ServiceInstallKind.WindowsService),
        [AlertServiceKey] = new(AlertServiceKey, "alerts-settings.json", "FIGAlertSvc.exe", 30, InstallerComponent.AutoTradeSystem, ServiceInstallKind.WindowsService),
        [AutoTraderAdminServiceKey] = new(AutoTraderAdminServiceKey, "appsettings.json", "FIGAutoTraderAdminSvc.exe", 40, InstallerComponent.AutoTradeWebAdmin, ServiceInstallKind.IisApplication, IncludeInServiceManager: false),
        [PriceSyncServiceKey] = new(PriceSyncServiceKey, "price-sync-settings.json", "FIGPriceSyncSvc.exe", 50, InstallerComponent.PriceSync, ServiceInstallKind.WindowsService),
        [SignalServiceKey] = new(SignalServiceKey, "signal-settings.json", "FIGSignalSvc.exe", 60, InstallerComponent.SignalProcessing, ServiceInstallKind.WindowsService),
        [BrokerServiceKey] = new(BrokerServiceKey, "broker-settings.json", "FIGBrokerSvc.exe", 70, InstallerComponent.Broker, ServiceInstallKind.WindowsService)
    };
    private const int ContentWidth = 700;

    private readonly InstructionFileLoader _loader = new();
    private readonly InstallationInstructionValidator _validator = new();
    private readonly List<WizardPage> _pages;
    private readonly Panel _contentPanel = new();
    private readonly Label _titleLabel = new();
    private readonly Label _descriptionLabel = new();
    private readonly Button _backButton = new();
    private readonly Button _nextButton = new();
    private readonly Button _cancelButton = new();

    private TextBox _instructionPathTextBox = null!;
    private Label _instructionStatusLabel = null!;
    private ListBox _validationListBox = null!;
    private CheckBox _autoTradeSystemCheckBox = null!;
    private CheckBox _autoTradeWebAdminCheckBox = null!;
    private CheckBox _priceSyncCheckBox = null!;
    private CheckBox _signalProcessingCheckBox = null!;
    private CheckBox _brokerCheckBox = null!;
    private ListBox _componentServicesListBox = null!;
    private TextBox _databaseAddressTextBox = null!;
    private TextBox _saPasswordTextBox = null!;
    private Button _toggleSaPasswordButton = null!;
    private TextBox _adminConnectionPreviewTextBox = null!;
    private TextBox _sqlDataPathTextBox = null!;
    private ListBox _databaseScriptsListBox = null!;
    private Label _databaseStatusLabel = null!;
    private Button _testDatabaseConnectionButton = null!;
    private TextBox _masterKeyNameTextBox = null!;
    private TextBox _masterKeyValueTextBox = null!;
    private TextBox _controllerUsernameTextBox = null!;
    private TextBox _controllerPasswordTextBox = null!;
    private TextBox _controllerUrlTextBox = null!;
    private TextBox _controllerPortTextBox = null!;
    private TextBox _authUrlTextBox = null!;
    private TextBox _certThumbprintTextBox = null!;
    private TextBox _logPathTextBox = null!;
    private TextBox _jwtKeyTextBox = null!;
    private TextBox _jwtIssuerTextBox = null!;
    private TextBox _jwtAudienceTextBox = null!;
    private TextBox _smtpUsernameTextBox = null!;
    private TextBox _smtpPasswordTextBox = null!;
    private TextBox _serviceInstallPathTextBox = null!;
    private ListBox _servicesToInstallListBox = null!;
    private ComboBox _serviceConfigurationComboBox = null!;
    private TextBox _settingsServiceNameTextBox = null!;
    private TextBox _serviceIdTextBox = null!;
    private TextBox _serviceFriendlyNameTextBox = null!;
    private TextBox _servicePortTextBox = null!;
    private TextBox _serviceManagerUsernameTextBox = null!;
    private TextBox _serviceManagerPasswordTextBox = null!;
    private TextBox _reviewTextBox = null!;
    private ListBox _planListBox = null!;
    private Label _reviewStatusLabel = null!;

    private InstallationInstructions? _instructions;
    private string? _instructionFile;
    private readonly List<string> _existingInstallationDatabaseNames = [];
    private readonly HashSet<string> _databaseNamesToSkip = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ServiceInstallSettings> _serviceSettings = new(StringComparer.OrdinalIgnoreCase);
    private int _pageIndex;
    private bool _installCompleted;
    private bool _loadingServiceSettings;

    public InstallerWizardForm(string? initialInstructionFile)
    {
        Text = "FIG Installer";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(820, 560);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Font = new Font("Segoe UI", 9F);
        BackColor = ShellBackColor;

        BuildShell();

        _pages =
        [
            new WizardPage(PageKind.Welcome, "Welcome", "Prepare a FIG Collection server installation.", CreateWelcomePage(), null),
            new WizardPage(PageKind.Instructions, "Installation Instructions", "Load the JSON file that provides the installer defaults.", CreateInstructionPage(), null),
            new WizardPage(PageKind.Types, "Installation Types", "Choose the FIG components to install on this server.", CreateInstallationTypesPage(), null),
            new WizardPage(PageKind.Database, "AutoTrade Database Setup", "Enter SQL Server address, SA password, data path, and review the script plan.", CreateDatabasePage(), RefreshDatabasePage),
            new WizardPage(PageKind.GlobalVars, "AutoTrade Global Variables", "Review controller, certificate, log, JWT, and SMTP values used in global_vars.json.", CreateGlobalVarsPage(), RefreshGlobalVarsPage),
            new WizardPage(PageKind.Services, "Windows Services", "Configure service folder, friendly names, and service IDs.", CreateServicesPage(), RefreshServicesPage),
            new WizardPage(PageKind.Review, "Ready To Install", "Review the planned installation actions before running them.", CreateReviewPage(), RefreshReviewPage)
        ];

        _instructionPathTextBox.Text = initialInstructionFile ?? "";
        ShowPage(0);
    }

    private void BuildShell()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = ShellBackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        Controls.Add(root);

        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(30, 18, 30, 14)
        };
        root.Controls.Add(header, 0, 0);

        _titleLabel.AutoSize = true;
        _titleLabel.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
        _titleLabel.ForeColor = Color.FromArgb(32, 35, 40);
        _titleLabel.Location = new Point(30, 20);
        header.Controls.Add(_titleLabel);

        _descriptionLabel.AutoEllipsis = true;
        _descriptionLabel.ForeColor = Color.FromArgb(86, 92, 104);
        _descriptionLabel.Location = new Point(32, 58);
        _descriptionLabel.Size = new Size(ContentWidth, 28);
        header.Controls.Add(_descriptionLabel);

        _contentPanel.Dock = DockStyle.Fill;
        _contentPanel.Padding = new Padding(30);
        root.Controls.Add(_contentPanel, 0, 1);

        var footer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ShellBackColor,
            Padding = new Padding(30, 16, 30, 16)
        };
        root.Controls.Add(footer, 0, 2);

        var buttonBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            Width = 330,
            WrapContents = false
        };
        footer.Controls.Add(buttonBar);

        ConfigureButton(_backButton, "< Back");
        ConfigureButton(_nextButton, "Next >");
        ConfigureButton(_cancelButton, "Cancel");

        _backButton.Click += (_, _) => MoveBack();
        _nextButton.Click += async (_, _) => await MoveNextAsync();
        _cancelButton.Click += (_, _) => Close();

        buttonBar.Controls.Add(_backButton);
        buttonBar.Controls.Add(_nextButton);
        buttonBar.Controls.Add(_cancelButton);
    }

    private Control CreateWelcomePage()
    {
        Panel page = CreatePagePanel();

        page.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
            ForeColor = Color.FromArgb(32, 35, 40),
            Text = "FIG Collection installation wizard"
        });

        page.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(0, 48),
            Size = new Size(ContentWidth, 90),
            ForeColor = Color.FromArgb(72, 78, 90),
            Text = "This wizard loads a JSON instruction file, lets you override defaults, and prepares the database, global variable, and Windows service setup plan for the selected FIG components."
        });

        page.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(0, 146),
            Size = new Size(ContentWidth, 96),
            ForeColor = AccentColor,
            Text = "The current implementation builds the full wizard and installation plan. The final Install button is still non-destructive until the database and Windows service execution layer is wired in."
        });

        return page;
    }

    private Control CreateInstructionPage()
    {
        Panel page = CreatePagePanel();

        var fileLabel = CreateFieldLabel("Instruction JSON file");
        fileLabel.Location = new Point(0, 0);
        page.Controls.Add(fileLabel);

        var fileRow = new TableLayoutPanel
        {
            Location = new Point(0, 28),
            Size = new Size(ContentWidth, 36),
            ColumnCount = 3,
            RowCount = 1
        };
        fileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98));
        fileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98));
        page.Controls.Add(fileRow);

        _instructionPathTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "Select installation-instructions.json"
        };
        fileRow.Controls.Add(_instructionPathTextBox, 0, 0);

        var browseButton = new Button { Dock = DockStyle.Fill, Text = "Browse" };
        browseButton.Click += (_, _) => BrowseInstructionFile();
        fileRow.Controls.Add(browseButton, 1, 0);

        var loadButton = new Button { Dock = DockStyle.Fill, Text = "Load" };
        loadButton.Click += (_, _) => TryLoadInstructions(showSuccessMessage: true);
        fileRow.Controls.Add(loadButton, 2, 0);

        _instructionStatusLabel = new Label
        {
            AutoSize = false,
            Location = new Point(0, 82),
            Size = new Size(ContentWidth, 28),
            ForeColor = Color.FromArgb(82, 88, 100),
            Text = "Select a JSON file and load it to continue."
        };
        page.Controls.Add(_instructionStatusLabel);

        _validationListBox = new ListBox
        {
            Location = new Point(0, 118),
            Size = new Size(ContentWidth, 280),
            IntegralHeight = false
        };
        page.Controls.Add(_validationListBox);

        return page;
    }

    private Control CreateInstallationTypesPage()
    {
        Panel page = CreatePagePanel();

        _autoTradeSystemCheckBox = CreateOptionCheckBox("AutoTrade System", 0, "Installs the controller, auto-trader, alert services, and the database system.");
        _autoTradeWebAdminCheckBox = CreateOptionCheckBox("AutoTrade Web Admin", 58, "Requires SQL Server and special web-admin instructions.");
        _priceSyncCheckBox = CreateOptionCheckBox("Price Sync component", 116, "Installs the price-sync service.");
        _signalProcessingCheckBox = CreateOptionCheckBox("Signal Processing Component", 174, "Installs the signal-processing service.");
        _brokerCheckBox = CreateOptionCheckBox("Broker", 232, "Installs the broker service.");

        foreach (CheckBox checkBox in new[] { _autoTradeSystemCheckBox, _autoTradeWebAdminCheckBox, _priceSyncCheckBox, _signalProcessingCheckBox, _brokerCheckBox })
        {
            checkBox.CheckedChanged += (_, _) =>
            {
                ResetDatabaseInstallationDecision();
                RefreshComponentServicesList();
                RefreshDatabasePage();
                RefreshReviewPage();
            };
            page.Controls.Add(checkBox);
        }

        page.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(0, 302),
            Size = new Size(ContentWidth, 24),
            Text = "Services selected by these components",
            ForeColor = Color.FromArgb(56, 62, 74)
        });

        _componentServicesListBox = new ListBox
        {
            Location = new Point(0, 330),
            Size = new Size(ContentWidth, 120),
            IntegralHeight = false
        };
        page.Controls.Add(_componentServicesListBox);

        return page;
    }

    private Control CreateDatabasePage()
    {
        Panel page = CreatePagePanel();
        TableLayoutPanel grid = CreateFormGrid(ContentWidth, 5);
        page.Controls.Add(grid);

        _databaseAddressTextBox = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "localhost" };
        _databaseAddressTextBox.TextChanged += (_, _) =>
        {
            ResetDatabaseInstallationDecision();
            RefreshDatabasePage();
            RefreshReviewPage();
        };
        AddLabeledControl(grid, "Database address", _databaseAddressTextBox);

        TableLayoutPanel passwordRow = CreatePasswordRow(out _saPasswordTextBox, out _toggleSaPasswordButton);
        _saPasswordTextBox.TextChanged += (_, _) =>
        {
            ResetDatabaseInstallationDecision();
            RefreshDatabasePage();
            RefreshReviewPage();
        };
        AddLabeledControl(grid, "SA password", passwordRow);

        _adminConnectionPreviewTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true
        };
        AddLabeledControl(grid, "Admin connection", _adminConnectionPreviewTextBox);

        var dataPathRow = CreatePathRow(out _sqlDataPathTextBox, BrowseSqlDataPath);
        _sqlDataPathTextBox.TextChanged += (_, _) =>
        {
            ResetDatabaseInstallationDecision();
            RefreshDatabasePage();
            RefreshReviewPage();
        };
        AddLabeledControl(grid, "SQL data path", dataPathRow);

        TableLayoutPanel validationRow = CreateDatabaseValidationRow();
        AddLabeledControl(grid, "Validation", validationRow);

        page.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(0, 232),
            Size = new Size(ContentWidth, 24),
            Text = "Database scripts from Resources",
            ForeColor = Color.FromArgb(56, 62, 74)
        });

        _databaseScriptsListBox = new ListBox
        {
            Location = new Point(0, 260),
            Size = new Size(ContentWidth, 140),
            IntegralHeight = false
        };
        page.Controls.Add(_databaseScriptsListBox);

        return page;
    }

    private Control CreateGlobalVarsPage()
    {
        Panel page = CreatePagePanel();
        TableLayoutPanel grid = CreateFormGrid(ContentWidth, 14);
        page.Controls.Add(grid);

        _masterKeyNameTextBox = new TextBox { Dock = DockStyle.Fill };
        AddLabeledControl(grid, "Master key env var", _masterKeyNameTextBox);

        TableLayoutPanel masterKeyValueRow = CreatePasswordRow(out _masterKeyValueTextBox, out _);
        AddLabeledControl(grid, "Master key value", masterKeyValueRow);

        _controllerUsernameTextBox = new TextBox { Dock = DockStyle.Fill };
        AddLabeledControl(grid, "Controller username", _controllerUsernameTextBox);

        TableLayoutPanel controllerPasswordRow = CreatePasswordRow(out _controllerPasswordTextBox, out _);
        AddLabeledControl(grid, "Controller password", controllerPasswordRow);

        _controllerUrlTextBox = new TextBox { Dock = DockStyle.Fill };
        AddLabeledControl(grid, "Controller URL", _controllerUrlTextBox);

        _controllerPortTextBox = new TextBox { Dock = DockStyle.Fill };
        AddLabeledControl(grid, "Controller port", _controllerPortTextBox);

        _authUrlTextBox = new TextBox { Dock = DockStyle.Fill };
        AddLabeledControl(grid, "Auth URL", _authUrlTextBox);

        _certThumbprintTextBox = new TextBox { Dock = DockStyle.Fill };
        AddLabeledControl(grid, "Cert thumbprint", _certThumbprintTextBox);

        var logPathRow = CreatePathRow(out _logPathTextBox, BrowseLogPath);
        AddLabeledControl(grid, "Log path", logPathRow);

        TableLayoutPanel jwtKeyRow = CreatePasswordRow(out _jwtKeyTextBox, out _);
        AddLabeledControl(grid, "JWT key", jwtKeyRow);

        TableLayoutPanel jwtIssuerRow = CreatePasswordRow(out _jwtIssuerTextBox, out _);
        AddLabeledControl(grid, "JWT issuer", jwtIssuerRow);

        TableLayoutPanel jwtAudienceRow = CreatePasswordRow(out _jwtAudienceTextBox, out _);
        AddLabeledControl(grid, "JWT audience", jwtAudienceRow);

        _smtpUsernameTextBox = new TextBox { Dock = DockStyle.Fill };
        AddLabeledControl(grid, "SMTP username", _smtpUsernameTextBox);

        TableLayoutPanel smtpPasswordRow = CreatePasswordRow(out _smtpPasswordTextBox, out _);
        AddLabeledControl(grid, "SMTP password", smtpPasswordRow);

        return page;
    }

    private Control CreateServicesPage()
    {
        Panel page = CreatePagePanel();

        var pathLabel = CreateFieldLabel("Service installation folder");
        pathLabel.Location = new Point(0, 0);
        page.Controls.Add(pathLabel);

        var servicePathRow = CreatePathRow(out _serviceInstallPathTextBox, BrowseServiceInstallPath);
        servicePathRow.Dock = DockStyle.None;
        servicePathRow.Location = new Point(0, 28);
        servicePathRow.Size = new Size(ContentWidth, 36);
        page.Controls.Add(servicePathRow);

        page.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(0, 88),
            Size = new Size(ContentWidth, 40),
            ForeColor = AccentColor,
            Text = "The service manager is included for Windows services. AutoTrade Web Admin is deployed as an IIS site."
        });

        page.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(0, 142),
            Size = new Size(ContentWidth, 24),
            Text = "Services and IIS apps to install",
            ForeColor = Color.FromArgb(56, 62, 74)
        });

        _servicesToInstallListBox = new ListBox
        {
            Location = new Point(0, 170),
            Size = new Size(ContentWidth, 108),
            IntegralHeight = false
        };
        _servicesToInstallListBox.SelectedIndexChanged += (_, _) => SelectServiceFromInstallList();
        page.Controls.Add(_servicesToInstallListBox);

        page.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(0, 302),
            Size = new Size(ContentWidth, 24),
            Text = "Per-service identity",
            ForeColor = Color.FromArgb(56, 62, 74)
        });

        TableLayoutPanel serviceGrid = CreateFormGrid(ContentWidth, 7);
        serviceGrid.Location = new Point(0, 330);
        page.Controls.Add(serviceGrid);

        _serviceConfigurationComboBox = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _serviceConfigurationComboBox.SelectedIndexChanged += (_, _) => LoadSelectedServiceSettingsIntoForm();
        AddLabeledControl(serviceGrid, "Service to configure", _serviceConfigurationComboBox);

        _settingsServiceNameTextBox = new TextBox { Dock = DockStyle.Fill };
        _settingsServiceNameTextBox.TextChanged += (_, _) => SaveSelectedServiceSettingsFromForm();
        AddLabeledControl(serviceGrid, "Service_Name", _settingsServiceNameTextBox);

        var serviceIdRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        serviceIdRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        serviceIdRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        serviceIdRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        _serviceIdTextBox = new TextBox { Dock = DockStyle.Top };
        _serviceIdTextBox.TextChanged += (_, _) => SaveSelectedServiceSettingsFromForm();
        serviceIdRow.Controls.Add(_serviceIdTextBox, 0, 0);
        var generateServiceIdButton = new Button
        {
            Dock = DockStyle.Top,
            Height = 27,
            Margin = new Padding(4, 0, 0, 0),
            Text = "New GUID"
        };
        generateServiceIdButton.Click += (_, _) => _serviceIdTextBox.Text = Guid.NewGuid().ToString("D").ToUpperInvariant();
        serviceIdRow.Controls.Add(generateServiceIdButton, 1, 0);
        AddLabeledControl(serviceGrid, "Service ID", serviceIdRow);

        _serviceFriendlyNameTextBox = new TextBox { Dock = DockStyle.Fill };
        _serviceFriendlyNameTextBox.TextChanged += (_, _) => SaveSelectedServiceSettingsFromForm();
        AddLabeledControl(serviceGrid, "Friendly name", _serviceFriendlyNameTextBox);

        _servicePortTextBox = new TextBox { Dock = DockStyle.Fill };
        _servicePortTextBox.TextChanged += (_, _) => SaveSelectedServiceSettingsFromForm();
        AddLabeledControl(serviceGrid, "Service_Port", _servicePortTextBox);

        _serviceManagerUsernameTextBox = new TextBox { Dock = DockStyle.Fill };
        AddLabeledControl(serviceGrid, "SvcMgr admin user", _serviceManagerUsernameTextBox);

        TableLayoutPanel serviceManagerPasswordRow = CreatePasswordRow(out _serviceManagerPasswordTextBox, out _);
        AddLabeledControl(serviceGrid, "SvcMgr admin password", serviceManagerPasswordRow);

        return page;
    }

    private Control CreateReviewPage()
    {
        Panel page = CreatePagePanel();

        _reviewTextBox = new TextBox
        {
            Location = new Point(0, 0),
            Size = new Size(ContentWidth, 150),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical
        };
        page.Controls.Add(_reviewTextBox);

        page.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(0, 170),
            Size = new Size(ContentWidth, 24),
            Text = "Planned actions",
            ForeColor = Color.FromArgb(56, 62, 74)
        });

        _planListBox = new ListBox
        {
            Location = new Point(0, 198),
            Size = new Size(ContentWidth, 210),
            IntegralHeight = false
        };
        page.Controls.Add(_planListBox);

        _reviewStatusLabel = new Label
        {
            AutoSize = false,
            Location = new Point(0, 424),
            Size = new Size(ContentWidth, 44),
            ForeColor = AccentColor,
            Text = "Click Install to run the selected installation actions."
        };
        page.Controls.Add(_reviewStatusLabel);

        return page;
    }

    private void BrowseInstructionFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select Installation Instruction JSON",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true
        };

        string currentPath = _instructionPathTextBox.Text.Trim();
        if (File.Exists(currentPath))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(Path.GetFullPath(currentPath));
            dialog.FileName = Path.GetFileName(currentPath);
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _instructionPathTextBox.Text = dialog.FileName;
            TryLoadInstructions(showSuccessMessage: true);
        }
    }

    private bool TryLoadInstructions(bool showSuccessMessage)
    {
        string instructionFile = _instructionPathTextBox.Text.Trim();
        _validationListBox.Items.Clear();

        if (string.IsNullOrWhiteSpace(instructionFile))
        {
            SetInstructionStatus("Select an instruction JSON file first.", ErrorColor);
            return false;
        }

        try
        {
            InstallationInstructions loadedInstructions = _loader.Load(instructionFile);
            List<string> validationErrors = _validator.Validate(loadedInstructions).ToList();

            if (validationErrors.Count > 0)
            {
                foreach (string validationError in validationErrors)
                {
                    _validationListBox.Items.Add(validationError);
                }

                _instructions = null;
                _instructionFile = null;
                ClearInstructionDetails();
                SetInstructionStatus("The instruction file loaded, but validation found issues.", ErrorColor);
                return false;
            }

            _instructions = loadedInstructions;
            _instructionFile = instructionFile;
            _validationListBox.Items.Add("Instruction file loaded and validated.");
            SetInstructionStatus("Instruction file is valid.", SuccessColor);
            ApplyInstructionDefaults();

            if (showSuccessMessage)
            {
                MessageBox.Show(this, "Installation instructions loaded successfully.", "FIG Installer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return true;
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException)
        {
            _instructions = null;
            _instructionFile = null;
            ClearInstructionDetails();
            _validationListBox.Items.Add(ex.Message);
            SetInstructionStatus("Could not load the instruction file.", ErrorColor);
            return false;
        }
    }

    private void ApplyInstructionDefaults()
    {
        if (_instructions is null)
        {
            return;
        }

        ResetDatabaseInstallationDecision();

        _autoTradeSystemCheckBox.Checked = _instructions.InstallationTypes.AutoTradeSystem;
        _autoTradeWebAdminCheckBox.Checked = _instructions.InstallationTypes.AutoTradeWebAdmin;
        _priceSyncCheckBox.Checked = _instructions.InstallationTypes.PriceSyncComponent;
        _signalProcessingCheckBox.Checked = _instructions.InstallationTypes.SignalProcessingComponent;
        _brokerCheckBox.Checked = _instructions.InstallationTypes.BrokerComponent;

        _databaseAddressTextBox.Text = ExtractDatabaseAddress(_instructions.Database.AdminConnectionString);
        _saPasswordTextBox.Text = "";
        _sqlDataPathTextBox.Text = _instructions.Database.SqlDataPath;

        _masterKeyNameTextBox.Text = _instructions.MasterKey.EnvironmentVariableName;
        _masterKeyValueTextBox.Text = _instructions.MasterKey.Value;
        _controllerUsernameTextBox.Text = _instructions.Controller.Username;
        _controllerPasswordTextBox.Text = _instructions.Controller.Password;
        _controllerUrlTextBox.Text = _instructions.Controller.Url;
        _controllerPortTextBox.Text = _instructions.Controller.Port.ToString();
        _authUrlTextBox.Text = _instructions.Controller.AuthUrl;
        _certThumbprintTextBox.Text = _instructions.Security.CertThumbprint;
        _logPathTextBox.Text = _instructions.Defaults.LogPath;
        _jwtKeyTextBox.Text = _instructions.Jwt.Key;
        _jwtIssuerTextBox.Text = _instructions.Jwt.Issuer;
        _jwtAudienceTextBox.Text = _instructions.Jwt.Audience;
        _smtpUsernameTextBox.Text = _instructions.Smtp.Username;
        _smtpPasswordTextBox.Text = _instructions.Smtp.Password;
        _serviceInstallPathTextBox.Text = _instructions.Defaults.ServiceInstallPath;

        RefreshDatabasePage();
        RefreshComponentServicesList();
        RefreshServicesPage();
        RefreshReviewPage();
    }

    private void MoveBack()
    {
        if (_installCompleted)
        {
            return;
        }

        int previousIndex = FindVisiblePage(_pageIndex, -1);
        if (previousIndex >= 0)
        {
            ShowPage(previousIndex);
        }
    }

    private async Task MoveNextAsync()
    {
        if (_installCompleted)
        {
            Close();
            return;
        }

        if (_pages[_pageIndex].Kind == PageKind.Instructions && !TryLoadInstructions(showSuccessMessage: false))
        {
            return;
        }

        if (_pages[_pageIndex].Kind == PageKind.Types && !AnyInstallationTypeSelected())
        {
            MessageBox.Show(this, "Select at least one installation type.", "FIG Installer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_pages[_pageIndex].Kind == PageKind.Database && !ValidateDatabaseInputs())
        {
            return;
        }

        if (_pages[_pageIndex].Kind == PageKind.Review)
        {
            await CompleteSkeletonInstallAsync();
            return;
        }

        int nextIndex = FindVisiblePage(_pageIndex, 1);
        if (nextIndex >= 0)
        {
            ShowPage(nextIndex);
        }
    }

    private int FindVisiblePage(int startIndex, int direction)
    {
        for (int index = startIndex + direction; index >= 0 && index < _pages.Count; index += direction)
        {
            if (IsPageVisible(_pages[index].Kind))
            {
                return index;
            }
        }

        return -1;
    }

    private bool IsPageVisible(PageKind kind) =>
        kind switch
        {
            PageKind.Database => ShouldRunDatabasePhase(),
            PageKind.GlobalVars => _autoTradeSystemCheckBox.Checked,
            PageKind.Services => SelectedServices().Count > 0,
            _ => true
        };

    private void ShowPage(int index)
    {
        _pageIndex = index;
        WizardPage page = _pages[index];

        page.OnEnter?.Invoke();

        _titleLabel.Text = page.Title;
        _descriptionLabel.Text = page.Description;

        _contentPanel.SuspendLayout();
        _contentPanel.Controls.Clear();
        page.Content.Dock = DockStyle.Fill;
        _contentPanel.Controls.Add(page.Content);
        _contentPanel.ResumeLayout();

        _backButton.Enabled = FindVisiblePage(index, -1) >= 0 && !_installCompleted;
        _nextButton.Text = page.Kind == PageKind.Review
            ? _installCompleted ? "Finish" : "Install"
            : "Next >";
        _cancelButton.Text = _installCompleted ? "Close" : "Cancel";
    }

    private void RefreshDatabasePage()
    {
        _databaseScriptsListBox.Items.Clear();
        if (_instructions is null)
        {
            _adminConnectionPreviewTextBox.Text = "";
            _databaseStatusLabel.Text = "";
            return;
        }

        foreach (DatabaseDeploymentInstruction database in _instructions.Database.Databases)
        {
            string skipReason = "";
            if (_databaseNamesToSkip.Contains(database.Name))
            {
                skipReason = " (skipped because it already exists)";
            }
            else if (IsBrokerDatabase(database.Name) && !ShouldInstallBrokerDatabase())
            {
                skipReason = " (skipped because Broker is not selected)";
            }
            else if (!IsBrokerDatabase(database.Name) && !_autoTradeSystemCheckBox.Checked)
            {
                skipReason = " (skipped because AutoTrade System is not selected)";
            }

            _databaseScriptsListBox.Items.Add($"{database.Script} -> {database.Name}{skipReason}");
        }

        string adminConnectionString = ConstructAdminConnectionString();
        _adminConnectionPreviewTextBox.Text = ConnectionStringRedactor.Redact(adminConnectionString);

        if (string.IsNullOrWhiteSpace(_databaseAddressTextBox.Text))
        {
            _databaseStatusLabel.Text = "Database address is required.";
            _databaseStatusLabel.ForeColor = ErrorColor;
        }
        else if (string.IsNullOrWhiteSpace(_saPasswordTextBox.Text))
        {
            _databaseStatusLabel.Text = "SA password is required before installation.";
            _databaseStatusLabel.ForeColor = ErrorColor;
        }
        else if (string.IsNullOrWhiteSpace(_sqlDataPathTextBox.Text))
        {
            _databaseStatusLabel.Text = "SQL data path is required.";
            _databaseStatusLabel.ForeColor = ErrorColor;
        }
        else
        {
            _databaseStatusLabel.Text = "Admin connection string was built from the JSON template.";
            _databaseStatusLabel.ForeColor = SuccessColor;
        }
    }

    private bool ValidateDatabaseInputs()
    {
        RefreshDatabasePage();

        if (string.IsNullOrWhiteSpace(_databaseAddressTextBox.Text))
        {
            MessageBox.Show(this, "Enter the database address.", "FIG Installer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _databaseAddressTextBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(_saPasswordTextBox.Text))
        {
            MessageBox.Show(this, "Enter the SA password.", "FIG Installer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _saPasswordTextBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(_sqlDataPathTextBox.Text))
        {
            MessageBox.Show(this, "Enter the SQL data path.", "FIG Installer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _sqlDataPathTextBox.Focus();
            return false;
        }

        return true;
    }

    private async Task TestDatabaseConnectionAsync()
    {
        if (!ValidateDatabaseInputs())
        {
            return;
        }

        _testDatabaseConnectionButton.Enabled = false;
        _databaseStatusLabel.Text = "Testing SQL Server connection...";
        _databaseStatusLabel.ForeColor = AccentColor;

        try
        {
            string connectionString = ConstructAdminConnectionString(initialCatalog: "master", connectTimeout: 5);
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            _databaseStatusLabel.Text = "Connection succeeded.";
            _databaseStatusLabel.ForeColor = SuccessColor;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException or ArgumentException)
        {
            _databaseStatusLabel.Text = $"Connection failed: {ex.Message}";
            _databaseStatusLabel.ForeColor = ErrorColor;
        }
        finally
        {
            _testDatabaseConnectionButton.Enabled = true;
        }
    }

    private async Task<bool> PrepareDatabaseInstallationAsync()
    {
        _databaseNamesToSkip.Clear();
        _existingInstallationDatabaseNames.Clear();

        if (!ShouldRunDatabasePhase())
        {
            return true;
        }

        if (!ValidateDatabaseInputs())
        {
            return false;
        }

        _reviewStatusLabel.Text = "Checking for an existing FIG database installation...";
        _reviewStatusLabel.ForeColor = AccentColor;

        try
        {
            if (_autoTradeSystemCheckBox.Checked)
            {
                IReadOnlyList<string> existingAutoTradeDatabases = await GetExistingDatabasesAsync(PreviousInstallationDatabaseNames);
                if (existingAutoTradeDatabases.Count > 0 &&
                    !ConfirmSkipExistingDatabases(
                        existingAutoTradeDatabases,
                        "A previous AutoTrade database installation was found."))
                {
                    return false;
                }
            }

            if (ShouldInstallBrokerDatabase() && HasBrokerDatabaseInstruction())
            {
                IReadOnlyList<string> existingBrokerDatabases = await GetExistingDatabasesAsync(["FIGBroker"]);
                if (existingBrokerDatabases.Count > 0 &&
                    !ConfirmSkipExistingDatabases(
                        existingBrokerDatabases,
                        "A previous FIGBroker database installation was found."))
                {
                    return false;
                }
            }
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException or ArgumentException)
        {
            _reviewStatusLabel.Text = "Could not check the SQL Server database list.";
            _reviewStatusLabel.ForeColor = ErrorColor;
            MessageBox.Show(this, $"Could not check for an existing database installation.\r\n\r\n{ex.Message}", "FIG Installer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        RefreshReviewPage();
        return true;
    }

    private bool ConfirmSkipExistingDatabases(IReadOnlyList<string> existingDatabases, string messageLead)
    {
        string databaseList = string.Join(", ", existingDatabases);
        DialogResult result = MessageBox.Show(
            this,
            $"{messageLead}\r\n\r\nExisting databases: {databaseList}\r\n\r\nClick OK to continue and skip only these existing databases.\r\nOther selected databases will still be checked and installed.\r\nClick Cancel to terminate setup.",
            "Previous Database Installation",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Cancel)
        {
            _reviewStatusLabel.Text = "Installation cancelled because an existing database was found.";
            _reviewStatusLabel.ForeColor = ErrorColor;
            Close();
            return false;
        }

        foreach (string databaseName in existingDatabases)
        {
            if (_databaseNamesToSkip.Add(databaseName))
            {
                _existingInstallationDatabaseNames.Add(databaseName);
            }
        }

        return true;
    }

    private async Task<IReadOnlyList<string>> GetExistingDatabasesAsync(IEnumerable<string> databaseNames)
    {
        string connectionString = ConstructAdminConnectionString(initialCatalog: "master", connectTimeout: 10);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return await GetExistingDatabasesAsync(connection, databaseNames);
    }

    private string ConstructAdminConnectionString(string? initialCatalog = null, int? connectTimeout = null)
    {
        if (_instructions is null)
        {
            return "";
        }

        string connectionString = _instructions.Database.AdminConnectionString
            .Replace("<Database_Address>", _databaseAddressTextBox.Text.Trim(), StringComparison.OrdinalIgnoreCase)
            .Replace("<Admin_Password>", _saPasswordTextBox.Text, StringComparison.OrdinalIgnoreCase);

        try
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            builder["Data Source"] = _databaseAddressTextBox.Text.Trim();
            builder["Password"] = _saPasswordTextBox.Text;
            if (!string.IsNullOrWhiteSpace(initialCatalog))
            {
                builder["Initial Catalog"] = initialCatalog;
            }

            if (connectTimeout is not null)
            {
                builder["Connect Timeout"] = connectTimeout.Value;
            }

            return builder.ConnectionString;
        }
        catch (ArgumentException)
        {
            return connectionString;
        }
    }

    private static string ExtractDatabaseAddress(string connectionStringTemplate)
    {
        if (string.IsNullOrWhiteSpace(connectionStringTemplate))
        {
            return "localhost";
        }

        if (connectionStringTemplate.Contains("<Database_Address>", StringComparison.OrdinalIgnoreCase))
        {
            return "localhost";
        }

        try
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionStringTemplate.Replace("<Admin_Password>", "", StringComparison.OrdinalIgnoreCase)
            };

            foreach (string key in new[] { "Data Source", "Server", "Address", "Addr", "Network Address" })
            {
                if (builder.TryGetValue(key, out object? value))
                {
                    return value?.ToString() ?? "localhost";
                }
            }
        }
        catch (ArgumentException)
        {
        }

        return "localhost";
    }

    private void RefreshGlobalVarsPage()
    {
    }

    private void RefreshServicesPage()
    {
        if (_serviceInstallPathTextBox is not null && string.IsNullOrWhiteSpace(_serviceInstallPathTextBox.Text) && _instructions is not null)
        {
            _serviceInstallPathTextBox.Text = _instructions.Defaults.ServiceInstallPath;
        }

        if (_servicesToInstallListBox is null)
        {
            return;
        }

        IReadOnlyList<string> services = SelectedServices(includeServiceManager: true);
        SyncServiceSettingsWithSelection(services);

        RefreshServiceConfigurationComboBox(services);
        RefreshServicesListBox(services);
    }

    private void RefreshServicesListBox(IReadOnlyList<string> services)
    {
        if (_servicesToInstallListBox is null)
        {
            return;
        }

        string? selectedService = (_servicesToInstallListBox.SelectedItem as ServiceListItem)?.InstallerServiceName
            ?? _serviceConfigurationComboBox?.SelectedItem?.ToString();

        _loadingServiceSettings = true;
        try
        {
            _servicesToInstallListBox.Items.Clear();
            foreach (string serviceName in services)
            {
                string resourceStatus = ServiceResourceExists(serviceName) ? "resource found" : "resource missing or pending";
                ServiceInstallSettings settings = GetServiceSettings(serviceName);
                _servicesToInstallListBox.Items.Add(new ServiceListItem(
                    serviceName,
                    settings.SettingsServiceName,
                    settings.FriendlyName,
                    settings.ServiceId,
                    settings.ServicePort,
                    resourceStatus));
            }

            if (!string.IsNullOrWhiteSpace(selectedService))
            {
                for (int i = 0; i < _servicesToInstallListBox.Items.Count; i++)
                {
                    if (_servicesToInstallListBox.Items[i] is ServiceListItem item &&
                        string.Equals(item.InstallerServiceName, selectedService, StringComparison.OrdinalIgnoreCase))
                    {
                        _servicesToInstallListBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
        finally
        {
            _loadingServiceSettings = false;
        }
    }

    private void SyncServiceSettingsWithSelection(IEnumerable<string> serviceNames)
    {
        foreach (string serviceName in serviceNames)
        {
            _ = GetServiceSettings(serviceName);
        }
    }

    private ServiceInstallSettings GetServiceSettings(string serviceName)
    {
        if (_serviceSettings.TryGetValue(serviceName, out ServiceInstallSettings? settings))
        {
            return settings;
        }

        settings = LoadDefaultServiceSettings(serviceName);
        _serviceSettings[serviceName] = settings;
        return settings;
    }

    private ServiceInstallSettings LoadDefaultServiceSettings(string serviceName)
    {
        string serviceId = Guid.NewGuid().ToString("D").ToUpperInvariant();
        string settingsServiceName = serviceName;
        string friendlyName = serviceName;
        string servicePort = "";

        if (ServiceDescriptors.TryGetValue(serviceName, out ServiceDescriptor? descriptor))
        {
            string settingsPath = ResolveServiceResourceFile(serviceName, descriptor.SettingsFile);
            if (File.Exists(settingsPath))
            {
                try
                {
                    JsonNode? root = JsonNode.Parse(File.ReadAllText(settingsPath));
                    JsonObject? vars = root?["Global"]?["Vars"]?.AsObject();
                    serviceId = vars?["Service_Id"]?.GetValue<string>() ?? serviceId;
                    settingsServiceName = vars?["Service_Name"]?.GetValue<string>() ?? settingsServiceName;
                    friendlyName = vars?["Service_FriendlyName"]?.GetValue<string>() ?? friendlyName;
                    servicePort = GetJsonValueAsString(vars?["Service_Port"]) ?? servicePort;
                }
                catch (JsonException)
                {
                }
            }
        }

        return new ServiceInstallSettings(
            serviceName,
            settingsServiceName,
            serviceId.ToUpperInvariant(),
            friendlyName,
            servicePort);
    }

    private void RefreshServiceConfigurationComboBox(IReadOnlyList<string> services)
    {
        if (_serviceConfigurationComboBox is null)
        {
            return;
        }

        string? previousSelection = _serviceConfigurationComboBox.SelectedItem?.ToString();
        _loadingServiceSettings = true;
        try
        {
            _serviceConfigurationComboBox.Items.Clear();
            foreach (string serviceName in services)
            {
                _serviceConfigurationComboBox.Items.Add(serviceName);
            }

            if (services.Count == 0)
            {
                _serviceConfigurationComboBox.SelectedIndex = -1;
                _settingsServiceNameTextBox.Text = "";
                _serviceIdTextBox.Text = "";
                _serviceFriendlyNameTextBox.Text = "";
                _servicePortTextBox.Text = "";
                _settingsServiceNameTextBox.Enabled = false;
                _serviceIdTextBox.Enabled = false;
                _serviceFriendlyNameTextBox.Enabled = false;
                _servicePortTextBox.Enabled = false;
                return;
            }

            int selectedIndex = previousSelection is null
                ? -1
                : services.ToList().FindIndex(serviceName => string.Equals(serviceName, previousSelection, StringComparison.OrdinalIgnoreCase));
            _serviceConfigurationComboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            _settingsServiceNameTextBox.Enabled = true;
            _serviceIdTextBox.Enabled = true;
            _serviceFriendlyNameTextBox.Enabled = true;
            _servicePortTextBox.Enabled = true;
        }
        finally
        {
            _loadingServiceSettings = false;
        }

        LoadSelectedServiceSettingsIntoForm();
    }

    private void LoadSelectedServiceSettingsIntoForm()
    {
        if (_loadingServiceSettings || _serviceConfigurationComboBox is null)
        {
            return;
        }

        string? serviceName = _serviceConfigurationComboBox.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return;
        }

        ServiceInstallSettings settings = GetServiceSettings(serviceName);
        _loadingServiceSettings = true;
        try
        {
            _settingsServiceNameTextBox.Text = settings.SettingsServiceName;
            _serviceIdTextBox.Text = settings.ServiceId;
            _serviceFriendlyNameTextBox.Text = settings.FriendlyName;
            _servicePortTextBox.Text = settings.ServicePort;
        }
        finally
        {
            _loadingServiceSettings = false;
        }
    }

    private void SaveSelectedServiceSettingsFromForm()
    {
        if (_loadingServiceSettings || _serviceConfigurationComboBox is null)
        {
            return;
        }

        string? serviceName = _serviceConfigurationComboBox.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return;
        }

        _serviceSettings[serviceName] = new ServiceInstallSettings(
            serviceName,
            _settingsServiceNameTextBox.Text.Trim(),
            _serviceIdTextBox.Text.Trim().ToUpperInvariant(),
            _serviceFriendlyNameTextBox.Text.Trim(),
            _servicePortTextBox.Text.Trim());
        RefreshServicesListBox(SelectedServices(includeServiceManager: true));
        RefreshReviewPage();
    }

    private void SelectServiceFromInstallList()
    {
        if (_loadingServiceSettings || _serviceConfigurationComboBox is null)
        {
            return;
        }

        if (_servicesToInstallListBox.SelectedItem is not ServiceListItem item)
        {
            return;
        }

        _serviceConfigurationComboBox.SelectedItem = item.InstallerServiceName;
    }

    private void RefreshReviewPage()
    {
        if (_instructions is null)
        {
            _reviewTextBox.Text = "";
            _planListBox.Items.Clear();
            _reviewStatusLabel.Text = "Load a valid instruction file first.";
            _reviewStatusLabel.ForeColor = ErrorColor;
            return;
        }

        _reviewTextBox.Text =
            $"Instruction file: {Path.GetFullPath(_instructionFile ?? "")}\r\n" +
            $"Installation: {Display(_instructions.Metadata.InstallationName)} / {Display(_instructions.Metadata.EnvironmentName)}\r\n" +
            $"Selected types: {string.Join(", ", SelectedInstallationTypeNames())}\r\n" +
            $"Database address: {Display(_databaseAddressTextBox.Text)}\r\n" +
            $"Service folder: {Display(_serviceInstallPathTextBox.Text)}\r\n" +
            $"Log path: {Display(_logPathTextBox.Text)}";

        _planListBox.Items.Clear();
        foreach (string action in BuildPlan())
        {
            _planListBox.Items.Add(action);
        }

        if (!_installCompleted)
        {
            _reviewStatusLabel.Text = "Click Install to run the selected installation actions.";
            _reviewStatusLabel.ForeColor = AccentColor;
        }
    }

    private IEnumerable<string> BuildPlan()
    {
        yield return "Load and validate installation instruction JSON.";

        if (ShouldRunDatabasePhase())
        {
            yield return $"Connect to SQL Server at {_databaseAddressTextBox.Text} using the constructed admin connection string.";
            if (_autoTradeSystemCheckBox.Checked)
            {
                yield return "Check SQL Server for existing FIGAutoTrader, FIGUser, and FIGAlert databases.";
            }

            if (ShouldInstallBrokerDatabase() && HasBrokerDatabaseInstruction())
            {
                yield return "Check SQL Server separately for an existing FIGBroker database.";
            }

            if (_databaseNamesToSkip.Count > 0)
            {
                yield return $"Skip database creation/scripts for existing databases: {string.Join(", ", _databaseNamesToSkip.OrderBy(databaseName => databaseName, StringComparer.OrdinalIgnoreCase))}.";
            }

            IReadOnlyList<DatabaseFilePlan> databaseFilePlans = DatabaseFilePlansToInstall();
            if (databaseFilePlans.Count > 0)
            {
                yield return $"Create SQL Server databases under {_sqlDataPathTextBox.Text}.";
                foreach (DatabaseFilePlan databaseFilePlan in databaseFilePlans)
                {
                    yield return $"Create {databaseFilePlan.Database.Name} with data file {databaseFilePlan.DataFilePath} and log file {databaseFilePlan.LogFilePath}.";
                    yield return $"Execute Resources\\{databaseFilePlan.Database.Script} against {databaseFilePlan.Database.Name}.";
                }
            }
            else
            {
                yield return "No new database creation is needed for the selected database components.";
            }

            if (!ShouldInstallBrokerDatabase() && HasBrokerDatabaseInstruction())
            {
                yield return "Skip FIGBroker database because Broker is not selected for installation.";
            }

            yield return $"Create SQL login {_instructions?.Database.RootsUsername ?? "roots"} with password policy disabled and password expiration disabled.";
            yield return "Map roots to selected FIG databases that exist after installation as db_owner and public.";

            if (_autoTradeSystemCheckBox.Checked)
            {
                yield return $"Ensure machine environment variable {_masterKeyNameTextBox.Text} exists.";
                yield return "Generate global_vars.json from Resources\\global_vars.json with token replacements.";
                yield return "Encrypt controller password, roots DB connection strings, admin DB connection string, JWT values, and SMTP values with ProtectedDataUtil.";
            }
        }

        if (_autoTradeWebAdminCheckBox.Checked)
        {
            yield return "AutoTrade Web Admin selected: SQL Server and web-admin special instructions still need to be wired.";
        }

        foreach (string serviceName in SelectedWindowsServices(includeServiceManager: true))
        {
            ServiceDescriptor descriptor = GetServiceDescriptor(serviceName);
            ServiceInstallSettings settings = GetServiceSettings(serviceName);
            yield return $"Copy Resources\\Distrib\\{descriptor.ResourceFolder} contents to {_serviceInstallPathTextBox.Text}.";
            yield return $"Patch {settings.SettingsServiceName} settings so Global.VarPath points to {_serviceInstallPathTextBox.Text}\\global_vars.json.";
            yield return $"Install Windows service {settings.SettingsServiceName} and configure automatic startup plus service recovery.";
        }

        if (SelectedWindowsServices().Count > 0)
        {
            ServiceInstallSettings serviceManagerSettings = GetServiceSettings(ServiceManagerServiceKey);
            yield return $"Ensure {serviceManagerSettings.SettingsServiceName} is installed once and ServiceManagerConfig.ManagedServices includes selected services.";
        }

        if (ShouldInstallAutoTraderAdminIisApplication())
        {
            AutoTraderAdminIisInstruction iis = GetAutoTraderAdminIisDefaults();
            ServiceDescriptor descriptor = GetServiceDescriptor(AutoTraderAdminServiceKey);
            yield return "Verify IIS is installed and the World Wide Web Publishing Service is running.";
            yield return $"Copy Resources\\Distrib\\{descriptor.ResourceFolder} to {_serviceInstallPathTextBox.Text}\\{descriptor.ResourceFolder}.";
            yield return $"Create or update IIS application pool {iis.ApplicationPoolName} as No Managed Code, Integrated, AlwaysRunning, idle timeout 0, and regular recycle interval 0.";
            yield return $"Create or update IIS site {iis.SiteName} using default HTTPS binding *:{iis.HttpsPort}: plus hostname {Display(iis.HostName)} and a selected certificate.";
        }

        yield return "Install button executes selected database, settings, IIS, and Windows service actions.";
    }

    private async Task CompleteSkeletonInstallAsync()
    {
        _nextButton.Enabled = false;
        string installPhase = "installation";

        try
        {
            installPhase = "database pre-check";
            if (!await PrepareDatabaseInstallationAsync())
            {
                return;
            }

            if (ShouldRunDatabasePhase() && DatabaseFilePlansToInstall().Count > 0)
            {
                installPhase = "database installation";
                await InstallDatabasesAsync();
            }

            if (ShouldRunDatabasePhase())
            {
                installPhase = "database user mapping";
                await EnsureRootsSqlLoginAndDatabaseUsersAsync();
            }

            if (SelectedServices().Count > 0)
            {
                installPhase = "application and service installation";
                if (!await InstallSelectedApplicationsAndServicesAsync())
                {
                    return;
                }
            }

            _installCompleted = true;
            RefreshReviewPage();
            _reviewStatusLabel.Text = _databaseNamesToSkip.Count > 0
                ? $"Installation completed. Existing databases were skipped: {string.Join(", ", _databaseNamesToSkip.OrderBy(databaseName => databaseName, StringComparer.OrdinalIgnoreCase))}."
                : "Installation completed. Database and selected Windows services were installed.";
            _reviewStatusLabel.ForeColor = SuccessColor;
            ShowPage(_pageIndex);
        }
        catch (Exception ex) when (ex is SqlException or IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            string failureTitle = $"{installPhase} failed.";
            _reviewStatusLabel.Text = failureTitle;
            _reviewStatusLabel.ForeColor = ErrorColor;
            MessageBox.Show(this, $"{failureTitle}\r\n\r\n{ex.Message}", "FIG Installer", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (!_installCompleted && !IsDisposed)
            {
                _nextButton.Enabled = true;
            }
        }
    }

    private async Task InstallDatabasesAsync()
    {
        IReadOnlyList<DatabaseFilePlan> databaseFilePlans = DatabaseFilePlansToInstall();
        if (databaseFilePlans.Count == 0)
        {
            return;
        }

        EnsureLocalSqlDataPathIfPossible();

        string connectionString = ConstructAdminConnectionString(initialCatalog: "master");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        IReadOnlyList<string> existingTargetDatabases = await GetExistingDatabasesAsync(connection, databaseFilePlans.Select(plan => plan.Database.Name));
        if (existingTargetDatabases.Count > 0)
        {
            throw new InvalidOperationException($"Cannot install database scripts because these target databases already exist: {string.Join(", ", existingTargetDatabases)}.");
        }

        foreach (DatabaseFilePlan databaseFilePlan in databaseFilePlans)
        {
            _reviewStatusLabel.Text = $"Creating database {databaseFilePlan.Database.Name}...";
            _reviewStatusLabel.ForeColor = AccentColor;
            await ExecuteSqlAsync(connection, "USE [master];");
            await ExecuteSqlAsync(connection, BuildCreateDatabaseSql(databaseFilePlan));

            _reviewStatusLabel.Text = $"Running script for {databaseFilePlan.Database.Name}...";
            string scriptPath = ResolveDatabaseScriptPath(databaseFilePlan.Database.Script);
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"Database script was not found: {scriptPath}", scriptPath);
            }

            string script = File.ReadAllText(scriptPath);
            await ExecuteSqlScriptAsync(connection, script);
            await VerifyDatabaseFileLocationsAsync(connection, databaseFilePlan);
        }
    }

    private async Task<IReadOnlyList<string>> GetExistingDatabasesAsync(SqlConnection connection, IEnumerable<string> databaseNames)
    {
        string[] requestedDatabaseNames = databaseNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requestedDatabaseNames.Length == 0)
        {
            return [];
        }

        string[] parameterNames = requestedDatabaseNames
            .Select((_, index) => $"@db{index}")
            .ToArray();

        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT [name] FROM sys.databases WHERE [name] IN ({string.Join(", ", parameterNames)});";

        for (int index = 0; index < requestedDatabaseNames.Length; index++)
        {
            command.Parameters.AddWithValue(parameterNames[index], requestedDatabaseNames[index]);
        }

        var foundDatabaseNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            foundDatabaseNames.Add(reader.GetString(0));
        }

        return requestedDatabaseNames
            .Where(foundDatabaseNames.Contains)
            .ToList();
    }

    private async Task ExecuteSqlScriptAsync(SqlConnection connection, string script)
    {
        foreach (string batch in SplitSqlBatches(script))
        {
            await ExecuteSqlAsync(connection, batch);
        }
    }

    private static async Task ExecuteSqlAsync(SqlConnection connection, string commandText)
    {
        if (string.IsNullOrWhiteSpace(commandText))
        {
            return;
        }

        await using SqlCommand command = connection.CreateCommand();
        command.CommandTimeout = 0;
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync();
    }

    private async Task VerifyDatabaseFileLocationsAsync(SqlConnection connection, DatabaseFilePlan databaseFilePlan)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT [type], [physical_name]
            FROM sys.master_files
            WHERE [database_id] = DB_ID(@databaseName)
              AND [type] IN (0, 1);
            """;
        command.Parameters.AddWithValue("@databaseName", databaseFilePlan.Database.Name);

        string? actualDataFilePath = null;
        string? actualLogFilePath = null;

        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            byte type = reader.GetByte(0);
            string physicalName = reader.GetString(1);

            if (type == 0)
            {
                actualDataFilePath = physicalName;
            }
            else if (type == 1)
            {
                actualLogFilePath = physicalName;
            }
        }

        if (!SamePath(actualDataFilePath, databaseFilePlan.DataFilePath) ||
            !SamePath(actualLogFilePath, databaseFilePlan.LogFilePath))
        {
            throw new InvalidOperationException(
                $"Database {databaseFilePlan.Database.Name} was created, but SQL Server reports unexpected file locations. Expected data/log files under {_sqlDataPathTextBox.Text.Trim()}.");
        }
    }

    private async Task<bool> InstallSelectedApplicationsAndServicesAsync()
    {
        EnsureRunningAsAdministrator();

        if (ShouldInstallAutoTraderAdminIisApplication())
        {
            await EnsureIisInstalledAndRunningAsync();
        }

        if (!PromptForServiceIdentitySettings())
        {
            _reviewStatusLabel.Text = "Installation cancelled.";
            _reviewStatusLabel.ForeColor = ErrorColor;
            return false;
        }

        if (!PromptForServiceManagerCredentialsIfNeeded())
        {
            _reviewStatusLabel.Text = "Installation cancelled.";
            _reviewStatusLabel.ForeColor = ErrorColor;
            return false;
        }

        IisSiteInstallSettings? autoTraderAdminIisSettings = null;
        if (ShouldInstallAutoTraderAdminIisApplication())
        {
            if (!PromptForAutoTraderAdminIisSettings(out autoTraderAdminIisSettings))
            {
                _reviewStatusLabel.Text = "IIS application installation cancelled.";
                _reviewStatusLabel.ForeColor = ErrorColor;
                return false;
            }

            SyncAutoTraderAdminServicePort(autoTraderAdminIisSettings);
        }

        ValidateServiceInstallInputs();

        string serviceInstallPath = _serviceInstallPathTextBox.Text.Trim();
        Directory.CreateDirectory(serviceInstallPath);
        EnsureMasterKeyEnvironmentVariable();

        string globalVarsPath = Path.Combine(serviceInstallPath, "global_vars.json");
        WriteGlobalVarsFile(globalVarsPath);

        foreach (string serviceName in OrderedWindowsServicesToInstall())
        {
            ServiceInstallSettings settings = GetServiceSettings(serviceName);
            string windowsServiceName = settings.SettingsServiceName;

            _reviewStatusLabel.Text = $"Installing {windowsServiceName}...";
            _reviewStatusLabel.ForeColor = AccentColor;

            ServiceDescriptor descriptor = GetServiceDescriptor(serviceName);
            string legacyWindowsServiceName = GetWindowsServiceName(descriptor);
            if (!string.Equals(legacyWindowsServiceName, windowsServiceName, StringComparison.OrdinalIgnoreCase))
            {
                await DeleteServiceIfExistsAsync(legacyWindowsServiceName);
            }

            await StopServiceIfExistsAsync(windowsServiceName);
            CopyServiceResourcesToInstallPath(serviceName, serviceInstallPath);
            PatchServiceSettings(serviceName, serviceInstallPath, globalVarsPath);

            string executablePath = Path.Combine(serviceInstallPath, descriptor.ExecutableFile);
            if (!File.Exists(executablePath))
            {
                throw new FileNotFoundException($"Service executable was not found after copying resources: {executablePath}", executablePath);
            }

            WindowsServiceAccount account = GetWindowsServiceAccount(serviceName);
            await InstallOrUpdateWindowsServiceAsync(
                windowsServiceName,
                windowsServiceName,
                settings.FriendlyName,
                BuildServiceBinaryPath(executablePath, windowsServiceName),
                account);
        }

        if (autoTraderAdminIisSettings is not null)
        {
            await InstallAutoTraderAdminIisApplicationAsync(serviceInstallPath, globalVarsPath, autoTraderAdminIisSettings);
        }

        return true;
    }

    private AutoTraderAdminIisInstruction GetAutoTraderAdminIisDefaults()
    {
        IisInstruction? iis = _instructions?.Iis;
        return iis?.AutoTraderAdmin ?? new AutoTraderAdminIisInstruction();
    }

    private bool PromptForAutoTraderAdminIisSettings(out IisSiteInstallSettings settings)
    {
        AutoTraderAdminIisInstruction defaults = GetAutoTraderAdminIisDefaults();
        settings = new IisSiteInstallSettings(
            defaults.SiteName,
            defaults.ApplicationPoolName,
            defaults.HttpsPort,
            defaults.HostName,
            "");

        IReadOnlyList<IisCertificateListItem> certificates;
        try
        {
            certificates = LoadLocalMachineServerCertificates();
        }
        catch (Exception ex) when (ex is CryptographicException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                this,
                $"Could not read LocalMachine certificate store.\r\n\r\n{ex.Message}",
                "FIG Installer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }

        if (certificates.Count == 0)
        {
            MessageBox.Show(
                this,
                "No LocalMachine\\My certificates with a private key were found. Install the IIS HTTPS certificate first, then run the installer again.",
                "FIG Installer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        using var dialog = new Form
        {
            Text = "IIS Site (AutoTrade Web Admin)",
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(560, 256),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            Font = Font
        };

        var headerLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 16),
            Size = new Size(510, 26),
            Text = $"{defaults.SiteName} / {defaults.ApplicationPoolName}",
            Font = new Font(Font, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        dialog.Controls.Add(headerLabel);

        var hostLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 58),
            Size = new Size(120, 24),
            Text = "Hostname",
            TextAlign = ContentAlignment.MiddleLeft
        };
        dialog.Controls.Add(hostLabel);

        var hostTextBox = new TextBox
        {
            Location = new Point(150, 58),
            Size = new Size(380, 24),
            Text = defaults.HostName
        };
        dialog.Controls.Add(hostTextBox);

        var portLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 98),
            Size = new Size(120, 24),
            Text = "HTTPS port",
            TextAlign = ContentAlignment.MiddleLeft
        };
        dialog.Controls.Add(portLabel);

        var portTextBox = new TextBox
        {
            Location = new Point(150, 98),
            Size = new Size(110, 24),
            Text = defaults.HttpsPort.ToString(CultureInfo.InvariantCulture)
        };
        dialog.Controls.Add(portTextBox);

        var bindingHintLabel = new Label
        {
            AutoSize = false,
            Location = new Point(272, 98),
            Size = new Size(258, 24),
            ForeColor = Color.FromArgb(86, 92, 104),
            Text = "Default binding is https/*:444:hostname."
        };
        dialog.Controls.Add(bindingHintLabel);

        var certificateLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 138),
            Size = new Size(120, 24),
            Text = "Certificate",
            TextAlign = ContentAlignment.MiddleLeft
        };
        dialog.Controls.Add(certificateLabel);

        var certificateComboBox = new ComboBox
        {
            Location = new Point(150, 138),
            Size = new Size(380, 24),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        foreach (IisCertificateListItem certificate in certificates)
        {
            certificateComboBox.Items.Add(certificate);
        }

        string defaultThumbprint = NormalizeCertificateThumbprint(_instructions?.Security.CertThumbprint ?? "");
        int selectedCertificateIndex = 0;
        if (!string.IsNullOrWhiteSpace(defaultThumbprint))
        {
            for (int index = 0; index < certificateComboBox.Items.Count; index++)
            {
                if (certificateComboBox.Items[index] is IisCertificateListItem item &&
                    string.Equals(item.Thumbprint, defaultThumbprint, StringComparison.OrdinalIgnoreCase))
                {
                    selectedCertificateIndex = index;
                    break;
                }
            }
        }

        certificateComboBox.SelectedIndex = selectedCertificateIndex;
        dialog.Controls.Add(certificateComboBox);

        var okButton = new Button
        {
            Location = new Point(336, 206),
            Size = new Size(90, 30),
            Text = "OK",
            DialogResult = DialogResult.OK
        };
        dialog.Controls.Add(okButton);

        var cancelButton = new Button
        {
            Location = new Point(440, 206),
            Size = new Size(90, 30),
            Text = "Cancel",
            DialogResult = DialogResult.Cancel
        };
        dialog.Controls.Add(cancelButton);

        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;

        while (dialog.ShowDialog(this) == DialogResult.OK)
        {
            string hostName = hostTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(hostName))
            {
                MessageBox.Show(dialog, "Enter the IIS hostname.", "FIG Installer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                hostTextBox.Focus();
                continue;
            }

            if (!int.TryParse(portTextBox.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int httpsPort) ||
                httpsPort is < 1 or > 65535)
            {
                MessageBox.Show(dialog, "Enter an HTTPS port between 1 and 65535.", "FIG Installer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                portTextBox.Focus();
                continue;
            }

            if (certificateComboBox.SelectedItem is not IisCertificateListItem certificate)
            {
                MessageBox.Show(dialog, "Select the IIS certificate.", "FIG Installer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                certificateComboBox.Focus();
                continue;
            }

            settings = new IisSiteInstallSettings(
                defaults.SiteName,
                defaults.ApplicationPoolName,
                httpsPort,
                hostName,
                certificate.Thumbprint);
            return true;
        }

        return false;
    }

    private static IReadOnlyList<IisCertificateListItem> LoadLocalMachineServerCertificates()
    {
        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);

        return store.Certificates
            .OfType<X509Certificate2>()
            .Where(certificate => certificate.HasPrivateKey)
            .OrderBy(certificate => certificate.Subject, StringComparer.OrdinalIgnoreCase)
            .Select(certificate => new IisCertificateListItem(
                NormalizeCertificateThumbprint(certificate.Thumbprint),
                certificate.Subject,
                certificate.NotAfter))
            .ToList();
    }

    private void SyncAutoTraderAdminServicePort(IisSiteInstallSettings settings)
    {
        ServiceInstallSettings current = GetServiceSettings(AutoTraderAdminServiceKey);
        _serviceSettings[AutoTraderAdminServiceKey] = current with
        {
            ServicePort = settings.HttpsPort.ToString(CultureInfo.InvariantCulture)
        };
    }

    private async Task InstallAutoTraderAdminIisApplicationAsync(
        string serviceInstallPath,
        string globalVarsPath,
        IisSiteInstallSettings settings)
    {
        ServiceDescriptor descriptor = GetServiceDescriptor(AutoTraderAdminServiceKey);
        _reviewStatusLabel.Text = $"Copying {descriptor.ResourceFolder} IIS application...";
        _reviewStatusLabel.ForeColor = AccentColor;
        string appPhysicalPath = CopyIisApplicationResourcesToInstallPath(AutoTraderAdminServiceKey, serviceInstallPath);
        PatchServiceSettingsInDirectory(AutoTraderAdminServiceKey, appPhysicalPath, globalVarsPath, settings);

        _reviewStatusLabel.Text = $"Configuring IIS application pool {settings.ApplicationPoolName}...";
        _reviewStatusLabel.ForeColor = AccentColor;
        await EnsureAutoTraderAdminApplicationPoolAsync(settings);

        _reviewStatusLabel.Text = $"Configuring IIS site {settings.SiteName}...";
        _reviewStatusLabel.ForeColor = AccentColor;
        await EnsureAutoTraderAdminSiteAsync(settings, appPhysicalPath);

        _reviewStatusLabel.Text = $"Binding HTTPS certificate for {settings.HostName}:{settings.HttpsPort}...";
        _reviewStatusLabel.ForeColor = AccentColor;
        await BindCertificateToIisHttpsEndpointAsync(settings);
    }

    private string CopyIisApplicationResourcesToInstallPath(string serviceName, string serviceInstallPath)
    {
        ServiceDescriptor descriptor = GetServiceDescriptor(serviceName);
        string sourceDirectory = ResolveServiceResourceDirectory(serviceName);
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"IIS application resources were not found for {serviceName}: {sourceDirectory}");
        }

        string targetDirectory = Path.Combine(serviceInstallPath, descriptor.ResourceFolder);
        CopyDirectory(sourceDirectory, targetDirectory);

        string webConfigPath = Path.Combine(targetDirectory, "web.config");
        if (!File.Exists(webConfigPath))
        {
            throw new FileNotFoundException($"IIS web.config was not found after copying resources: {webConfigPath}", webConfigPath);
        }

        return targetDirectory;
    }

    private async Task EnsureIisInstalledAndRunningAsync()
    {
        _ = ResolveAppCmdPath();

        ScCommandResult serviceQuery = await RunScCommandRawAsync(["query", "W3SVC"]);
        if (serviceQuery.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "IIS is not installed or the World Wide Web Publishing Service (W3SVC) is not available. Install IIS with the management tools and start W3SVC before continuing.");
        }

        if (!serviceQuery.Output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "IIS is installed, but the World Wide Web Publishing Service (W3SVC) is not running. Start W3SVC before continuing.");
        }
    }

    private async Task EnsureAutoTraderAdminApplicationPoolAsync(IisSiteInstallSettings settings)
    {
        if (!await IisApplicationPoolExistsAsync(settings.ApplicationPoolName))
        {
            await RunAppCmdAsync(["add", "apppool", $"/name:{settings.ApplicationPoolName}"]);
        }

        await RunAppCmdAsync(
        [
            "set",
            "apppool",
            $"/apppool.name:{settings.ApplicationPoolName}",
            "/managedRuntimeVersion:",
            "/managedPipelineMode:Integrated",
            "/enable32BitAppOnWin64:false",
            "/queueLength:1000",
            "/startMode:AlwaysRunning",
            "/processModel.identityType:ApplicationPoolIdentity",
            "/processModel.idleTimeout:00:00:00",
            "/recycling.periodicRestart.time:00:00:00"
        ]);

        ScCommandResult startResult = await RunAppCmdRawAsync(["start", "apppool", $"/apppool.name:{settings.ApplicationPoolName}"]);
        if (startResult.ExitCode != 0 &&
            !startResult.Output.Contains("already", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Could not start IIS application pool {settings.ApplicationPoolName}.\r\n{startResult.Output}");
        }
    }

    private async Task EnsureAutoTraderAdminSiteAsync(IisSiteInstallSettings settings, string physicalPath)
    {
        string bindings = BuildIisHttpsBindings(settings);
        if (!await IisSiteExistsAsync(settings.SiteName))
        {
            await RunAppCmdAsync(
            [
                "add",
                "site",
                $"/name:{settings.SiteName}",
                $"/bindings:{bindings}",
                $"/physicalPath:{physicalPath}"
            ]);
        }
        else
        {
            await RunAppCmdAsync(["set", "site", $"/site.name:{settings.SiteName}", $"/bindings:{bindings}"]);
            await RunAppCmdAsync(["set", "vdir", $"/vdir.name:{settings.SiteName}/", $"/physicalPath:{physicalPath}"]);
        }

        await RunAppCmdAsync(["set", "app", $"/app.name:{settings.SiteName}/", $"/applicationPool:{settings.ApplicationPoolName}"]);
        await RunAppCmdAsync(
        [
            "set",
            "site",
            $"/site.name:{settings.SiteName}",
            $"/bindings.[protocol='https',bindingInformation='*:{settings.HttpsPort}:'].sslFlags:0"
        ]);

        if (!string.IsNullOrWhiteSpace(settings.HostName))
        {
            await RunAppCmdAsync(
            [
                "set",
                "site",
                $"/site.name:{settings.SiteName}",
                $"/bindings.[protocol='https',bindingInformation='*:{settings.HttpsPort}:{settings.HostName.Trim()}'].sslFlags:1"
            ]);
        }

        ScCommandResult startResult = await RunAppCmdRawAsync(["start", "site", $"/site.name:{settings.SiteName}"]);
        if (startResult.ExitCode != 0 &&
            !startResult.Output.Contains("already", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Could not start IIS site {settings.SiteName}.\r\n{startResult.Output}");
        }
    }

    private async Task BindCertificateToIisHttpsEndpointAsync(IisSiteInstallSettings settings)
    {
        string certificateThumbprint = NormalizeCertificateThumbprint(settings.CertificateThumbprint);
        if (string.IsNullOrWhiteSpace(certificateThumbprint))
        {
            throw new InvalidOperationException("IIS certificate thumbprint is required.");
        }

        await ConfigureSslCertificateBindingAsync($"ipport=0.0.0.0:{settings.HttpsPort}", certificateThumbprint);
        if (!string.IsNullOrWhiteSpace(settings.HostName))
        {
            await ConfigureSslCertificateBindingAsync($"hostnameport={settings.HostName.Trim()}:{settings.HttpsPort}", certificateThumbprint);
        }
    }

    private static async Task ConfigureSslCertificateBindingAsync(string endpointArgument, string certificateThumbprint)
    {
        _ = await RunNetshRawAsync(["http", "delete", "sslcert", endpointArgument]);
        await RunNetshCommandAsync(
        [
            "http",
            "add",
            "sslcert",
            endpointArgument,
            $"certhash={certificateThumbprint}",
            $"appid={{{AutoTraderAdminSslBindingAppId:D}}}",
            "certstorename=MY"
        ]);
    }

    private void ValidateServiceInstallInputs()
    {
        if (string.IsNullOrWhiteSpace(_serviceInstallPathTextBox.Text))
        {
            throw new InvalidOperationException("Service installation folder is required.");
        }

        foreach (string serviceName in SelectedServices(includeServiceManager: true))
        {
            ServiceInstallSettings settings = GetServiceSettings(serviceName);
            if (!Guid.TryParse(settings.ServiceId, out _))
            {
                throw new InvalidOperationException($"{serviceName} Service ID must be a valid GUID.");
            }

            if (string.IsNullOrWhiteSpace(settings.SettingsServiceName))
            {
                throw new InvalidOperationException($"{serviceName} Service_Name is required.");
            }

            if (!ValidateWindowsServiceName(settings.SettingsServiceName, out string serviceNameError))
            {
                throw new InvalidOperationException($"{serviceName} {serviceNameError}");
            }

            if (string.IsNullOrWhiteSpace(settings.FriendlyName))
            {
                throw new InvalidOperationException($"{serviceName} friendly name is required.");
            }

            if (!ValidateOptionalServicePort(settings.ServicePort, out string servicePortError))
            {
                throw new InvalidOperationException($"{serviceName} {servicePortError}");
            }
        }

        if (ShouldInstallServiceManager())
        {
            ServiceInstallSettings serviceManagerSettings = GetServiceSettings(ServiceManagerServiceKey);
            if (string.IsNullOrWhiteSpace(_serviceManagerUsernameTextBox.Text))
            {
                throw new InvalidOperationException($"{serviceManagerSettings.SettingsServiceName} admin account username is required.");
            }

            if (string.IsNullOrWhiteSpace(_serviceManagerPasswordTextBox.Text))
            {
                throw new InvalidOperationException($"{serviceManagerSettings.SettingsServiceName} admin account password is required.");
            }
        }
    }

    private bool PromptForServiceIdentitySettings()
    {
        string[] services = OrderedServicesToInstall().ToArray();
        for (int index = 0; index < services.Length; index++)
        {
            if (!PromptForServiceIdentitySettings(services[index], index + 1, services.Length))
            {
                return false;
            }
        }

        RefreshServicesPage();
        return true;
    }

    private bool PromptForServiceIdentitySettings(string serviceName, int index, int total)
    {
        ServiceInstallSettings settings = GetServiceSettings(serviceName);
        ServiceDescriptor descriptor = GetServiceDescriptor(serviceName);
        using var dialog = new Form
        {
            Text = $"Service Identity ({index} of {total})",
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(540, 304),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            Font = Font
        };

        var headerLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 16),
            Size = new Size(482, 26),
            Text = serviceName,
            Font = new Font(Font, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        dialog.Controls.Add(headerLabel);

        var sourceLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 42),
            Size = new Size(482, 24),
            ForeColor = Color.FromArgb(86, 92, 104),
            Text = $"Defaults loaded from {descriptor.SettingsFile}."
        };
        dialog.Controls.Add(sourceLabel);

        var settingsServiceNameLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 80),
            Size = new Size(122, 24),
            Text = "Service_Name",
            TextAlign = ContentAlignment.MiddleLeft
        };
        dialog.Controls.Add(settingsServiceNameLabel);

        var settingsServiceNameTextBox = new TextBox
        {
            Location = new Point(150, 80),
            Size = new Size(370, 24),
            Text = settings.SettingsServiceName
        };
        dialog.Controls.Add(settingsServiceNameTextBox);

        var friendlyNameLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 120),
            Size = new Size(122, 24),
            Text = "Friendly name",
            TextAlign = ContentAlignment.MiddleLeft
        };
        dialog.Controls.Add(friendlyNameLabel);

        var friendlyNameTextBox = new TextBox
        {
            Location = new Point(150, 120),
            Size = new Size(370, 24),
            Text = settings.FriendlyName
        };
        dialog.Controls.Add(friendlyNameTextBox);

        var serviceIdLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 160),
            Size = new Size(122, 24),
            Text = "Service ID",
            TextAlign = ContentAlignment.MiddleLeft
        };
        dialog.Controls.Add(serviceIdLabel);

        var serviceIdTextBox = new TextBox
        {
            Location = new Point(150, 160),
            Size = new Size(268, 24),
            Text = settings.ServiceId
        };
        dialog.Controls.Add(serviceIdTextBox);

        var generateButton = new Button
        {
            Location = new Point(428, 158),
            Size = new Size(92, 28),
            Text = "New GUID"
        };
        generateButton.Click += (_, _) => serviceIdTextBox.Text = Guid.NewGuid().ToString("D").ToUpperInvariant();
        dialog.Controls.Add(generateButton);

        var servicePortLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 200),
            Size = new Size(122, 24),
            Text = "Service_Port",
            TextAlign = ContentAlignment.MiddleLeft
        };
        dialog.Controls.Add(servicePortLabel);

        var servicePortTextBox = new TextBox
        {
            Location = new Point(150, 200),
            Size = new Size(120, 24),
            Text = settings.ServicePort
        };
        dialog.Controls.Add(servicePortTextBox);

        var servicePortHintLabel = new Label
        {
            AutoSize = false,
            Location = new Point(282, 200),
            Size = new Size(238, 24),
            ForeColor = Color.FromArgb(86, 92, 104),
            Text = "Leave blank if not used."
        };
        dialog.Controls.Add(servicePortHintLabel);

        var okButton = new Button
        {
            Location = new Point(326, 256),
            Size = new Size(90, 30),
            Text = index == total ? "OK" : "Next",
            DialogResult = DialogResult.OK
        };
        dialog.Controls.Add(okButton);

        var cancelButton = new Button
        {
            Location = new Point(430, 256),
            Size = new Size(90, 30),
            Text = "Cancel",
            DialogResult = DialogResult.Cancel
        };
        dialog.Controls.Add(cancelButton);

        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;

        while (dialog.ShowDialog(this) == DialogResult.OK)
        {
            string settingsServiceName = settingsServiceNameTextBox.Text.Trim();
            string friendlyName = friendlyNameTextBox.Text.Trim();
            string serviceId = serviceIdTextBox.Text.Trim().ToUpperInvariant();
            string servicePort = servicePortTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(settingsServiceName))
            {
                MessageBox.Show(dialog, "Enter the Service_Name.", "FIG Installer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                settingsServiceNameTextBox.Focus();
                continue;
            }

            if (!ValidateWindowsServiceName(settingsServiceName, out string serviceNameError))
            {
                MessageBox.Show(dialog, serviceNameError, "FIG Installer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                settingsServiceNameTextBox.Focus();
                continue;
            }

            if (string.IsNullOrWhiteSpace(friendlyName))
            {
                MessageBox.Show(dialog, "Enter the friendly name.", "FIG Installer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                friendlyNameTextBox.Focus();
                continue;
            }

            if (!Guid.TryParse(serviceId, out Guid parsedServiceId))
            {
                MessageBox.Show(dialog, "Enter a valid Service ID GUID.", "FIG Installer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                serviceIdTextBox.Focus();
                continue;
            }

            if (!ValidateOptionalServicePort(servicePort, out string servicePortError))
            {
                MessageBox.Show(dialog, servicePortError, "FIG Installer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                servicePortTextBox.Focus();
                continue;
            }

            _serviceSettings[serviceName] = new ServiceInstallSettings(
                serviceName,
                settingsServiceName,
                parsedServiceId.ToString("D").ToUpperInvariant(),
                friendlyName,
                servicePort);
            return true;
        }

        return false;
    }

    private bool PromptForServiceManagerCredentialsIfNeeded()
    {
        if (!ShouldInstallServiceManager())
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(_serviceManagerUsernameTextBox.Text) &&
            !string.IsNullOrWhiteSpace(_serviceManagerPasswordTextBox.Text))
        {
            return true;
        }

        ServiceInstallSettings serviceManagerSettings = GetServiceSettings(ServiceManagerServiceKey);
        using var dialog = new Form
        {
            Text = $"{serviceManagerSettings.SettingsServiceName} Account",
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(430, 180),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            Font = Font
        };

        var usernameLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 22),
            Size = new Size(122, 24),
            Text = "Windows username",
            TextAlign = ContentAlignment.MiddleLeft
        };
        dialog.Controls.Add(usernameLabel);

        var usernameTextBox = new TextBox
        {
            Location = new Point(150, 22),
            Size = new Size(250, 24),
            Text = _serviceManagerUsernameTextBox.Text
        };
        dialog.Controls.Add(usernameTextBox);

        var passwordLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 62),
            Size = new Size(122, 24),
            Text = "Password",
            TextAlign = ContentAlignment.MiddleLeft
        };
        dialog.Controls.Add(passwordLabel);

        var passwordTextBox = new TextBox
        {
            Location = new Point(150, 62),
            Size = new Size(190, 24),
            UseSystemPasswordChar = true,
            Text = _serviceManagerPasswordTextBox.Text
        };
        dialog.Controls.Add(passwordTextBox);

        var togglePasswordButton = new Button
        {
            Location = new Point(346, 61),
            Size = new Size(54, 27),
            Text = "Show"
        };
        togglePasswordButton.Click += (_, _) =>
        {
            passwordTextBox.UseSystemPasswordChar = !passwordTextBox.UseSystemPasswordChar;
            togglePasswordButton.Text = passwordTextBox.UseSystemPasswordChar ? "Show" : "Hide";
        };
        dialog.Controls.Add(togglePasswordButton);

        var hintLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 100),
            Size = new Size(382, 28),
            ForeColor = Color.FromArgb(86, 92, 104),
            Text = @"Use DOMAIN\User, .\User, or MachineName\User."
        };
        dialog.Controls.Add(hintLabel);

        var okButton = new Button
        {
            Location = new Point(206, 138),
            Size = new Size(90, 30),
            Text = "OK",
            DialogResult = DialogResult.OK
        };
        dialog.Controls.Add(okButton);

        var cancelButton = new Button
        {
            Location = new Point(310, 138),
            Size = new Size(90, 30),
            Text = "Cancel",
            DialogResult = DialogResult.Cancel
        };
        dialog.Controls.Add(cancelButton);

        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;

        while (dialog.ShowDialog(this) == DialogResult.OK)
        {
            if (string.IsNullOrWhiteSpace(usernameTextBox.Text))
            {
                MessageBox.Show(dialog, "Enter the Windows username.", "FIG Installer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                usernameTextBox.Focus();
                continue;
            }

            if (string.IsNullOrWhiteSpace(passwordTextBox.Text))
            {
                MessageBox.Show(dialog, "Enter the Windows password.", "FIG Installer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                passwordTextBox.Focus();
                continue;
            }

            _serviceManagerUsernameTextBox.Text = usernameTextBox.Text.Trim();
            _serviceManagerPasswordTextBox.Text = passwordTextBox.Text;
            return true;
        }

        return false;
    }

    private void EnsureRunningAsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            throw new UnauthorizedAccessException("Run FIG Installer as Administrator before installing Windows services or IIS applications.");
        }
    }

    private void EnsureMasterKeyEnvironmentVariable()
    {
        const string runtimeMasterKeyName = "FIG_MASTER_KEY";
        string masterKeyValue = _masterKeyValueTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(masterKeyValue))
        {
            throw new InvalidOperationException("Master key value is required before service settings can be encrypted.");
        }

        Environment.SetEnvironmentVariable(runtimeMasterKeyName, masterKeyValue, EnvironmentVariableTarget.Machine);
        Environment.SetEnvironmentVariable(runtimeMasterKeyName, masterKeyValue, EnvironmentVariableTarget.Process);

        string requestedMasterKeyName = _masterKeyNameTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(requestedMasterKeyName) &&
            !string.Equals(requestedMasterKeyName, runtimeMasterKeyName, StringComparison.OrdinalIgnoreCase))
        {
            Environment.SetEnvironmentVariable(requestedMasterKeyName, masterKeyValue, EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable(requestedMasterKeyName, masterKeyValue, EnvironmentVariableTarget.Process);
        }
    }

    private async Task EnsureRootsSqlLoginAndDatabaseUsersAsync()
    {
        if (_instructions is null)
        {
            return;
        }

        string rootsUsername = _instructions.Database.RootsUsername;
        string rootsPassword = _instructions.Database.RootsPassword;
        if (string.IsNullOrWhiteSpace(rootsUsername) || string.IsNullOrWhiteSpace(rootsPassword))
        {
            throw new InvalidOperationException("Roots SQL username and password are required before service connection strings can be generated.");
        }

        string connectionString = ConstructAdminConnectionString(initialCatalog: "master");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        string loginName = DelimitSqlIdentifier(rootsUsername);
        string loginNameLiteral = EscapeSqlString(rootsUsername);
        string passwordLiteral = EscapeSqlString(rootsPassword);

        await ExecuteSqlAsync(connection, $"""
            IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE [name] = N'{loginNameLiteral}')
            BEGIN
                CREATE LOGIN {loginName}
                WITH PASSWORD = N'{passwordLiteral}', CHECK_POLICY = OFF, CHECK_EXPIRATION = OFF;
            END;
            """);

        foreach (DatabaseDeploymentInstruction database in DatabaseInstructionsToMapRoots())
        {
            string databaseName = DelimitSqlIdentifier(database.Name);
            string databaseLiteral = EscapeSqlString(database.Name);
            await ExecuteSqlAsync(connection, $"""
                IF DB_ID(N'{databaseLiteral}') IS NOT NULL
                BEGIN
                    DECLARE @sql nvarchar(max) = N'
                        USE {databaseName};
                        IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = N''{loginNameLiteral}'')
                        BEGIN
                            CREATE USER {loginName} FOR LOGIN {loginName};
                        END;

                        IF IS_ROLEMEMBER(N''db_owner'', N''{loginNameLiteral}'') <> 1
                        BEGIN
                            ALTER ROLE [db_owner] ADD MEMBER {loginName};
                        END;';
                    EXEC sys.sp_executesql @sql;
                END;
                """);
        }
    }

    private void WriteGlobalVarsFile(string globalVarsPath)
    {
        var globalVars = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Auth_URL"] = _authUrlTextBox.Text.Trim(),
            ["CertThumbprint"] = _certThumbprintTextBox.Text.Trim(),
            ["Controller_Connection"] = $"{_controllerUrlTextBox.Text.Trim().TrimEnd('/')}:{{{{Controller_Port}}}}/hubs/broker-agent",
            ["Controller_Password"] = ProtectConfigurationValue(_controllerPasswordTextBox.Text),
            ["Controller_Port"] = _controllerPortTextBox.Text.Trim(),
            ["Controller_Username"] = _controllerUsernameTextBox.Text.Trim(),
            ["DBConnection_Admin"] = ProtectConfigurationValue(ConstructAdminConnectionString()),
            ["DBConnection_Alert"] = ProtectConfigurationValue(BuildRootsConnectionString("FIGAlert")),
            ["DBConnection_AutoTrader"] = ProtectConfigurationValue(BuildRootsConnectionString("FIGAutoTrader")),
            ["DBConnection_Users"] = ProtectConfigurationValue(BuildRootsConnectionString("FIGUser")),
            ["Jwt_Audience"] = ProtectConfigurationValue(_jwtAudienceTextBox.Text),
            ["Jwt_Issuer"] = ProtectConfigurationValue(_jwtIssuerTextBox.Text),
            ["Jwt_Key"] = ProtectConfigurationValue(_jwtKeyTextBox.Text),
            ["Log_Path"] = EnsureTrailingDirectorySeparator(_logPathTextBox.Text.Trim()),
            ["SMTP_Password"] = ProtectConfigurationValue(_smtpPasswordTextBox.Text),
            ["SMTP_Username"] = ProtectConfigurationValue(_smtpUsernameTextBox.Text)
        };

        string json = JsonSerializer.Serialize(globalVars, InstallerJsonOptions);
        File.WriteAllText(globalVarsPath, json);
    }

    private string ProtectConfigurationValue(string value)
    {
        string? protectedValue = ProtectedDataUtil.Protect(value);
        if (protectedValue is null)
        {
            throw new InvalidOperationException("Could not encrypt service configuration values. Verify the master key is a Base64 encoded 32-byte key.");
        }

        return protectedValue;
    }

    private string BuildRootsConnectionString(string databaseName)
    {
        if (_instructions is null)
        {
            return "";
        }

        var builder = new SqlConnectionStringBuilder(ConstructAdminConnectionString(initialCatalog: databaseName))
        {
            InitialCatalog = databaseName,
            UserID = _instructions.Database.RootsUsername,
            Password = _instructions.Database.RootsPassword,
            IntegratedSecurity = false,
            PersistSecurityInfo = false
        };

        return builder.ConnectionString;
    }

    private void CopyServiceResourcesToInstallPath(string serviceName, string serviceInstallPath)
    {
        ServiceDescriptor descriptor = GetServiceDescriptor(serviceName);
        string sourceDirectory = ResolveServiceResourceDirectory(serviceName);
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"Service resource folder was not found: {sourceDirectory}");
        }

        CopyDirectory(sourceDirectory, serviceInstallPath);

        string sourceExecutablePath = Path.Combine(sourceDirectory, descriptor.ExecutableFile);
        if (!File.Exists(sourceExecutablePath))
        {
            throw new FileNotFoundException(
                $"Service executable was not found in the installer resources for {serviceName}: {sourceExecutablePath}",
                sourceExecutablePath);
        }

        string targetExecutablePath = Path.Combine(serviceInstallPath, descriptor.ExecutableFile);
        File.Copy(sourceExecutablePath, targetExecutablePath, overwrite: true);
        if (!File.Exists(targetExecutablePath))
        {
            throw new FileNotFoundException(
                $"Service executable could not be copied for {serviceName}. Source: {sourceExecutablePath}. Target: {targetExecutablePath}",
                targetExecutablePath);
        }
    }

    private void PatchServiceSettings(string serviceName, string serviceInstallPath, string globalVarsPath) =>
        PatchServiceSettingsInDirectory(serviceName, serviceInstallPath, globalVarsPath);

    private void PatchServiceSettingsInDirectory(
        string serviceName,
        string installDirectory,
        string globalVarsPath,
        IisSiteInstallSettings? iisSiteSettings = null)
    {
        ServiceDescriptor descriptor = GetServiceDescriptor(serviceName);
        ServiceInstallSettings settings = GetServiceSettings(serviceName);
        string settingsPath = Path.Combine(installDirectory, descriptor.SettingsFile);
        if (!File.Exists(settingsPath))
        {
            throw new FileNotFoundException($"Service settings file was not found after copying resources: {settingsPath}", settingsPath);
        }

        JsonObject root = JsonNode.Parse(File.ReadAllText(settingsPath))?.AsObject()
            ?? throw new InvalidOperationException($"Service settings file is not valid JSON: {settingsPath}");

        JsonObject global = GetOrCreateJsonObject(root, "Global");
        SetJsonStringUnlessTemplate(global, "VarPath", globalVarsPath);
        JsonObject vars = GetOrCreateJsonObject(global, "Vars");
        SetJsonStringUnlessTemplate(vars, "Service_Name", settings.SettingsServiceName);
        SetJsonStringUnlessTemplate(vars, "Service_Id", settings.ServiceId.ToUpperInvariant());
        SetJsonStringUnlessTemplate(vars, "Service_FriendlyName", settings.FriendlyName);
        if (string.IsNullOrWhiteSpace(settings.ServicePort))
        {
            RemoveJsonPropertyUnlessTemplate(vars, "Service_Port");
        }
        else
        {
            SetJsonIntUnlessTemplate(vars, "Service_Port", int.Parse(settings.ServicePort));
        }

        if (iisSiteSettings is not null && IsIisApplicationInstallable(serviceName))
        {
            JsonObject redirect = GetOrCreateJsonObject(root, "Redirect");
            string hostName = string.IsNullOrWhiteSpace(iisSiteSettings.HostName)
                ? "localhost"
                : iisSiteSettings.HostName.Trim();
            SetJsonStringUnlessTemplate(redirect, "BaseUrl", $"https://{hostName}:{iisSiteSettings.HttpsPort}");
        }

        if (descriptor.WritesManagedServices)
        {
            JsonObject serviceManagerConfig = GetOrCreateJsonObject(root, "ServiceManagerConfig");
            var managedServices = new JsonArray();
            foreach (string managedService in ManagedServicesForServiceManager())
            {
                managedServices.Add(GetServiceSettings(managedService).SettingsServiceName);
            }

            SetJsonArrayUnlessTemplate(serviceManagerConfig, "ManagedServices", managedServices);
        }

        File.WriteAllText(settingsPath, root.ToJsonString(InstallerJsonOptions));
    }

    private async Task InstallOrUpdateWindowsServiceAsync(
        string serviceName,
        string displayName,
        string description,
        string binaryPath,
        WindowsServiceAccount account)
    {
        bool exists = await ServiceExistsAsync(serviceName);
        var arguments = new List<string>
        {
            exists ? "config" : "create",
            serviceName,
            "binPath=",
            binaryPath,
            "start=",
            "auto",
            "DisplayName=",
            displayName
        };

        if (account.RequiresPassword)
        {
            arguments.AddRange(["obj=", account.Username, "password=", account.Password]);
        }
        else
        {
            arguments.AddRange(["obj=", account.Username]);
        }

        await RunScCommandAsync(arguments);
        await RunScCommandAsync(["description", serviceName, description]);
        await RunScCommandAsync(["failure", serviceName, "reset=", "86400", "actions=", "restart/60000/restart/60000/restart/60000"]);
        await RunScCommandAsync(["failureflag", serviceName, "1"]);
    }

    private async Task StopServiceIfExistsAsync(string serviceName)
    {
        if (!await ServiceExistsAsync(serviceName))
        {
            return;
        }

        ScCommandResult result = await RunScCommandRawAsync(["stop", serviceName]);
        if (result.ExitCode != 0 &&
            !result.Output.Contains("1062", StringComparison.OrdinalIgnoreCase) &&
            !result.Output.Contains("has not been started", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Could not stop existing service {serviceName}.\r\n{result.Output}");
        }
    }

    private async Task DeleteServiceIfExistsAsync(string serviceName)
    {
        if (!await ServiceExistsAsync(serviceName))
        {
            return;
        }

        await StopServiceIfExistsAsync(serviceName);
        ScCommandResult result = await RunScCommandRawAsync(["delete", serviceName]);
        if (result.ExitCode != 0 &&
            !result.Output.Contains("1060", StringComparison.OrdinalIgnoreCase) &&
            !result.Output.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Could not delete existing service {serviceName}.\r\n{result.Output}");
        }
    }

    private async Task<bool> ServiceExistsAsync(string serviceName)
    {
        ScCommandResult result = await RunScCommandRawAsync(["query", serviceName]);
        if (result.ExitCode == 0)
        {
            return true;
        }

        if (result.Output.Contains("1060", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        throw new InvalidOperationException($"Could not query Windows service {serviceName}.\r\n{result.Output}");
    }

    private async Task RunScCommandAsync(IEnumerable<string> arguments)
    {
        ScCommandResult result = await RunScCommandRawAsync(arguments);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Windows service command failed.\r\n{result.Output}");
        }
    }

    private static async Task<ScCommandResult> RunScCommandRawAsync(IEnumerable<string> arguments)
    {
        string scPath = Path.Combine(Environment.SystemDirectory, "sc.exe");
        var startInfo = new ProcessStartInfo
        {
            FileName = scPath,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start Windows service control command.");
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new ScCommandResult(process.ExitCode, $"{output}{error}");
    }

    private async Task<bool> IisApplicationPoolExistsAsync(string applicationPoolName)
    {
        ScCommandResult result = await RunAppCmdRawAsync(["list", "apppool", applicationPoolName]);
        return result.ExitCode == 0 &&
            result.Output.Contains(applicationPoolName, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> IisSiteExistsAsync(string siteName)
    {
        ScCommandResult result = await RunAppCmdRawAsync(["list", "site", siteName]);
        return result.ExitCode == 0 &&
            result.Output.Contains(siteName, StringComparison.OrdinalIgnoreCase);
    }

    private async Task RunAppCmdAsync(IEnumerable<string> arguments)
    {
        ScCommandResult result = await RunAppCmdRawAsync(arguments);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"IIS command failed.\r\n{result.Output}");
        }
    }

    private static Task<ScCommandResult> RunAppCmdRawAsync(IEnumerable<string> arguments) =>
        RunProcessRawAsync(ResolveAppCmdPath(), arguments, "Could not start IIS appcmd.");

    private static async Task RunNetshCommandAsync(IEnumerable<string> arguments)
    {
        ScCommandResult result = await RunNetshRawAsync(arguments);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"IIS SSL certificate binding command failed.\r\n{result.Output}");
        }
    }

    private static Task<ScCommandResult> RunNetshRawAsync(IEnumerable<string> arguments) =>
        RunProcessRawAsync(Path.Combine(Environment.SystemDirectory, "netsh.exe"), arguments, "Could not start netsh.");

    private static async Task<ScCommandResult> RunProcessRawAsync(
        string fileName,
        IEnumerable<string> arguments,
        string startFailureMessage)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException(startFailureMessage);
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new ScCommandResult(process.ExitCode, $"{output}{error}");
    }

    private static string ResolveAppCmdPath()
    {
        string windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string[] candidates =
        [
            Path.Combine(windowsDirectory, "Sysnative", "inetsrv", "appcmd.exe"),
            Path.Combine(windowsDirectory, "System32", "inetsrv", "appcmd.exe")
        ];

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("IIS management tools were not found. Install IIS Management Console/appcmd before continuing.");
    }

    private static string BuildIisHttpsBindings(IisSiteInstallSettings settings)
    {
        string defaultBinding = $"https/*:{settings.HttpsPort}:";
        string hostName = settings.HostName.Trim();
        return string.IsNullOrWhiteSpace(hostName)
            ? defaultBinding
            : $"{defaultBinding},https/*:{settings.HttpsPort}:{hostName}";
    }

    private static string NormalizeCertificateThumbprint(string thumbprint) =>
        new(thumbprint
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());

    private IEnumerable<string> OrderedServicesToInstall()
    {
        HashSet<string> selected = SelectedServices(includeServiceManager: true).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return ServiceDescriptors
            .Where(service => selected.Contains(service.Key))
            .OrderBy(service => service.Value.InstallOrder)
            .Select(service => service.Key);
    }

    private IEnumerable<string> OrderedWindowsServicesToInstall() =>
        OrderedServicesToInstall()
            .Where(IsWindowsServiceInstallable);

    private IEnumerable<string> ManagedServicesForServiceManager() =>
        SelectedServices()
            .Where(serviceName =>
            {
                ServiceDescriptor descriptor = GetServiceDescriptor(serviceName);
                return descriptor.IncludeInServiceManager && IsWindowsServiceInstallable(serviceName);
            });

    private WindowsServiceAccount GetWindowsServiceAccount(string serviceName)
    {
        if (GetServiceDescriptor(serviceName).RequiresCustomServiceAccount)
        {
            return new WindowsServiceAccount(
                NormalizeWindowsServiceAccountName(_serviceManagerUsernameTextBox.Text.Trim()),
                _serviceManagerPasswordTextBox.Text,
                true);
        }

        return new WindowsServiceAccount(@"NT AUTHORITY\LocalService", "", false);
    }

    private static string NormalizeWindowsServiceAccountName(string username)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            username.Contains('\\') ||
            username.Contains('@', StringComparison.Ordinal))
        {
            return username;
        }

        return $@".\{username}";
    }

    private void RefreshComponentServicesList()
    {
        if (_componentServicesListBox is null)
        {
            return;
        }

        _componentServicesListBox.Items.Clear();
        foreach (string serviceName in SelectedServices(includeServiceManager: true))
        {
            ServiceInstallSettings settings = GetServiceSettings(serviceName);
            _componentServicesListBox.Items.Add(FormatServiceDisplayName(serviceName, settings));
        }

        if (_componentServicesListBox.Items.Count == 0)
        {
            _componentServicesListBox.Items.Add("No services selected yet.");
        }

        RefreshServicesPage();
    }

    private IReadOnlyList<string> SelectedServices(bool includeServiceManager = false)
    {
        InstallerComponent selectedComponents = SelectedInstallerComponents();
        var services = ServiceDescriptors
            .Where(service =>
                service.Value.Component != InstallerComponent.None &&
                selectedComponents.HasFlag(service.Value.Component))
            .OrderBy(service => service.Value.InstallOrder)
            .Select(service => service.Key)
            .ToList();

        if (includeServiceManager && services.Any(IsWindowsServiceInstallable))
        {
            services.Insert(0, ServiceManagerServiceKey);
        }

        return services.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private IReadOnlyList<string> SelectedWindowsServices(bool includeServiceManager = false) =>
        SelectedServices(includeServiceManager)
            .Where(IsWindowsServiceInstallable)
            .ToList();

    private static string FormatServiceDisplayName(string installerServiceName, ServiceInstallSettings settings) =>
        string.Equals(installerServiceName, settings.SettingsServiceName, StringComparison.OrdinalIgnoreCase)
            ? settings.SettingsServiceName
            : $"{settings.SettingsServiceName} ({installerServiceName})";

    private bool ShouldInstallAutoTraderAdminIisApplication() =>
        SelectedServices().Contains(AutoTraderAdminServiceKey, StringComparer.OrdinalIgnoreCase);

    private bool ShouldInstallServiceManager() =>
        SelectedServices(includeServiceManager: true)
            .Any(serviceName => GetServiceDescriptor(serviceName).WritesManagedServices);

    private InstallerComponent SelectedInstallerComponents()
    {
        InstallerComponent components = InstallerComponent.None;
        if (_autoTradeSystemCheckBox.Checked)
        {
            components |= InstallerComponent.AutoTradeSystem;
        }

        if (_autoTradeWebAdminCheckBox.Checked)
        {
            components |= InstallerComponent.AutoTradeWebAdmin;
        }

        if (_priceSyncCheckBox.Checked)
        {
            components |= InstallerComponent.PriceSync;
        }

        if (_signalProcessingCheckBox.Checked)
        {
            components |= InstallerComponent.SignalProcessing;
        }

        if (_brokerCheckBox.Checked)
        {
            components |= InstallerComponent.Broker;
        }

        return components;
    }

    private IReadOnlyList<DatabaseDeploymentInstruction> DatabaseInstructionsToInstall()
    {
        if (_instructions is null)
        {
            return [];
        }

        return _instructions.Database.Databases
            .Where(database => !string.IsNullOrWhiteSpace(database.Name))
            .Where(database => !_databaseNamesToSkip.Contains(database.Name))
            .Where(database => IsBrokerDatabase(database.Name)
                ? ShouldInstallBrokerDatabase()
                : _autoTradeSystemCheckBox.Checked)
            .ToList();
    }

    private IReadOnlyList<DatabaseDeploymentInstruction> DatabaseInstructionsToMapRoots()
    {
        if (_instructions is null)
        {
            return [];
        }

        return _instructions.Database.Databases
            .Where(database => !string.IsNullOrWhiteSpace(database.Name))
            .ToList();
    }

    private IReadOnlyList<DatabaseFilePlan> DatabaseFilePlansToInstall() =>
        DatabaseInstructionsToInstall()
            .Select(database => new DatabaseFilePlan(
                database,
                BuildSqlDatabaseFilePath(database.Name, ".mdf"),
                BuildSqlDatabaseFilePath($"{database.Name}_log", ".ldf")))
            .ToList();

    private string BuildCreateDatabaseSql(DatabaseFilePlan databaseFilePlan)
    {
        string databaseName = DelimitSqlIdentifier(databaseFilePlan.Database.Name);
        string dataLogicalName = EscapeSqlString(databaseFilePlan.Database.Name);
        string logLogicalName = EscapeSqlString($"{databaseFilePlan.Database.Name}_log");
        string dataFilePath = EscapeSqlString(databaseFilePlan.DataFilePath);
        string logFilePath = EscapeSqlString(databaseFilePlan.LogFilePath);

        return $"""
            CREATE DATABASE {databaseName}
            ON PRIMARY
            (
                NAME = N'{dataLogicalName}',
                FILENAME = N'{dataFilePath}'
            )
            LOG ON
            (
                NAME = N'{logLogicalName}',
                FILENAME = N'{logFilePath}'
            );
            """;
    }

    private string BuildSqlDatabaseFilePath(string fileNameWithoutExtension, string extension)
    {
        string safeFileName = MakeSafeFileName(fileNameWithoutExtension);
        string sqlDataPath = _sqlDataPathTextBox.Text.Trim();
        return Path.Combine(sqlDataPath, $"{safeFileName}{extension}");
    }

    private void EnsureLocalSqlDataPathIfPossible()
    {
        string sqlDataPath = _sqlDataPathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(sqlDataPath) || !IsLocalDatabaseAddress(_databaseAddressTextBox.Text.Trim()))
        {
            return;
        }

        Directory.CreateDirectory(sqlDataPath);
    }

    private string ResolveDatabaseScriptPath(string script)
    {
        if (Path.IsPathRooted(script))
        {
            return script;
        }

        string resourceRoot = ResolveResourcePath("Resources");
        string resourceScriptPath = Path.Combine(resourceRoot, script);
        if (File.Exists(resourceScriptPath))
        {
            return resourceScriptPath;
        }

        string appScriptPath = Path.Combine(AppContext.BaseDirectory, "Resources", script);
        if (File.Exists(appScriptPath))
        {
            return appScriptPath;
        }

        return resourceScriptPath;
    }

    private static IEnumerable<string> SplitSqlBatches(string script)
    {
        using var reader = new StringReader(script);
        var batch = new StringBuilder();

        while (reader.ReadLine() is { } line)
        {
            int repeatCount = GetGoBatchRepeatCount(line);
            if (repeatCount > 0)
            {
                string batchText = batch.ToString();
                batch.Clear();

                for (int index = 0; index < repeatCount; index++)
                {
                    if (!string.IsNullOrWhiteSpace(batchText))
                    {
                        yield return batchText;
                    }
                }

                continue;
            }

            batch.AppendLine(line);
        }

        string finalBatch = batch.ToString();
        if (!string.IsNullOrWhiteSpace(finalBatch))
        {
            yield return finalBatch;
        }
    }

    private static int GetGoBatchRepeatCount(string line)
    {
        string trimmed = line.Trim();
        if (!trimmed.StartsWith("GO", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (trimmed.Length == 2)
        {
            return 1;
        }

        if (!char.IsWhiteSpace(trimmed[2]))
        {
            return 0;
        }

        string repeatText = trimmed[2..].Trim();
        int commentIndex = repeatText.IndexOf("--", StringComparison.Ordinal);
        if (commentIndex >= 0)
        {
            repeatText = repeatText[..commentIndex].Trim();
        }

        if (string.IsNullOrWhiteSpace(repeatText))
        {
            return 1;
        }

        return int.TryParse(repeatText, out int repeatCount) && repeatCount > 0
            ? repeatCount
            : 0;
    }

    private bool ShouldInstallBrokerDatabase() =>
        SelectedInstallerComponents().HasFlag(InstallerComponent.Broker);

    private bool ShouldRunDatabasePhase() =>
        _autoTradeSystemCheckBox.Checked ||
        (ShouldInstallBrokerDatabase() && HasBrokerDatabaseInstruction());

    private bool HasBrokerDatabaseInstruction() =>
        _instructions?.Database.Databases.Any(database => IsBrokerDatabase(database.Name)) == true;

    private static bool IsBrokerDatabase(string databaseName) =>
        string.Equals(databaseName, "FIGBroker", StringComparison.OrdinalIgnoreCase);

    private static bool IsIisApplicationInstallable(string serviceName) =>
        GetServiceDescriptor(serviceName).InstallKind == ServiceInstallKind.IisApplication;

    private static bool IsWindowsServiceInstallable(string serviceName) =>
        GetServiceDescriptor(serviceName).InstallKind == ServiceInstallKind.WindowsService;

    private static string MakeSafeFileName(string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(character => invalidChars.Contains(character) ? '_' : character).ToArray());
    }

    private static string DelimitSqlIdentifier(string identifier) =>
        $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";

    private static string EscapeSqlString(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);

    private static bool SamePath(string? actualPath, string expectedPath) =>
        string.Equals(
            actualPath?.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            expectedPath.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);

    private static bool IsLocalDatabaseAddress(string databaseAddress)
    {
        if (string.IsNullOrWhiteSpace(databaseAddress))
        {
            return false;
        }

        string serverName = databaseAddress.Split('\\')[0].Split(',')[0].Trim();
        return string.Equals(serverName, ".", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(serverName, "(local)", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(serverName, "localhost", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(serverName, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(serverName, "::1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(serverName, Environment.MachineName, StringComparison.OrdinalIgnoreCase);
    }

    private void ResetDatabaseInstallationDecision()
    {
        _existingInstallationDatabaseNames.Clear();
        _databaseNamesToSkip.Clear();
    }

    private IReadOnlyList<string> SelectedInstallationTypeNames()
    {
        var names = new List<string>();
        if (_autoTradeSystemCheckBox.Checked) names.Add("AutoTrade System");
        if (_autoTradeWebAdminCheckBox.Checked) names.Add("AutoTrade Web Admin");
        if (_priceSyncCheckBox.Checked) names.Add("Price Sync");
        if (_signalProcessingCheckBox.Checked) names.Add("Signal Processing");
        if (_brokerCheckBox.Checked) names.Add("Broker");
        return names;
    }

    private bool AnyInstallationTypeSelected() =>
        _autoTradeSystemCheckBox.Checked ||
        _autoTradeWebAdminCheckBox.Checked ||
        _priceSyncCheckBox.Checked ||
        _signalProcessingCheckBox.Checked ||
        _brokerCheckBox.Checked;

    private bool ServiceResourceExists(string serviceName)
    {
        if (_instructions is null)
        {
            return false;
        }

        return Directory.Exists(ResolveServiceResourceDirectory(serviceName));
    }

    private string ResolveResourcePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        string instructionRelative = _instructionFile is null
            ? ""
            : Path.Combine(Path.GetDirectoryName(Path.GetFullPath(_instructionFile)) ?? "", path);

        if (!string.IsNullOrWhiteSpace(instructionRelative) && Directory.Exists(instructionRelative))
        {
            return instructionRelative;
        }

        return Path.Combine(AppContext.BaseDirectory, path);
    }

    private string ResolveServiceResourceDirectory(string serviceName)
    {
        string distributionPath = _instructions?.Resources.DistributionPath ?? @"Resources\Distrib";
        ServiceDescriptors.TryGetValue(serviceName, out ServiceDescriptor? descriptor);
        string resourceFolder = descriptor?.ResourceFolder ?? serviceName;

        var existingDirectories = new List<string>();
        foreach (string resourcePath in ResolveResourcePathCandidates(distributionPath))
        {
            string serviceDirectory = Path.Combine(resourcePath, resourceFolder);
            if (!Directory.Exists(serviceDirectory))
            {
                continue;
            }

            existingDirectories.Add(serviceDirectory);
            if (descriptor is null || File.Exists(Path.Combine(serviceDirectory, descriptor.ExecutableFile)))
            {
                return serviceDirectory;
            }
        }

        if (existingDirectories.Count > 0)
        {
            return existingDirectories[0];
        }

        return Path.Combine(ResolveResourcePath(distributionPath), resourceFolder);
    }

    private string ResolveServiceResourceFile(string serviceName, string fileName) =>
        Path.Combine(ResolveServiceResourceDirectory(serviceName), fileName);

    private IEnumerable<string> ResolveResourcePathCandidates(string path)
    {
        var candidates = new List<string>();
        if (Path.IsPathRooted(path))
        {
            candidates.Add(path);
        }
        else
        {
            if (_instructionFile is not null)
            {
                candidates.Add(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(_instructionFile)) ?? "", path));
            }

            candidates.Add(Path.Combine(AppContext.BaseDirectory, path));
        }

        return candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        foreach (string directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(targetDirectory, relativePath));
        }

        foreach (string file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceDirectory, file);
            string targetFile = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? targetDirectory);
            File.Copy(file, targetFile, overwrite: true);
        }
    }

    private static JsonObject GetOrCreateJsonObject(JsonObject parent, string propertyName)
    {
        if (parent[propertyName] is JsonObject existingObject)
        {
            return existingObject;
        }

        var createdObject = new JsonObject();
        parent[propertyName] = createdObject;
        return createdObject;
    }

    private static void SetJsonStringUnlessTemplate(JsonObject parent, string propertyName, string value)
    {
        if (IsTemplateValue(parent[propertyName]))
        {
            return;
        }

        parent[propertyName] = value;
    }

    private static void SetJsonIntUnlessTemplate(JsonObject parent, string propertyName, int value)
    {
        if (IsTemplateValue(parent[propertyName]))
        {
            return;
        }

        parent[propertyName] = value;
    }

    private static void SetJsonArrayUnlessTemplate(JsonObject parent, string propertyName, JsonArray value)
    {
        if (IsTemplateValue(parent[propertyName]))
        {
            return;
        }

        parent[propertyName] = value;
    }

    private static void RemoveJsonPropertyUnlessTemplate(JsonObject parent, string propertyName)
    {
        if (IsTemplateValue(parent[propertyName]))
        {
            return;
        }

        parent.Remove(propertyName);
    }

    private static bool IsTemplateValue(JsonNode? node)
    {
        if (node is null || node.GetValueKind() != JsonValueKind.String)
        {
            return false;
        }

        string? value = node.GetValue<string>();
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        int openIndex = value.IndexOf("{{", StringComparison.Ordinal);
        if (openIndex < 0)
        {
            return false;
        }

        return value.IndexOf("}}", openIndex + 2, StringComparison.Ordinal) >= 0;
    }

    private static string EnsureTrailingDirectorySeparator(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "";
        }

        return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : $"{path}{Path.DirectorySeparatorChar}";
    }

    private static ServiceDescriptor GetServiceDescriptor(string serviceName)
    {
        if (ServiceDescriptors.TryGetValue(serviceName, out ServiceDescriptor? descriptor))
        {
            return descriptor;
        }

        throw new InvalidOperationException($"No installer resource descriptor is configured for {serviceName}.");
    }

    private static string GetWindowsServiceName(ServiceDescriptor descriptor) =>
        Path.GetFileNameWithoutExtension(descriptor.ExecutableFile);

    private void ClearInstructionDetails()
    {
        _componentServicesListBox.Items.Clear();
        _databaseScriptsListBox.Items.Clear();
        _servicesToInstallListBox.Items.Clear();
        _planListBox.Items.Clear();
        _reviewTextBox.Text = "";
    }

    private void SetInstructionStatus(string message, Color color)
    {
        _instructionStatusLabel.Text = message;
        _instructionStatusLabel.ForeColor = color;
    }

    private void BrowseSqlDataPath() => BrowseFolderInto(_sqlDataPathTextBox, "Select SQL data folder");

    private void BrowseLogPath() => BrowseFolderInto(_logPathTextBox, "Select log folder");

    private void BrowseServiceInstallPath() => BrowseFolderInto(_serviceInstallPathTextBox, "Select service installation folder");

    private void BrowseFolderInto(TextBox target, string description)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = description,
            UseDescriptionForTitle = true
        };

        if (Directory.Exists(target.Text))
        {
            dialog.SelectedPath = target.Text;
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            target.Text = dialog.SelectedPath;
        }
    }

    private static Panel CreatePagePanel() =>
        new()
        {
            AutoScroll = true,
            BackColor = ShellBackColor
        };

    private static TableLayoutPanel CreateFormGrid(int width, int rows)
    {
        var grid = new TableLayoutPanel
        {
            Location = new Point(0, 0),
            Size = new Size(width, rows * 42),
            ColumnCount = 2,
            RowCount = 0
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return grid;
    }

    private static void AddLabeledControl(TableLayoutPanel grid, string labelText, Control control)
    {
        int row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var label = CreateFieldLabel(labelText);
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;

        control.Margin = new Padding(0, 4, 0, 4);
        grid.Controls.Add(label, 0, row);
        grid.Controls.Add(control, 1, row);
        grid.Height = grid.RowCount * 42;
    }

    private static Label CreateFieldLabel(string text) =>
        new()
        {
            AutoSize = false,
            Size = new Size(170, 24),
            Text = text,
            ForeColor = Color.FromArgb(56, 62, 74)
        };

    private static CheckBox CreateOptionCheckBox(string title, int top, string description)
    {
        var checkBox = new CheckBox
        {
            AutoSize = false,
            Location = new Point(0, top),
            Size = new Size(ContentWidth, 52),
            Text = $"{title}\r\n{description}",
            ForeColor = Color.FromArgb(32, 35, 40),
            UseVisualStyleBackColor = true
        };
        return checkBox;
    }

    private static TableLayoutPanel CreatePathRow(out TextBox textBox, Action browseAction)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        row.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));

        textBox = new TextBox { Dock = DockStyle.Top };
        row.Controls.Add(textBox, 0, 0);

        var browseButton = new Button
        {
            Dock = DockStyle.Top,
            Height = 27,
            Margin = new Padding(4, 0, 0, 0),
            Text = "Browse"
        };
        browseButton.Click += (_, _) => browseAction();
        row.Controls.Add(browseButton, 1, 0);

        return row;
    }

    private TableLayoutPanel CreateDatabaseValidationRow()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        row.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));

        _databaseStatusLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(82, 88, 100)
        };
        row.Controls.Add(_databaseStatusLabel, 0, 0);

        _testDatabaseConnectionButton = new Button
        {
            Dock = DockStyle.Top,
            Height = 27,
            Margin = new Padding(4, 0, 0, 0),
            Text = "Test Connection"
        };
        _testDatabaseConnectionButton.Click += async (_, _) => await TestDatabaseConnectionAsync();
        row.Controls.Add(_testDatabaseConnectionButton, 1, 0);

        return row;
    }

    private static TableLayoutPanel CreatePasswordRow(out TextBox textBox, out Button toggleButton)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        row.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));

        textBox = new TextBox
        {
            Dock = DockStyle.Top,
            UseSystemPasswordChar = true
        };
        row.Controls.Add(textBox, 0, 0);

        toggleButton = new Button
        {
            Dock = DockStyle.Top,
            Height = 27,
            Margin = new Padding(4, 0, 0, 0),
            Text = "👁"
        };

        TextBox passwordTextBox = textBox;
        Button passwordToggleButton = toggleButton;
        passwordToggleButton.Click += (_, _) =>
        {
            passwordTextBox.UseSystemPasswordChar = !passwordTextBox.UseSystemPasswordChar;
            passwordToggleButton.Text = passwordTextBox.UseSystemPasswordChar ? "👁" : "Hide";
        };

        row.Controls.Add(toggleButton, 1, 0);
        return row;
    }

    private static void ConfigureButton(Button button, string text)
    {
        button.Text = text;
        button.Size = new Size(96, 34);
        button.Margin = new Padding(4, 0, 4, 0);
        button.UseVisualStyleBackColor = true;
    }

    private static string Display(string value) =>
        string.IsNullOrWhiteSpace(value) ? "(not set)" : value;

    private static string? GetJsonValueAsString(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        return node.GetValueKind() == JsonValueKind.String
            ? node.GetValue<string>()
            : node.ToJsonString();
    }

    private static string BuildServiceBinaryPath(string executablePath, string windowsServiceName) =>
        $"{QuoteServiceBinaryPathPart(executablePath)} --windows-service-name {QuoteServiceBinaryPathPart(windowsServiceName)}";

    private static string QuoteServiceBinaryPathPart(string value) =>
        $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

    private static bool ValidateWindowsServiceName(string serviceName, out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            error = "Service_Name is required.";
            return false;
        }

        if (serviceName.Length > 256)
        {
            error = "Service_Name must be 256 characters or less.";
            return false;
        }

        if (serviceName.Contains('\\') || serviceName.Contains('/') || serviceName.Contains('"') || serviceName.Any(char.IsControl))
        {
            error = "Service_Name cannot contain slash, backslash, quote, or control characters.";
            return false;
        }

        return true;
    }

    private static bool ValidateOptionalServicePort(string servicePort, out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(servicePort))
        {
            return true;
        }

        if (!int.TryParse(servicePort, out int parsedPort) || parsedPort is < 1 or > 65535)
        {
            error = "Service_Port must be a number between 1 and 65535, or blank if the service does not use a port.";
            return false;
        }

        return true;
    }

    private enum PageKind
    {
        Welcome,
        Instructions,
        Types,
        Database,
        GlobalVars,
        Services,
        Review
    }

    [Flags]
    private enum InstallerComponent
    {
        None = 0,
        AutoTradeSystem = 1,
        AutoTradeWebAdmin = 2,
        PriceSync = 4,
        SignalProcessing = 8,
        Broker = 16
    }

    private enum ServiceInstallKind
    {
        WindowsService,
        IisApplication
    }

    private sealed record WizardPage(PageKind Kind, string Title, string Description, Control Content, Action? OnEnter);

    private sealed record DatabaseFilePlan(DatabaseDeploymentInstruction Database, string DataFilePath, string LogFilePath);

    private sealed record ServiceDescriptor(
        string ResourceFolder,
        string SettingsFile,
        string ExecutableFile,
        int InstallOrder,
        InstallerComponent Component,
        ServiceInstallKind InstallKind,
        bool IncludeInServiceManager = true,
        bool RequiresCustomServiceAccount = false,
        bool WritesManagedServices = false);

    private sealed record ServiceInstallSettings(
        string InstallerServiceName,
        string SettingsServiceName,
        string ServiceId,
        string FriendlyName,
        string ServicePort);

    private sealed record IisSiteInstallSettings(
        string SiteName,
        string ApplicationPoolName,
        int HttpsPort,
        string HostName,
        string CertificateThumbprint);

    private sealed record IisCertificateListItem(
        string Thumbprint,
        string Subject,
        DateTime Expires)
    {
        public override string ToString() =>
            $"{Subject} | expires {Expires:yyyy-MM-dd} | {Thumbprint}";
    }

    private sealed record ServiceListItem(
        string InstallerServiceName,
        string SettingsServiceName,
        string FriendlyName,
        string ServiceId,
        string ServicePort,
        string ResourceStatus)
    {
        public override string ToString()
        {
            string displayName = string.Equals(InstallerServiceName, SettingsServiceName, StringComparison.OrdinalIgnoreCase)
                ? SettingsServiceName
                : $"{SettingsServiceName} ({InstallerServiceName})";
            return string.IsNullOrWhiteSpace(ServicePort)
                ? $"{displayName} | {FriendlyName} | {ServiceId} ({ResourceStatus})"
                : $"{displayName} | {FriendlyName} | {ServiceId} | Port {ServicePort} ({ResourceStatus})";
        }
    }

    private sealed record WindowsServiceAccount(string Username, string Password, bool RequiresPassword);

    private sealed record ScCommandResult(int ExitCode, string Output);
}
