#include "fig/studies/study_sem.hpp"
namespace fig::studies
{
StudySEM::StudySEM(std::size_t period, std::string source, std::size_t revision_depth)
    : period_(period), source_(std::move(source)), states_(revision_depth)
{
    if (!period_)
        throw std::invalid_argument("SEM period cannot be zero");
    const auto n = static_cast<Decimal>(period_);
    sum_x_ = n * (n + 1) / 2;
    const auto sum_xx = n * (n + 1) * (2 * n + 1) / 6;
    denominator_ = n * sum_xx - sum_x_ * sum_x_;
    if (denominator_ == 0)
        throw std::invalid_argument("SEM period must exceed one");
}
std::optional<Decimal> StudySEM::process(const PriceBar& bar)
{
    auto state = states_.begin(bar.raw_time);
    const auto value = source_value(bar, source_);
    if (state.window.size() < period_)
    {
        state.window.push_back(value);
        if (state.window.size() == period_)
        {
            state.sum_y = 0;
            state.sum_xy = 0;
            for (std::size_t i{}; i < period_; ++i)
            {
                state.sum_y += state.window[i];
                state.sum_xy += static_cast<Decimal>(period_ - i) * state.window[i];
            }
        }
    }
    else
    {
        const auto oldest = state.window.front(), old_sum = state.sum_y;
        state.window.pop_front();
        state.window.push_back(value);
        state.sum_xy += old_sum + value - oldest * static_cast<Decimal>(period_ + 1);
        state.sum_y += value - oldest;
    }
    if (state.window.size() == period_ && state.sum_y != 0 && state.sum_xy != 0)
    {
        const auto n = static_cast<Decimal>(period_);
        const auto slope = (n * state.sum_xy - sum_x_ * state.sum_y) / denominator_;
        state.value = (state.sum_y - slope * sum_x_) / n;
    }
    else
        state.value = std::nullopt;
    return states_.commit(std::move(state)).value;
}
} // namespace fig::studies
