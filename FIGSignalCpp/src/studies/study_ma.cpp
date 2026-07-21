#include "fig/studies/study_ma.hpp"
namespace fig::studies
{
StudyMA::StudyMA(std::size_t period, std::string source, std::size_t revision_depth)
    : period_(period), source_(std::move(source)), states_(revision_depth)
{
    if (!period_)
        throw std::invalid_argument("MA period cannot be zero");
}
std::optional<Decimal> StudyMA::process(const PriceBar& bar)
{
    auto state = states_.begin(bar.raw_time);
    const auto value = source_value(bar, source_);
    state.window.push_back(value);
    state.sum += value;
    if (state.window.size() > period_)
    {
        state.sum -= state.window.front();
        state.window.pop_front();
    }
    state.value = state.window.size() == period_ ? std::optional<Decimal>(state.sum / static_cast<Decimal>(period_))
                                                 : std::nullopt;
    ++evaluations_;
    return states_.commit(std::move(state)).value;
}
} // namespace fig::studies
