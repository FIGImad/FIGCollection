#pragma once
#include "fig/studies/study_atr.hpp"

namespace fig::collections::ladx2
{
struct PriceShockValues
{
    std::optional<Decimal> atr;
    Decimal down_points{}, up_points{}, range_points{}, pullback_points{};
    Decimal down_atr{}, up_atr{}, range_atr{}, pullback_atr{}, score{};
};

// Private to LADX2. Uses the common ATR without depending on the ADX collection DLL.
class StudyPriceShock final
{
  public:
    explicit StudyPriceShock(std::size_t period = 14, std::size_t depth = studies::default_revision_depth);
    [[nodiscard]] PriceShockValues process(const PriceBar& bar);

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::optional<Decimal> previous_close;
        PriceShockValues value;
    };
    studies::StudyATR atr_;
    studies::LiveStateHistory<State> states_;
};
} // namespace fig::collections::ladx2
