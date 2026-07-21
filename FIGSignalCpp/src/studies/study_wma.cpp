#include "fig/studies/study_wma.hpp"
namespace fig::studies
{
StudyWMA::StudyWMA(std::size_t period, std::string source, std::size_t revision_depth)
    : period_(period), source_(std::move(source)), states_(revision_depth)
{
    if (!period_)
        throw std::invalid_argument("WMA period cannot be zero");
}
std::optional<Decimal> StudyWMA::process(const PriceBar& bar)
{
    auto state = states_.begin(bar.raw_time);
    const auto value = source_value(bar, source_);
    if (state.window.size() < period_)
    {
        state.window.push_back(value);
        state.sum += value;
        state.weighted_sum = 0;
        for (std::size_t i{}; i < state.window.size(); ++i)
            state.weighted_sum += state.window[i] * static_cast<Decimal>(i + 1);
    }
    else
    {
        const auto old_sum = state.sum, oldest = state.window.front();
        state.window.pop_front();
        state.window.push_back(value);
        state.weighted_sum = state.weighted_sum - old_sum + static_cast<Decimal>(period_) * value;
        state.sum = old_sum - oldest + value;
    }
    state.value = state.window.size() == period_
                      ? std::optional<Decimal>(state.weighted_sum / static_cast<Decimal>(period_ * (period_ + 1) / 2))
                      : std::nullopt;
    return states_.commit(std::move(state)).value;
}
} // namespace fig::studies
