#include "fig/studies/study_cycle_dir.hpp"
#include <algorithm>
namespace fig::studies
{
CycleDirValues StudyCycleDir::process(const PriceBar& bar, const BollingerBands& bands)
{
    auto s = states_.begin(bar.raw_time);
    const auto previous = s.value;
    if (!bands.upper.value_or(0) || !bands.lower.value_or(0) || !bands.middle.value_or(0))
        s.value = {1, bar.high, bar.low};
    else
    {
        const bool upper = previous.direction == 1 && bar.high >= *bands.upper &&
                           ((s.previous_close && *s.previous_close > *bands.middle) || bar.close > *bands.middle);
        const bool lower = previous.direction == -1 && bar.low <= *bands.lower &&
                           ((s.previous_close && *s.previous_close < *bands.middle) || bar.close < *bands.middle);
        s.value.direction = upper || lower ? -previous.direction : previous.direction;
        if (s.value.direction == -1)
        {
            s.value.high = std::max(bar.high, previous.high);
            if (s.value.high > previous.high || (s.value.high == previous.high && bar.low < previous.low))
                s.value.low = bar.low;
        }
        else
        {
            s.value.low = std::min(bar.low, previous.low);
            if (s.value.low < previous.low || (s.value.low == previous.low && bar.high > previous.high))
                s.value.high = bar.high;
        }
        if (s.value.direction != previous.direction)
        {
            s.value.high = bar.high;
            s.value.low = bar.low;
        }
    }
    s.previous_close = bar.close;
    return states_.commit(std::move(s)).value;
}
} // namespace fig::studies
