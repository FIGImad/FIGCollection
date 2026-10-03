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
SharedMovingAverages::SharedMovingAverages(std::vector<Key> requests, std::size_t depth)
{
    for (const auto& key : requests)
    {
        (void)source_value(PriceBar{}, key.second);
        studies_.try_emplace(key, key.first, key.second, depth);
    }
}
void SharedMovingAverages::process(const PriceBar& bar)
{
    values_.clear(); // Revisions must recalculate, even when the timestamp is unchanged.
    for (auto& [key, study] : studies_)
        values_.emplace(key, study.process(bar));
}
std::optional<Decimal> SharedMovingAverages::value(std::size_t period, const std::string& source) const
{
    return values_.at({period, source});
}
std::size_t SharedMovingAverages::calculation_count() const noexcept
{
    std::size_t count{};
    for (const auto& [key, study] : studies_)
        count += study.evaluation_count();
    return count;
}
} // namespace fig::studies
