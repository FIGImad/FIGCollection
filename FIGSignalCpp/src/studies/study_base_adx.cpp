#include "fig/studies/study_base_adx.hpp"
#include <algorithm>
#include <cmath>
namespace fig::studies
{
StudyLBaseADX::StudyLBaseADX(AdxBaseOptions o, std::size_t depth) : StudyLBaseADX(o, false, depth)
{
}
StudyLBaseADX::StudyLBaseADX(AdxBaseOptions o, bool short_side, std::size_t depth)
    : options_(o), short_(short_side), states_(depth)
{
    options_.validate();
}

StudyBar StudyLBaseADX::process(const AdxBaseInput& i)
{
    int raw_time = -1;
    if (i.price.raw_time == 1790699100)
    {
        raw_time = 1790699100;
    }
    auto s = states_.begin(i.price.raw_time);
    const auto& o = options_;
    const auto* p = s.previous ? &*s.previous : nullptr;
    if (p && s.bars_since_exit < 1000000)
        ++s.bars_since_exit;
    const auto slope = i.guide_slope.slope.value_or(0);
    const auto width = std::max<Decimal>(0, i.guide_upper - i.guide_lower);
    const bool chop_inputs = i.shock.atr && *i.shock.atr > 0 && width > 0 && i.guide_slope.slope;
    const auto width_atr = chop_inputs ? width / *i.shock.atr : 0;
    const auto slope_atr = chop_inputs ? std::abs(slope) / *i.shock.atr : 0;
    const bool chop = chop_inputs && o.chop_min_width_atr > 0 && width_atr <= o.chop_min_width_atr &&
                      slope_atr <= o.chop_max_slope_atr;
    const bool beyond = short_ ? i.price_ma <= i.guide_middle && i.price.close <= i.guide_middle
                               : i.price_ma >= i.guide_middle && i.price.close >= i.guide_middle;
    const bool was_beyond = p && (short_ ? p->price_ma <= p->guide_middle && p->price.close <= p->guide_middle
                                         : p->price_ma >= p->guide_middle && p->price.close >= p->guide_middle);
    if (!beyond)
        s.bars_since_guide_break = 1000000;
    else if (!was_beyond)
        s.bars_since_guide_break = 0;
    else if (s.bars_since_guide_break < 1000000)
        ++s.bars_since_guide_break;

    if (i.active.adx && i.active.bias)
    {
        const auto bias = *i.active.bias;
        if (s.side == 0)
        {
            s.start_block_reason.clear();
            s.continuation_ready = false;
            if (o.cooldown_bars > 0 && s.bars_since_exit < o.cooldown_bars)
                s.start_block_reason = "COOLDOWN";
            else
            {
                const bool recovering = p && (short_ ? i.price_ma <= p->price_ma : i.price_ma >= p->price_ma);
                const bool bias_ok =
                    !p || !p->active.bias || (short_ ? bias <= *p->active.bias : bias >= *p->active.bias);
                const bool window = o.continuation_bars > 0 && s.bars_since_guide_break <= o.continuation_bars;
                const bool slope_ok = (short_ ? slope < 0 : slope > 0) &&
                                      (o.continuation_min_slope_atr <= 0 || slope_atr >= o.continuation_min_slope_atr);
                const auto shock = short_ ? i.shock.up_atr : i.shock.score;
                const bool shock_ok = o.continuation_max_shock_atr <= 0 || shock <= o.continuation_max_shock_atr;
                const bool pullback = !chop && (short_ ? i.price_ma > i.guide_middle : i.price_ma < i.guide_middle) &&
                                      recovering && bias_ok;
                const bool protective = s.last_exit_reason == "SHOCK_RANGE" ||
                                        s.last_exit_reason == (short_ ? "SHOCK_UP" : "SHOCK_DROP") ||
                                        s.last_exit_reason == (short_ ? "ARMED_UP" : "ARMED_DROP");
                const bool reentry = !chop && o.trend_reentry_bars > 0 && s.bars_since_exit <= o.trend_reentry_bars &&
                                     protective && beyond && recovering && shock <= o.trend_reentry_shock_max;
                const bool continuation = !chop && window && beyond && recovering && bias_ok;
                s.continuation_ready = continuation;
                // Preserve the pasted EFS expressions, including the asymmetric short thresholds.
                // Confirmation, continuation slope/shock and long trend-reentry gating are commented out there.
                const bool start = short_ ? bias < o.start_bias && i.confirm_slope.slope && *i.confirm_slope.slope < -1
                                          : bias > o.start_bias && (pullback || continuation);
                if (start)
                {
                    s.side = short_ ? -1 : 1;
                    s.bars_in_trade = s.weak_bars = 0;
                    s.entry_extreme = short_ ? i.price.high : i.price.low;
                    s.entry_close = i.price.close;
                    s.entry_ma = i.price_ma;
                    s.bias_extreme = bias;
                    s.max_impulse = 0;
                    s.entry_mode = reentry ? "TREND_REENTRY" : continuation ? "CONTINUATION" : "PULLBACK";
                    s.stop_reason.clear();
                }
                else if (!short_)
                {
                    s.start_block_reason = bias <= o.start_bias  ? "BIAS"
                                           : chop                ? "CHOP"
                                           : beyond && !window   ? "CONT_WINDOW"
                                           : beyond && !slope_ok ? "CONT_SLOPE"
                                           : beyond && !shock_ok ? "CONT_SHOCK"
                                                                 : "NO_SETUP";
                }
            }
        }
        else
        {
            ++s.bars_in_trade;
            if (p)
            {
                const bool weak =
                    s.previous_previous_close &&
                    (short_ ? p->price_ma > p->guide_middle && p->price.close > *s.previous_previous_close
                            : p->price_ma < p->guide_middle && p->price.close < *s.previous_previous_close);
                const bool recovered =
                    (short_ ? p->price_ma <= p->guide_middle : p->price_ma >= p->guide_middle) ||
                    (s.previous_previous_close && (short_ ? p->price.close <= *s.previous_previous_close
                                                          : p->price.close >= *s.previous_previous_close));
                if (weak)
                    ++s.weak_bars;
                else if (recovered)
                    s.weak_bars = 0;
            }
            if (!s.bias_extreme || (short_ ? bias < *s.bias_extreme : bias > *s.bias_extreme))
                s.bias_extreme = bias;
            s.max_impulse = std::max(s.max_impulse, short_ ? i.shock.down_atr : i.shock.up_atr);
            const auto shock = short_ ? i.shock.up_atr : i.shock.score;
            const auto adverse = short_ ? i.shock.up_atr : i.shock.down_atr;
            std::string reason;
            if (short_ && i.confirm_slope.slope && *i.confirm_slope.slope > 1)
                reason = "CONF_SLOPE";
            else if (short_ ? bias > o.stop_bias : bias < o.stop_bias)
                reason = "ADX_BIAS";
            else if (shock >= o.shock_drop_atr)
                reason = short_ ? "SHOCK_UP" : "SHOCK_DROP";
            else if (s.max_impulse >= o.shock_arm_up_atr && shock >= o.shock_armed_drop_atr)
                reason = short_ ? "ARMED_UP" : "ARMED_DROP";
            else if (i.shock.range_atr >= o.shock_range_atr && adverse > 0)
                reason = "SHOCK_RANGE";
            // ENTRY_LOW/HIGH, PULLBACK_FAIL and GUIDE_LB/UB exits are disabled in the supplied EFS.
            if (!reason.empty())
            {
                s.stop_reason = s.last_exit_reason = reason;
                s.bars_since_exit = 0;
                s.bars_in_trade = s.weak_bars = 0;
                s.entry_extreme = s.entry_close = s.entry_ma = s.bias_extreme = std::nullopt;
                s.max_impulse = 0;
                s.side = 0;
            }
        }
    }

    StudyBar out{i.price, {}};
    const std::string tag = short_ ? "STrend_SBaseADX_" : "STrend_LBaseADX_";
    auto put = [&](std::string_view name, std::optional<Decimal> value)
    { out.studies[tag + std::string(name)] = value; };
    put("Side", s.side);
    put("PriceMA", i.price_ma);
    put("GuideMA", i.guide_middle);
    put("GuideUB", i.guide_upper);
    put("GuideLB", i.guide_lower);
    put("ADX", i.active.adx);
    put("ADXBias", i.active.bias);
    put("ADXConf", i.confirm.adx);
    put("ADXConfBias", i.confirm.bias);
    put("ADXSlope", i.roc.slope);
    put("ADXTrigger", short_ ? -o.start_bias : o.start_bias);
    put("StopBias", short_ ? -o.stop_bias : o.stop_bias);
    put("BarsInTrade", static_cast<Decimal>(s.bars_in_trade));
    put("WeakBars", static_cast<Decimal>(s.weak_bars));
    put(short_ ? "EntryHigh" : "EntryLow", s.entry_extreme);
    put(short_ ? "MinBias" : "MaxBias", s.bias_extreme);
    put(short_ ? "MaxDownATR" : "MaxUpATR", s.max_impulse);
    put("BarsSinceExit", static_cast<Decimal>(s.bars_since_exit));
    put("ShockScore", short_ ? i.shock.up_atr : i.shock.score);
    put("ShockDownATR", i.shock.down_atr);
    put("ShockUpATR", i.shock.up_atr);
    put("ShockPullbackATR", i.shock.pullback_atr);
    put("ShockRangeATR", i.shock.range_atr);
    put("GuideSlopeValue", slope);
    put("GuideWidthATR", width_atr);
    put("GuideSlopeATR", slope_atr);
    put("Chop", chop ? 1 : 0);
    put(short_ ? "BarsSinceGuideBreakdown" : "BarsSinceGuideBreakout", static_cast<Decimal>(s.bars_since_guide_break));
    put("ContinuationReady", s.continuation_ready ? 1 : 0);
    out.text_studies[tag + "EntryMode"] = s.entry_mode;
    out.text_studies[tag + "LastExitReason"] = s.last_exit_reason;
    out.text_studies[tag + "ChopReason"] = chop ? "SQUEEZE_FLAT" : "";
    out.text_studies[tag + "StartBlockReason"] = s.start_block_reason;
    out.text_studies[tag + "StopReason"] = s.stop_reason;
    s.previous_previous_close = p ? std::optional<Decimal>(p->price.close) : std::nullopt;
    s.previous = i;
    states_.commit(std::move(s));
    return out;
}
} // namespace fig::studies
