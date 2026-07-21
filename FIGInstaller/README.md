# FIG Installer

Wizard-style Windows installation app for FIGCollection.

The installer reads an installation instruction JSON file, validates the required setup defaults, and shows an installation wizard for component selection, AutoTrade database setup, global variables, Windows service locations, and the final installation action.

The final Install button can create databases, set machine environment variables, write service folders, install Windows services, and configure the AutoTrade Admin IIS site.

For the current function flow and "what should I edit?" map, see [INSTALLER_FLOW.md](INSTALLER_FLOW.md).

## Run

```powershell
dotnet run --project .\FIGInstaller\FIGInstaller.csproj -- --instructions .\FIGInstaller\installation-instructions.sample.json
```

If no instruction path is supplied, the app opens with `installation-instructions.sample.json` from its output folder.

## Current instruction shape

- `InstallationTypes`: default component selections.
- `Database`: SQL admin connection string, SQL data path, roots login, and script/database mappings.
- `MasterKey`: `FIG_MASTER_KEY` environment variable name and default value.
- `Controller`, `Security`, `Jwt`, `Smtp`: values used to build `global_vars.json`.
- `Defaults`: service install path and log path.
- `Resources`: installer resource/template paths.
