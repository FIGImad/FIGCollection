#include "fig/collections/sadx3/study_col_sadx3.hpp"
#include <algorithm>

namespace fig::collections::sadx3
{
namespace
{
std::size_t lookback(const Options& o, int interval)
{
    o.validate();
    if (interval <= 0) throw std::invalid_argument("SADX3 interval must be positive");
    return std::max({2 * o.adx_length, o.fast_ma_length, o.guide_ma_length});
}
std::vector<studies::SharedMovingAverages::Key> ma_requests(const Options& o)
{
    std::vector<studies::SharedMovingAverages::Key> requests{};
    if (o.fast_ma_length != o.guide_ma_length)
        requests.emplace_back(o.fast_ma_length, "ohlc4");
    return requests;
}
} // namespace
StudyColSADX3::StudyColSADX3(Options o, std::size_t depth, int interval_seconds)
    : lookback_(lookback(o, interval_seconds)), fast_period_(o.fast_ma_length), guide_period_(o.guide_ma_length),
      averages_(ma_requests(o), depth), guide_(o.guide_ma_length, o.guide_deviation, "ohlc4", depth),
      active_(o.adx_length, o.adx_length, depth), signal_(o, depth)
{
}
StudyBar StudyColSADX3::process(const PriceBar& bar)
{
    SADX3Input i;
    i.price = bar;
    const auto bands = guide_.process(bar);
    averages_.process(bar);
    i.price_ma = (fast_period_ == guide_period_ ? bands.middle : averages_.value(fast_period_, "ohlc4")).value_or(0);
    i.guide_middle = bands.middle.value_or(0);
    i.guide_upper = bands.upper.value_or(0);
    i.guide_lower = bands.lower.value_or(0);
    i.active = active_.process(bar);
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
    return out;
}
} // namespace fig::collections::sadx3
