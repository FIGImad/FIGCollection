#include "fig/studies/study_atr.hpp"
#include <algorithm>
#include <cmath>
namespace fig::studies
{
StudyATR::StudyATR(std::size_t period, std::size_t depth) : period_(period), states_(depth)
{
    if (!period)
        throw std::invalid_argument("ATR period cannot be zero");
}
std::optional<Decimal> StudyATR::process(const PriceBar& bar)
{
    auto s = states_.begin(bar.raw_time);
    // A full true range requires the preceding close, as in the EFS ATR formula.
    if (s.previous_close)
    {
        const auto tr = std::max({std::abs(bar.high - bar.low),
                                  std::abs(bar.high - *s.previous_close),
                                  std::abs(bar.low - *s.previous_close)});
        if (s.count < period_)
        {
            s.sum += tr;
            if (++s.count == period_)
                s.value = s.sum / static_cast<Decimal>(period_);
        }
        else
            s.value = (*s.value * static_cast<Decimal>(period_ - 1) + tr) / static_cast<Decimal>(period_);
    }
    s.previous_close = bar.close;
    return states_.commit(std::move(s)).value;
}
} // namespace fig::studies
