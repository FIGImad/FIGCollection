#include "fig/studies/study_volatility_ratio.hpp"
namespace fig::studies
{
VolatilityRatioValues StudyVolatilityRatio::process(const PriceBar& bar)
{
    const auto a = short_.process(bar), b = long_.process(bar);
    VolatilityRatioValues v{a.value_or(0), b.value_or(0), 0, 0};
    if (a && b && *b != 0)
    {
        v.ratio = *a / *b;
        v.state = v.ratio >= 1.20L ? 1 : v.ratio <= 0.75L ? -1 : 0;
    }
    return v;
}
} // namespace fig::studies
