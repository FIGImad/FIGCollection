#include "fig/studies/study_sma_slope.hpp"
#include <cmath>
namespace fig::studies
{
StudySMASlope::StudySMASlope(std::size_t n, std::size_t lookback, Decimal flat, std::size_t depth)
    : ma_(n, "close", depth), lookback_(lookback), flat_(flat), states_(depth)
{
    if (!lookback || lookback == std::numeric_limits<std::size_t>::max() || !std::isfinite(flat) || flat < 0)
        throw std::invalid_argument("Invalid SMA slope parameters");
}
SmaSlopeValues StudySMASlope::process(const PriceBar& bar)
{
    return process(bar.raw_time, ma_.process(bar));
}
SmaSlopeValues StudySMASlope::process(std::int64_t raw_time, std::optional<Decimal> moving_average)
{
    auto s = states_.begin(raw_time);
    s.values.push_back(moving_average);
    if (s.values.size() > lookback_ + 1)
        s.values.pop_front();
    s.value = {};
    if (s.values.size() == lookback_ + 1 && s.values.front() && s.values.back())
    {
        const auto slope = (*s.values.back() - *s.values.front()) / static_cast<Decimal>(lookback_);
        s.value = {s.values.back(), slope, std::abs(slope) <= flat_ ? 0 : slope > 0 ? 1 : -1};
    }
    return states_.commit(std::move(s)).value;
}
} // namespace fig::studies
