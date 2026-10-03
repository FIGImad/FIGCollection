#pragma once
#include "fig/studies/live_state.hpp"
namespace fig::studies
{
struct AdxRocValues
{
    std::optional<Decimal> roc, percent, slope;
};
class StudyADXROC final
{
  public:
    explicit StudyADXROC(std::size_t lookback, std::size_t depth = default_revision_depth);
    [[nodiscard]] AdxRocValues process(std::int64_t time, std::optional<Decimal> value);

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::deque<Decimal> values;
        AdxRocValues value;
    };
    std::size_t lookback_;
    LiveStateHistory<State> states_;
};
} // namespace fig::studies
