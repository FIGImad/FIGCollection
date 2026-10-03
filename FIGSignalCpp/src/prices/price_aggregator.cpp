#include "fig/prices/price_aggregator.hpp"
#include <stdexcept>

namespace fig
{
PriceAggregator::PriceAggregator(int interval_seconds) : seconds_(interval_seconds)
{
    if (interval_seconds < 60 || interval_seconds % 60 != 0)
        throw std::invalid_argument("Interval must be a positive whole number of minutes");
}
std::optional<PriceBar> PriceAggregator::process(const PriceBar& input)
{
    if (previous_ && (input.raw_time <= previous_->raw_time || input.data_set_id != previous_->data_set_id))
        throw std::invalid_argument("Aggregation requires strictly increasing timestamps from one dataset");
    const auto remainder = (input.raw_time % seconds_ + seconds_) % seconds_;
    const auto bucket = input.raw_time - remainder;
    std::optional<PriceBar> completed;
    if (!pending_ || pending_->raw_time != bucket)
    {
        completed = pending_;
        pending_ = input;
        pending_->id = -1;
        pending_->raw_time = bucket;
    }
    else
    {
        if (input.high > pending_->high)
            pending_->high = input.high;
        if (input.low < pending_->low)
            pending_->low = input.low;
        pending_->close = input.close;
        pending_->volume += input.volume;
    }
    previous_ = input;
    return completed;
}
std::optional<PriceBar> PriceAggregator::finish()
{
    auto result = pending_;
    pending_.reset();
    return result;
}
std::vector<PriceBar> aggregate_prices(std::span<const PriceBar> prices, int interval_seconds)
{
    PriceAggregator aggregator(interval_seconds);
    std::vector<PriceBar> result;
    for (const auto& input : prices)
    {
        if (const auto bar = aggregator.process(input))
            result.push_back(*bar);
    }
    if (const auto bar = aggregator.finish())
        result.push_back(*bar);
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
