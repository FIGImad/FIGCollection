#include "fig/studies/study_price_shock.hpp"
#include <algorithm>
namespace fig::studies
{
StudyPriceShock::StudyPriceShock(std::size_t n, std::size_t depth) : atr_(n, depth), states_(depth)
{
}
PriceShockValues StudyPriceShock::process(const PriceBar& bar)
{
    auto s = states_.begin(bar.raw_time);
    s.value = {};
    auto& v = s.value;
    v.atr = atr_.process(bar);
    if (s.previous_close && v.atr && *v.atr > 0)
    {
        v.down_points = std::max<Decimal>(0, *s.previous_close - bar.close);
        v.up_points = std::max<Decimal>(0, bar.close - *s.previous_close);
        v.range_points = std::max<Decimal>(0, bar.high - bar.low);
        v.pullback_points = std::max<Decimal>(0, bar.high - bar.close);
        v.down_atr = v.down_points / *v.atr;
        v.up_atr = v.up_points / *v.atr;
        v.range_atr = v.range_points / *v.atr;
        v.pullback_atr = v.pullback_points / *v.atr;
        v.score = std::max(v.down_atr, v.pullback_atr);
    }
    s.previous_close = bar.close;
    return states_.commit(std::move(s)).value;
}
} // namespace fig::studies
