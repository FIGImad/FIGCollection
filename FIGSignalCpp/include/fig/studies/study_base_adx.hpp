#pragma once
#include "fig/studies/study_adx.hpp"
#include "fig/studies/study_adx_base_options.hpp"
#include "fig/studies/study_adx_roc.hpp"
#include "fig/studies/study_price_shock.hpp"
#include "fig/studies/study_sma_slope.hpp"
namespace fig::studies
{
struct AdxBaseInput
{
    PriceBar price;
    AdxValues active, confirm;
    Decimal price_ma{}, stop_ma{}, guide_middle{}, guide_upper{}, guide_lower{};
    AdxRocValues roc;
    PriceShockValues shock;
    SmaSlopeValues confirm_slope, guide_slope;
};

class StudyLBaseADX
{
  public:
    explicit StudyLBaseADX(AdxBaseOptions options = {}, std::size_t depth = default_revision_depth);
    [[nodiscard]] StudyBar process(const AdxBaseInput&);

  protected:
    StudyLBaseADX(AdxBaseOptions options, bool short_side, std::size_t depth);

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::optional<AdxBaseInput> previous;
        std::optional<Decimal> previous_previous_close;
        int side{};
        std::size_t bars_in_trade{}, weak_bars{}, bars_since_exit{1000000}, bars_since_guide_break{1000000};
        std::optional<Decimal> entry_extreme, entry_close, entry_ma, bias_extreme;
        Decimal max_impulse{};
        bool continuation_ready{};
        std::string entry_mode, last_exit_reason, stop_reason, start_block_reason;
    };
    AdxBaseOptions options_;
    bool short_;
    LiveStateHistory<State> states_;
};
class StudySBaseADX final : public StudyLBaseADX
{
  public:
    explicit StudySBaseADX(AdxBaseOptions options = {}, std::size_t depth = default_revision_depth)
        : StudyLBaseADX(options, true, depth)
    {
    }
};
} // namespace fig::studies
