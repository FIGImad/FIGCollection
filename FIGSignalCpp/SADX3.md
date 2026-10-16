# SADX3 reduced refinement collection

`FIGStudyCollection.SADX3.dll` registers `SADX3`. The reference in `efs/SADX3.efs` is the supplied reduced script, preserved verbatim.

Only the OHLC4 price SMA, OHLC4 guide bands and active Wilder ADX/DMI run per bar. Shock/ATR, guide slope, and market-trend aggregation are not constructed or calculated. Their source files remain available for later refinements but are excluded from this project. Warmup uses only the active periods: max(2 * adxLength, priceMaLength, guideMaLength), currently 48 bars.

Defaults match OPT_PARAMETERS: ADX 24, price MA 4, guide MA 16, deviation 0.4, entry bias 0.5, exit bias -2.6, cooldown 2 and continuation window 5. Entry and exit always use startBiasShortTrend and stopBiasShortTrend. LongTrend thresholds, shock settings, chop settings and marketTrendSettings remain accepted for configuration compatibility; they do not affect trading. The optimizer preset no longer searches inactive trend parameters.

Both above-guide pullbacks and below-guide continuations produce SADX3_SIDE = -1.1; flat is 0. SADX3_ShortSide is the normalized -1/0 backtest signal. Entry requires non-increasing price MA and directional bias, bias strictly below the entry threshold, and completed cooldown. Continuations require MA and close at/below the middle guide. The supplied script saves `barsSinceBreakout` but restores `barsSinceBreakout`: its counter therefore resets to 1,000,000 on each bar. For normal continuationEntryBars values, only the first breakout bar qualifies. SADX3 preserves this exact behavior; the configured window is not a multi-bar window until that reference mismatch is deliberately corrected. Pullbacks require MA above the middle guide, with no market-trend requirement. Chop does not gate entry.

Exit priority is ADX_BIAS, then TREND_CROSS. The latter name is retained for compatibility: it now means previous low at/below the previous LOWER guide band and current low strictly above the current LOWER band, regardless of market trend. The existing exitOnCrossTrend switch enables it. After that exit, entry requires price MA strictly below the UPPER guide band. No shock exits are evaluated.

Inactive strategy diagnostics remain zero/null for EFS parity. Chop is a compatibility flag (1 with omitted chop settings) and is not calculated from slope/ATR or used for decisions. Upstream Shock_*, SLGuide_* and MarketTrend outputs are absent. BarsSinceBreakout is published. Repeated-bar and historical correction behavior retains rollback/replay support.

Build the x64 Release project, then verify:

```powershell
node tests/verify_sadx3.mjs Release
```

The verifier compares native outputs with the supplied EFS, including entry/exit signals and diagnostics. Native self-tests cover cooldown, lower-band exits, upper-band recovery, parameter validation, reduced warmup, absent disabled-study outputs and historical revisions. Platform OHLC4/indicator mocks do not establish proprietary eSignal seeding parity.

For refinement use `sadx3-parameters.json`, collection `SADX3`, and signal `SADX3_ShortSide`. The existing optimizer preset remains available at `optimizer-search-sadx3-5m-1000.json`; it searches only active parameters, including bias ranges around the new defaults.

Release build and parity verification use a temporary debug-symbol override to work around the local Visual Studio C1902 database-manager mismatch. Project build settings remain unchanged.

