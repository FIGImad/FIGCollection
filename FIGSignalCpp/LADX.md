# LADX collection

`FIGStudyCollection.LADX.dll` registers the collection type `LADX`. It ports the active studies from the EFS attached on 2026-10-06. Native outputs use the `LADX_` prefix, including `LADX_SIDE` for the entry subtype and `LADX_LongSide` for the normalized 0/1 backtest signal.

The project links only `FIGStudyCore.lib`; it has no dependency on `FIGStudyCollection.ADX.dll` or that collection's private study implementations.

## Build and run

Build `FIGSignalCpp.slnx` in x64 Debug or Release. The new project can also be built independently as `FIGStudyCollection.LADX.vcxproj`. To include it in another solution, use **Add > Existing Project** and select that project.

From the `FIGSignalCpp` directory:

```cmd
.\x64\Release\FIGSignalCpp.exe --calculate-studies --dataset 3 --collection-dll .\x64\Release\FIGStudyCollection.LADX.dll --collection LADX --parameters .\ladx-parameters.json --interval-minutes 5 --settings .\signal-settings.json --output .\ladx.csv
```

For Visual Studio debugging, use the active configuration's DLL:

```text
--calculate-studies --dataset 3 --collection-dll "$(TargetDir)FIGStudyCollection.LADX.dll" --collection LADX --parameters "$(ProjectDir)ladx-parameters.json" --interval-minutes 5 --settings "$(ProjectDir)signal-settings.json" --output "$(ProjectDir)ladx.csv"
```

Unlike the default ADX collection, LADX requires an explicit `--collection-dll`. Match the DLL's Debug/Release configuration to the executable. No executable source changes or rebuild are required to load LADX through the existing plugin interface.

`ladx-parameters.json` contains the attached EFS `preMain` defaults. Omitting `--parameters` uses the EFS constructor fallbacks instead: ADX 50, price MA 4, guide 8 with 0.6 deviations, shock ATR 14, start bias 1, stop bias -0.75, and the constructor's shock/chop settings. JSON field names match the EFS, are case-sensitive, and unknown/duplicate fields are rejected. `null` retains the constructor default. Periods must be positive integers; the cooldown count is floored as in the EFS.

## Study mapping

| Active EFS study | Native implementation | Output |
| --- | --- | --- |
| `Study_SMA(FastMALen, "ohlc4", "Price")` | Common `StudyMA` | `MAPrice` |
| `Study_BB(GuideMALen, GuideStdDev, "ohlc4", "Guide", "1")` | Common `StudyBB` | `MIDGuide`, `UBGuide`, `LBGuide` |
| `Study_ADX(AdxLen, AdxLen, "Active")` | Common `StudyADX` | `Active_ADX`, `Active_PDI`, `Active_NDI`, `Active_BIAS` |
| `Study_PriceShock(ShockATRLen, "Shock")` | LADX-private `StudyPriceShock`, common `StudyATR` | Ten `Shock_` measurements |
| `Study_SMASlope(GuideMALen, 5, 0.1, "SLGuide")` | Common `StudySMASlope`, close SMA | Three `SLGuide_` fields |
| `Study_LADX("STrend", params)` | LADX-private `StudyLADX` | `LADX_` numeric values and text reasons |

Price SMA reuses the guide BB's mean when their periods match. Guide slope uses **close**, as the EFS built-in `sma()` does; it must not reuse the guide's **ohlc4** mean. State belongs to each collection instance.

`Study_SEM` and `Study_ADXROC` are defined in the attachment but are not instantiated by its active collection; their implementations already exist in the common core. Commented confirmation ADX, stop MA, ADXROC, additional slopes, and volatility studies remain inactive. Chart drawing, the JavaScript TradeEngine, sizing, commissions, and strategy-monitor UI are outside this study DLL.

## Signals and backtesting

The original `LADX_SIDE` values are preserved:

| Value | Meaning |
| --- | --- |
| `0` | Flat |
| `1.1` | Trend re-entry / price above guide |
| `1.2` | Pullback entry / price below guide |

Entry requires a recovering price SMA, non-deteriorating ADX bias strictly above `StartBias`, no chop condition, and the applicable pullback or re-entry setup. Exit priority is `ADX_BIAS`, `SHOCK_DROP`, `ARMED_DROP`, then `SHOCK_RANGE`.

The collection additionally publishes **`LADX_LongSide`**, equal to 1 for either long subtype and 0 when flat. The updated native backtester reads the exact configured `Signal` column: positive means long, negative means short, and zero means flat, for every collection. Changes between 1.1 and 1.2 keep the existing long position without another entry or commission. Missing and non-finite values are invalid. The supplied LADX study currently produces long and flat signals only; its EFS behavior is unchanged.

Older native executables reject fractional `LADX_SIDE` values. Rebuild or replace `FIGSignalCpp.exe` to use the subtype column directly; no change to the study DLL or C# runner is required. After replacing the native executable, use `run` to prepare a new batch with its checksum, rather than retrying a batch pinned to the old binary. To select the new build without replacing a deployed executable, pass `--executable C:\Users\Imad\source\FIGCollection\FIGSignalCpp\x64\Release\FIGSignalCpp.exe` to the runner.

