#include "fig/studies/study_adx_roc.hpp"
namespace fig::studies
{
StudyADXROC::StudyADXROC(std::size_t n, std::size_t depth) : lookback_(n), states_(depth)
{
    if (!n || n == std::numeric_limits<std::size_t>::max())
        throw std::invalid_argument("Invalid ADXROC lookback");
}
AdxRocValues StudyADXROC::process(std::int64_t time, std::optional<Decimal> value)
{
    auto s = states_.begin(time);
    s.value = {};
    if (value)
    {
        s.values.push_back(*value);
        if (s.values.size() > lookback_ + 1)
            s.values.pop_front();
        if (s.values.size() >= 2)
        {
            const auto previous = s.values[s.values.size() - 2];
            s.value.roc = *value - previous;
            if (previous != 0)
                s.value.percent = 100 * (*value - previous) / previous;
        }
        if (s.values.size() > lookback_)
            s.value.slope = (*value - s.values.front()) / static_cast<Decimal>(lookback_);
    }
    return states_.commit(std::move(s)).value;
}
} // namespace fig::studies
