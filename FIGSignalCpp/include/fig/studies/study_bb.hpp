#pragma once
#include "fig/studies/live_state.hpp"
namespace fig::studies
{
struct BollingerBands
{
    std::optional<Decimal> middle, upper, lower;
};
class StudyBB final
{
  public:
    StudyBB(std::size_t period,
            Decimal standard_deviations,
            std::string source = "close",
            std::size_t revision_depth = 10);
    [[nodiscard]] BollingerBands process(const PriceBar& bar);

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::deque<Decimal> window;
        Decimal sum{}, sum_squares{};
        BollingerBands value;
    };
    std::size_t period_;
    Decimal deviations_;
    std::string source_;
    LiveStateHistory<State> states_;
};
} // namespace fig::studies
