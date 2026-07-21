# FIGInstaller Flow Reference

This is a quick map for changing installer behavior without re-reading the whole `InstallerWizardForm`.

## Main Entry Flow

```mermaid
flowchart TD
    A["Program.Main(args)"] --> B["ResolveInstructionFile(args)"]
    B --> C["new InstallerWizardForm(initialInstructionFile)"]
    C --> D["BuildShell()"]
    C --> E["Create wizard pages"]
    E --> E1["CreateWelcomePage()"]
    E --> E2["CreateInstructionPage()"]
    E --> E3["CreateInstallationTypesPage()"]
    E --> E4["CreateDatabasePage()"]
    E --> E5["CreateGlobalVarsPage()"]
    E --> E6["CreateServicesPage()"]
    E --> E7["CreateReviewPage()"]
    C --> F["ShowPage(0)"]
    F --> G["MoveNextAsync()"]
    G --> H{"Current page"}
    H -->|Instructions| I["TryLoadInstructions()"]
    I --> I1["InstructionFileLoader.Load()"]
    I --> I2["InstallationInstructionValidator.Validate()"]
    I --> I3["ApplyInstructionDefaults()"]
    H -->|Types| J["AnyInstallationTypeSelected()"]
    H -->|Database| K["ValidateDatabaseInputs()"]
    H -->|Review| L["CompleteSkeletonInstallAsync()"]
```

## Install Button Execution Flow

`CompleteSkeletonInstallAsync()` is the real install orchestrator, despite the older "Skeleton" name.

```mermaid
flowchart TD
    A["CompleteSkeletonInstallAsync()"] --> B["PrepareDatabaseInstallationAsync()"]
    B --> C{"ShouldRunDatabasePhase()?"}
    C -->|Yes| D{"DatabaseFilePlansToInstall().Count > 0?"}
    C -->|No| E["Skip database creation and user mapping"]
    D -->|Yes| F["InstallDatabasesAsync()"]
    D -->|No| G["Skip database creation"]
    F --> H["EnsureRootsSqlLoginAndDatabaseUsersAsync()"]
    G --> H
    E --> I{"SelectedServices().Count > 0?"}
    H --> I
    I -->|Yes| J["InstallSelectedApplicationsAndServicesAsync()"]
    I -->|No| K["Mark install completed"]
    J --> K
```

## Database Flow

```mermaid
flowchart TD
    A["PrepareDatabaseInstallationAsync()"] --> B["ShouldRunDatabasePhase()"]
    B --> C["ValidateDatabaseInputs()"]
    C --> D["GetExistingDatabasesAsync(names)"]
    D --> E["ConstructAdminConnectionString(master, timeout)"]
    D --> F["GetExistingDatabasesAsync(connection, names)"]
    F --> G["SELECT name FROM sys.databases"]
    G --> H["ConfirmSkipExistingDatabases()"]
    H --> I["DatabaseFilePlansToInstall()"]
    I --> J["InstallDatabasesAsync()"]
    J --> K["EnsureLocalSqlDataPathIfPossible()"]
    J --> L["GetExistingDatabasesAsync(connection, target names)"]
    L --> M{"Target database already exists?"}
    M -->|Yes| N["Throw / stop install"]
    M -->|No| O["BuildCreateDatabaseSql()"]
    O --> P["ExecuteSqlAsync(CREATE DATABASE)"]
    P --> Q["ResolveDatabaseScriptPath()"]
    Q --> R["ExecuteSqlScriptAsync()"]
    R --> S["SplitSqlBatches()"]
    S --> T["ExecuteSqlAsync(each batch)"]
    T --> U["VerifyDatabaseFileLocationsAsync()"]
```

## Services And IIS Flow

```mermaid
flowchart TD
    A["InstallSelectedApplicationsAndServicesAsync()"] --> B["EnsureRunningAsAdministrator()"]
    B --> C{"AutoTrade Web Admin selected?"}
    C -->|Yes| D["EnsureIisInstalledAndRunningAsync()"]
    C -->|No| E["PromptForServiceIdentitySettings()"]
    D --> E
    E --> F["PromptForServiceManagerCredentialsIfNeeded()"]
    F --> G{"IIS admin selected?"}
    G -->|Yes| H["PromptForAutoTraderAdminIisSettings()"]
    H --> I["SyncAutoTraderAdminServicePort()"]
    G -->|No| J["ValidateServiceInstallInputs()"]
    I --> J
    J --> K["Directory.CreateDirectory(serviceInstallPath)"]
    K --> L["EnsureMasterKeyEnvironmentVariable()"]
    L --> M["WriteGlobalVarsFile(global_vars.json)"]
    M --> N["foreach OrderedWindowsServicesToInstall()"]
    N --> O["DeleteServiceIfExistsAsync(legacy name)"]
    O --> P["StopServiceIfExistsAsync(current name)"]
    P --> Q["CopyServiceResourcesToInstallPath()"]
    Q --> R["PatchServiceSettings()"]
    R --> S["InstallOrUpdateWindowsServiceAsync()"]
    S --> T{"IIS admin selected?"}
    T -->|Yes| U["InstallAutoTraderAdminIisApplicationAsync()"]
    T -->|No| V["Done"]
    U --> V
```

## IIS Admin App Flow

