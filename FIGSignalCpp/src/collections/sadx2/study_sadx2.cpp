#include "fig/collections/sadx2/study_sadx2.hpp"
#include <algorithm>
#include <cmath>

namespace fig::collections::sadx2
{
StudySADX2::StudySADX2(Options options, std::size_t depth) : options_(options), states_(depth)
{
    options_.validate();
}
StudyBar StudySADX2::process(const SADX2Input& i)
{
    auto s = states_.begin(i.price.raw_time);
    const auto& o = options_; 
    const int marketTrend = i.market_trend;
    const auto start_bias = marketTrend == -1 ? o.start_bias_short : o.start_bias_long;
    const auto stop_bias = marketTrend == -1 ? o.stop_bias_short : o.stop_bias_long;
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

    const bool beyond = i.price_ma <= i.guide_middle && i.price.close <= i.guide_middle;
    const bool was_beyond = s.previous && s.previous->price_ma <= s.previous->guide_middle &&
                            s.previous->price.close <= s.previous->guide_middle;
    if (!beyond)
        s.bars_since_guide_break = 1000000;
    else if (!was_beyond)
        s.bars_since_guide_break = 0;
    else if (s.bars_since_guide_break < 1000000)
        ++s.bars_since_guide_break;

    if (s.side != 0)
    {
        s.stop_reason.clear();
        s.max_down_atr = std::max(s.max_down_atr, i.shock.down_atr);
    }
    if (i.active.adx && bias)
    {
        if (s.side == 0)
        {
            const bool cooldown = o.cooldown_bars > 0 && s.bars_since_exit < o.cooldown_bars;
            const auto previous_bias = s.previous && s.previous->active.pdi && s.previous->active.ndi
                                           ? std::optional<Decimal>(*s.previous->active.pdi - *s.previous->active.ndi)
                                           : std::nullopt;
            const bool recovering = s.previous && i.price_ma <= s.previous->price_ma;
            const bool bias_not_deteriorating = !previous_bias || *bias <= *previous_bias;
            const bool pullback = marketTrend == 1 && i.price_ma > i.guide_middle;
            const bool reentry = beyond && o.continuation_bars > 0 && s.bars_since_guide_break <= o.continuation_bars;
            const bool guide_exit_recovery = !s.require_guide_exit_recovery || i.price_ma < i.guide_upper;
            if (!cooldown && !chop && guide_exit_recovery && bias_not_deteriorating && *bias < start_bias &&
                recovering && (pullback || reentry))
            {
                s.side = reentry ? -1.1L : -1.2L;
                s.require_guide_exit_recovery = false;
                s.max_down_atr = 0;
            }
        }
        else
        {
            if (*bias > stop_bias)
                s.stop_reason = "ADX_BIAS";
            else if (i.shock.score >= o.shock_rise_atr)
                s.stop_reason = "SHOCK_RISE";
            else if (s.max_down_atr >= o.shock_arm_down_atr && i.shock.score >= o.shock_armed_rise_atr)
                s.stop_reason = "ARMED_RISE";
            else if (i.shock.range_atr >= o.shock_range_atr && i.shock.up_atr > 0)
                s.stop_reason = "SHOCK_RANGE";
            else if (o.exit_on_guide_cross_long_trend && marketTrend == 1 && s.previous &&
                     std::isfinite(s.previous->price.low) && std::isfinite(i.price.low) &&
                     std::isfinite(s.previous->guide_upper) && std::isfinite(i.guide_upper) &&
                     s.previous->price.low <= s.previous->guide_upper && i.price.low > i.guide_upper)
                s.stop_reason = "LONG_TREND_GUIDE_CROSS";
            if (!s.stop_reason.empty())
            {
                s.side = 0;
                s.require_guide_exit_recovery = s.stop_reason == "LONG_TREND_GUIDE_CROSS";
                s.bars_since_exit = 0;
                s.max_down_atr = 0;
            }
        }
    }

    StudyBar out{i.price, {}};
    const std::string tag = "SADX2_";
    const auto put = [&](std::string_view name, std::optional<Decimal> value)
    { out.studies[tag + std::string(name)] = value; };
    put("SIDE", s.side);
    // Host backtests expect 0/-1. Keep the EFS entry subtype in Side.
    put("ShortSide", s.side != 0 ? -1 : 0);
    put("PriceMA", i.price_ma);
    put("GuideMA", i.guide_middle);
    put("GuideUB", i.guide_upper);
    put("GuideLB", i.guide_lower);
    put("ADX", i.active.adx);
    put("ADXBias", bias);
    put("ADXSlope", std::nullopt); // Active_ADXROC is commented out in the EFS collection.
    put("ADXTrigger", start_bias);
    put("StopBias", stop_bias);
    put("MarketTrend", marketTrend);
    put("MaxDownATR", s.max_down_atr);
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
} // namespace fig::collections::sadx2
