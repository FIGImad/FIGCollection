#pragma once
#include "fig/studies/live_state.hpp"
namespace fig::studies
{
// Defaults of new StudyCollection({}), rather than the separate preMain UI defaults.
struct AdxBaseOptions
{
    std::size_t adx_length{50}, confirm_length{6}, fast_ma_length{3}, slow_ma_length{3}, guide_ma_length{24};
    Decimal guide_deviation{0.6L}, start_bias{1}, stop_bias{-0.75L};
    std::size_t fail_fast_bars{2}, cooldown_bars{1}, trend_reentry_bars{6}, shock_atr_length{14}, continuation_bars{8};
    Decimal fail_fast_buffer{}, trend_reentry_shock_max{4L}, shock_drop_atr{3L}, shock_armed_drop_atr{1.85L};
    Decimal shock_arm_up_atr{1.85L}, shock_range_atr{1.85L}, chop_min_width_atr{0.8L}, chop_max_slope_atr{0.06L};
    Decimal continuation_min_slope_atr{0.02L}, continuation_max_shock_atr{1};
    void validate() const;
    static AdxBaseOptions from_json(std::string_view);
};
} // namespace fig::studies
