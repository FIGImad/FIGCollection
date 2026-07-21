#pragma once
#include "fig/models.hpp"
#include <span>

namespace fig
{
[[nodiscard]] std::vector<PriceBar> aggregate_prices(std::span<const PriceBar> prices, int interval_seconds);
void merge_price_range(std::vector<PriceBar>& destination, std::span<const PriceBar> updates);
} // namespace fig
