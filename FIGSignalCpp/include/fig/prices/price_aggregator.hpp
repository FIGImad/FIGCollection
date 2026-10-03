#pragma once
#include "fig/database/models.hpp"
#include <span>

namespace fig
{
// Ordered historical stream, one dataset per instance. Buckets use epoch-aligned start times.
class PriceAggregator final
{
  public:
    explicit PriceAggregator(int interval_seconds);
    [[nodiscard]] std::optional<PriceBar> process(const PriceBar& input);
    [[nodiscard]] std::optional<PriceBar> finish();

  private:
    int seconds_;
    std::optional<PriceBar> pending_;
    std::optional<PriceBar> previous_;
};
[[nodiscard]] std::vector<PriceBar> aggregate_prices(std::span<const PriceBar> prices, int interval_seconds);
void merge_price_range(std::vector<PriceBar>& destination, std::span<const PriceBar> updates);
} // namespace fig
