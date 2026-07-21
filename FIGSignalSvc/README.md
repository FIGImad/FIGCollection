# FIGSignalExSvc study plugins

`FIGSignalExSvc` resolves `StudyCol.ColType` through collection plugins loaded from the configured `StudyPlugins:Path` directory.

## Included plugin

The build copies the ADXTrend package to:

```text
Plugins/ADXTrend/
  plugin.json
  FIG.StudyCollections.ADXTrend.dll
```

The database value `ColType = ADXTrend` maps to `ADXTrendFactory.ColType`; the database does not contain a DLL or class name.

## Adding a collection

1. Create a class library targeting the same framework as the host.
2. Reference `FIG.Studies.Core` and, when needed, `FIG.Studies.Common`.
3. Implement exactly one `IStudyCollectionFactory`.
4. Add a `plugin.json` containing `colType`, `assembly`, `apiVersion`, and `version`.
5. Deploy the manifest and collection DLL into their own plugin folder.
6. Restart `FIGSignalExSvc`, then enable the matching `StudyCol` row.

Do not deploy private copies of `FIG.Studies.Core`, `FIG.Studies.Common`, or `FIGCommon` inside plugin folders. They are shared by the host.

Use the following command to verify discovery without starting price processing:

```powershell
dotnet FIGSignalExSvc.dll --list-plugins
```

Replacing an active plugin requires a service restart. Hot replacement is intentionally not supported because collection instances contain live replay and smoothing state.
