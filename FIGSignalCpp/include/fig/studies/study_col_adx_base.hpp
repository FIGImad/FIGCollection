#pragma once
#include "fig/studies/study.hpp"
#include "fig/studies/study_base_adx.hpp"
#include "fig/studies/study_bb.hpp"
namespace fig::studies
{
class StudyColADXBase final : public StudyCollection
{
  public:
    explicit StudyColADXBase(AdxBaseOptions options = {}, std::size_t depth = default_revision_depth);
    [[nodiscard]] std::string_view type() const noexcept override
    {
        return "ADXBase";
    }
    [[nodiscard]] std::size_t maximum_lookback() const noexcept override
    {
        return lookback_;
    }
    [[nodiscard]] StudyBar process(const PriceBar&) override;

  private:
    std::size_t lookback_;
    std::size_t fast_period_, slow_period_, guide_period_;
    SharedMovingAverages moving_averages_;
    StudyBB guide_;
    StudyADX active_, confirm_;
    StudyADXROC roc_;
    StudyPriceShock shock_;
    StudySMASlope conf_slope_, guide_slope_;
    StudyLBaseADX long_signal_;
    StudySBaseADX short_signal_;
};
} // namespace fig::studies
