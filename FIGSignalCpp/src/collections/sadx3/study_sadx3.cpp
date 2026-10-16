#include "fig/collections/sadx3/study_sadx3.hpp"
#include <algorithm>
#include <cmath>

namespace fig::collections::sadx3
{
StudySADX3::StudySADX3(Options options, std::size_t depth) : options_(options), states_(depth)
{
    options_.validate();
}
StudyBar StudySADX3::process(const SADX3Input& i)
{
    auto s = states_.begin(i.price.raw_time);
    const auto& o = options_; 
    const auto start_bias = o.start_bias_short;
    const auto stop_bias = o.stop_bias_short;
    // Match ADXBase: increment from the preceding bar snapshot, including while in a trade.
    if (s.previous && s.bars_since_exit < 1000000)
        ++s.bars_since_exit;
    const auto bias =
        i.active.pdi && i.active.ndi ? std::optional<Decimal>(*i.active.pdi - *i.active.ndi) : std::nullopt;
    // EFS compatibility diagnostic only; chop no longer gates entry.
    const bool chop = o.chop_max_slope_atr > 0 && o.chop_min_width_atr > 0;

    // Reference saves barsSinceBreakout but restores barsSinceBreakout.
    // Preserve its reset-to-sentinel behavior until the reference is refined.
    s.bars_since_guide_break = 1000000;
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
            const bool pullback = i.price_ma > i.guide_middle;
            const bool reentry = beyond && o.continuation_bars > 0 && s.bars_since_guide_break <= o.continuation_bars;
            const bool exit_recovery = !s.require_exit_recovery || i.price_ma < i.guide_upper;
            if (!cooldown && exit_recovery && bias_not_deteriorating && *bias < start_bias &&
                recovering && (pullback || reentry))
            {
                s.side = -1.1L;
                s.require_exit_recovery = false;
            }
        }
        else
        {
            if (*bias > stop_bias)
                s.stop_reason = "ADX_BIAS";
            else if (o.exit_on_cross_trend && s.previous &&
                     std::isfinite(s.previous->price.low) && std::isfinite(i.price.low) &&
                     std::isfinite(s.previous->guide_lower) && std::isfinite(i.guide_lower) &&
                     s.previous->price.low <= s.previous->guide_lower && i.price.low > i.guide_lower)
                s.stop_reason = "TREND_CROSS";
            if (!s.stop_reason.empty())
            {
                s.side = 0;
                s.require_exit_recovery = s.stop_reason == "TREND_CROSS";
                s.bars_since_exit = 0;
            }
        }
    }

    StudyBar out{i.price, {}};
    const std::string tag = "SADX3_";
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
    put("MarketTrend", 0);
    put("MaxDownATR", 0);
    put("BarsSinceExit", static_cast<Decimal>(s.bars_since_exit));
    put("ShockScore", 0);
    put("ShockDownATR", 0);
    put("ShockUpATR", 0);
    put("ShockPullbackATR", 0);
    put("ShockRangeATR", 0);
    put("GuideWidthATR", 0); // EFS calculates a local value, never assigns its published member.
    put("Chop", chop ? 1 : 0);
    put("BarsSinceBreakout", static_cast<Decimal>(s.bars_since_guide_break));
    out.text_studies[tag + "LastExitReason"] = "";
    out.text_studies[tag + "StopReason"] = s.stop_reason;
    s.previous = i;
    states_.commit(std::move(s));
    return out;
}
} // namespace fig::collections::sadx3

