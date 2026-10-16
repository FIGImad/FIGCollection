#include "diagnostics.hpp"
#include "fig/studies/collection_plugin.hpp"
#include "fig/studies/study_col_adx_base.hpp"
#include "fig/studies/study_col_adx_trend.hpp"
#include "registration.hpp"
#include <regex>

namespace fig
{
static Decimal numeric_option(std::string_view json, std::string_view key)
{
    const std::regex expression("\\\"" + std::string(key) + "\\\"\\s*:\\s*(-?[0-9]+(?:\\.[0-9]+)?)", std::regex::icase);
    const std::string input(json);
    std::smatch match;
    if (!std::regex_search(input, match, expression))
        throw std::invalid_argument("Missing ADXTrend option: " + std::string(key));
    return std::stold(match[1].str());
}
void register_adx_collections(StudyRegistry& registry)
{
    register_adx_diagnostics(registry);
    registry.add(
        "ADXBase",
        [](const StudyCollectionConfig& c, const Ticker&, const Interval&)
        { return std::make_unique<studies::StudyColADXBase>(studies::AdxBaseOptions::from_json(c.parameters)); });
    registry.add("ADXTrend",
                 [](const StudyCollectionConfig& c, const Ticker&, const Interval&)
                 {
                     studies::AdxTrendOptions o;
                     o.price_ma_length = static_cast<std::size_t>(numeric_option(c.parameters, "priceMALen"));
                     o.guide_ma_length = static_cast<std::size_t>(numeric_option(c.parameters, "guideMALen"));
                     o.guide_deviation = numeric_option(c.parameters, "guideStdDev");
                     o.adx_length = static_cast<std::size_t>(numeric_option(c.parameters, "adxLen"));
                     o.adx_smoothing = static_cast<std::size_t>(numeric_option(c.parameters, "adxSmooth"));
                     o.adx_trigger = numeric_option(c.parameters, "adxTrigger");
                     return std::make_unique<studies::StudyColADXTrend>(o);
                 });
}
} // namespace fig

extern "C" __declspec(dllexport) const fig::CollectionPluginApi* __cdecl FIGGetCollectionPlugin() noexcept
{
    static const fig::CollectionPluginApi api{.name = "ADX",
                                              .register_collections = fig::register_adx_collections,
                                              .self_test = fig::adx_collection_self_test};
    return &api;
}
