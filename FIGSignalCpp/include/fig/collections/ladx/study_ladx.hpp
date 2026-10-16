#pragma once
#include "fig/collections/ladx/options.hpp"
#include "fig/collections/ladx/study_price_shock.hpp"
#include "fig/studies/study_adx.hpp"
#include "fig/studies/study_sma_slope.hpp"

namespace fig::collections::ladx
{
struct LADXInput
{
    PriceBar price;
    studies::AdxValues active;
    Decimal price_ma{}, guide_middle{}, guide_upper{}, guide_lower{};
    PriceShockValues shock;
    studies::SmaSlopeValues guide_slope;
};

class StudyLADX final
{
  public:
    explicit StudyLADX(Options options = {}, std::size_t depth = studies::default_revision_depth);
    [[nodiscard]] StudyBar process(const LADXInput& input);

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::optional<LADXInput> previous;
        Decimal side{}, max_up_atr{};
        std::size_t bars_since_exit{1000000}, bars_since_guide_break{1000000};
        std::string stop_reason;
    };
    Options options_;
    studies::LiveStateHistory<State> states_;
};
} // namespace fig::collections::ladx
