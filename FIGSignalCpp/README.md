# FIGSignalCpp

Native Windows/C++ rewrite of `FIGSignalSvc`. It is designed as one executable: study collections are registered at compile time and there is no assembly, manifest, or DLL loading.

## Current parity gate

Build x64 and run `FIGSignalCpp.exe --self-test`. Tests cover native primitives, aggregation, study collections, signal transitions, and timestamp replacement/rollback. The supplied EFS collection is registered as `ADXBase`; see [EFS parity and usage](EFS_PARITY.md) for its parameters, reference comparisons, and remaining platform differences.

This directory is an active port. It must not replace `FIGSignalSvc` in production until the parity checklist in `PORTING.md` is complete.

## Project layout

For parameter optimization, open `FIGOptimizationRunner.slnx`. The [C# optimization runner](../FIGOptimizationRunner/README.md) lives in the sibling `FIGOptimizationRunner` directory and provides an extensible alternative to `Run-Optimization.ps1` while using the same native studies and backtester.

Headers live under `include/fig` and implementations under `src`, grouped by responsibility:

| Folder | Contents |
| --- | --- |
| `studies` | Study classes, rollback history, collection registry, calculator, and CSV reporting |
| `database` | ODBC access, repository queries, and model types in `models.hpp` |
| `prices` | Price aggregation and range merging |
| `infrastructure` | Configuration and logging |
| `src/app` | Console entry point, command handling, and self-tests |

Visual Studio's Solution Explorer uses matching groups through `FIGSignalCpp.vcxproj.filters`. Include paths follow the folders, for example `fig/database/database.hpp` and `fig/studies/study.hpp`. Namespaces and public class names are unchanged. Database models remain independent of ODBC headers so studies can use `PriceBar` without including Windows database APIs.

## Live study rule

Every native study is a separate class under `include/fig/studies` with its implementation under `src/studies`. `process(...)` is the primary API and consumes one timestamped bar (or one timestamped upstream study value). The class retains its rolling state and a bounded rollback history so a live current-bar update replaces that bar rather than appending or recalculating all history. Collection startup uses `StudyCollection::replay`, which feeds historical bars through the same live path.

`raw_time` identifies a bar within a study instance:

- A newer timestamp appends one bar.
- The same timestamp replaces that bar and calculates its value again from the preceding state.
- An earlier timestamp discards that bar and all later calculations, restores the preceding state, and calculates the supplied replacement. Processing now ends at that timestamp. Feed later bars again in chronological order if you want to rebuild them. A previously unseen timestamp between retained bars follows the same rule.

All studies default to a `revision_depth` of 20 and accept a custom positive depth in their constructors. Depth counts bars after the bar being replaced: 20 permits replacing the current bar or a bar up to 20 bars ago. The history also retains the predecessor needed to recalculate the oldest allowed bar. Once older snapshots have been discarded, a revision without a retained predecessor throws `std::out_of_range` before changing state. Rewinding does not recover previously discarded snapshots; use a fresh instance and historical replay for older corrections.

```cpp
fig::studies::StudyMA ma(3, "close", 20); // period, source, rollback depth
(void)ma.process(fig::PriceBar{.raw_time = 100, .close = 1});
(void)ma.process(fig::PriceBar{.raw_time = 200, .close = 2});
auto value = ma.process(fig::PriceBar{.raw_time = 300, .close = 3}); // 2
value = ma.process(fig::PriceBar{.raw_time = 300, .close = 6});      // 3: replaces 3
value = ma.process(fig::PriceBar{.raw_time = 200, .close = 5});      // no value yet: only two bars
value = ma.process(fig::PriceBar{.raw_time = 300, .close = 6});      // 4: replay after rewind
```

Caller-owned output arrays or stored results must likewise replace the supplied timestamp and discard later results. `process` returns only the supplied bar's result; it cannot modify results already stored by the caller. `--self-test` checks replacements, rewinds, replay equivalence, and rollback boundaries across the studies and the ADXTrend collection.

## Calculate studies from SQL Server

For repeated parameter searches and monthly NAV reports using the actual ADXBase long signal, see [native batch optimization](OPTIMIZATION.md). `Run-Optimization.ps1` generates reproducible parameter files and runs one native batch against a shared aggregated input history.

Use `-IntervalMinutes 5` (native option `--interval-minutes 5`) to aggregate the database's one-minute rows **before** any study calculation. The default is 1, preserving the original rows and IDs. For example:

```powershell
.\Run-StudyReport.ps1 -DataSetId 1 -IntervalMinutes 5 -Collection ADXBase -Parameters .\adx-base-parameters.json -Output .\dataset-1-studies-adx-base-5min.csv
```

Aggregation streams one bucket at a time: first open, maximum high, minimum low, last close, and summed volume. Five-minute buckets are aligned to epoch-time multiples of 300 seconds (10:00 through 10:04 belong to the bucket stamped 10:00). Aggregated IDs are -1. No empty bars are generated across gaps. Partial first/last buckets are included, including a live unfinished final bucket. Study periods now count aggregated bars, so MA 128 at five minutes uses 128 five-minute bars. `-MaxBars` continues to limit **source database rows**, before aggregation, and may end partway through a bucket. The completion message counts output study bars.

Build x64 (Release recommended for a full database export), then run from PowerShell:

```powershell
.\Run-StudyReport.ps1 -DataSetId 1 -Period 128 -Output .\dataset-1-studies-128.csv
```

The script reads `ODBCName`, `ODBCUser`, and encrypted `ODBCPassword` from `signal-settings.json`. The registered 64-bit DSN supplies the driver, server, database, and connection options. The executable decrypts the password using the existing `FIG_MASTER_KEY`; there are no password prompts or SQL credential environment variables during report runs. The script prefers a Release executable when available. `-AdxSmoothing` defaults to `-Period`; `-Source` defaults to `close`. An existing output file is overwritten on each run.

The command performs a parameterized, read-only query of `FIGAutoTrader.dbo.PriceData`, filters by `DataSetId`, and streams rows in ascending `RawTime` order. It calculates all available history from the earliest bar, preserving warmup and recursive ADX state. It does not modify the table or fire its change triggers. `PriceDate` is not required for the studies; the CSV retains `Id`, `DataSetId`, `RawTime`, OHLC, and volume to identify each input row.

The export reads values as they are fetched; it does not lock the dataset for a point-in-time snapshot. A live feed can update recent bars after they are read. Rerun to overwrite the report with refreshed calculations.

The CSV contains MA, WMA, ROC, SEM, Bollinger Bands, Extended values, ADX/PDI/NDI, slope, and ADXTrend collection values (including LBaseADX signals). Settings are:

- The selected period applies to MA, WMA, ROC, SEM, BB, Extended, and ADX length. ADX smoothing is separately configurable.
- The selected source applies to the price-based primitive studies. ADX uses high/low/close. The existing ADXTrend collection uses `ohlc4` for its moving averages and bands, with both MA lengths set to the selected period.
- BB uses 2 deviations; Extended uses 1 and 2 deviations. Slope uses a 3-bar lookback and a 0.5-point flat threshold. ADXTrend uses a trigger of 20.
- Empty CSV fields represent absent study values during warmup. Values use the project's existing binary floating-point `Decimal` and existing study formulas, including its custom ROC calculation.

For a small initial sample, add `-MaxBars 256`. This reads the **earliest** 256 bars; omit it for the full dataset. Results near the start may still be in warmup (ADX 128/128 first becomes available on bar 256).

The native command is also available for application integration:

```text
FIGSignalCpp.exe --calculate-studies --dataset 1 --period 128 --adx-smoothing 128 --source close --output dataset-1-studies-128.csv
```

The executable loads `signal-settings.json` from the current working directory by default; use `--settings PATH` for another file. It requires a nonempty `ODBCName` and constructs `DSN=configured-name;`. SQL credentials come exclusively from `ODBCUser` and `ODBCPassword` in that file. These must either both be populated or both be absent for DSN-only authentication. Plaintext passwords are not accepted. The script accepts `-Settings PATH` and defaults to the settings file beside the script. The query still explicitly targets `FIGAutoTrader.dbo.PriceData`.

For C++ callers, `MainRepository::for_each_price` provides the ordered stream and `StudyCalculator::process` returns a `StudyBar` containing the named study values. `StudyReportOptions` selects period, smoothing, and source. Use a separate calculator per dataset.

### Encrypted database credentials

To save or update the SQL login, run once in PowerShell 7.4 or newer:

```powershell
pwsh -File .\Set-DatabaseCredentials.ps1 -UserName roots
```

This prompts for the password and writes `ODBCUser` and an encrypted `ODBCPassword` into `signal-settings.json`, preserving the other settings. Normal report runs then need no password prompt. The existing master key is neither generated nor changed by this script.

The encryption exactly matches `FIGCommon/Utilities/ProtectedDataUtil.cs` (`Protect`/`Unprotect`): AES-256-GCM, UTF-8 plaintext, 12-byte random nonce, 16-byte authentication tag, and Base64 of `nonce + ciphertext + tag`. `FIG_MASTER_KEY` must be Base64 encoding of 32 bytes. Both implementations look first for `/run/secrets/FIG_MASTER_KEY`, then Machine, User, and Process environment settings, in that order. The master key stays outside JSON. Existing values produced by the C# utility can be used directly as `ODBCPassword`.

Missing/invalid keys, invalid Base64, and failed authentication tags stop processing before the output file is opened. The native self-test includes a known AES-GCM vector and rejection of an incorrect key. SQL credentials are no longer read from `FIG_SQL_USER` or `FIG_SQL_PASSWORD`.

## Formatting

The project uses the checked-in `.clang-format` file: C++20, four-space indentation, no tabs, Allman braces, and a 120-column limit. Format new or modified C++ files with `clang-format -i --style=file` before building.
