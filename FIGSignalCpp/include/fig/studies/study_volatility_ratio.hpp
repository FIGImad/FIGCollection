#pragma once
#include "fig/studies/study_atr.hpp"
namespace fig::studies
{
struct VolatilityRatioValues
{
    Decimal short_atr{}, long_atr{}, ratio{};
    int state{};
};
class StudyVolatilityRatio final
{
  public:
    StudyVolatilityRatio(std::size_t short_length = 5,
                         std::size_t long_length = 48,
                         std::size_t depth = default_revision_depth)
        : short_(short_length, depth), long_(long_length, depth)
    {
    }
    [[nodiscard]] VolatilityRatioValues process(const PriceBar&);

  private:
    StudyATR short_, long_;
};
} // namespace fig::studies
