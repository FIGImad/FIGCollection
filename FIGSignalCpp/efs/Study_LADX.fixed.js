// LADX long-position signal, matching the corrected C++ collection.
// Dependencies: Study_ADX, Study_PriceShock, Study_SMASlope and global cClose.
// Contract: StudyCollection copies studyArr[1] into studyArr[0] before EVERY Calc,
// including current-bar updates. Counters are bar counts, not minutes or ticks.
// This class emits signals only; fills, sizing, commissions and NAV belong to the backtester.
class Study_LADX {
    // Configuration and initial state. Null/missing parameters use the defaults below.
    constructor(label, params) {
        params = params || {};

        // Output namespace and four snapshots maintained by the parent collection.
        this.tag = label;
        this.adxTag = 'Active';
        this.studyArr = [{side: 0}, {}, {}, {}];

        // Directional bias is +DI minus -DI, not ADX strength. Entry uses >, exit uses <.
        this.startBias = params.StartBias == null ? 1.0 : params.StartBias;
        this.stopBias = params.StopBias == null ? -0.75 : params.StopBias;
        // Cooldown 2: exit age 0; next bar age 1 is blocked; age 2 is eligible.
        // Continuation 5: breakout age 0 through 5 inclusive; 0 disables this entry path.
        this.reEntryCooldownBars = params.ReEntryCooldownBars == null ? 1 : Math.floor(params.ReEntryCooldownBars);
        this.continuationEntryBars = params.ContinuationEntryBars == null ? 5 : Math.floor(params.ContinuationEntryBars);
        // Accepted for compatibility; not used by the corrected entry rule.
        this.trendReEntryShockMax = params.TrendReEntryShockMax == null ? 0.65 : params.TrendReEntryShockMax;
        // Shock thresholds are multiples of ATR. A trade starts unarmed.
        this.shockDropATR = params.ShockDropATR == null ? 1.15 : params.ShockDropATR;
        this.shockArmedDropATR = params.ShockArmedDropATR == null ? 0.85 : params.ShockArmedDropATR;
        this.shockArmUpATR = params.ShockArmUpATR == null ? 0.75 : params.ShockArmUpATR;
        this.shockRangeATR = params.ShockRangeATR == null ? 1.8 : params.ShockRangeATR;

        // Block entry when BOTH the band is narrow AND its slope is nearly flat.
        this.chopMinGuideWidthATR = params.ChopMinGuideWidthATR == null ? 0.80 : params.ChopMinGuideWidthATR;
        this.chopMaxGuideSlopeATR = params.ChopMaxGuideSlopeATR == null ? 0.06 : params.ChopMaxGuideSlopeATR;

        // Current-bar inputs read from the upstream studies by ResetSwitches.
        this.adxVal = undefined;
        this.adxBias = undefined;
        this.adxSlope = undefined;
        this.priceMA = undefined;
        this.guideMA = undefined;
        this.guideLB = undefined;
        this.guideUB = undefined;
        this.shockScore = 0;
        this.shockDownATR = 0;
        this.shockUpATR = 0;
        this.shockRangeATRValue = 0;
        this.shockPullbackATR = 0;
        this.shockATR = undefined;
        // Compatibility diagnostic: stays zero; the actual chop test uses a local width.
        this.guideWidthATR = 0;
        this.isChop = false;

        // 1,000,000 means no recent exit/breakout; counters saturate at this value.
        this.maxUpATR = 0;
        this.barsSinceExit = 1000000;
        this.barsSinceGuideBreakout = 1000000;
        this.wasBeyondGuide = false;
        // Kept blank to match current C++ reporting; StopReason carries the exit label.
        this.lastExitReason = "";
        this.stopReason = "";
    }

    // 0 = flat, 1.1 = above-guide continuation, 1.2 = below-guide pullback.
    // Both positive subtypes represent one long position; this class does not emit shorts.
    get Side() {
        return this.studyArr[0].side == null ? 0 : this.studyArr[0].side;
    }
    set Side(val) {
        this.studyArr[0].side = val == null ? 0 : val;
    }
    // Previous completed-bar side, retained for callers; exits are reset directly in Calc.
    get pSide() {
        return this.studyArr[1].side == null ? 0 : this.studyArr[1].side;
    }

