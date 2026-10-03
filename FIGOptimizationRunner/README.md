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

Visual Studio includes three launch profiles: **Prepare candidates (no database)**, **Small native verification**, and **Full optimization**. Preparation is the default. Starting without command arguments prints help.

## Configuration and reports

Use the existing `optimizer-search.json`. Paths inside it are relative to the configuration file. Command-line paths are relative to the working directory. Supported overrides are `--samples`, `--max-source-bars`, `--jobs`, `--dataset`, `--executable`, and `--output-dir`. The default executable is `x64/Release/FIGSignalCpp.exe`; optionally add `"Executable"` to the configuration. Output directories must be new.

`Samples` counts additional unique candidates; the baseline is first. `MaxSourceBars: 0` uses all available source rows within the configured dates. Candidate values come from the explicit `Ranges` arrays, with a fixed random seed. The generator sorts keys ordinally, so its sequence may differ from PowerShell's culture-based ordering with the same seed. Keep the archived parameter files for exact reproduction; the batch records the .NET runtime as well.

Each batch contains:

- `parameters/`: individual parameter JSON files.
- `search-settings.json` and `batch.json`: effective configuration, status, timestamps, candidate hashes, native executable hash, and attempt details.
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
| `Search/ParameterSet.cs` | Numeric parameter sets, serialization, and identity |
| `Execution/IBatchExecutor.cs` | Execution extension point |
| `Execution/NativeBatchExecutor.cs` | Launch, logging, and cancellation of C++ batches |
| `Results/ResultReader.cs` | Typed results from native CSV |
| `OptimizationRunner.cs` | Preparation, integrity checks, execution, and result verification |
| `Program.cs` | Commands and dependency selection |

To add grid search or another search method, implement `ICandidateGenerator` and select it in `Program.cs`. Generators must return the baseline first and exactly `Samples` additional unique candidates. For adaptive optimization, use `ResultReader` output to propose a subsequent batch; an adaptive feedback loop is not implemented yet. This also provides a place for future Bayesian or AI-assisted proposals, with explicit candidate budgets and validation before execution.

## Scale and recovery

The native executor reads and aggregates prices once per batch, then evaluates candidates with the configured worker count. Separate batches read the data again by default. To reuse raw and aggregated prices, set `"PriceCacheDirectory": "price-cache"` and `"PriceCacheMode": "use"` in the search configuration. Use `"refresh"` after database changes, then switch back to `"use"`. Cache freshness is explicit, not automatic. Both executables must be rebuilt/republished to enable this feature. See [cache details](../FIGSignalCpp/OPTIMIZATION.md#reusing-prices-between-batches). There is no distributed execution. The current C# configuration permits up to 100,000 additional candidates, but memory, report storage, and runtime still constrain practical batch size.

Ctrl+C stops the child process tree, records cancellation, and preserves files. `execute` can retry a failed or cancelled batch; it reruns the entire batch into a new `reports-attempt-002` directory rather than skipping completed candidates. It refuses changed parameter files or a changed native executable; prepare a new batch after rebuilding C++. Batches left `Running` by a hard crash are not automatically recovered. A lock prevents simultaneous execution of the same batch.
