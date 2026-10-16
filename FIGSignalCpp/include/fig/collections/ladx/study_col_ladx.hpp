#pragma once
#include "fig/collections/ladx/study_ladx.hpp"
#include "fig/studies/study.hpp"
#include "fig/studies/study_bb.hpp"

namespace fig::collections::ladx
{
class StudyColLADX final : public StudyCollection
{
  public:
    explicit StudyColLADX(Options options = {}, std::size_t depth = studies::default_revision_depth);
    [[nodiscard]] std::string_view type() const noexcept override
    {
        return "LADX";
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
    StudyLADX signal_;
};
void register_collections(StudyRegistry& registry);
void self_test();
} // namespace fig::collections::ladx
