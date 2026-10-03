# ADXBase native batch optimization

The optimizer uses the existing C++ `ADXBase` collection and `STrend_LBaseADX_Side`. It does not duplicate the strategy from FIGBacktestOptimizer. The study-report command continues to work unchanged.

## Reusing prices between batches

The C# runner and native batch command support a two-layer disk cache. Add these properties to the search configuration after rebuilding both executables:

```json
"PriceCacheDirectory": "price-cache",
"PriceCacheMode": "use"
```

Paths are relative to the search configuration. `off` (the backward-compatible default) reads the database every batch. `use` creates missing snapshots and reuses existing ones. `refresh` reloads the database and rebuilds the requested aggregation; change back to `use` after that run. Refresh is explicit: database inserts, historical corrections, and deletions are not detected automatically. Also refresh if an ODBC DSN is redirected to another server/database without changing the settings file.

The first run streams raw minute bars to a binary file and saves aggregated bars separately. Later batches with the same interval/end date load aggregated bars without querying PriceData. A different interval or end date reuses raw bars and creates another aggregate snapshot. The raw namespace includes the settings file location/content, dataset ID, and source-row limit; aggregation keys include the raw content fingerprint, interval, and exclusive end date. Start/training/validation dates and study parameters do not affect cached prices: earlier bars remain available for study warm-up.

Snapshots contain prices, not study values or credentials. All studies/trades are recalculated for each candidate. Cache files have version/length/checksum validation and are published atomically. A busy cache fails with a retry message rather than allowing concurrent writers. Corrupt files require `refresh`. Old aggregate snapshots remain on disk; they are not automatically pruned. Changing aggregation semantics in future code requires incrementing the aggregate cache version.

Native usage adds `--price-cache-dir DIR --price-cache-mode use` to `--backtest-batch`. `batch-settings.txt` records `PriceCache`, `PriceCacheFile`, and `DatabaseRowsReadThisRun`; `SourceRowsRead` remains the snapshot's original source count. Settings and credential decryption are still required at startup, even on cache hits, but the database connection is only opened on a miss/refresh. The standard report files, including `prices.csv`, are still written for each batch.

The cache removes repeated loading and aggregation; it does not remove per-candidate study calculations, which can dominate a large optimization's runtime. Verify using `tests/verify_price_cache.ps1` (a limited, read-only database sample).

## Run

An extensible .NET 10 alternative is now available in [FIGOptimizationRunner](../FIGOptimizationRunner/README.md), using this same search configuration and native backtester. Run from the FIGSignalCpp directory:

```powershell
dotnet run --project ..\FIGOptimizationRunner -c Release --no-launch-profile -- run --config .\optimizer-search.json
```

Open `FIGOptimizationRunner.slnx` to develop or debug it in Visual Studio. The PowerShell commands below remain available.

Use PowerShell 7 and a Release x64 build:

```powershell
# Baseline plus 1,000 reproducible random candidates on dataset 3.
.\Run-Optimization.ps1

# Smaller verification run (MaxSourceBars counts database rows, not aggregated bars).
.\Run-Optimization.ps1 -Samples 2 -MaxSourceBars 80000

# Generate the parameter files without accessing the database or running backtests.
.\Run-Optimization.ps1 -PrepareOnly
```

Edit `optimizer-search.json` to change the dataset, dates, finances, candidate values, or worker count. File paths inside that configuration are relative to its directory. Use `-Config PATH` for another configuration and `-Executable PATH` for a different build. Each invocation creates a new directory under `optimizer-output`; an explicitly supplied `-OutputDirectory` must not already exist.

The baseline is always run-0001. `Samples` is the number of additional unique combinations. Ranges are explicit numeric choices, not min/max bounds. Candidate generation uses a fixed seed, fixed sorted key order, and sampling without replacement. The saved search configuration records overrides. Use the same PowerShell/.NET runtime and configuration for identical generated sequences. Every generated JSON can also be passed to `--calculate-studies --collection ADXBase --parameters FILE`.

The initial prepared files are under `optimizer-candidates/starter/parameters` (baseline plus five candidates) and `optimizer-candidates/search-1000/parameters` (baseline plus 1,000 candidates). Generated candidates and reports are local ignored files. They can be regenerated with the commands above.

To run an already prepared directory directly:

```powershell
.\x64\Release\FIGSignalCpp.exe --backtest-batch --dataset 3 --interval-minutes 5 --parameters-dir .\optimizer-candidates\search-1000\parameters --output-dir .\optimizer-output\prepared-full --settings .\signal-settings.json
```

The native command validates all candidate files before reading prices. It reads/aggregates the SQL stream once and shares the resulting immutable price vector between workers, each owning an independent collection. The saved `prices.csv` contains the exact aggregated input used by every run, including warmup rows. This uses RAM proportional to aggregated history, not candidate count. Output is flushed on file close. If a batch fails, completed per-run files remain; `results.csv` is written only after all candidates succeed. Resume is not implemented; choose a new output directory to retry. No database data is modified.

## Accounting and execution

Defaults: 15 contracts, multiplier 20, initial capital $9,255,000, commission $1.60 per contract **per side**, zero slippage. Thus each fill costs $24 and a completed round trip costs $48. These settings are configurable. Capital is a fixed NAV denominator. There is no reinvestment, pyramiding, margin simulation, or liquidation rule on negative equity.

