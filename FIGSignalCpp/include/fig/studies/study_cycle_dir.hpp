#pragma once
#include "fig/studies/study_bb.hpp"
namespace fig::studies
{
struct CycleDirValues
{
    int direction{1};
    Decimal high{}, low{};
};
class StudyCycleDir final
{
  public:
    explicit StudyCycleDir(std::size_t depth = default_revision_depth) : states_(depth)
    {
    }
    [[nodiscard]] CycleDirValues process(const PriceBar&, const BollingerBands&);

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::optional<Decimal> previous_close;
        CycleDirValues value;
    };
    LiveStateHistory<State> states_;
};
} // namespace fig::studies
