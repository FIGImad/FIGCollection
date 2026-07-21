#include "fig/studies/study_slope_trend.hpp"
#include <cmath>
namespace fig::studies
{
StudySlopeTrend::StudySlopeTrend(std::size_t lookback, Decimal flat_points, std::size_t depth)
    : lookback_(lookback), flat_points_(flat_points), states_(depth)
{
    if (!lookback_)
        throw std::invalid_argument("Slope lookback cannot be zero");
}
SlopeTrendValues StudySlopeTrend::process(std::int64_t time, std::optional<Decimal> ma)
{
    auto s = states_.begin(time);
    const auto now = ma.value_or(0);
    s.values.push_back(now);
    if (s.values.size() > lookback_ + 1)
        s.values.pop_front();
    s.value = {0, now, 0};
    if (s.values.size() == lookback_ + 1 && now != 0 && s.values.front() != 0)
    {
        s.value.slope_per_bar = (now - s.values.front()) / static_cast<Decimal>(lookback_);
        s.value.direction = std::abs(s.value.slope_per_bar) <= flat_points_ ? 0 : (s.value.slope_per_bar > 0 ? 1 : -1);
    }
    return states_.commit(std::move(s)).value;
}
} // namespace fig::studies
