#pragma once
#include "fig/studies/live_state.hpp"
namespace fig::studies
{
class StudyATR final
{
  public:
    explicit StudyATR(std::size_t period, std::size_t depth = default_revision_depth);
    [[nodiscard]] std::optional<Decimal> process(const PriceBar&);

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::optional<Decimal> previous_close, value;
        std::size_t count{};
        Decimal sum{};
    };
    std::size_t period_;
    LiveStateHistory<State> states_;
};
} // namespace fig::studies