    // Restore previous state, refresh inputs, then make at most one entry OR exit decision.
    Calc(studies) {
        this.ResetSwitches(studies);

        // Missing study rows must not reuse inputs left over from an earlier call.
        if (studies[0] == null || studies[1] == null || this.adxVal == null || this.adxBias == null || this.priceMA == null
            || this.guideMA == null || this.guideLB == null || this.guideUB == null) {
            this.writeStudies(studies);
            return;
        }

        if (this.Side === 0) {
            this.Side = this.TestStart(studies);
            // Ignore the entry bar's upward shock: arming begins on held bars.
            if (this.Side !== 0) this.maxUpATR = 0;
        }
        else {
            if (this.TestStop(studies)) {
                this.Side = 0;
                // Reset immediately on the exit bar, not by detecting pSide next time.
                this.barsSinceExit = 0;
                this.maxUpATR = 0;
            }
        }
        this.writeStudies(studies);
    }

    // Save all mutable trading state first, then publish this bar's diagnostics.
    writeStudies(studies) {
        const state = this.studyArr[0];
        state.maxUpATR = this.maxUpATR;
        state.barsSinceExit = this.barsSinceExit;
        state.barsSinceGuideBreakout = this.barsSinceGuideBreakout;
        state.beyondGuide = this.wasBeyondGuide;
        state.stopReason = this.stopReason;
        // Tells the next bar to increment the cooldown once from this snapshot.
        state.hasPrevious = true;
        const cStudy = studies[0];
        if (cStudy == null) return;

        cStudy[this.Tag('Side')] = this.Side;
        cStudy[this.Tag('PriceMA')] = this.priceMA;
        cStudy[this.Tag('GuideMA')] = this.guideMA;
        cStudy[this.Tag('GuideUB')] = this.guideUB;
        cStudy[this.Tag('GuideLB')] = this.guideLB;
        cStudy[this.Tag('ADX')] = this.adxVal;
        cStudy[this.Tag('ADXBias')] = this.adxBias;
        cStudy[this.Tag('ADXSlope')] = this.adxSlope;
        cStudy[this.Tag('ADXTrigger')] = this.startBias;
        cStudy[this.Tag('StopBias')] = this.stopBias;
        // Arming/cooldown values are post-decision: both are zero on an exit bar.
        cStudy[this.Tag('MaxUpATR')] = this.maxUpATR;
        cStudy[this.Tag('BarsSinceExit')] = this.barsSinceExit;
        cStudy[this.Tag('LastExitReason')] = this.lastExitReason;
        cStudy[this.Tag('ShockScore')] = this.shockScore;
        cStudy[this.Tag('ShockDownATR')] = this.shockDownATR;
        cStudy[this.Tag('ShockUpATR')] = this.shockUpATR;
        cStudy[this.Tag('ShockPullbackATR')] = this.shockPullbackATR;
        cStudy[this.Tag('ShockRangeATR')] = this.shockRangeATRValue;
        cStudy[this.Tag('GuideWidthATR')] = this.guideWidthATR;
        cStudy[this.Tag('Chop')] = this.isChop ? 1 : 0;
        cStudy[this.Tag('StopReason')] = this.stopReason;
    }

