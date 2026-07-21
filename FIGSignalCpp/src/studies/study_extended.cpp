#include "fig/studies/study_extended.hpp"
#include <cmath>
namespace fig::studies
{
StudyExtended::StudyExtended(std::size_t period, std::string source, Decimal d1, Decimal d2, std::size_t depth)
    : period_(period), source_(std::move(source)), deviation1_(d1), deviation2_(d2), states_(depth)
{
    if (!period_)
        throw std::invalid_argument("Extended period cannot be zero");
    const auto n = static_cast<Decimal>(period_);
    sum_x_ = n * (n + 1) / 2;
    const auto sum_xx = n * (n + 1) * (2 * n + 1) / 6;
    denominator_ = n * sum_xx - sum_x_ * sum_x_;
}
ExtendedValues StudyExtended::process(const PriceBar& bar)
{
    auto s = states_.begin(bar.raw_time);
    const auto v = source_value(bar, source_);
    if (s.window.size() < period_)
    {
        s.window.push_back(v);
        if (s.window.size() == period_)
        {
            s.sum_y = s.sum_xy = s.sum_yy = 0;
            for (std::size_t i{}; i < period_; ++i)
            {
                const auto y = s.window[i];
                s.sum_y += y;
                s.sum_xy += static_cast<Decimal>(period_ - i) * y;
                s.sum_yy += y * y;
            }
        }
    }
    else
    {
        const auto old = s.window.front(), old_sum = s.sum_y;
        s.window.pop_front();
        s.window.push_back(v);
        s.sum_xy += old_sum + v - old * static_cast<Decimal>(period_ + 1);
        s.sum_y += v - old;
        s.sum_yy += v * v - old * old;
    }
    s.value = {};
    if (s.window.size() == period_)
    {
        const auto n = static_cast<Decimal>(period_), mean = s.sum_y / n;
        std::optional<Decimal> sem;
        if (denominator_ != 0)
        {
            const auto slope = (n * s.sum_xy - sum_x_ * s.sum_y) / denominator_;
            sem = (s.sum_y - slope * sum_x_) / n;
        }
        const auto variance = std::max<Decimal>(0, s.sum_yy / n - mean * mean), sd = std::sqrt(variance);
        s.value = {mean,
                   sem,
                   mean - deviation1_ * sd,
                   mean + deviation1_ * sd,
                   mean - deviation2_ * sd,
                   mean + deviation2_ * sd};
    }
    return states_.commit(std::move(s)).value;
}
} // namespace fig::studies
