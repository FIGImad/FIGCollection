#pragma once
#include "fig/studies/live_state.hpp"
#include <string_view>

namespace fig::collections::ladx
{
// Defaults of the EFS constructors. ladx-parameters.json supplies preMain's UI defaults.
struct Options
{
    std::size_t adx_length{50}, fast_ma_length{4}, guide_ma_length{8}, shock_atr_length{14};
    Decimal guide_deviation{0.6L}, start_bias{1}, stop_bias{-0.75L};
    std::size_t cooldown_bars{1}, continuation_bars{5};
    Decimal reentry_shock_max{0.65L}, shock_drop_atr{1.15L}, shock_armed_drop_atr{0.85L};
    Decimal shock_arm_up_atr{0.75L}, shock_range_atr{1.8L};
    Decimal chop_min_width_atr{0.8L}, chop_max_slope_atr{0.06L};
    // Accepted for parity with preMain, but never read by the supplied Study_LADX.
    Decimal continuation_max_shock_atr{1};
    void validate() const;
    static Options from_json(std::string_view json);
};
} // namespace fig::collections::ladx
