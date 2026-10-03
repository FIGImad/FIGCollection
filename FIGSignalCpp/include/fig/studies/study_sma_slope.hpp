#pragma once
#include "fig/studies/study_ma.hpp"
namespace fig::studies
{
struct SmaSlopeValues
{
    std::optional<Decimal> ma, slope, flag;
};
class StudySMASlope final
{
  public:
    StudySMASlope(std::size_t period,
                  std::size_t lookback,
                  Decimal flat_points,
                  std::size_t depth = default_revision_depth);
    [[nodiscard]] SmaSlopeValues process(const PriceBar&);
    // Alternative to standalone processing: supply the shared close-SMA for the configured period.
    // Use one input mode consistently for an instance's lifetime.
    [[nodiscard]] SmaSlopeValues process(std::int64_t raw_time, std::optional<Decimal> moving_average);

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::deque<std::optional<Decimal>> values;
        SmaSlopeValues value;
    };
    StudyMA ma_;
    std::size_t lookback_;
    Decimal flat_;
    LiveStateHistory<State> states_;
};
} // namespace fig::studies
