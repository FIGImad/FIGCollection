#include "fig/studies/study_bb.hpp"
#include <cmath>
namespace fig::studies
{
StudyBB::StudyBB(std::size_t period, Decimal deviations, std::string source, std::size_t revision_depth)
    : period_(period), deviations_(deviations), source_(std::move(source)), states_(revision_depth)
{
    if (!period_ || deviations_ <= 0)
        throw std::invalid_argument("Invalid Bollinger Band parameters");
}
BollingerBands StudyBB::process(const PriceBar& bar)
{
    auto state = states_.begin(bar.raw_time);
    const auto value = source_value(bar, source_);
    state.window.push_back(value);
    state.sum += value;
    state.sum_squares += value * value;
    if (state.window.size() > period_)
    {
        const auto old = state.window.front();
        state.window.pop_front();
        state.sum -= old;
        state.sum_squares -= old * old;
    }
    state.value = {};
    if (state.window.size() == period_ && state.sum != 0 && state.sum_squares != 0)
    {
        const auto mean = state.sum / static_cast<Decimal>(period_);
        const auto variance = std::max<Decimal>(0, state.sum_squares / static_cast<Decimal>(period_) - mean * mean);
        const auto width = deviations_ * std::sqrt(variance);
        state.value = {mean, mean + width, mean - width};
    }
    return states_.commit(std::move(state)).value;
}
} // namespace fig::studies
