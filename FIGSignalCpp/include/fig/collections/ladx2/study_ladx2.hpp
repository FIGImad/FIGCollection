#pragma once
#include "fig/collections/ladx2/options.hpp"
#include "fig/collections/ladx2/study_price_shock.hpp"
#include "fig/studies/study_adx.hpp"
#include "fig/studies/study_sma_slope.hpp"

namespace fig::collections::ladx2
{
struct LADX2Input
{
    PriceBar price;
    studies::AdxValues active;
    Decimal price_ma{}, guide_middle{}, guide_upper{}, guide_lower{};
    PriceShockValues shock;
    studies::SmaSlopeValues guide_slope;
    int market_trend{};
};

class StudyLADX2 final
{
  public:
    explicit StudyLADX2(Options options = {}, std::size_t depth = studies::default_revision_depth);
    [[nodiscard]] StudyBar process(const LADX2Input& input);

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::optional<LADX2Input> previous;
        Decimal side{}, max_up_atr{};
        std::size_t bars_since_exit{1000000}, bars_since_guide_break{1000000};
        bool require_guide_exit_recovery{};
        std::string stop_reason;
    };
    Options options_;
    studies::LiveStateHistory<State> states_;
};
} // namespace fig::collections::ladx2