```cmd
.\x64\Release\FIGSignalCpp.exe --backtest-batch --collection-dll .\x64\Release\FIGStudyCollection.LADX.dll --collection LADX --signal LADX_LongSide --parameters-dir .\ladx-candidates --output-dir .\ladx-results --dataset 3 --interval-minutes 5 --settings .\signal-settings.json
```

Create `ladx-candidates` containing one or more LADX parameter JSON files before running the batch. Execution/sizing options are separate from study parameters; native backtest defaults do not reproduce the attachment's TradeEngine settings automatically. Use the LADX search configuration below with the updated C# runner. The older PowerShell random-search wrapper remains specific to ADX.

## Optimization: 200 cases at three-minute intervals

`optimizer-search-ladx-3m-200.json` uses the values in `ladx-parameters.json` as the baseline. `Samples: 199` means **200 total cases**, with the exact baseline values in `run-0001.json`. The other cases are unique seeded random combinations from explicit choices around the baseline. This is a sampled search, not an exhaustive grid.

The prepared batch is `optimizer-candidates/ladx-3m-200`. Execute it from the `FIGSignalCpp` directory using the updated runner build:

```cmd
.\x64\Release\optimizer\FIGOptimizationRunner.exe execute .\optimizer-candidates\ladx-3m-200
```

Results will be in `optimizer-candidates/ladx-3m-200/reports/results.csv`, with per-case trades and monthly reports alongside it. The batch is prepared only; historical database backtests have not been run. Native executable and LADX DLL checksums are recorded; if either binary changes, prepare a fresh batch.

To prepare or run a fresh batch after editing the base file or search ranges:

```cmd
.\x64\Release\optimizer\FIGOptimizationRunner.exe prepare --config .\optimizer-search-ladx-3m-200.json
.\x64\Release\optimizer\FIGOptimizationRunner.exe run --config .\optimizer-search-ladx-3m-200.json
```

Each command creates a new batch directory under `optimizer-output/ladx-3m`. Use `execute DIRECTORY` to run an existing prepared batch; `run` creates its own new batch.

The configuration uses dataset 3, three-minute aggregation, two workers, and all available source rows. It retains the existing EFS search's finance/date settings: starting capital 1,200,000, dynamic quantity at 100% allocation, multiplier 20, commission 1.60 per contract per side, zero slippage, no forced final exit, and accounting from January 1, 2016. Training ends January 1, 2023; validation ends January 1, 2025; later data is the test segment. Prices are cached locally in `price-cache`; use `PriceCacheMode: "refresh"` after database corrections.

The 14 varied parameters are ADX length, fast/guide MA lengths, guide deviation, start/stop bias, trend re-entry shock maximum, shock ATR length and four shock thresholds, and the two chop thresholds. Every range includes its baseline value. `ReEntryCooldownBars` stays at 2 and `ContinuationMaxShockATR` stays at 1 because the supplied EFS behavior makes them ineffective for this search.

The updated runner accepts `CollectionDll`, `Collection`, and `Signal` in its search configuration. This configuration selects the Release LADX DLL and `LADX_LongSide`. It also records/checks the collection DLL hash, so rebuilding the studies cannot silently change a prepared batch.

The sibling `FIGOptimizationRunner` source directory was read-only to this chat. The runnable build above includes the changes; the corresponding source update is saved in [build/FIGOptimizationRunner-LADX.patch](build/FIGOptimizationRunner-LADX.patch). To apply it from the `FIGSignalCpp` directory with write access to the sibling project:

```cmd
git -C .. apply --check FIGSignalCpp/build/FIGOptimizationRunner-LADX.patch
git -C .. apply FIGSignalCpp/build/FIGOptimizationRunner-LADX.patch
dotnet run --project ..\FIGOptimizationRunner -c Release --no-launch-profile -- self-test
```

After applying the patch, the source runner also supports `dotnet run --project ..\FIGOptimizationRunner -c Release --no-launch-profile -- run --config .\optimizer-search-ladx-3m-200.json`, plus LADX prepare/run Visual Studio profiles.

To complete both the sibling source rename and the `C:\Roots\OptimizationV2` deployment update, run this once in PowerShell 7 from the `FIGSignalCpp` directory:

```powershell
.\build\Complete-LADX-Rename.ps1
```

The script applies the runner rename patch and installs the tested Release DLL and runner. It backs up the original deployment files, renames the base parameter file and output folder, updates prepared/failed batch configurations to `LADX` and `LADX_LongSide`, records the new DLL hash, and verifies that candidate files and the native host still match their recorded hashes. Prior attempt reports remain intact. Orphan manifests without parameter files are archived. Active or completed batches are not rewritten. No historical backtests are started. The original source and deployment paths remain unchanged until this script succeeds; this chat could not write to either location.

