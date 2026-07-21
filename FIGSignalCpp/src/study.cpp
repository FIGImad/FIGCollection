#include "fig/study.hpp"
#include "fig/studies/study_col_adx_trend.hpp"
#include "fig/studies/study_ma.hpp"
#include "fig/studies/study_roc.hpp"
#include "fig/studies/study_wma.hpp"
#include <algorithm>
#include <cctype>
#include <regex>
#include <stdexcept>
namespace fig
{
static std::string normalized(std::string v)
{
    std::ranges::transform(v, v.begin(), [](unsigned char c) { return static_cast<char>(std::toupper(c)); });
    return v;
}
void StudyRegistry::add(std::string type, Factory factory)
{
    if (!factory || !factories_.emplace(normalized(std::move(type)), std::move(factory)).second)
        throw std::invalid_argument("Duplicate or invalid collection registration");
}
std::unique_ptr<StudyCollection> StudyRegistry::create(const StudyCollectionConfig& c,
                                                       const Ticker& t,
                                                       const Interval& i) const
{
    const auto found = factories_.find(normalized(c.type));
    if (found == factories_.end())
        throw std::invalid_argument("Unsupported integrated ColType: " + c.type);
    return found->second(c, t, i);
}
std::vector<std::string> StudyRegistry::types() const
{
    std::vector<std::string> r;
    for (const auto& [key, unused] : factories_)
    {
        (void)unused;
        r.push_back(key);
    }
    std::ranges::sort(r);
    return r;
}
std::vector<StudyBar> StudyCollection::replay(std::span<const PriceBar> prices)
{
    std::vector<StudyBar> result;
    result.reserve(prices.size());
    for (const auto& bar : prices)
        result.push_back(process(bar));
    return result;
}
std::vector<std::optional<Decimal>> simple_moving_average(std::span<const Decimal> v, std::size_t n)
{
    studies::StudyMA s(n);
    std::vector<std::optional<Decimal>> r;
    r.reserve(v.size());
    for (std::size_t i{}; i < v.size(); ++i)
        r.push_back(s.process(PriceBar{.raw_time = static_cast<std::int64_t>(i), .close = v[i]}));
    return r;
}
std::vector<std::optional<Decimal>> weighted_moving_average(std::span<const Decimal> v, std::size_t n)
{
    studies::StudyWMA s(n);
    std::vector<std::optional<Decimal>> r;
    r.reserve(v.size());
    for (std::size_t i{}; i < v.size(); ++i)
        r.push_back(s.process(PriceBar{.raw_time = static_cast<std::int64_t>(i), .close = v[i]}));
    return r;
}
std::vector<std::optional<Decimal>> rate_of_change(std::span<const Decimal> v, std::size_t n)
{
    studies::StudyROC s(n);
    std::vector<std::optional<Decimal>> r;
    r.reserve(v.size());
    for (std::size_t i{}; i < v.size(); ++i)
        r.push_back(s.process(static_cast<std::int64_t>(i), v[i]));
    return r;
}
static Decimal numeric_option(std::string_view json, std::string_view key)
{
    const std::regex expression("\\\"" + std::string(key) + "\\\"\\s*:\\s*(-?[0-9]+(?:\\.[0-9]+)?)", std::regex::icase);
    const std::string input(json);
    std::smatch match;
    if (!std::regex_search(input, match, expression))
        throw std::invalid_argument("Missing ADXTrend option: " + std::string(key));
    return std::stold(match[1].str());
}
void register_integrated_collections(StudyRegistry& registry)
{
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
