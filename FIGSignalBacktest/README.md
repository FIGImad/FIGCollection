# FIGSignalBacktest

.NET 10 / x64 backtest host for `FIG.Studies.IStudyCollectionFactory` plugins.
It calls `StudyColBase.Calc` directly; no C++ adapter, extraction cache, or native
study DLL is involved. Each candidate gets a fresh factory and collection instance.

## Price source

For eSignal exports with local `Date` / `Time` and `Vol` columns, use the Windows UI in `../FIGPriceCsvConverter`. It selects the source time zone, converts to UTC Unix seconds, and sorts prices oldest first. Point `PricesCsv` to its converted output.

Database input requires an explicit positive `--dataset ID` and `--connection-string "Data Source=SERVER;Initial Catalog=DATABASE;Integrated Security=True"`. There is no default dataset ID.

For file input, use `--prices-csv C:\data\prices.csv` instead of `--dataset`. This works with both `--backtest-batch` and `--calculate-studies`, without a settings file or database connection. CSV selection takes precedence if a dataset is also supplied. CSV is read directly on every run, bypassing the database cache.

CSV headers must include `RawTime,Open,High,Low,Close,Volume`; `Id` and `DataSetId` are optional (defaulting to -1 and 0). The existing exported `prices.csv` is accepted. Use UTC Unix seconds for `RawTime`, invariant decimal numbers, integer volume, strictly increasing timestamps, and a single dataset per file. Header names are case-insensitive and quoted CSV fields are supported. The normal interval aggregation, maximum source bars, warmup, and date filtering still apply; use the source file's interval when replaying already aggregated exports.

Keep database connection details in `ConnectionStrings`; CSV paths are input options, not connection strings.

## Build and debug

Open `FIGSignalBacktest.slnx` in Visual Studio, select Debug or Release, and set
FIGSignalBacktest as the startup project. The solution includes the LADX plugin
source. Build the solution, select the **LADX plugin debugging** launch profile,
and set a breakpoint in `LADXEngine.Process` or `StudyColLADX`. F5 runs a small
plugin test in the same managed process. For a real backtest, replace the launch
arguments with the batch arguments below. The host is always x64.

The project is also included in `FIGSignalCpp.slnx`.

```powershell
dotnet build FIGSignalBacktest.csproj -c Release
dotnet publish FIGSignalBacktest.csproj -c Release -p:PublishProfile=FolderProfile
```

For a complete folder with the current managed LADX plugin included, run
`./Publish-LADX.ps1`. This builds both projects and places the plugin and its PDB
under `bin/Release/net10.0/publish/Plugins/LADX`. Copy that **entire publish folder**
to the destination, not only the EXE. Other plugins can be added in their own folders.

Deploy the complete publish folder to a new application directory. Copy your
managed plugin DLL (and any plugin-specific dependencies) into `Plugins/LADX` or
another directory of your choice. The host already supplies `FIG.Studies.Core`,
`FIGCommon`, and the ASP.NET shared framework dependencies. Do not replace those
shared contract assemblies with mismatched versions from a plugin build. The
machine needs x64 .NET 10 and ASP.NET Core 10, and access to SQL Server using the supplied connection string.

## Existing optimizer

FIGOptimizationRunner needs no code changes. Change these fields in its JSON:

```json
{
  "Executable": "C:\\Roots\\OptimizationV3\\FIGSignalBacktest\\FIGSignalBacktest.exe",
  "CollectionDll": "C:\\Roots\\OptimizationV3\\FIGSignalBacktest\\Plugins\\LADX\\FIG.StudyCollections.LADX.dll",
  "Collection": "LADX",
  "Signal": "LADX_SIDE"
}
```

Keep the other parameters, date ranges and price-cache configuration. Set ConnectionStrings in the optimizer configuration; it is passed as --connection-string.
`CollectionDll` must name the actual managed plugin, **not**
`FIGStudyCollection.DotNetLADX.dll`. See `optimizer-ladx.example.json` for a full
workspace example. Relative config paths are resolved by FIGOptimizationRunner
relative to the config file, as before.

## Commands

```powershell
FIGSignalBacktest.exe --list-collections --collection-dll Plugins/LADX/FIG.StudyCollections.LADX.dll
FIGSignalBacktest.exe --test-collection --collection-dll Plugins/LADX/FIG.StudyCollections.LADX.dll --collection LADX --interval-minutes 5
FIGSignalBacktest.exe --backtest-batch --collection-dll Plugins/LADX/FIG.StudyCollections.LADX.dll --collection LADX --signal LADX_SIDE --parameters-dir candidates --output-dir reports --dataset 3 --interval-minutes 5 --jobs 2 --connection-string "Data Source=SERVER;Initial Catalog=DATABASE;Integrated Security=True"
FIGSignalBacktest.exe --calculate-studies --collection-dll Plugins/LADX/FIG.StudyCollections.LADX.dll --collection LADX --parameters ladx-parameters.json --interval-minutes 5 --dataset 3 --output studies.csv
FIGSignalBacktest.exe --self-test
```

Add `--fixed-output-dir` to clear all existing contents of `--output-dir` before execution and write reports directly into that directory. Without it, the directory must be new or empty. Cleanup occurs after input validation and price loading, before reports are written. Use a dedicated output directory outside your inputs, application, and price cache; directory links are rejected.