```mermaid
flowchart TD
    A["InstallAutoTraderAdminIisApplicationAsync()"] --> B["CopyIisApplicationResourcesToInstallPath()"]
    B --> C["PatchServiceSettingsInDirectory(..., iisSettings)"]
    C --> D["EnsureAutoTraderAdminApplicationPoolAsync()"]
    D --> E["IisApplicationPoolExistsAsync()"]
    D --> F["RunAppCmdAsync(add/set/start apppool)"]
    F --> G["EnsureAutoTraderAdminSiteAsync()"]
    G --> H["IisSiteExistsAsync()"]
    G --> I["BuildIisHttpsBindings()"]
    G --> J["RunAppCmdAsync(add/set/start site)"]
    J --> K["BindCertificateToIisHttpsEndpointAsync()"]
    K --> L["ConfigureSslCertificateBindingAsync()"]
    L --> M["RunNetshRawAsync(delete sslcert)"]
    L --> N["RunNetshCommandAsync(add sslcert)"]
```

## What To Edit

| Goal | Primary function(s) |
| --- | --- |
| Add/remove wizard pages | `InstallerWizardForm(...)`, `Create*Page()`, `IsPageVisible()` |
| Change instruction JSON loading | `TryLoadInstructions()`, `InstructionFileLoader.Load()` |
| Change instruction validation rules | `InstallationInstructionValidator.Validate*()` |
| Change component-to-service selection | `ServiceDescriptors`, `SelectedInstallerComponents()`, `SelectedServices()`, `SelectedWindowsServices()`, `OrderedServicesToInstall()` |
| Change whether database phase runs | `ShouldRunDatabasePhase()`, `ShouldInstallBrokerDatabase()`, `HasBrokerDatabaseInstruction()` |
| Check if databases already exist | `PrepareDatabaseInstallationAsync()`, `GetExistingDatabasesAsync(...)` |
| Change existing-database behavior | `ConfirmSkipExistingDatabases()`, `_databaseNamesToSkip` handling |
| Create databases | `InstallDatabasesAsync()`, `BuildCreateDatabaseSql()` |
| Change SQL script lookup | `ResolveDatabaseScriptPath()`, `ResolveResourcePath()` |
| Change SQL batch parsing | `SplitSqlBatches()`, `GetGoBatchRepeatCount()` |
| Verify SQL file placement | `VerifyDatabaseFileLocationsAsync()`, `SamePath()` |
| Create roots SQL login/users | `EnsureRootsSqlLoginAndDatabaseUsersAsync()` |
| Build service connection strings | `BuildRootsConnectionString()`, `ConstructAdminConnectionString()` |
| Write `global_vars.json` | `WriteGlobalVarsFile()`, `ProtectConfigurationValue()` |
| Set machine master key | `EnsureMasterKeyEnvironmentVariable()` |
| Copy service binaries/resources | `CopyServiceResourcesToInstallPath()`, `CopyDirectory()`, `ResolveServiceResourceDirectory()` |
| Patch service settings JSON | `PatchServiceSettings()`, `PatchServiceSettingsInDirectory()` |
| Preserve `{{...}}` template values | `SetJsonStringUnlessTemplate()`, `SetJsonIntUnlessTemplate()`, `SetJsonArrayUnlessTemplate()`, `RemoveJsonPropertyUnlessTemplate()`, `IsTemplateValue()` |
| Create/update Windows services | `InstallOrUpdateWindowsServiceAsync()` |
| Check if a Windows service exists | `ServiceExistsAsync()` |
| Stop/delete Windows services | `StopServiceIfExistsAsync()`, `DeleteServiceIfExistsAsync()` |
| Run `sc.exe` | `RunScCommandAsync()`, `RunScCommandRawAsync()` |
| Install IIS admin app | `InstallAutoTraderAdminIisApplicationAsync()` |
| Check IIS prerequisites | `EnsureIisInstalledAndRunningAsync()`, `ResolveAppCmdPath()` |
| Create/update IIS app pool | `EnsureAutoTraderAdminApplicationPoolAsync()`, `IisApplicationPoolExistsAsync()` |
| Create/update IIS site | `EnsureAutoTraderAdminSiteAsync()`, `IisSiteExistsAsync()`, `BuildIisHttpsBindings()` |
| Bind IIS HTTPS certificate | `BindCertificateToIisHttpsEndpointAsync()`, `ConfigureSslCertificateBindingAsync()` |
| Run IIS / netsh commands | `RunAppCmdAsync()`, `RunAppCmdRawAsync()`, `RunNetshCommandAsync()`, `RunNetshRawAsync()`, `RunProcessRawAsync()` |

## Key Data Decisions

- `ServiceDescriptors` maps installer package keys to resource folders, settings files, executable names, component membership, install order, and install kind.
- `SelectedServices(...)` returns installer package keys. The installed Windows service name comes from `ServiceInstallSettings.SettingsServiceName`, loaded from `Global:Vars:Service_Name`.
- `SelectedServices(includeServiceManager: true)` adds the descriptor marked with `WritesManagedServices` when any Windows service descriptor is selected.
- AutoTrade Web Admin is treated as an IIS app by `IsIisApplicationInstallable()`, not as a Windows service.
- `PreviousInstallationDatabaseNames` checks for `FIGAutoTrader`, `FIGUser`, and `FIGAlert` before AutoTrade database installation.
- `FIGBroker` is installed only when the Broker component is selected and a `FIGBroker` database instruction exists.
- Existing databases confirmed by the user are added to `_databaseNamesToSkip`, so the installer skips them instead of recreating them.
