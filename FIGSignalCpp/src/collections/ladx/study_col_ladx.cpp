#include "fig/collections/ladx/study_col_ladx.hpp"
#include <algorithm>

namespace fig::collections::ladx
{
namespace
{
std::size_t lookback(const Options& o)
{
    o.validate();
    return std::max({2 * o.adx_length, o.fast_ma_length, o.guide_ma_length + 5, o.shock_atr_length + 1});
}
std::vector<studies::SharedMovingAverages::Key> ma_requests(const Options& o)
{
    std::vector<studies::SharedMovingAverages::Key> requests{{o.guide_ma_length, "close"}};
    if (o.fast_ma_length != o.guide_ma_length)
        requests.emplace_back(o.fast_ma_length, "ohlc4");
    return requests;
}
} // namespace
StudyColLADX::StudyColLADX(Options o, std::size_t depth)
    : lookback_(lookback(o)), fast_period_(o.fast_ma_length), guide_period_(o.guide_ma_length),
      averages_(ma_requests(o), depth), guide_(o.guide_ma_length, o.guide_deviation, "ohlc4", depth),
      active_(o.adx_length, o.adx_length, depth), shock_(o.shock_atr_length, depth),
      guide_slope_(o.guide_ma_length, 5, 0.1L, depth), signal_(o, depth)
{
}
StudyBar StudyColLADX::process(const PriceBar& bar)
{
    LADXInput i;
    i.price = bar;
    const auto bands = guide_.process(bar);
    averages_.process(bar);
    i.price_ma = (fast_period_ == guide_period_ ? bands.middle : averages_.value(fast_period_, "ohlc4")).value_or(0);
    i.guide_middle = bands.middle.value_or(0);
    i.guide_upper = bands.upper.value_or(0);
    i.guide_lower = bands.lower.value_or(0);
    i.active = active_.process(bar);
    i.shock = shock_.process(bar);
    i.guide_slope = guide_slope_.process(bar.raw_time, averages_.value(guide_period_, "close"));
    auto out = signal_.process(i);
    auto& v = out.studies;
    v["MAPrice"] = i.price_ma;
    v["MIDGuide"] = i.guide_middle;
    v["UBGuide"] = i.guide_upper;
    v["LBGuide"] = i.guide_lower;
    v["Active_ADX"] = i.active.adx;
    v["Active_PDI"] = i.active.pdi;
    v["Active_NDI"] = i.active.ndi;
    v["Active_BIAS"] = i.active.bias;
    v["Shock_ATR"] = i.shock.atr;
    v["Shock_DownPoints"] = i.shock.down_points;
    v["Shock_UpPoints"] = i.shock.up_points;
    v["Shock_RangePoints"] = i.shock.range_points;
    v["Shock_PullbackPoints"] = i.shock.pullback_points;
    v["Shock_DownATR"] = i.shock.down_atr;
    v["Shock_UpATR"] = i.shock.up_atr;
    v["Shock_RangeATR"] = i.shock.range_atr;
    v["Shock_PullbackATR"] = i.shock.pullback_atr;
    v["Shock_Score"] = i.shock.score;
    v["SLGuide_MA"] = i.guide_slope.ma;
    v["SLGuide_MASlope"] = i.guide_slope.flag;
    v["SLGuide_MASlopeValue"] = i.guide_slope.slope;
    return out;
}
} // namespace fig::collections::ladx
