#pragma once
#include "fig/studies/live_state.hpp"
namespace fig::studies
{
struct SlopeTrendValues
{
    int direction{};
    Decimal moving_average{}, slope_per_bar{};
};
class StudySlopeTrend final
{
  public:
    StudySlopeTrend(std::size_t lookback, Decimal flat_points, std::size_t revision_depth = 10);
    [[nodiscard]] SlopeTrendValues process(std::int64_t raw_time, std::optional<Decimal> moving_average);

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::deque<Decimal> values;
        SlopeTrendValues value;
    };
    std::size_t lookback_;
    Decimal flat_points_;
    LiveStateHistory<State> states_;
};
} // namespace fig::studies
