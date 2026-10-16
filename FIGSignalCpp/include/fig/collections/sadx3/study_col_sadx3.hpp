#pragma once
#include "fig/collections/sadx3/study_sadx3.hpp"
#include "fig/studies/study.hpp"
#include "fig/studies/study_bb.hpp"
#include "fig/studies/study_ma.hpp"

namespace fig::collections::sadx3
{
class StudyColSADX3 final : public StudyCollection
{
  public:
    explicit StudyColSADX3(Options options = {},
                           std::size_t depth = studies::default_revision_depth,
                           int interval_seconds = 300);
    [[nodiscard]] std::string_view type() const noexcept override
    {
        return "SADX3";
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
    StudySADX3 signal_;
};
void register_collections(StudyRegistry& registry);
void self_test();
} // namespace fig::collections::sadx3

