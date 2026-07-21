namespace FIGInstaller.Models;

public sealed class InstallationInstructions
{
    public InstallationMetadata Metadata { get; init; } = new();

    public InstallationTypeDefaults InstallationTypes { get; init; } = new();

    public InstallerDefaults Defaults { get; init; } = new();

    public DatabaseInstruction Database { get; init; } = new();

    public MasterKeyInstruction MasterKey { get; init; } = new();

    public ControllerInstruction Controller { get; init; } = new();

    public SecurityInstruction Security { get; init; } = new();

    public JwtInstruction Jwt { get; init; } = new();

    public SmtpInstruction Smtp { get; init; } = new();

    public IisInstruction Iis { get; init; } = new();

    public ResourceInstruction Resources { get; init; } = new();
}

public sealed class InstallationMetadata
{
    public string InstallationName { get; init; } = "";

    public string EnvironmentName { get; init; } = "";

    public string Version { get; init; } = "";
}

public sealed class InstallationTypeDefaults
{
    public bool AutoTradeSystem { get; init; } = true;

    public bool AutoTradeWebAdmin { get; init; }

    public bool PriceSyncComponent { get; init; }

    public bool SignalProcessingComponent { get; init; }

    public bool BrokerComponent { get; init; }
}

public sealed class InstallerDefaults
{
    public string ServiceInstallPath { get; init; } = @"D:\Roots\FIGServices";

    public string LogPath { get; init; } = @"C:\Roots\FIGLogs\";
}

public sealed class DatabaseInstruction
{
    public string Provider { get; init; } = "SqlServer";

    public string AdminConnectionString { get; init; } = "";

    public string SqlDataPath { get; init; } = @"D:\Data";

    public string RootsUsername { get; init; } = "roots";

    public string RootsPassword { get; init; } = "";

    public IReadOnlyList<DatabaseDeploymentInstruction> Databases { get; init; } =
    [
        new("FIGAutoTrader", "fig_autotrader_db.sql"),
        new("FIGBroker", "fig_broker_db.sql"),
        new("FIGUser", "fig_user_db.sql"),
        new("FIGAlert", "fig_alert_db.sql")
    ];
}

public sealed record DatabaseDeploymentInstruction(string Name, string Script);

public sealed class MasterKeyInstruction
{
    public string EnvironmentVariableName { get; init; } = "FIG_MASTER_KEY";

    public string Value { get; init; } = "hZ/61al5ups2SJAFH/Cfx0kz+nNFy0R9xokE7bD5YVQ=";
}

public sealed class ControllerInstruction
{
    public string Username { get; init; } = "SVC";

    public string Password { get; init; } = "";

    public string Url { get; init; } = "https://localhost";

    public int Port { get; init; } = 5000;

    public string AuthUrl { get; init; } = "https://localhost";
}

public sealed class SecurityInstruction
{
    public string CertThumbprint { get; init; } = "";
}

public sealed class JwtInstruction
{
    public string Key { get; init; } = "";

    public string Issuer { get; init; } = "";

    public string Audience { get; init; } = "";
}

public sealed class SmtpInstruction
{
    public string Username { get; init; } = "";

    public string Password { get; init; } = "";
}

public sealed class IisInstruction
{
    public AutoTraderAdminIisInstruction AutoTraderAdmin { get; init; } = new();
}

public sealed class AutoTraderAdminIisInstruction
{
    public string SiteName { get; init; } = "FIG_AutoTraderAdmin";

    public string ApplicationPoolName { get; init; } = "FIG_AutoTraderAdmin";

    public int HttpsPort { get; init; } = 444;

    public string HostName { get; init; } = "ats.kunoozfund.com";
}

public sealed class ResourceInstruction
{
    public string GlobalVarsTemplatePath { get; init; } = @"Resources\global_vars.json";

    public string DistributionPath { get; init; } = @"Resources\Distrib";
}
