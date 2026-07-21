#pragma once
#include "fig/studies/live_state.hpp"
namespace fig::studies
{
struct ExtendedValues
{
    std::optional<Decimal> moving_average, sem, lower1, upper1, lower2, upper2;
};
class StudyExtended final
{
  public:
    StudyExtended(std::size_t period,
                  std::string source = "ohlc4",
                  Decimal deviation1 = 1,
                  Decimal deviation2 = 2,
                  std::size_t revision_depth = 10);
    [[nodiscard]] ExtendedValues process(const PriceBar& bar);

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::deque<Decimal> window;
        Decimal sum_y{}, sum_xy{}, sum_yy{};
        ExtendedValues value;
    };
    std::size_t period_;
    std::string source_;
    Decimal deviation1_, deviation2_, sum_x_{}, denominator_{};
    LiveStateHistory<State> states_;
};
} // namespace fig::studies
