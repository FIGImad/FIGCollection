# .NET 10 LADX collection bridge

## Visual Studio

Open `FIGSignalCpp.slnx`, select **Release | x64**, then right-click
**FIGStudyCollection.DotNetLADX** and choose **Build**. The project automatically
locates an installed x64 .NET 10 host pack, publishes the managed bridge and LADX
dependencies, and embeds them together with `nethost.dll` in the native adapter.
The outputs go to `x64/Release` (or `x64/Debug` for a Debug build).
Build FIGSignalCpp in the same configuration before running it.

Visual Studio needs the v145 C++ toolset and the x64 .NET 10 SDK. No separate
PowerShell publish step is needed. To run the adapter self-test from the IDE,
set FIGSignalCpp as the startup project and set its **Debugging > Command Arguments**
to `--test-collection --collection-dll "$(TargetDir)FIGStudyCollection.DotNetLADX.dll"`.

## PowerShell

Build from PowerShell with `./Build-DotNetLADX.ps1`. Requires the x64 .NET 10 SDK
and the same MSVC v145 toolset/configuration used to build FIGSignalCpp.
The default source is the sibling
`FIGSignalCollection/FIG.StudyCollections.LADX` project; use `-LadxProject` to
override its project path. Use `-Configuration Debug` for a Debug host.
If MSVC reports C1902/LNK1101 PDB-service errors, use `-NoPdb` to build without
the native program database. This was required in the restricted build environment.

The bridge loads the actual `FIG.StudyCollections.LADX.dll`; no C# calculations
are copied into the native project. The managed wrapper calls its public
`LADXEngine`, which is also used by `StudyColLADX`. The factory supplies interval
validation and maximum lookback. This adapter is specific to LADX's engine API,
not a general loader for every .NET study plugin.

Run with the existing host:

```powershell
./x64/Release/FIGSignalCpp.exe --list-collections --collection-dll ./x64/Release/FIGStudyCollection.DotNetLADX.dll
./x64/Release/FIGSignalCpp.exe --test-collection --collection-dll ./x64/Release/FIGStudyCollection.DotNetLADX.dll
```

For existing backtest/optimization commands, select that DLL and `--collection
LADX`, and supply the .NET LADX parameter file. The dataset interval must be a
positive whole number of minutes and satisfy the plugin's market-trend interval
validation. Native LADX and .NET LADX may represent different strategy versions;
do not assume their parameters or results are interchangeable.

Deploy **only `FIGStudyCollection.DotNetLADX.dll`** beside your existing native
application. The target still needs x64 .NET 10 and ASP.NET Core 10 runtimes.
Do not deploy the C# plugin DLL by itself. The native adapter embeds the full
managed publish output and `nethost.dll`; old adjacent copies are ignored.

On first use it extracts into `%LOCALAPPDATA%/FIGSignalCpp/LADX/<package-sha256>`.
Set `FIG_LADX_CACHE` to override the cache root (use a writable local directory).
Every startup checks cached file contents against the embedded package and repairs
missing or changed files. A cross-process file lock serializes extraction; a crashed
worker releases the lock automatically. Files are installed with atomic replacement.
New package contents select a new cache directory, leaving running old workers alone.
Restart the application after updating the adapter. Old cache versions can be removed
when no process uses them. Extraction and validation happen once per process, not per
bar or candidate. The DLL is larger because it includes the managed dependencies.

Each collection owns its managed engine and a reusable 64 KiB binary output
buffer. There is one managed call per bar, with no per-bar JSON, reflection, or
per-indicator calls. Numeric/null/text outputs are preserved. Prices cross as
float64 (matching MSVC's native Decimal representation) and are converted to C#
decimal using round-trip formatting; native inputs cannot represent all 28-digit
C# decimal values. Volume must fit the C# model's signed 32-bit integer.

Independent instances can run on separate optimization workers. A single instance
must not be processed concurrently. Revisions use the engine's retained-history
rules. A processing error invalidates that instance; recreate it and replay.
Handles are released on destruction; the .NET runtime remains loaded for process
lifetime. The current native interface processes one bar at a time; batch
optimization calls are not implemented. Speed must be measured on real data.

## Verification

Run `./tests/Test-DotNetLADXPackage.ps1` to test an isolated deployment containing
only the host EXE and adapter DLL. It starts four concurrent workers with an empty
cache, then verifies warm-cache use and repair of missing/corrupted files.

Build `tests/FIGCollectionProbe.vcxproj` with the same native configuration, then
run `./tests/Test-DotNetLADX.ps1`. The test generates a separate reference through
the actual C# collection factory and compares every output with native bridge
replay. It includes warmup, same-bar replacement and a historical rewind.
The Release validation passed 1,655 input rows and 72 study outputs, with exact
text/null matching and numeric tolerance of `1e-12 * max(1, abs(expected))`.
The host's `--test-collection` also checks corrected history against clean replay
using independent instances and rejects an invalid interval.
