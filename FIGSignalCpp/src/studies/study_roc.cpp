#include "fig/studies/study_roc.hpp"
#include <cmath>
namespace fig::studies
{
StudyROC::StudyROC(std::size_t length, std::size_t depth) : length_(length), states_(depth)
{
    if (!length_)
        throw std::invalid_argument("ROC length cannot be zero");
}
std::optional<Decimal> StudyROC::process(std::int64_t raw_time, Decimal value)
{
    auto s = states_.begin(raw_time);
    s.values.push_back(value);
    if (s.values.size() > length_)
        s.values.pop_front();
    const auto earliest = s.values.front(), current = s.values.back();
    Decimal multiplier{};
    if (current > 0)
        multiplier = std::pow(10.0L, std::ceil(std::log10(current))) / 10.0L;
    s.value =
        current != 0 ? std::optional<Decimal>((earliest - current) * multiplier / current) : std::optional<Decimal>(0);
    return states_.commit(std::move(s)).value;
}
} // namespace fig::studies
