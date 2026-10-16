# Study collection DLLs

Open `FIGSignalCpp.slnx` and build **x64 Debug or Release**. The host, core library, and collection DLLs use the same settings in `build/NativeStudies.props`. Outputs go to `x64/<Configuration>`. Building `FIGSignalCpp.vcxproj` alone also builds its core library and default ADX DLL; the Example and LADX DLLs are separate solution projects.

| Project | Responsibility |
| --- | --- |
| `FIGStudyCore.lib` | `StudyADX`, `StudyADXROC`, `StudyATR`, `StudyBB`, `StudyCycleDir`, `StudyExtended`, `StudyMA`, `StudyROC`, `StudySEM`, `StudySMASlope`, `StudyVolatilityRatio`, `StudyWMA`, shared state/model types, and the collection interface/registry |
| `FIGStudyCollection.ADX.dll` | All existing collection-specific studies: ADX options, BaseADX, LBaseADX and variants, PriceShock, SlopeTrend, plus `ADXBase`, `ADXTrend`, and the legacy diagnostic report |
| `FIGSignalCpp.exe` | DLL loading, database access, price aggregation, reporting, backtesting, and command handling |
| `FIGStudyCollection.Example.dll` | An independent `ExampleMA` collection demonstrating reuse of the core without ADX dependencies |
| `FIGStudyCollection.LADX.dll` | The attached EFS's SMA/BB/ADX/shock/slope/LADX collection; see [LADX usage and parity](LADX.md) |
| `FIGStudyCollection.LADX2.dll` | The newer EFS's confirmed hourly/daily market trend and regime-dependent LADX signals; see [LADX2 usage and parity](LADX2.md) |
| `FIGStudyCollection.SADX2.dll` | Short counterpart of LADX2; see [SADX2 usage and parity](SADX2.md) |

The common studies are shared as source and a **static library** linked into each DLL. Each collection instance owns its study state; datasets and collections do not share mutable state. There is no extra core DLL to deploy. Existing study headers retain their paths under `include/fig/studies`; project membership determines ownership.

## Select a collection at runtime

`--collection-dll` selects a DLL. `--collection` selects a type registered by that DLL (case-insensitive). A DLL can contain several related types: the existing ADX family exposes `ADXBase` and `ADXTrend`. The host does not scan directories or automatically load other DLLs.

```powershell
# List the types available in the default ADX DLL.
.\x64\Release\FIGSignalCpp.exe --list-collections

# Existing report command, now explicitly choosing its collection DLL.
.\Run-StudyReport.ps1 -DataSetId 1 -IntervalMinutes 5 `
    -CollectionDll .\x64\Release\FIGStudyCollection.ADX.dll `
    -Collection ADXBase -Parameters .\adx-base-parameters.json -Output .\adx.csv

# Switch to the independent example without rebuilding the executable.
.\Run-StudyReport.ps1 -DataSetId 1 `
    -CollectionDll .\x64\Release\FIGStudyCollection.Example.dll `
    -Collection ExampleMA -Output .\example.csv

.\x64\Release\FIGSignalCpp.exe --test-collection `
    --collection-dll .\x64\Release\FIGStudyCollection.Example.dll
