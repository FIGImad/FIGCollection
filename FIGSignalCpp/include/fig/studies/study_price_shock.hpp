#pragma once
#include "fig/studies/study_atr.hpp"
namespace fig::studies
{
struct PriceShockValues
{
    std::optional<Decimal> atr;
    Decimal down_points{}, up_points{}, range_points{}, pullback_points{};
    Decimal down_atr{}, up_atr{}, range_atr{}, pullback_atr{}, score{};
};
class StudyPriceShock final
{
  public:
    explicit StudyPriceShock(std::size_t period = 14, std::size_t depth = default_revision_depth);
    [[nodiscard]] PriceShockValues process(const PriceBar&);

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::optional<Decimal> previous_close;
        PriceShockValues value;
    };
    StudyATR atr_;
    LiveStateHistory<State> states_;
};
} // namespace fig::studies
