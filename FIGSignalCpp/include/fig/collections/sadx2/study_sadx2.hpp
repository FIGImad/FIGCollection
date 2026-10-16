#pragma once
#include "fig/collections/sadx2/options.hpp"
#include "fig/collections/sadx2/study_price_shock.hpp"
#include "fig/studies/study_adx.hpp"
#include "fig/studies/study_sma_slope.hpp"

namespace fig::collections::sadx2
{
struct SADX2Input
{
    PriceBar price;
    studies::AdxValues active;
    Decimal price_ma{}, guide_middle{}, guide_upper{}, guide_lower{};
    PriceShockValues shock;
    studies::SmaSlopeValues guide_slope;
    int market_trend{};
};

class StudySADX2 final
{
  public:
    explicit StudySADX2(Options options = {}, std::size_t depth = studies::default_revision_depth);
    [[nodiscard]] StudyBar process(const SADX2Input& input);

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::optional<SADX2Input> previous;
        Decimal side{}, max_down_atr{};
        std::size_t bars_since_exit{1000000}, bars_since_guide_break{1000000};
        bool require_guide_exit_recovery{};
        std::string stop_reason;
    };
    Options options_;
    studies::LiveStateHistory<State> states_;
};
} // namespace fig::collections::sadx2
