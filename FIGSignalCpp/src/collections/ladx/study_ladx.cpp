#include "fig/collections/ladx/study_ladx.hpp"
#include <algorithm>
#include <cmath>

namespace fig::collections::ladx
{
StudyLADX::StudyLADX(Options options, std::size_t depth) : options_(options), states_(depth)
{
    options_.validate();
}
StudyBar StudyLADX::process(const LADXInput& i)
{
    auto s = states_.begin(i.price.raw_time);
    const auto& o = options_;
    // Match ADXBase: increment from the preceding bar snapshot, including while in a trade.
    if (s.previous && s.bars_since_exit < 1000000)
        ++s.bars_since_exit;
    const auto bias =
        i.active.pdi && i.active.ndi ? std::optional<Decimal>(*i.active.pdi - *i.active.ndi) : std::nullopt;
    const auto width = std::max<Decimal>(0, i.guide_upper - i.guide_lower);
    const bool has_chop_inputs = i.shock.atr && *i.shock.atr > 0 && width > 0 && i.guide_slope.slope;
    const auto width_atr = has_chop_inputs ? width / *i.shock.atr : 0;
    const auto slope_atr = has_chop_inputs ? std::abs(*i.guide_slope.slope) / *i.shock.atr : 0;
    // EFS does not include has_chop_inputs in the final chop condition.
    const bool chop = o.chop_max_slope_atr > 0 && slope_atr <= o.chop_max_slope_atr && o.chop_min_width_atr > 0 &&
                      width_atr <= o.chop_min_width_atr;

    const bool beyond = i.price_ma >= i.guide_middle && i.price.close >= i.guide_middle;
    const bool was_beyond = s.previous && s.previous->price_ma >= s.previous->guide_middle &&
                            s.previous->price.close >= s.previous->guide_middle;
    if (!beyond)
        s.bars_since_guide_break = 1000000;
    else if (!was_beyond)
        s.bars_since_guide_break = 0;
    else if (s.bars_since_guide_break < 1000000)
        ++s.bars_since_guide_break;

    if (s.side != 0)
    {
        s.stop_reason.clear();
        s.max_up_atr = std::max(s.max_up_atr, i.shock.up_atr);
    }
    if (i.active.adx && bias)
    {
        if (s.side == 0)
        {
            const bool cooldown = o.cooldown_bars > 0 && s.bars_since_exit < o.cooldown_bars;
            const auto previous_bias = s.previous && s.previous->active.pdi && s.previous->active.ndi
                                           ? std::optional<Decimal>(*s.previous->active.pdi - *s.previous->active.ndi)
                                           : std::nullopt;
            const bool recovering = s.previous && i.price_ma >= s.previous->price_ma;
            const bool bias_not_deteriorating = !previous_bias || *bias >= *previous_bias;
            const bool pullback = i.price_ma < i.guide_middle;
            const bool reentry =
                beyond && o.continuation_bars > 0 && s.bars_since_guide_break <= o.continuation_bars;
            if (!cooldown && !chop && bias_not_deteriorating && *bias > o.start_bias && recovering &&
                (pullback || reentry))
                {
                    s.side = reentry ? 1.1L : 1.2L;
                    s.max_up_atr = 0; // Arm only from shocks within this trade, as in ADXBase.
                }
        }
        else
        {
            if (*bias < o.stop_bias)
                s.stop_reason = "ADX_BIAS";
            else if (i.shock.score >= o.shock_drop_atr)
                s.stop_reason = "SHOCK_DROP";
            else if (s.max_up_atr >= o.shock_arm_up_atr && i.shock.score >= o.shock_armed_drop_atr)
                s.stop_reason = "ARMED_DROP";
            else if (i.shock.range_atr >= o.shock_range_atr && i.shock.down_atr > 0)
                s.stop_reason = "SHOCK_RANGE";
            if (!s.stop_reason.empty())
            {
                s.side = 0;
                s.bars_since_exit = 0;
                s.max_up_atr = 0;
            }
        }
    }

    StudyBar out{i.price, {}};
    const std::string tag = "LADX_";
    const auto put = [&](std::string_view name, std::optional<Decimal> value)
    { out.studies[tag + std::string(name)] = value; };
    put("SIDE", s.side);
    // Host backtests expect 0/1. Keep the EFS entry subtype in Side.
    put("LongSide", s.side != 0 ? 1 : 0);
    put("PriceMA", i.price_ma);
    put("GuideMA", i.guide_middle);
    put("GuideUB", i.guide_upper);
    put("GuideLB", i.guide_lower);
    put("ADX", i.active.adx);
    put("ADXBias", bias);
    put("ADXSlope", std::nullopt); // Active_ADXROC is commented out in the EFS collection.
    put("ADXTrigger", o.start_bias);
    put("StopBias", o.stop_bias);
    put("MaxUpATR", s.max_up_atr);
    put("BarsSinceExit", static_cast<Decimal>(s.bars_since_exit));
    put("ShockScore", i.shock.score);
    put("ShockDownATR", i.shock.down_atr);
    put("ShockUpATR", i.shock.up_atr);
    put("ShockPullbackATR", i.shock.pullback_atr);
    put("ShockRangeATR", i.shock.range_atr);
    put("GuideWidthATR", 0); // EFS calculates a local value, never assigns its published member.
    put("Chop", chop ? 1 : 0);
    out.text_studies[tag + "LastExitReason"] = "";
    out.text_studies[tag + "StopReason"] = s.stop_reason;
    s.previous = i;
    states_.commit(std::move(s));
    return out;
}
} // namespace fig::collections::ladx
