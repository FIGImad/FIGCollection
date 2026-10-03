# EFS study collection

`register_integrated_collections` registers `ADXBase`, matching the active `StudyCollection` in the supplied EFS script. `ADXTrend` remains available for existing callers.

## Running it

```powershell
.\Run-StudyReport.ps1 -DataSetId 1 -Collection ADXBase -Parameters .\adx-base-parameters.json -Output .\dataset-1-studies-adx-base.csv
```

Add `-MaxBars 4096` for an initial source-row sample and `-IntervalMinutes 5` to aggregate one-minute prices before calculating the collection. The default interval is one minute. Periods count the resulting bars. The parameters file contains the script's `preMain` defaults. Without a parameters file, the native collection uses the EFS constructors' fallback defaults, which differ (for example fast/slow 4, guide 8, shock ATR 14).

For C++ callers, set `StudyCollectionConfig.type` to `ADXBase` and `parameters` to the JSON document, then use `StudyRegistry::create` and `process(bar)`. Use one instance per dataset. Numeric results are in `StudyBar.studies`; entry modes and reasons are in `StudyBar.text_studies`. CSV exports include both; missing numeric results are empty fields.

## Mapping and execution order

| EFS | Native class | Active collection |
| --- | --- | --- |
| Study_SMA | StudyMA | Price, Stop, using ohlc4 |
| Study_BB | StudyBB | Guide, using ohlc4 |
| Study_ADX | StudyADX | Active, Conf |
| Study_ADXROC | StudyADXROC | Active, reading **Active_BIAS**, lookback 10 |
| Study_PriceShock | StudyPriceShock / StudyATR | Shock |
| Study_SMASlope | StudySMASlope | SLConf, SLGuide, using close |
| Study_LBaseADX | StudyLBaseADX | STrend |
| Study_SBaseADX | StudySBaseADX | STrend |
| Study_SEM | StudySEM | Available separately |
| Study_CycleDir | StudyCycleDir | Available separately, accepts BB results |
| Study_VolatilityRatio | StudyVolatilityRatio | Available separately |
| Study_LBaseADXAttempt / New | StudyLBaseADXAttempt / StudyLBaseADXNew | Available separately |

The active collection publishes 90 numeric fields and 10 text fields. Its SMA/BB warmup outputs are zero, matching the script; the primitive classes retain optional values. Valid zero-valued BB and SEM windows now calculate correctly.

`SharedMovingAverages` registers unique `(period, source)` dependencies and evaluates each once for every collection `process(bar)` call. Price and Stop share their SMA when their periods match. The slope studies consume supplied close-SMA values through `StudySMASlope::process(raw_time, moving_average)`; their standalone `process(bar)` remains available. If both slopes have the same SMA period they also share a result. A Price/Stop SMA matching the Guide period reuses the BB middle value. Close and ohlc4 always remain separate sources.

The shared set belongs to one collection instance, so datasets and aggregation intervals cannot contaminate each other's history. Dependencies are fixed before streaming so every calculation receives the complete input history. Reading a result never advances state. Each new invocation recomputes the values, including same-timestamp corrections; historical revisions retain the existing rollback contract. This sharing currently applies to SMA dependencies in ADXBase, not all indicator types or separate collection instances.

The unused BaseStudy/BaseStudies assembly is not registered; its constituent indicators are available separately. Study_Cross_SEMMA and Study_Cross_MAMA are excluded as requested. Study_BuiltInADX delegates to eSignal APIs whose implementations were not supplied; native StudyADX implements the supplied custom ADX algorithm. Chart drawing, TradeEngine and order strategies are outside the study collection.

## Deliberately preserved script behavior

- Long entry uses bias plus pullback/continuation. The commented confirmation, continuation slope and shock gates remain disabled.
- Short entry uses `bias < StartBias` and confirmation MA slope below -1. Short exit uses `bias > StopBias`, with confirmation slope above 1 taking priority. These thresholds are not silently sign-corrected; displayed short thresholds still follow the script's negation.
- Fail-fast counters are maintained, but the commented entry-low/high, pullback-failure and guide-band exits remain disabled.
- Trend reentry can name a qualifying entry; its commented standalone entry condition stays disabled.

## Revisions and numerical limits

All native calculations restore the preceding state when a timestamp is replaced. A historical replacement truncates later results and supports the configured revision depth (20 bars by default); callers replay subsequent prices. This includes signal counters, extrema, reasons and modes. It deliberately fixes the EFS custom ADX's repeated smoothing when revisiting its initial DM/ADX seed bar, and avoids carrying mutable signal fields across repeated evaluations of the same bar. Cycle direction advances its state once per bar.

ADX initializes from `length` directional ranges, averages `smoothing` valid DX values, then uses Wilder smoothing. First DI is available at bar `length + 1`; first ADX at bar `length + smoothing`. Zero true range produces missing DI/ADX; zero directional movement with positive range produces DX zero. Recursive indicators require historical replay to reproduce an established state; `maximum_lookback()` describes warmup, not an exact restart horizon.

ATR follows the initial arithmetic mean and subsequent Wilder recurrence documented by [eSignal](https://download.esignal.com/products/workstation/help/charts/studies/atr.htm). The native seed requires a previous close. Exact first-bar initialization of eSignal's built-in `atr()` remains unverified without an exported platform reference; this can affect early shock and volatility values. SEM length one remains unsupported because its regression denominator is zero. Decimal is the project's binary floating-point type, so tiny rounding differences from JavaScript are expected.

## Verification

`FIGSignalCpp.exe --self-test` checks independent ADX calculations, warmup and zero inputs, signal transitions and gate behavior, and current-bar / 5-bar / 20-bar revisions against fresh replay (including text outputs).

```powershell
node .\tests\verify_efs_adx.mjs .\dataset-1-studies-adx-base-sample.csv .\adx-base-parameters.json
```

This reference harness uses the pasted EFS custom ADX calculation with mocked EFS price accessors and independently checks the collection's moving averages, bands, slopes and bias ROC. Dataset 1's 4,096-bar sample passed 90,112 value comparisons; the largest absolute difference was 2.842170943040401e-14. This is formula parity, not a claim that the entire eSignal runtime or trade engine has been reproduced.
