using System.Data.Common;
using FIGInstaller.Models;

namespace FIGInstaller.Services;

public sealed class InstallationInstructionValidator
{
    public IEnumerable<string> Validate(InstallationInstructions instructions)
    {
        foreach (string error in ValidateDatabase(instructions.Database))
        {
            yield return error;
        }

        foreach (string error in ValidateMasterKey(instructions.MasterKey))
        {
            yield return error;
        }

        foreach (string error in ValidateController(instructions.Controller))
        {
            yield return error;
        }

        foreach (string error in ValidateDefaults(instructions.Defaults))
        {
            yield return error;
        }

        foreach (string error in ValidateResources(instructions.Resources))
        {
            yield return error;
        }

        foreach (string error in ValidateIis(instructions.Iis))
        {
            yield return error;
        }
    }

    private static IEnumerable<string> ValidateDatabase(DatabaseInstruction database)
    {
        if (database is null)
        {
            yield return "Database section is required.";
            yield break;
        }

        if (string.IsNullOrWhiteSpace(database.Provider))
        {
            yield return "Database.Provider is required.";
        }

        if (string.IsNullOrWhiteSpace(database.AdminConnectionString))
        {
            yield return "Database.AdminConnectionString is required.";
        }
        else
        {
            string? connectionStringError = TryValidateConnectionString(database.AdminConnectionString);
            if (connectionStringError is not null)
            {
                yield return $"Database.AdminConnectionString is not valid: {connectionStringError}";
            }
        }

        if (string.IsNullOrWhiteSpace(database.SqlDataPath))
        {
            yield return "Database.SqlDataPath is required.";
        }

        if (string.IsNullOrWhiteSpace(database.RootsUsername))
        {
            yield return "Database.RootsUsername is required.";
        }

        if (string.IsNullOrWhiteSpace(database.RootsPassword))
        {
            yield return "Database.RootsPassword is required.";
        }

        if (database.Databases is null || database.Databases.Count == 0)
        {
            yield return "Database.Databases must include at least one database/script mapping.";
            yield break;
        }

        for (int index = 0; index < database.Databases.Count; index++)
        {
            DatabaseDeploymentInstruction item = database.Databases[index];
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                yield return $"Database.Databases[{index}].Name is required.";
            }

            if (string.IsNullOrWhiteSpace(item.Script))
            {
                yield return $"Database.Databases[{index}].Script is required.";
            }
        }
    }

    private static IEnumerable<string> ValidateMasterKey(MasterKeyInstruction masterKey)
    {
        if (masterKey is null)
        {
            yield return "MasterKey section is required.";
            yield break;
        }

        if (string.IsNullOrWhiteSpace(masterKey.EnvironmentVariableName))
        {
            yield return "MasterKey.EnvironmentVariableName is required.";
        }

        if (string.IsNullOrWhiteSpace(masterKey.Value))
        {
            yield return "MasterKey.Value is required.";
        }
        else
        {
            string? masterKeyError = null;
            try
            {
                byte[] key = Convert.FromBase64String(masterKey.Value);
                if (key.Length != 32)
                {
                    masterKeyError = "MasterKey.Value must be a Base64 encoded 32-byte key.";
                }
            }
            catch (FormatException)
            {
                masterKeyError = "MasterKey.Value must be Base64 encoded.";
            }

            if (masterKeyError is not null)
            {
                yield return masterKeyError;
            }
        }
    }

    private static IEnumerable<string> ValidateController(ControllerInstruction controller)
    {
        if (controller is null)
        {
            yield return "Controller section is required.";
            yield break;
        }

        if (string.IsNullOrWhiteSpace(controller.Username))
        {
            yield return "Controller.Username is required.";
        }

        if (string.IsNullOrWhiteSpace(controller.Url))
        {
            yield return "Controller.Url is required.";
        }

        if (controller.Port <= 0 || controller.Port > 65535)
        {
            yield return "Controller.Port must be between 1 and 65535.";
        }
    }

    private static IEnumerable<string> ValidateDefaults(InstallerDefaults defaults)
    {
        if (defaults is null)
        {
            yield return "Defaults section is required.";
            yield break;
        }

        if (string.IsNullOrWhiteSpace(defaults.ServiceInstallPath))
        {
            yield return "Defaults.ServiceInstallPath is required.";
        }

        if (string.IsNullOrWhiteSpace(defaults.LogPath))
        {
            yield return "Defaults.LogPath is required.";
        }
    }

    private static IEnumerable<string> ValidateResources(ResourceInstruction resources)
    {
        if (resources is null)
        {
            yield return "Resources section is required.";
            yield break;
        }

        if (string.IsNullOrWhiteSpace(resources.GlobalVarsTemplatePath))
        {
            yield return "Resources.GlobalVarsTemplatePath is required.";
        }

        if (string.IsNullOrWhiteSpace(resources.DistributionPath))
        {
            yield return "Resources.DistributionPath is required.";
        }
    }

    private static IEnumerable<string> ValidateIis(IisInstruction iis)
    {
        if (iis is null)
        {
            yield return "Iis section is required.";
            yield break;
        }

        AutoTraderAdminIisInstruction admin = iis.AutoTraderAdmin;
        if (admin is null)
        {
            yield return "Iis.AutoTraderAdmin section is required.";
            yield break;
        }

        if (string.IsNullOrWhiteSpace(admin.SiteName))
        {
            yield return "Iis.AutoTraderAdmin.SiteName is required.";
        }

        if (string.IsNullOrWhiteSpace(admin.ApplicationPoolName))
        {
            yield return "Iis.AutoTraderAdmin.ApplicationPoolName is required.";
        }

        if (admin.HttpsPort is < 1 or > 65535)
        {
            yield return "Iis.AutoTraderAdmin.HttpsPort must be between 1 and 65535.";
        }
    }

    private static string? TryValidateConnectionString(string connectionString)
    {
        try
        {
            _ = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };
            return null;
        }
        catch (ArgumentException ex)
        {
            return ex.Message;
        }
    }
}