Afterward, from `C:\Roots\OptimizationV2`, start a fresh batch with `FIGOptimizationRunner.exe run --config search-settings.json --samples 199`, or retry an existing migrated failed batch with `FIGOptimizationRunner.exe execute BATCH_DIRECTORY` if its recorded executable/DLL checksums still match. `LADX_LongSide` works with older hosts; the updated host also accepts `LADX_SIDE` directly.

Verification: the updated runner's self-tests passed, including LADX-specific range validation, path/argument forwarding, preserved ADX defaults, and DLL integrity checks. `node tests/verify_ladx_optimization.mjs` verified all 200 cases, baseline equality, determinism, and 120,000 native signal rows from synthetic three-minute bars. It does not measure historical trading performance or exercise database aggregation.

## Preserved EFS behavior

These behaviors are intentional, as requested, for comparison with the supplied file:

- As requested on 2026-10-08, exit cooldown now matches ADXBase: every exit resets `BarsSinceExit` to zero, subsequent bars increment it up to 1,000,000, and entries require `BarsSinceExit >= ReEntryCooldownBars`. With 2, the next bar is blocked and the second bar is eligible. Values 0 and 1 allow the next bar. This is an intentional signal change from the supplied EFS; `LastExitReason` stays empty. The subsequent per-trade armed-shock correction is documented below. Use a fresh report folder and retain the previous DLL for comparison.
- `StopReason` persists while flat and on an entry bar; it is cleared at the start of the next bar processed while already in a trade.
- `GuideWidthATR` remains zero in the published diagnostics. The local width and slope ratios still participate in the chop decision. Missing chop inputs use local zero ratios and can therefore classify as chop.
- `LADX_ADXSlope` remains absent/blank because the ADXROC study is commented out.
- `ContinuationMaxShockATR` is accepted because `preMain` exposes it, but the supplied signal never uses it.
- SMA/BB warmup outputs are zero; missing ADX, ATR, and slope values are empty numeric fields. There are **41 numeric outputs and 2 text outputs**, including the additional normalized long signal.

Timestamp replacement and historical rewind follow the existing core contract: recompute from the preceding snapshot, truncate later state, and support a default 20-bar revision depth. This intentionally avoids accumulating stale intra-bar state from the EFS's mutable properties. `maximum_lookback()` describes warmup only; recursive ADX/ATR and signal state require chronological replay for exact restart equivalence.

## Verification and limits

```cmd
.\x64\Release\FIGSignalCpp.exe --test-collection --collection-dll .\x64\Release\FIGStudyCollection.LADX.dll
node .\tests\verify_ladx.mjs Release
node .\tests\verify_ladx.mjs Debug
```

The DLL self-test covers parameter validation, entry gates and subtypes, stop precedence, preserved EFS quirks, repeated current-bar replacement, 1/5/20/32-bar rollback, boundary rejection, and replay equivalence including text outputs.

`verify_ladx.mjs` runs the actual DLL using the offline `FIGCollectionProbe.exe` and compares every output against the study classes with a **cooldown-only runtime adaptation** (the fixture remains unmodified) in `tests/fixtures/ladx/reference.efs.js`. It tests eight parameter/price scenarios covering 7,580 bars and 325,940 output comparisons, checks both entry subtypes, rejects invalid parameters before creating output, and runs from a deployment containing only the probe and LADX DLL. Neither SQL access nor the ADX DLL is needed.

The reference harness supplies independent mocks of EFS's built-in SMA and Wilder ATR. The first ATR seed requires a previous close, matching the existing native core. Exact eSignal platform seeding remains unverified without a platform export. The native core also clamps a tiny negative BB variance caused by rounding to zero. These checks establish formula parity on the test scenarios, not complete eSignal runtime or TradeEngine parity.

Original attachment SHA-256: `AE271E12DFF907A4173C3DAECCE285825A02B28C9323302A2921BA1186D34920`.

## Above-guide entry update (2026-10-08)

LADX now uses ADXBase's continuation entry window. `ContinuationEntryBars` defaults to 5 to match ADXBase case 0924; zero disables above-guide entries. Both price MA and close must be at or above the guide. The breakout bar has age zero, entry is allowed through age 5 inclusive, and a move below the guide resets the window. The counter advances even during open trades. Recovery, bias, chop, and cooldown gates still apply. Above-guide entries no longer use `TrendReEntryShockMax`, matching ADXBase; this field remains accepted for existing parameter files. Pullback entries and exits are unchanged, with the later armed-shock update described below. The reference harness adapts the cooldown and window rules without modifying the original EFS fixture.

## Per-trade armed-shock update (2026-10-08)

`MaxUpATR` now resets to zero at every entry and every exit, matching ADXBase. The entry bar does not arm the trade. On subsequent held bars, an upward shock at or above `ShockArmUpATR` enables the `ShockArmedDropATR` exit for that trade only; the same bar can both arm and exit. Cooldown and the above-guide window remain enabled. Other exit priorities and diagnostics are unchanged. The reference harness now also resets arming at trade transitions; the original fixture remains untouched. Use `FIGStudyCollection.LADX.ArmedReset.dll` and a fresh `reports-LADX-0924-C` directory for comparison with run B.
