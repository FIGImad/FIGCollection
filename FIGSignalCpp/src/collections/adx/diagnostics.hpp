#pragma once
#include "fig/studies/study_col_adx_trend.hpp"
#include "fig/studies/study_extended.hpp"
#include "fig/studies/study_report.hpp"
#include "fig/studies/study_roc.hpp"
#include "fig/studies/study_sem.hpp"
#include "fig/studies/study_slope_trend.hpp"
#include "fig/studies/study_wma.hpp"
#include <iosfwd>

namespace fig
{
class AdxDiagnostics final : public StudyCollection
{
  public:
    explicit AdxDiagnostics(const StudyReportOptions& options = {});
    [[nodiscard]] StudyBar process(const PriceBar& bar) override;
    std::string_view type() const noexcept override
    {
        return "ADXDiagnostics";
    }
    std::size_t maximum_lookback() const noexcept override
    {
        return trend_.maximum_lookback();
    }

  private:
    std::string source_;
    studies::StudyMA ma_;
    studies::StudyWMA wma_;
    studies::StudySEM sem_;
    studies::StudyBB bb_;
    studies::StudyExtended extended_;
    studies::StudyADX adx_;
    studies::StudyROC roc_;
    studies::StudySlopeTrend slope_;
    studies::StudyColADXTrend trend_;
};

void register_adx_diagnostics(StudyRegistry&);
} // namespace fig
