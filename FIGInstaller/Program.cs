using FIGInstaller.UI;

namespace FIGInstaller;

internal static class Program
{
    private const string DefaultInstructionFileName = "installation-instructions.sample.json";

    [STAThread]
    public static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (args.Any(IsHelpArgument))
        {
            MessageBox.Show(
                "Usage:\r\nFIGInstaller --instructions <path-to-installation-json>",
                "FIG Installer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.Run(new InstallerWizardForm(ResolveInstructionFile(args)));
    }

    private static string ResolveInstructionFile(string[] args)
    {
        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            if (arg.Equals("--instructions", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-i", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 < args.Length)
                {
                    return args[index + 1];
                }
            }
        }

        if (args.Length == 1 && !args[0].StartsWith("-", StringComparison.Ordinal))
        {
            return args[0];
        }

        return Path.Combine(AppContext.BaseDirectory, DefaultInstructionFileName);
    }

    private static bool IsHelpArgument(string arg) =>
        arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals("/?", StringComparison.OrdinalIgnoreCase);
}