```

When `--collection-dll` is omitted, the host loads `FIGStudyCollection.ADX.dll` **beside the executable**, independent of the working directory. Explicit relative DLL paths are relative to the working directory. Use matching Debug/Release executable and DLL paths, particularly when using `Run-StudyReport.ps1 -Executable`.

In Visual Studio's **Debugging > Command Arguments**, omit `--collection-dll` for the default ADX collection. If specifying it explicitly, use `--collection-dll "$(TargetDir)FIGStudyCollection.ADX.dll"` so changing the active build configuration selects the matching DLL. The default iterator debug level is `2` in Debug and `0` in Release; a mismatch must be rejected because C++ objects cross the DLL boundary. The loader error reports both host and DLL build values.

Existing commands retain their defaults. Reports without `--collection` use `ADXDiagnostics`, which preserves the old combined CSV columns and period/source options. For another DLL, specify its collection type. `ADXDiagnostics` is a compatibility adapter, not a new trading strategy.

The native batch command also accepts `--collection-dll PATH --collection TYPE --signal OUTPUT_NAME`. Defaults remain `ADXBase` and `STrend_LBaseADX_Side`. The selected numeric output uses its sign: **positive is long, negative is short, and zero is flat**. Any finite magnitude is accepted; changes within the same sign keep the existing position. Opposite signs close the current trade and open the opposite position at the signal bar close. Missing and non-finite values are rejected. Trade reports include `Side` (+1/-1); `Contracts` is a positive quantity and `OpenContracts` is signed. The batch settings record the resolved DLL path, type, and actual selected signal. `Run-Optimization.ps1 -CollectionDll PATH` selects the DLL for its existing ADX parameter search; its parameter generator remains ADX-specific.

## Add the next collection

1. Copy `FIGStudyCollection.Example.vcxproj` to a new project name, assign a new `ProjectGuid` and `RootNamespace`, and add the project to the solution. Keep the shared properties import and `FIGStudyCore.vcxproj` reference.
2. Create its own source folder under `src/collections`, update the project's source entries, and implement `fig::StudyCollection`. Put its private studies in this project only. Reuse the 12 common study classes by including their existing headers.
3. In its `plugin.cpp`, register its type(s) and factories in `StudyRegistry`. Parse and validate `StudyCollectionConfig.parameters` in the factory. Factories receive ticker and interval context and must create independent instances safely when called concurrently.
4. Export the exact `FIGGetCollectionPlugin` entry point shown in the example, with a unique plugin name, registration function, and optional self-test. No executable registration change or import-library link is needed.
5. Build the new DLL, inspect it with `--list-collections --collection-dll PATH`, run its self-test, and select it in report/backtest commands.

`ExampleMA` is deliberately a small scaffold with a fixed 3-bar close SMA. It accepts the default `{}` parameters, returns `MA` and `AboveMA`, and demonstrates current-bar replacement. It does not define the strategy for your next collection.

For C++ callers:

```cpp
#include "fig/studies/collection_plugin.hpp"

fig::StudyRegistry registry;
fig::CollectionPlugin plugin(L"collections/MyCollection.dll");
plugin.register_collections(registry);
auto study = registry.create(
    fig::StudyCollectionConfig{.type = "MyCollection", .parameters = "{}"},
    fig::Ticker{}, fig::Interval{});
auto result = study->process(price);
```

Registration is atomic and rejects duplicate type names. Registered factories and created instances retain the DLL, even when the plugin object or original registry is destroyed. Destroying the final owner unloads the DLL. Host-facing errors distinguish missing DLLs, missing entry points, incompatible interfaces/builds, and unknown collection types. Plugin exceptions are copied into host-owned exceptions while the DLL is still loaded.

## Build and deployment contract

This is a versioned **native C++ interface** with an unmangled discovery export, not a portable C interface. Build the executable, core, and plugins with the same MSVC compiler version, architecture, C++20 settings, default structure packing, and shared runtime (`/MD` for Release, `/MDd` for Debug). The loader checks API version, descriptor size, compiler version, and iterator-debug level before calling registration. Do not mix Debug and Release DLLs. The shared property file enforces the runtime settings for these projects.

When changing `study.hpp`, `database/models.hpp`, or the plugin interface incompatibly, increment `collection_plugin_api_version` and rebuild the host and all plugins. Algorithm-only changes to a collection can be deployed by rebuilding/replacing that DLL while the host is stopped. DLLs execute native code and must come from a trusted build.

Deploy `FIGSignalCpp.exe` and the selected collection DLL(s), plus the matching Microsoft Visual C++ runtime. `.lib` files are build inputs and are not needed at runtime. An explicitly selected collection works without the default ADX DLL present. Dependencies of the selected DLL are resolved from its own directory and Windows system directories.

## Verify

```powershell
.\x64\Release\FIGSignalCpp.exe --self-test
.\tests\verify_collection_plugins.ps1 -Configuration Release
```

The normal self-test retains primitive, aggregation, report, backtest, EFS, signal, and rollback tests. ADX-specific tests execute inside the ADX DLL. Loader tests cover case-insensitive lookup, duplicate registration, copied registries, instances outliving registries, unload, and missing DLLs. The integration script tests discovery, an independent deployment with no ADX DLL, invalid paths/exports/API, runtime configuration mismatch when the other configuration is built, and command validation before output creation. No SQL connection is needed for these tests.
