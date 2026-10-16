# C# optimization runner

This .NET 10 console project replaces the orchestration performed by `Run-Optimization.ps1`. It generates parameter files and launches the existing C++ batch backtester. Studies, signal processing, price aggregation, and NAV accounting still run in C++, so there is one strategy implementation to maintain. No additional NuGet packages are required.

## Getting started

Open `FIGSignalCpp/FIGOptimizationRunner.slnx` in Visual Studio, or run these commands from the sibling `FIGSignalCpp` directory. Build the native Release x64 executable first; building this C# solution does not build C++.

```powershell
# Small verification: baseline plus two candidates, using 12,000 source minute bars.
dotnet run --project ..\FIGOptimizationRunner -c Release --no-launch-profile -- run --config .\optimizer-search.json --samples 2 --max-source-bars 12000

# Full search using the existing configuration (baseline plus 1,000 candidates).
dotnet run --project ..\FIGOptimizationRunner -c Release --no-launch-profile -- run --config .\optimizer-search.json

# Prepare files without accessing the database.
dotnet run --project ..\FIGOptimizationRunner -c Release --no-launch-profile -- prepare --config .\optimizer-search.json --samples 5 --output-dir .\optimizer-candidates\my-batch

# Execute that prepared batch, using its saved configuration.
dotnet run --project ..\FIGOptimizationRunner -c Release --no-launch-profile -- execute .\optimizer-candidates\my-batch

# Local tests without database access.
dotnet run --project ..\FIGOptimizationRunner -c Release --no-launch-profile -- self-test
```

Visual Studio includes **Prepare candidates (no database)**, **Small native verification**, **Full optimization**, and LDAX prepare/run profiles for 200 cases at three-minute intervals. Preparation is the default. Starting without command arguments prints help.

## LDAX: 200 cases at three-minute intervals

From `FIGSignalCpp`, use `optimizer-search-ldax-3m-200.json`. It keeps `ldax-parameters.json` as case 1 and adds 199 unique seeded variations around its values. Build the native Release host and LDAX DLL first, then run:

```cmd
dotnet run --project ..\FIGOptimizationRunner -c Release --no-launch-profile -- run --config .\optimizer-search-ldax-3m-200.json
```