`--help` lists all compatible batch switches: fixed or initial-capital/HLC3
sizing, allocation fraction, long/short positions and reversals, commission,
slippage, optional forced closing, exclusive UTC date boundaries, continuous
train/validation/test segments, parallel candidates, source row limits, and
`off|use|refresh` caches. Earlier prices warm up collections before the start date.
Partial aggregation buckets are retained; gaps are not filled. Months without
bars retain marked equity. Ranking uses training profit, with stable ties.

Study CSV export supports arbitrary managed collections. Omitting `--collection`
selects the ported legacy ADXDiagnostics report, including `--period`,
`--adx-smoothing` and `--source`. Native strategy DLLs themselves are not .NET
plugins: use a managed implementation of each strategy. Live trading/service
hosting is outside this backtest application (the C++ host's service mode was
also only a placeholder).

The plugin test is a host-level determinism/isolation smoke test; the shared
plugin interface does not define a plugin-specific self-test method.

## Output compatibility

Batch output preserves names, headers, ordering and meaning:

- `results.csv`, ranked by training net profit.
- `prices.csv`, `candidate-files.csv`, `batch-settings.txt`.
- `run-NNNN-monthly.csv`, `run-NNNN-trades.csv`, `run-NNNN-parameters.json`.
- `run-NNNN-chart.figdata`: FIGDATA v1, the same packed headers, column layout,
  validity masks, UTF-8 dictionaries, and LADX2 display/color rules. FIGCharts and
  the existing export tools can read it directly.

Database access uses Microsoft.Data.SqlClient and the same FIGCommon RepositoryBase connection-string decryption as FIGSignalSvc. Pass `--connection-string` with a plain SQL Server connection string (including `Data Source=`) or the service's encrypted value. Encrypted values require `FIG_MASTER_KEY`.

The `--settings` option and settings-file loading have been removed. Supply a complete connection string directly; settings overlays, variable files, and `{{variable}}` substitutions are no longer loaded. The query reads `dbo.PriceData` in RawTime order with parameterized dataset/row limits, retaining history for warmup.

The optimizer accepts `PriceParams.ConnectionStrings` and forwards it as one `--connection-string` argument. CSV input ignores the connection string. Optimizer configuration snapshots include the configured connection value; prefer integrated authentication or the supported encrypted form when saving batches.

Price-cache v1 binary format remains compatible, but cache names now derive from a SHA-256 hash of the resolved connection string, dataset, and source limit. Existing settings-file-based caches are not reused. Connection values are not written into backtest reports or cache names. Corrupt caches fail explicitly; use refresh to rebuild.

The ledger uses float64 to match MSVC's `long double` representation. Plugin input
prices are converted to decimal with round-trip formatting; study numeric outputs
are converted to float64 for native-compatible reporting. Preserve the plugin's
exact build and parameters when comparing strategies.

## Tests

```powershell
bin/Release/net10.0/FIGSignalBacktest.exe --self-test
node tests/parity.mjs
```

The parity suite requires built native FIGSignalCpp, FIGCollectionProbe, ADX and
DotNetLADX DLLs, and a built .NET LADX plugin. It uses synthetic cached prices,
without any database credentials or connection. It compares every CSV field
(numbers within relative tolerance `1e-10`) and requires byte-identical FIGDATA
for fixed sizing, capital sizing, slippage/forced close, two parallel candidates,
5/10-minute bars and all three time segments. It also compares the legacy
diagnostic CSV for several price sources. Native and managed plugin versions must
match for parity tests. This is not a speed benchmark.
The suite also runs FIGOptimizationRunner against the published host and requires
its batch to finish with status Completed. Build FIGOptimizationRunner Release
and publish FIGSignalBacktest before running the suite.

Database connectivity needs verification in the deployment environment;
all output-parity tests above run offline from native-compatible price caches.



## L2 profiling reports

Before launching FIGOptimizationRunner from Command Prompt, enable profiling:

```cmd
set FIG_L2_PROFILE=1
FIGOptimizationRunner.exe run --config ".\L2_Backtest\optimizer-l2.json"
```

With the profiling-capable L2 plugin, each completed candidate writes
`reports/run-NNNN-profiling.csv`. Rows include the study index/name, call count,
total/average/maximum elapsed milliseconds, allocated bytes, and profiled bar
count. The snapshot contains all studies, sorted by total elapsed time, including
the final partial reporting interval. Collection processing includes warmup bars
before StartDate. These are study timings, not total database/chart/host timings.
The CSV is written at candidate completion, not periodically; interrupted
candidates do not have a final profiling report. Set FIG_L2_PROFILE=0 to disable.
The host reads the optional GetProfilingSnapshot API directly, so no logger or
FIG_L2_PROFILE_EVERY_BARS setting is required for CSV export. Other plugins
without this API continue to run without profiling files.

Batch charts allow study fields to appear during initialization; earlier missing
values use FIGDATA validity masks. Initial bars without the selected signal are
processed with a flat position and counted in the console warmup message. A signal
that never appears, becomes nonnumeric, or disappears after initialization is an
error. StartDate still controls the first reported/traded bar.
