#pragma once
#include "fig/database/database.hpp"
#include "fig/studies/study.hpp"
#include <filesystem>
#include <iosfwd>

namespace fig
{
struct StudyReportOptions
{
    std::size_t period{14};
    std::size_t adx_smoothing{14};
    std::string source{"close"};
    int interval_minutes{1};
    std::filesystem::path collection_dll;
};

// One instance per dataset. Uses the same timestamp/revision rules as individual studies.
class StudyCalculator final
{
  public:
    explicit StudyCalculator(const StudyReportOptions& options = {});
    [[nodiscard]] StudyBar process(const PriceBar& bar);

  private:
    std::unique_ptr<StudyCollection> collection_;
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
