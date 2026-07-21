#pragma once
#include "fig/studies/study_adx.hpp"
#include "fig/studies/study_bb.hpp"
#include "fig/studies/study_lbase_adx.hpp"
#include "fig/studies/study_ma.hpp"
#include "fig/study.hpp"
namespace fig::studies
{
struct AdxTrendOptions
{
    std::size_t price_ma_length{}, guide_ma_length{};
    Decimal guide_deviation{};
    std::size_t adx_length{}, adx_smoothing{};
    Decimal adx_trigger{};
};
class StudyColADXTrend final : public StudyCollection
{
  public:
    explicit StudyColADXTrend(AdxTrendOptions options, std::size_t revision_depth = 10);
    [[nodiscard]] std::string_view type() const noexcept override
    {
        return "ADXTrend";
    }
    [[nodiscard]] std::size_t maximum_lookback() const noexcept override
    {
        return maximum_lookback_;
    }
    [[nodiscard]] StudyBar process(const PriceBar& bar) override;

  private:
    std::size_t maximum_lookback_{};
    StudyMA price_ma_, guide_ma_;
    StudyBB guide_bands_;
    StudyADX adx_;
    StudyLBaseADXEx signal_;
};
} // namespace fig::studies