For EFS-compatible position sizing, set `"InitialCapital": 1200000`, `"Quantity": 0`, and `"QuantityPct": 1.0`. On each entry, quantity is `ceil(InitialCapital * QuantityPct / Multiplier / ((High + Low + Close) / 3))`, using the aggregated signal bar. This matches EFS's previous-bar HLC3 when it processes that signal on the following bar. A positive `Quantity` retains fixed sizing; zero selects dynamic sizing. `QuantityPct` is a fraction greater than zero and at most one (0.5 means 50%). Native flags are `--capital 1200000 --quantity 0 --quantity-pct 1`.

Quantity remains fixed throughout each open trade; commissions, mark-to-market profit, and exits use that entry quantity. Each new entry sizes from original capital, not accumulated equity. Trade CSV `Contracts` and monthly/summary `OpenContracts` report actual quantities. Ceiling rounding can slightly exceed the target allocation. A positive finite entry HLC3 and a quantity fitting a positive integer are required. NAV remains `100 * equity / InitialCapital`. This matches the requested sizing formula, not a claim of complete EFS trade/date parity. The 0740 fine-search configuration now uses these EFS sizing settings and starts on January 1, 2016.

- Enter at the signal bar's close when the long signal changes from 0 to 1; exit when it returns to 0. This is an explicit same-close simulation assumption, not a promise of executable fills. Slippage adds points to buys and subtracts points from sells.
- Gross realized profit = `(exit - entry) * quantity * multiplier`.
- Unrealized profit = `(current close - entry) * quantity * multiplier`.
- Equity = `initial capital + gross realized profit + unrealized profit - commissions`.
- NAV = `100 * equity / initial capital`.
- Drawdown percent = `100 * (equity / running peak equity - 1)`; drawdown NAV points = `100 * (equity - peak) / initial capital`. Both are nonpositive. The latter corresponds to the supplied EFS non-reinvestment drawdown convention.

Commissions are deducted exactly once per fill. Trade net profit includes both entry and exit fees. With an open final position, run net profit also includes its unrealized value and entry commission; it therefore need not equal the sum of closed-trade profits. Profit factor uses net closed-trade profits and is blank when there are no losing trades (rather than writing infinity).

Open positions carry across months and across train/validation/test boundaries. By default the final position remains open and marked at the last close. `ForceCloseAtEnd` closes it at that close with exit costs; the trade is labeled END_OF_DATA. A signal opening on the final bar will also be closed under this option.

Dates and monthly grouping are **UTC**, not the workstation's local time. All upper boundaries are exclusive. Bars before StartDate warm the entire study collection; portfolio accounting begins flat at StartDate and enters on its first observed active long signal. Partial first and last aggregate buckets are included. A month with no bars carries forward the previous equity/position and has Bars=0 and the previous LastPriceRawTime. No synthetic trades occur during gaps. Month-end open P&L is marked at the last available price, and the last report month can be incomplete.

## Reports

Each batch contains `parameters`, `search-settings.json`, and `reports`:

| Report | Meaning |
| --- | --- |
| results.csv | Every candidate, ranked by training-period net profit; full-run and separate train/validation/test metrics; full parameter JSON |
| run-NNNN-monthly.csv | Start/end equity and NAV, monthly net profit and return, gross realized profit, ending unrealized profit, costs, closed trades, open contracts, and worst drawdown from the run's peak observed in that month |
| run-NNNN-trades.csv | Each completed trade's timestamps, fill prices, size, gross/net profit, costs and exit reason |
| run-NNNN-parameters.json | Exact candidate supplied to the collection |
| prices.csv | Shared aggregated input snapshot |
| candidate-files.csv | Run ID to original parameter file mapping |
| batch-settings.txt | Effective dates, costs, sizing, data bounds and execution assumptions |

Monthly net profit is the **change in equity**, not realized profit plus ending unrealized profit. It includes the change in unrealized profit on positions carried into the month. Returns are blank when starting equity is nonpositive. Segment metrics likewise use changes in continuous equity, including open P&L at each boundary. Segment drawdown restarts its peak at the segment's starting equity; all-run drawdown does not reset. Segments without observations have blank profit/return/drawdown and zero bars. Ranking is tied if no training data is present.

## Search scope

The provided search varies ADX length, fast SMA length, Guide length/deviation, start/stop bias, cooldown, shock ATR length and thresholds, chop thresholds, and continuation window. Confirmation ADX, Stop SMA, fail-fast settings, trend-reentry labeling, and disabled continuation slope/shock gates are held fixed because they do not currently change long trade decisions. A full Cartesian product of the active choices would be very large; the initial 1,000 samples are a starting search, not an exhaustive maximum-profit result. Refine the Ranges around robust candidates in a second configuration.

Default segments are 2010–2022 training, 2023–2024 validation, and 2025 onward test. Ranking uses only training net profit. Use validation to assess shortlisted candidates, and avoid repeatedly choosing against the final test period. These are chronological reporting splits of a fixed parameter run, not an adaptive walk-forward optimizer. Dataset 2 overlaps the later period of dataset 3; it is not extra independent history. ATR/platform parity still has the limitations described in EFS_PARITY.md.

## Verification

`--self-test` includes independent accounting cases for fees, repeated active signals, open positions, forced liquidation, slippage, month gaps, and segment boundaries. The study suite also checks replay/rollback. To independently reconcile a default-cost, no-forced-close baseline against a study export containing exactly the same bars:

```powershell
node .\tests\verify_backtest.mjs .\study-export.csv .\optimizer-output\BATCH\reports
```
