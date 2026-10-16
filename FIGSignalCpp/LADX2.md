# LADX2 collection

`FIGStudyCollection.LADX2.dll` registers `LADX2` and implements the active study logic of [the attached EFS](efs/LADX2.efs). It is an independent project in `FIGSignalCpp.slnx`, links only `FIGStudyCore.lib`, and leaves the original LADX collection unchanged.

## Parameters and outputs

The default options and `ladx2-parameters.json` use the script's **5-minute defaults**. `ladx2-2min-parameters.json` supplies its separate 2-minute preset; presets are selected explicitly, not automatically from the chart interval. Unlike the script's currently disabled JSON input, native JSON overrides are enabled. Omitted fields inherit the 5-minute defaults.

Use the canonical groups `indicators`, `entry`, `exit`, and `marketTrendSettings`, with the same camelCase field names as the EFS. The script's flat aliases are also accepted, including `StartBias` / `StopBias` (each sets both regime thresholds), `FastMALen` / `PriceMALen`, and `MarketTrendSettings`. Unknown fields, duplicate aliases, nulls, wrong types, fractional counts, and invalid settings are rejected. Periods and counts have a native upper bound of 1,000,000.

`ladx2-run0738-parameters.json` matches the attached RUN0738 script configuration: entry bias 0.4/0.5, exit bias -1.4/-1.0, bearish guide-cross exit enabled, and market SMA/slope-lookback/DMI/ADX/EMA/ATR lengths 40/4/8/18/132/32 with flat-slope threshold 0.05. Select this file explicitly to reproduce that script; the native defaults remain the original preset. The attachment's PriceShock `upward=true` branch computes the same values as native LADX2.

Numeric outputs use `LADX2_`, with `LADX2_SIDE` retaining the EFS subtypes: 0 flat, 1.1 above-guide continuation, 1.2 bearish below-guide pullback. `LADX2_LongSide` normalizes these to 0/1 for backtests. Both subtypes hold one long position. The `MarketTrend`, `MarketTrend_*`, `MT_Slope_*`, and `MT_ADX_*` fields retain their EFS names. `LADX2_StopReason`, `LADX2_LastExitReason`, and `MarketTrend_TIMEFRAME` are text outputs.

## Market trend and signal rules

Only **completed chart candles** enter the hourly/daily aggregate. An aggregate is published when the next chart candle crosses its clock boundary or leaves the configured session. No future price data or current incomplete aggregate enters the confirmed trend. The first partial aggregate is discarded; gaps and maintenance breaks create no synthetic candles. Repeated updates and retained historical revisions recompute from the preceding snapshot.

The default market settings use clock hours, a 200-candle close SMA, five-candle SMA slope, 14/14 DMI/ADX, and a 20-candle EMA and ATR channel. The four direction votes are SMA slope, confirmed swings, ADX-qualified DMI, and the confirmed EMA/ATR channel. Neutral consensus uses the vote balance; ties retain direction, with the EFS slope-based initial fallback.

Settings accept hourly or daily aggregation and UTC or America/New_York. New York follows the EFS's US DST rules and **requires 2007 or later**. Chart intervals must be aligned whole minutes dividing 1440 and the session boundaries; hourly aggregation additionally requires intervals dividing 60 and whole-hour session boundaries. Known plugin interval metadata is validated; callers without metadata use five minutes for validation/lookback sizing. `maximum_lookback()` includes aggregate indicator warmup converted to chart bars; gaps can require additional history.

Confirmed bullish trend selects the long-trend entry/exit bias thresholds. Bearish and not-ready states select short-trend thresholds. Below-guide pullback entries require a confirmed bearish trend. Continuation entries require both price MA and close at/above the guide within the inclusive breakout window. Both entry paths require cooldown completion, no chop, recovering price MA, and non-deteriorating bias strictly above the selected entry threshold.

Exit priority is `ADX_BIAS`, `SHOCK_DROP`, `ARMED_DROP`, `SHOCK_RANGE`, then `SHORT_TREND_GUIDE_CROSS`. The final exit is optional and bearish-only: the previous high must be at/above the previous lower guide band and the current high strictly below the current lower band. Entry-bar upward shocks do not arm a new trade; exits reset arming and cooldown immediately. As in the EFS, `GuideWidthATR` and `LastExitReason` remain compatibility diagnostics (zero and blank).

Chart drawing, the EFS TradeEngine, position sizing, and commissions are outside this study DLL. Native backtest execution settings remain separate.

After `SHORT_TREND_GUIDE_CROSS`, re-entry additionally requires the current price MA strictly above the current lower guide band. Equality remains blocked, even after cooldown. This gate persists across bars and regime changes until an entry succeeds; other exit reasons do not activate it. Current-bar revisions restore the gate from the preceding snapshot. The EFS study definition matches the supplied update; existing parameter presets remain unchanged.

## Build and run

Build `FIGSignalCpp.slnx` or `FIGStudyCollection.LADX2.vcxproj` in x64 Debug/Release. Use the DLL matching the host configuration:

Local verification builds disabled debug-symbol generation to work around this machine's Visual Studio `C1902` program database manager mismatch. The project retains the same standard build settings as LADX.

```powershell
.\x64\Release\FIGSignalCpp.exe --test-collection --collection-dll .\x64\Release\FIGStudyCollection.LADX2.dll
.\x64\Release\FIGSignalCpp.exe --calculate-studies --dataset 3 --collection-dll .\x64\Release\FIGStudyCollection.LADX2.dll --collection LADX2 --parameters .\ladx2-parameters.json --interval-minutes 5 --settings .\signal-settings.json --output .\ladx2.csv
```

For batch backtests, select `--collection LADX2 --signal LADX2_LongSide` and the new DLL. For two-minute calculations use `--interval-minutes 2 --parameters .\ladx2-2min-parameters.json`.

## Verification

```powershell
node tests/verify_ladx2.mjs Release
node tests/verify_ladx2.mjs Debug
```

An optional third argument supplies a different EFS reference file directly. Verification also includes the RUN0738 preset.

The offline probe compares all published numeric/text study values against the unmodified EFS definitions, including both presets, fast hourly/daily settings, UTC, session gaps, partial initial aggregates, and both DST transitions. Built-in chart SMA/ATR are mocked consistently with the existing native core; this does not establish parity with eSignal's platform ATR seeding. DLL self-tests cover JSON rejection, entry/exit boundaries, regime changes, cooldown, arming, continuation windows, repeated updates, and past-bar replay.
