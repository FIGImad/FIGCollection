#pragma once
#include "fig/studies/live_state.hpp"
namespace fig::studies
{
struct AdxValues
{
    std::optional<Decimal> adx, pdi, ndi;
};
class StudyADX final
{
  public:
    StudyADX(std::size_t length, std::size_t smoothing, std::size_t revision_depth = 10);
    [[nodiscard]] AdxValues process(const PriceBar& bar);

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::optional<PriceBar> previous;
        std::size_t dm_count{}, dx_count{};
        Decimal tr_sum{}, pdm_sum{}, ndm_sum{}, dx_sum{};
        std::optional<Decimal> last_adx;
        AdxValues value;
    };
    std::size_t length_, smoothing_;
    LiveStateHistory<State> states_;
};
} // namespace fig::studies
