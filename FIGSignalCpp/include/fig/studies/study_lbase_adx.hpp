#pragma once
#include "fig/studies/live_state.hpp"
namespace fig::studies
{
struct LBaseAdxInput
{
    std::optional<Decimal> adx, pdi, ndi, guide_upper, guide_lower, guide_middle, price_ma;
};
struct LBaseAdxValues
{
    Decimal side{}, price_ma{}, guide_ma{}, guide_upper{}, guide_lower{}, adx_bias{}, adx{};
    int bars_since_close{};
};
class StudyLBaseADXEx final
{
  public:
    StudyLBaseADXEx(Decimal trigger, Decimal bias_exit = -0.75L, std::size_t revision_depth = 10);
    [[nodiscard]] LBaseAdxValues process(std::int64_t raw_time, const LBaseAdxInput&);

  private:
    struct State
    {
        std::int64_t raw_time{};
        Decimal side{};
        int bars_since_close{};
        LBaseAdxValues value;
    };
    Decimal trigger_, bias_exit_;
    LiveStateHistory<State> states_;
};
} // namespace fig::studies
