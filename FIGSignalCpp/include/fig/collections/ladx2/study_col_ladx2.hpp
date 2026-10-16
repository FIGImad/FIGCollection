#pragma once
#include "fig/collections/ladx2/market_trend.hpp"
#include "fig/collections/ladx2/study_ladx2.hpp"
#include "fig/studies/study.hpp"
#include "fig/studies/study_bb.hpp"

namespace fig::collections::ladx2
{
class StudyColLADX2 final : public StudyCollection
{
  public:
    explicit StudyColLADX2(Options options = {},
                           std::size_t depth = studies::default_revision_depth,
                           int interval_seconds = 300);
    [[nodiscard]] std::string_view type() const noexcept override
    {
        return "LADX2";
    }
    [[nodiscard]] std::size_t maximum_lookback() const noexcept override
    {
        return lookback_;
    }
    [[nodiscard]] StudyBar process(const PriceBar& bar) override;

  private:
    std::size_t lookback_, fast_period_, guide_period_;
    studies::SharedMovingAverages averages_;
    studies::StudyBB guide_;
    studies::StudyADX active_;
    StudyPriceShock shock_;
    studies::StudySMASlope guide_slope_;
    StudyLADX2 signal_;
    MarketTrend market_;
};
void register_collections(StudyRegistry& registry);
void self_test();
} // namespace fig::collections::ladx2