Replace `run` with `prepare` to save the 200 cases without running the backtests. See [LDAX optimization](../FIGSignalCpp/LDAX.md#optimization-200-cases-at-three-minute-intervals) for ranges, dates, finances, and executing a prepared batch.

Collection selection is configurable through `CollectionDll`, `Collection`, and `Signal`. DLL paths resolve relative to the configuration file, like `Executable`. Their defaults retain the adjacent ADX DLL, `ADXBase`, and `STrend_LBaseADX_Side`. LDAX uses `FIGStudyCollection.LDAX.dll`, `LDAX`, and `STrend_LADX_LongSide` (the normalized 0/1 signal). Match the DLL's Debug/Release configuration to the native host.

The random generator checks active parameter names for ADX and LDAX separately. In LDAX, `TrendReEntryShockMax` is active; the preserved `ReEntryCooldownBars` and unused `ContinuationMaxShockATR` stay fixed. For other plugins, ranges must refer to keys in the baseline and the native DLL validates the parameter values before loading prices. A custom collection must specify its own 0/1 long signal.

## Configuration and reports

### Nested and mixed-type parameters

The baseline can contain nested objects and arrays, numbers, booleans, strings, and nulls. Candidate generation preserves the complete JSON tree and changes only the selected paths. Existing flat numeric configurations still work. JSON nesting is supported up to 1,024 levels.

Use dot-separated paths in `Ranges` for ordinary property names:

```json
"Ranges": {
  "indicators.adxLength": [40, 55],
  "entry.startBiasLongTrend": [1, 1.5],
  "exit.exitOnGuideCrossShortTrend": [false, true],
  "marketTrendSettings.timeframe": ["hourly", "daily"]
}
```

Each range remains an array of choices. Choices may also be null, arrays, or complete objects when the target collection supports them. For example, `"optionalThreshold": [null, 2]` or `"weights": [[1, 2], [2, 1]]`. The native collection still validates its own parameter types and values; accepting a value in the generic runner does not mean every DLL accepts it.

Use JSON Pointer paths for array elements and property names containing punctuation:

```json
"Ranges": {
  "/strategies/0/settings/threshold": [1, 2],
  "/group/property.with.dots": [true, false],
  "/group/a~1b": ["first", "second"]
}
```

Array indices start at zero. In a pointer, `~1` represents `/` and `~0` represents `~`; an empty final segment addresses an empty property name. For backwards compatibility, an exact top-level property name is resolved before interpreting a path. For example, a literal `"a.b"` at the root wins over dot traversal; use `/a/b` to select the nested property instead.

Every search path must exist in the baseline. Missing paths, duplicate object keys, equivalent range choices, and overlapping ranges (such as both `entry` and `entry.startBiasLongTrend`) are rejected before preparation writes a batch. Equivalent numbers such as `1`, `1.0`, and `1e0` count as one choice, while `1`, `true`, and `"1"` are distinct. Object property order does not affect candidate identity; array order does.

The baseline remains candidate 1, `Samples` counts additional candidates, and a fixed seed gives repeatable choices. Existing ADX/LDAX schema checks are retained. Other collections, including LADX2, use the generic tree handling and their DLL's validation.

For LADX2, see `../FIGSignalCpp/optimizer-search-ladx2-5m-example.json`. It selects `FIGStudyCollection.LADX2.dll`, collection `LADX2`, and `LADX2_LongSide`, and demonstrates 16 combinations. These ranges illustrate the format rather than recommend trading settings. Prepare without accessing the database:

```powershell
dotnet run --project ..\FIGOptimizationRunner -c Release --no-launch-profile -- prepare --config .\optimizer-search-ladx2-5m-example.json
```

Run this from the `FIGSignalCpp` directory. Use `run` instead of `prepare` when ready to execute the search.

Use the existing `optimizer-search.json`. Paths inside it are relative to the configuration file. Command-line paths are relative to the working directory. Supported overrides are `--samples`, `--max-source-bars`, `--jobs`, `--dataset`, `--executable`, and `--output-dir`. The default executable is `x64/Release/FIGSignalCpp.exe`; optionally add `"Executable"` to the configuration. Output directories must be new.

`Samples` counts additional unique candidates; the baseline is first. `MaxSourceBars: 0` uses all available source rows within the configured dates. Candidate values come from the explicit `Ranges` arrays, with a fixed random seed. The generator sorts keys ordinally, so its sequence may differ from PowerShell's culture-based ordering with the same seed. Keep the archived parameter files for exact reproduction; the batch records the .NET runtime as well.

Each batch contains:

- `parameters/`: individual parameter JSON files.
- `search-settings.json` and `batch.json`: effective configuration, status, timestamps, candidate hashes, native executable and collection DLL hashes, and attempt details.
- `native-attempt-001.log`: native output and errors.
- `reports/`: native `results.csv`, candidate mapping, per-run parameters, trades, and monthly NAV reports, plus `top-candidates.json` for the first ten ranked results.

Ranking remains based on training profit. The current configuration uses dataset 3, five-minute aggregation, 15 contracts, multiplier 20, and the existing training/validation/test date split. Financial accounting, monthly reporting, and cost assumptions are described in [OPTIMIZATION.md](../FIGSignalCpp/OPTIMIZATION.md).

For EFS position sizing, use `"InitialCapital": 1200000`, `"Quantity": 0`, and `"QuantityPct": 1.0`. Each entry uses `ceil(InitialCapital * QuantityPct / Multiplier / HLC3)` from the signal bar. Its quantity is held until exit, and later entries use original capital (no reinvestment). Positive `Quantity` retains fixed contracts. Rebuild/republish both executables before selecting dynamic sizing. The 0740 fine-search configuration uses dynamic sizing from January 2016.

The C# runner passes the settings filename to C++; database credentials and their decryption remain in the native application. No AI service is called.

## Where to extend it

| File | Responsibility |
| --- | --- |
| `Configuration/SearchConfiguration.cs` | Search, finance, date, and execution settings |
| `Search/ICandidateGenerator.cs` | Candidate generation extension point |
| `Search/RandomCandidateGenerator.cs` | Current seeded random search without duplicates |
| `Search/ParameterSet.cs` | JSON parameter trees, path resolution, and immutable replacements |
| `Search/ParameterValue.cs` | Typed JSON choices, serialization, and exact canonical identity |
| `Execution/IBatchExecutor.cs` | Execution extension point |
| `Execution/NativeBatchExecutor.cs` | Launch, logging, and cancellation of C++ batches |
| `Results/ResultReader.cs` | Typed results from native CSV |
| `OptimizationRunner.cs` | Preparation, integrity checks, execution, and result verification |
| `Program.cs` | Commands and dependency selection |

To add grid search or another search method, implement `ICandidateGenerator` and select it in `Program.cs`. Generators must return the baseline first and exactly `Samples` additional unique candidates. For adaptive optimization, use `ResultReader` output to propose a subsequent batch; an adaptive feedback loop is not implemented yet. This also provides a place for future Bayesian or AI-assisted proposals, with explicit candidate budgets and validation before execution.

## Scale and recovery

The native executor reads and aggregates prices once per batch, then evaluates candidates with the configured worker count. Separate batches read the data again by default. To reuse raw and aggregated prices, set `"PriceCacheDirectory": "price-cache"` and `"PriceCacheMode": "use"` in the search configuration. Use `"refresh"` after database changes, then switch back to `"use"`. Cache freshness is explicit, not automatic. Both executables must be rebuilt/republished to enable this feature. See [cache details](../FIGSignalCpp/OPTIMIZATION.md#reusing-prices-between-batches). There is no distributed execution. The current C# configuration permits up to 100,000 additional candidates, but memory, report storage, and runtime still constrain practical batch size.

Ctrl+C stops the child process tree, records cancellation, and preserves files. `execute` can retry a failed or cancelled batch; it reruns the entire batch into a new `reports-attempt-002` directory rather than skipping completed candidates. It refuses changed parameter files, a changed native executable, or a changed collection DLL; prepare a new batch after rebuilding either native binary. Older manifests without a DLL checksum acquire it at execution. Batches left `Running` by a hard crash are not automatically recovered. A lock prevents simultaneous execution of the same batch.
