#pragma once
#include "fig/collections/sadx3/options.hpp"
#include "fig/studies/study_adx.hpp"

namespace fig::collections::sadx3
{
struct SADX3Input
{
    PriceBar price;
    studies::AdxValues active;
    Decimal price_ma{}, guide_middle{}, guide_upper{}, guide_lower{};
};

class StudySADX3 final
{
  public:
    explicit StudySADX3(Options options = {}, std::size_t depth = studies::default_revision_depth);
    [[nodiscard]] StudyBar process(const SADX3Input& input);

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::optional<SADX3Input> previous;
        Decimal side{};
        std::size_t bars_since_exit{1000000}, bars_since_guide_break{1000000};
        bool require_exit_recovery{};
        std::string stop_reason;
    };
    Options options_;
    studies::LiveStateHistory<State> states_;
};
} // namespace fig::collections::sadx3
