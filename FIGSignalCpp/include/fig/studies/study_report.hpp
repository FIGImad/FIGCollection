#pragma once
#include "fig/database/database.hpp"
#include "fig/studies/study_col_adx_trend.hpp"
#include "fig/studies/study_extended.hpp"
#include "fig/studies/study_roc.hpp"
#include "fig/studies/study_sem.hpp"
#include "fig/studies/study_slope_trend.hpp"
#include "fig/studies/study_wma.hpp"
#include <iosfwd>

namespace fig
{
struct StudyReportOptions
{
    std::size_t period{14};
    std::size_t adx_smoothing{14};
    std::string source{"close"};
    int interval_minutes{1};
};

// One instance per dataset. Uses the same timestamp/revision rules as individual studies.
class StudyCalculator final
{
  public:
    explicit StudyCalculator(const StudyReportOptions& options = {});
    [[nodiscard]] StudyBar process(const PriceBar& bar);

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

// Empty optional values are emitted as empty CSV fields (study warmup).
// maximum limits source rows before aggregation; return value counts emitted study bars.
std::size_t write_study_report(const MainRepository& repository,
                               int data_set_id,
                               const StudyReportOptions& options,
                               std::ostream& output,
                               int maximum = 0,
                               const StudyCollectionConfig* collection = nullptr);
void study_report_self_test();
} // namespace fig