    // Recompute from the preceding snapshot. Do not accumulate intra-bar updates.
    ResetSwitches(studies) {
        const cStudy = studies[0];
        const pStudy = studies[1];


        // StudyCollection restores studyArr[0] from the preceding bar before every Calc.
        // Restore all trading state too, so repeated current-bar updates recompute it.
        const state = this.studyArr[0];
        this.maxUpATR = state.maxUpATR == null ? 0 : state.maxUpATR;
        this.barsSinceExit = state.barsSinceExit == null ? 1000000 : state.barsSinceExit;
        this.barsSinceGuideBreakout = state.barsSinceGuideBreakout == null ? 1000000 : state.barsSinceGuideBreakout;
        this.wasBeyondGuide = state.beyondGuide === true;
        this.stopReason = state.stopReason == null ? "" : state.stopReason;
        if (state.hasPrevious && this.barsSinceExit < 1000000) this.barsSinceExit++;

        // Restore state even during warmup; Calc suppresses decisions without both rows.
        if (cStudy == null || pStudy == null) return;


        // Read directional indicators and price/guide moving averages.
        this.adxVal = cStudy[Study_ADX.Label(this.adxTag, 'ADX')];
        const pdiVal = cStudy[Study_ADX.Label(this.adxTag, 'PDI')];
        const ndiVal = cStudy[Study_ADX.Label(this.adxTag, 'NDI')];
        this.adxBias = pdiVal == null || ndiVal == null ? undefined : pdiVal - ndiVal;

        this.priceMA = cStudy.MAPrice;
        this.guideMA = cStudy.MIDGuide;
        this.guideLB = cStudy.LBGuide;
        this.guideUB = cStudy.UBGuide;
        this.adxSlope = cStudy[`${this.adxTag}_ADX_SLOPE`];

        // Missing shock components default to zero, matching the supplied EFS behavior.
        this.shockScore = cStudy[Study_PriceShock.Label('Shock', 'Score')] ?? 0;
        this.shockDownATR = cStudy[Study_PriceShock.Label('Shock', 'DownATR')] ?? 0;
        this.shockUpATR = cStudy[Study_PriceShock.Label('Shock', 'UpATR')] ?? 0;
        this.shockRangeATRValue = cStudy[Study_PriceShock.Label('Shock', 'RangeATR')] ?? 0;
        this.shockPullbackATR = cStudy[Study_PriceShock.Label('Shock', 'PullbackATR')] ?? 0;
        this.shockATR = cStudy[Study_PriceShock.Label('Shock', 'ATR')];

        // Normalize guide width and absolute slope by ATR for the chop filter.
        // Missing inputs use zero ratios and may classify as chop; preserved for parity.
        const guideSlopePerBar = cStudy[Study_SMASlope.Label('SLGuide', 'MASlopeValue')];
        const guideWidth = this.guideUB != null && this.guideLB != null ? Math.max(0, this.guideUB - this.guideLB) : 0;
        const hasChopInputs = this.shockATR != null && this.shockATR > 0 && guideWidth > 0 && guideSlopePerBar != null;

        // Chop means the guide band is both narrow and nearly flat
        const guideWidthATR = hasChopInputs ? guideWidth / this.shockATR : 0;
        const guideSlopeATR = hasChopInputs ? Math.abs(guideSlopePerBar) / this.shockATR : 0;
        this.isChop =  this.chopMaxGuideSlopeATR > 0 && guideSlopeATR <= this.chopMaxGuideSlopeATR
                    && this.chopMinGuideWidthATR > 0 && guideWidthATR <= this.chopMinGuideWidthATR;

        // Track the largest single-bar upward shock within this trade (not cumulative gain).
        // A held bar may both reach the arming threshold and trigger an armed exit.
        if (this.Side !== 0) {
            this.stopReason = "";
            if (this.shockUpATR > this.maxUpATR) {
                this.maxUpATR = this.shockUpATR;
            }
        }

        // Track the guide breakout on every bar, including while in a trade.
        // BOTH the MA and close must be at/above the guide. Any break resets the window.
        const beyond = this.priceMA >= this.guideMA && cClose >= this.guideMA;
        if (!beyond) this.barsSinceGuideBreakout = 1000000;
        else if (!this.wasBeyondGuide) this.barsSinceGuideBreakout = 0;
        else if (this.barsSinceGuideBreakout < 1000000) this.barsSinceGuideBreakout++;
        this.wasBeyondGuide = beyond;
    }

