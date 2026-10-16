# SADX2 collection

SADX2 is the full short-side collection using the RUN0738 defaults in sadx2-parameters.json and the reference in efs/SADX2.efs. It includes shock ATR, guide slope/chop and confirmed hourly/daily market trend. SADX2_SIDE is 0, -1.1 (continuation), or -1.2 (bullish pullback); SADX2_ShortSide is 0/-1.

Entry and exit thresholds depend on market trend. Entry requires non-increasing bias and price MA, cooldown completion, and no chop. Exit priority is ADX_BIAS, SHOCK_RISE, ARMED_RISE, SHOCK_RANGE, then LONG_TREND_GUIDE_CROSS across the upper guide band in a bullish regime. Guide-exit recovery requires MA below the upper band.

Build FIGStudyCollection.SADX2.vcxproj in x64 Release/Debug. Verify with node tests/verify_sadx2.mjs Release (or Debug). Tests compare published values to the EFS and cover rollback/replay. The reference harness mocks platform SMA/ATR and does not establish proprietary eSignal ATR-seeding parity.

For the reduced refinement implementation without shock, slope or market-trend calculation, use the separate SADX3 project. SADX2 retains its full calculation path.
