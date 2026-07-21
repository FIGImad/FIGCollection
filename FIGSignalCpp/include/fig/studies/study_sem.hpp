#pragma once
#include "fig/studies/live_state.hpp"
namespace fig::studies
{
class StudySEM final
{
  public:
    StudySEM(std::size_t period, std::string source = "close", std::size_t revision_depth = 10);
    [[nodiscard]] std::optional<Decimal> process(const PriceBar& bar);

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::deque<Decimal> window;
        Decimal sum_y{};
        Decimal sum_xy{};
        std::optional<Decimal> value;
    };
    std::size_t period_;
    std::string source_;
    Decimal sum_x_{}, denominator_{};
    LiveStateHistory<State> states_;
};
} // namespace fig::studies
