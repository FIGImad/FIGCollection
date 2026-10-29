# L2 EFS comparison

## Independent base signal tests

```powershell
dotnet build tests/FIGL2Efs.Regression -c Release -m:1
node tests/FIGL2Efs.Regression/base-signal-reference.mjs tests/FIGL2Efs.Regression/obj/base-signal-fixtures.json
dotnet tests/FIGL2Efs.Regression/bin/Release/net10.0/FIGL2Efs.Regression.dll --base-signal tests/FIGL2Efs.Regression/obj/base-signal-fixtures.json
dotnet tests/FIGL2Efs.Regression/bin/Release/net10.0/FIGL2Efs.Regression.dll --checkpoint
```

The JavaScript reference transcribes the user's new Study_BaseSignal, with a
completed-bar state carry helper standing in for the EFS host. Ten manually
specified boundary cases and 5,000 deterministic generated bars exercise the
port. The .NET test additionally checks same-bar rollback, three-bar corrections,
JSON checkpoint continuation, parameter reuse versus cloning, assignment history,
output selection, and output names independent of the existing trading strategy.

The independent-study addition passed all 5,010 reference bars and the collection
checkpoint test. A full replay of 761,922 saved OptimizationV3 price bars produced
the same 3,566 completed trades as the then-current `run-0001-trades.csv`, including
raw entry/exit timestamps, prices and four-decimal side codes. These counts refer
to the current backtest file, not the earlier EFS comparison recorded below.

The reference is the existing `FIGSignalCollection/FIG.StudyCollections.L2/L2_V3_Oct14.efs` file. The tests read its actual strategy class; they do not copy its entry/exit rules into another implementation.

From FIGCollection:

```powershell
dotnet build tests/FIGL2Efs.Regression -c Release -m:1
node tests/FIGL2Efs.Regression/verify.mjs
dotnet tests/FIGL2Efs.Regression/bin/Release/net10.0/FIGL2Efs.Regression.dll --checkpoint
```

`verify.mjs` probes ResetSwitches, TestStart and TestStop with every entry trigger found in the EFS source, positive/zero/negative prices, and three simplified indicator profiles. It checks entry/exit decisions plus selected trigger state. These probes do not prove every indicator formula or every market scenario is equivalent. Differences are written to `obj/parity/differences.json`.

To replay saved aggregated prices using the full .NET collection:

```powershell
dotnet tests/FIGL2Efs.Regression/bin/Release/net10.0/FIGL2Efs.Regression.dll --replay <prices.csv> <trades.csv> [exclusive-end-unix-time]
python tests/FIGL2Efs.Regression/compare_csv.py <trades.csv> <esignal.csv> [prices.csv]
```

Replay uses the reported run's settings: periods 276–5796 in increments of 276, standard deviations 1 and 2, and five-minute bars. It preserves the entry signal until the direction changes. It emits completed trades without sizing, commissions or forced closing. The comparison converts EFS Eastern timestamps to UTC and adds five minutes to the .NET bar-start timestamps. It pairs exact times/prices first, then unique price pairs within three days of entry and exit (including weekend session gaps). It compares signal codes rounded to four decimals and reports unmatched trades separately. It independently checks the chronological sequence of all entry/exit prices and side codes. Timing-only matches are candidates for review, not proof of identical execution.

The checkpoint mode verifies JSON roundtrip/continuation/state and last-three-bar correction replay after the rule change. Historical hashes from the old strategy are not a reference for EFS equivalence.

## Verified changes (October 22, 2026)

- EFS ResetSwitches unconditionally assigns `basicTrigger_DL1 = 7.34` before the guarded 7.34 branch. The previous .NET port assigned it only inside that branch, which selected different trigger families. The .NET port now mirrors the unconditional assignment, without setting firsttime there.
- EFS TestStart accepts trigger 1.9442 without requiring `cHigh < C1.SEM`. The .NET port had that additional restriction; it has been removed.

The unchanged EFS source and corrected .NET strategy agree in 62,532 simplified method probes. The corrected full .NET collection was replayed over all 761,922 saved price bars from the user's OptimizationV3 run. In the supplied EFS export period (January 2, 2017 through October 14, 2026), both contain 2,725 completed trades. Their chronological entry/exit price and four-decimal entry side sequences match completely. 2,707 pairs also have matching aligned timestamps; 18 have timing differences. In all 18, the EFS event time equals the next available source price-bar timestamp, including ten-minute, hourly, holiday and weekend gaps. Passing prices.csv as the final comparison argument repeats this check.

These results validate this supplied price history and export, not every possible study state or intrabar update. No EFS source, live service, deployed plugin or existing user output was modified. A newly built assembly has a different checkpoint build identity, so old checkpoints are not compatible with it.
