#pragma once
#include "fig/database/database.hpp"
#include <filesystem>

namespace fig
{
struct BacktestOptions
{
    int dataset{3}, interval_minutes{5}, maximum_source_rows{}, jobs{2};
    int quantity{15};
    // quantity > 0: fixed contracts; quantity == 0: EFS sizing from initial capital and entry HLC3.
    Decimal quantity_pct{1};
    Decimal capital{9255000}, multiplier{20}, commission{1.60L}, slippage_points{};
    bool force_close{};
    // UTC dates, exclusive upper boundaries. Earlier prices still warm the studies.
    std::string start_date{"2010-01-01"}, end_date, train_end{"2023-01-01"}, validation_end{"2025-01-01"};
    std::filesystem::path parameters_directory, output_directory;
    std::filesystem::path price_cache_directory;
    std::string price_cache_mode{"off"}, price_cache_source;
};
void run_backtest_batch(const MainRepository&, const BacktestOptions&);
void backtest_self_test();
} // namespace fig
