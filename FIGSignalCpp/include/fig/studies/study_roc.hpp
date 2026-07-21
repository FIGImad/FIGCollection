#pragma once
#include "fig/studies/live_state.hpp"
namespace fig::studies
{
class StudyROC final
{
  public:
    StudyROC(std::size_t length, std::size_t revision_depth = 10);
    [[nodiscard]] std::optional<Decimal> process(std::int64_t raw_time, Decimal value);

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::deque<Decimal> values;
        std::optional<Decimal> value;
    };
    std::size_t length_;
    LiveStateHistory<State> states_;
};
} // namespace fig::studies