    // Flat-position entry rules. Cooldown, chop, bias and recovery gates apply to both paths.
    TestStart(studies) {
        const cStudy = studies[0];
        const pStudy = studies[1];
        const isReEntryCoolDownStillActive =
            this.reEntryCooldownBars > 0 && this.barsSinceExit < this.reEntryCooldownBars;

        if (isReEntryCoolDownStillActive || cStudy == null || pStudy == null) {
            return 0.0;
        }

        // Check if BIAS value is not deteriorating, get previous BIAS Value first
        const pdiVal = pStudy[Study_ADX.Label(this.adxTag, 'PDI')];
        const ndiVal = pStudy[Study_ADX.Label(this.adxTag, 'NDI')];
        const pAdxBias = pdiVal == null || ndiVal == null ? undefined : pdiVal - ndiVal;
        const biasNotDeteriorating = pAdxBias == null || this.adxBias >= pAdxBias;

        // No shock/slope gate on continuation: matches corrected C++/ADXBase.
        const continuationAboveGuide = this.priceMA >= this.guideMA && cClose >= this.guideMA;
        const continuationWindow = this.continuationEntryBars > 0
            && this.barsSinceGuideBreakout <= this.continuationEntryBars;
        const pullbackEntry = this.priceMA < this.guideMA;
        const trendReEntry = continuationAboveGuide && continuationWindow;

        // start only when
        // - the band is not simultaneously narrow AND flat (!this.isChop)
        // - and the price is recovering (cStudy.MAPrice >= pStudy.MAPrice)
        // - and the ADX bias is above the start threshold
        // - and either a below-guide pullback or an in-window above-guide continuation is present
        const shouldStart = !this.isChop
            && biasNotDeteriorating
            && this.adxBias > this.startBias
            && cStudy.MAPrice >= pStudy.MAPrice
            && (
                pullbackEntry
                || trendReEntry
            )
        if (shouldStart) {
            return trendReEntry ? 1.1 : pullbackEntry ? 1.2 : 0.0;
        }
        return 0.0;
    }

    // Exit priority: bias, unconditional shock, armed shock, then adverse wide-range bar.
    // First match wins. Threshold equality triggers shock exits, but not the bias exit.
    TestStop(studies) {
        // maxUpATR: maximum observed upward move from the previous bar’s close while the trade is open.
        //             this value stays "armed" until the trade is closed, and is used to determine if a
        //             shock drop should trigger a stop. it is compared to the shockArmUpATR threshold.
        const shockArmed = this.maxUpATR >= this.shockArmUpATR;

        // rapidDrop: the shock score is above the shockDropATR threshold, which indicates a rapid drop in price.
        //                shockScore measures the current downward move or pullback, relative to ATR.
        //                It takes the larger of these two values:
        //                     DownATR     = Math.max(0, pClose - cClose) / ATR;
        //                     PullbackATR = Math.max(0, cHigh - cClose) / ATR;
        //                    shockScore = Math.max(DownATR, PullbackATR);
        //              ATR is the average true range, which is a measure of volatility. A higher ATR indicates
        //              more volatility, while a lower ATR indicates less volatility.
        const rapidDrop = this.shockScore >= this.shockDropATR;

        // armedDrop: the shock score is above the shockArmedDropATR threshold, which indicates a rapid drop
        // in price after the trade has been armed.
        const armedDrop = shockArmed && this.shockScore >= this.shockArmedDropATR;

        // shockRangeDrop: the shock range ATR value is above the shockRangeATR threshold, which indicates
        // a rapid drop in price within a range.
        const shockRangeDrop = this.shockRangeATRValue >= this.shockRangeATR && this.shockDownATR > 0;

        if (this.adxBias < this.stopBias) {
            this.stopReason = "ADX_BIAS";
            return true;
        }
        if (rapidDrop) {
            this.stopReason = "SHOCK_DROP";
            return true;
        }
        if (armedDrop) {
            this.stopReason = "ARMED_DROP";
            return true;
        }
        if (shockRangeDrop) {
            this.stopReason = "SHOCK_RANGE";
            return true;
        }
        return false;
    }

    // Preserve EFS column names, e.g. STrend_LADX_Side; C++ uses LADX_SIDE.
    Tag(varName) {
        return Study_LADX.Label(this.tag, varName);
    }
    static Label(tag, varName) {
        return `${tag}_LADX_${varName}`;
    }
}
