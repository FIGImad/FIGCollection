#pragma once
#include "fig/studies/live_state.hpp"
#include <string_view>
namespace fig::collections::sadx2
{
struct MarketTrendSettings
{
    std::string timeframe{"hourly"}, time_zone{"America/New_York"};
    std::size_t sma_length{40}, slope_lookback{4}, dmi_length{8}, adx_length{18};
    std::size_t ema_length{132}, atr_length{32}, line_confirm_bars{2};
    std::size_t session_start_minute{1080}, session_end_minute{1020};
    Decimal slope_flat_points{0.05L}, atr_multiplier{2};
};
struct Options
{
    std::size_t adx_length{40}, fast_ma_length{4}, guide_ma_length{20}, shock_atr_length{7};
    Decimal guide_deviation{0.5L}, start_bias_long{-0.5L}, start_bias_short{-0.4L};
    Decimal stop_bias_long{1}, stop_bias_short{1.4L};
    std::size_t cooldown_bars{2}, continuation_bars{5};
    Decimal shock_rise_atr{4.5L}, shock_armed_rise_atr{2}, shock_arm_down_atr{2.75L}, shock_range_atr{2.5L};
    Decimal chop_min_width_atr{0.6L}, chop_max_slope_atr{0.11L};
    bool exit_on_guide_cross_long_trend{true};
    MarketTrendSettings market;
    void validate() const;
    static Options from_json(std::string_view json);
};
} // namespace fig::collections::sadx2
