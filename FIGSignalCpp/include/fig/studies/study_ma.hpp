#pragma once
#include "fig/studies/live_state.hpp"
namespace fig::studies
{
class StudyMA final
{
  public:
    StudyMA(std::size_t period, std::string source = "close", std::size_t revision_depth = 10);
    [[nodiscard]] std::optional<Decimal> process(const PriceBar& bar);
    [[nodiscard]] std::size_t evaluation_count() const noexcept
    {
        return evaluations_;
    }

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::deque<Decimal> window;
        Decimal sum{};
        std::optional<Decimal> value;
    };
    std::size_t period_;
    std::string source_;
    std::size_t evaluations_{};
    LiveStateHistory<State> states_;
};
} // namespace fig::studies
