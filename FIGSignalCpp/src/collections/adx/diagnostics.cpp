#include "diagnostics.hpp"
#include <regex>
#include <stdexcept>
namespace fig
{
AdxDiagnostics::AdxDiagnostics(const StudyReportOptions& o)
    : source_(o.source), ma_(o.period, o.source), wma_(o.period, o.source), sem_(o.period, o.source),
      bb_(o.period, 2, o.source), extended_(o.period, o.source), adx_(o.period, o.adx_smoothing), roc_(o.period),
      slope_(3, 0.5L), trend_({o.period, o.period, 2, o.period, o.adx_smoothing, 20})
{
    (void)studies::source_value(PriceBar{}, o.source); // Validate before any database/output work.
}

StudyBar AdxDiagnostics::process(const PriceBar& bar)
{
    StudyBar result{bar, {}};
    auto& v = result.studies;
    const auto ma = ma_.process(bar);
    v["MA"] = ma;
    v["WMA"] = wma_.process(bar);
    v["SEM"] = sem_.process(bar);
    v["ROC"] = roc_.process(bar.raw_time, studies::source_value(bar, source_));
    const auto bb = bb_.process(bar);
    v["BB_Middle"] = bb.middle;
    v["BB_Upper"] = bb.upper;
    v["BB_Lower"] = bb.lower;
    const auto e = extended_.process(bar);
    v["Extended_MA"] = e.moving_average;
    v["Extended_SEM"] = e.sem;
    v["Extended_Lower1"] = e.lower1;
    v["Extended_Upper1"] = e.upper1;
    v["Extended_Lower2"] = e.lower2;
    v["Extended_Upper2"] = e.upper2;
    const auto a = adx_.process(bar);
    v["ADX"] = a.adx;
    v["PDI"] = a.pdi;
    v["NDI"] = a.ndi;
    const auto s = slope_.process(bar.raw_time, ma);
    // The underlying slope study uses zero for missing inputs; keep warmup blank in the report.
    v["Slope_Direction"] = ma ? std::optional<Decimal>(s.direction) : std::nullopt;
    v["Slope_PerBar"] = ma ? std::optional<Decimal>(s.slope_per_bar) : std::nullopt;
    for (const auto& [name, value] : trend_.process(bar).studies)
        v["ADXTrend_" + name] = value;
    return result;
}

void register_adx_diagnostics(StudyRegistry& registry)
{
    registry.add("ADXDiagnostics",
                 [](const StudyCollectionConfig& config, const Ticker&, const Interval&)
                 {
                     // Host-generated options for the legacy all-studies CSV report.
                     const std::regex pattern(
                         R"json(\{"period":([0-9]+),"adx_smoothing":([0-9]+),"source":"([a-z0-9]+)"\})json");
                     std::smatch match;
                     if (!std::regex_match(config.parameters, match, pattern))
                         throw std::invalid_argument("ADXDiagnostics requires period, adx_smoothing and source");
                     return std::make_unique<AdxDiagnostics>(
                         StudyReportOptions{static_cast<std::size_t>(std::stoull(match[1].str())),
                                            static_cast<std::size_t>(std::stoull(match[2].str())),
                                            match[3].str()});
                 });
}
} // namespace fig
