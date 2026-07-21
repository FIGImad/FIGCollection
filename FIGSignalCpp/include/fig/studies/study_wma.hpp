#pragma once
#include "fig/studies/live_state.hpp"
namespace fig::studies
{
class StudyWMA final
{
  public:
    StudyWMA(std::size_t period, std::string source = "close", std::size_t revision_depth = 10);
    [[nodiscard]] std::optional<Decimal> process(const PriceBar& bar);

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::deque<Decimal> window;
        Decimal sum{};
        Decimal weighted_sum{};
        std::optional<Decimal> value;
    };
    std::size_t period_;
    std::string source_;
    LiveStateHistory<State> states_;
};
} // namespace fig::studies
