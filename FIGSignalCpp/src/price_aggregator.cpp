#include "fig/price_aggregator.hpp"
#include <stdexcept>

namespace fig
{
std::vector<PriceBar> aggregate_prices(std::span<const PriceBar> prices, int interval_seconds)
{
    if (interval_seconds < 60 || interval_seconds % 60 != 0)
        throw std::invalid_argument("Interval must be a positive whole number of minutes");
    std::vector<PriceBar> result;
    for (const auto& input : prices)
    {
        const auto bucket = input.raw_time - input.raw_time % interval_seconds;
        if (result.empty() || result.back().raw_time != bucket)
        {
            auto bar = input;
            bar.id = -1;
            bar.raw_time = bucket;
            result.push_back(bar);
        }
        else
        {
            auto& bar = result.back();
            if (input.high > bar.high)
                bar.high = input.high;
            if (input.low < bar.low)
                bar.low = input.low;
            bar.close = input.close;
            bar.volume += input.volume;
        }
    }
    return result;
}

void merge_price_range(std::vector<PriceBar>& destination, std::span<const PriceBar> updates)
{
    for (const auto& update : updates)
    {
        if (destination.empty() || update.raw_time > destination.back().raw_time)
        {
            destination.push_back(update);
            continue;
        }
        for (auto it = destination.rbegin(); it != destination.rend(); ++it)
        {
            if (it->raw_time == update.raw_time)
            {
                *it = update;
                break;
            }
            if (it->raw_time < update.raw_time)
                break;
        }
    }
}
} // namespace fig
