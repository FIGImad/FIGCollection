#include "fig/collections/sadx2/options.hpp"
#include <cmath>
#include <functional>
#include <map>
#include <regex>
#include <set>
namespace fig::collections::sadx2
{
void Options::validate() const
{
    for (auto n : {adx_length,
                   fast_ma_length,
                   guide_ma_length,
                   shock_atr_length,
                   market.sma_length,
                   market.slope_lookback,
                   market.dmi_length,
                   market.adx_length,
                   market.ema_length,
                   market.atr_length,
                   market.line_confirm_bars})
        if (!n || n > 1000000)
            throw std::invalid_argument("Invalid SADX2 period (1..1000000)");
    for (auto v : {guide_deviation,
                   start_bias_long,
                   start_bias_short,
                   stop_bias_long,
                   stop_bias_short,
                   shock_rise_atr,
                   shock_armed_rise_atr,
                   shock_arm_down_atr,
                   shock_range_atr,
                   chop_min_width_atr,
                   chop_max_slope_atr,
                   market.slope_flat_points,
                   market.atr_multiplier})
        if (!std::isfinite(v))
            throw std::invalid_argument("SADX2 parameters must be finite");
    if (guide_deviation < 0.01L || start_bias_long < -10 || start_bias_short < -10 || stop_bias_long < -100 ||
        stop_bias_short < -100 || market.atr_multiplier <= 0)
        throw std::invalid_argument("SADX2 threshold below minimum");
    for (auto v : {shock_rise_atr,
                   shock_armed_rise_atr,
                   shock_arm_down_atr,
                   shock_range_atr,
                   chop_min_width_atr,
                   chop_max_slope_atr,
                   market.slope_flat_points})
        if (v < 0)
            throw std::invalid_argument("SADX2 threshold cannot be negative");
    if (market.timeframe != "daily" && market.timeframe != "hourly")
        throw std::invalid_argument("Invalid timeframe");
    if (market.time_zone != "UTC" && market.time_zone != "America/New_York")
        throw std::invalid_argument("Invalid timeZone");
    if (market.session_start_minute >= 1440 || market.session_end_minute >= 1440 ||
        (market.timeframe == "hourly" && (market.session_start_minute % 60 || market.session_end_minute % 60)))
        throw std::invalid_argument("Invalid market session boundaries");
}
Options Options::from_json(std::string_view json)
{
    Options o;
    const std::map<std::string, std::size_t*> counts{
        {"indicators.adxLength", &o.adx_length},
        {"indicators.priceMaLength", &o.fast_ma_length},
        {"indicators.guideMaLength", &o.guide_ma_length},
        {"indicators.shockAtrLength", &o.shock_atr_length},
        {"entry.reEntryCooldownBars", &o.cooldown_bars},
        {"entry.continuationEntryBars", &o.continuation_bars},
        {"marketTrendSettings.smaLength", &o.market.sma_length},
        {"marketTrendSettings.slopeLookback", &o.market.slope_lookback},
        {"marketTrendSettings.dmiLength", &o.market.dmi_length},
        {"marketTrendSettings.adxLength", &o.market.adx_length},
        {"marketTrendSettings.emaLength", &o.market.ema_length},
        {"marketTrendSettings.atrLength", &o.market.atr_length},
        {"marketTrendSettings.lineConfirmBars", &o.market.line_confirm_bars},
        {"marketTrendSettings.sessionStartMinute", &o.market.session_start_minute},
        {"marketTrendSettings.sessionEndMinute", &o.market.session_end_minute}};
    const std::map<std::string, Decimal*> numbers{{"indicators.guideStdDev", &o.guide_deviation},
                                                  {"entry.startBiasLongTrend", &o.start_bias_long},
                                                  {"entry.startBiasShortTrend", &o.start_bias_short},
                                                  {"exit.stopBiasLongTrend", &o.stop_bias_long},
                                                  {"exit.stopBiasShortTrend", &o.stop_bias_short},
                                                  {"entry.chopMinGuideWidthAtr", &o.chop_min_width_atr},
                                                  {"entry.chopMaxGuideSlopeAtr", &o.chop_max_slope_atr},
                                                  {"exit.shockArmDownAtr", &o.shock_arm_down_atr},
                                                  {"exit.shockArmedRiseAtr", &o.shock_armed_rise_atr},
                                                  {"exit.shockRiseAtr", &o.shock_rise_atr},
                                                  {"exit.shockRangeAtr", &o.shock_range_atr},
                                                  {"marketTrendSettings.slopeFlatPoints", &o.market.slope_flat_points},
                                                  {"marketTrendSettings.atrMultiplier", &o.market.atr_multiplier}};
    const std::map<std::string, std::string> aliases{{"AdxLen", "indicators.adxLength"},
                                                     {"FastMALen", "indicators.priceMaLength"},
                                                     {"PriceMALen", "indicators.priceMaLength"},
                                                     {"GuideMALen", "indicators.guideMaLength"},
                                                     {"GuideStdDev", "indicators.guideStdDev"},
                                                     {"ShockATRLen", "indicators.shockAtrLength"},
                                                     {"ReEntryCooldownBars", "entry.reEntryCooldownBars"},
                                                     {"ContinuationEntryBars", "entry.continuationEntryBars"},
                                                     {"ChopMinGuideWidthATR", "entry.chopMinGuideWidthAtr"},
                                                     {"ChopMaxGuideSlopeATR", "entry.chopMaxGuideSlopeAtr"},
                                                     {"ShockArmDownATR", "exit.shockArmDownAtr"},
                                                     {"ShockArmedRiseATR", "exit.shockArmedRiseAtr"},
                                                     {"ShockRiseATR", "exit.shockRiseAtr"},
                                                     {"ShockRangeATR", "exit.shockRangeAtr"},
                                                     {"startBiasLongTrend", "entry.startBiasLongTrend"},
                                                     {"startBiasShortTrend", "entry.startBiasShortTrend"},
                                                     {"stopBiasLongTrend", "exit.stopBiasLongTrend"},
                                                     {"stopBiasShortTrend", "exit.stopBiasShortTrend"}};
    std::set<std::string> seen;
    std::function<void(std::string, const std::string&)> assign = [&](std::string key, const std::string& value)
    {
        if (aliases.contains(key))
            key = aliases.at(key);
        if (key == "indicators.fastMaLength")
            key = "indicators.priceMaLength";
        if (key == "StartBias" || key == "entry.startBias")
        {
            assign("entry.startBiasLongTrend", value);
            assign("entry.startBiasShortTrend", value);
            return;
        }
        if (key == "StopBias" || key == "exit.stopBias")
        {
            assign("exit.stopBiasLongTrend", value);
            assign("exit.stopBiasShortTrend", value);
            return;
        }
        if (!seen.insert(key).second)
            throw std::invalid_argument("Duplicate SADX2 parameter: " + key);
        if (key == "marketTrendSettings.timeframe" || key == "marketTrendSettings.timeZone")
        {
            if (value.size() < 2 || value.front() != '"')
                throw std::invalid_argument("Expected string: " + key);
            (key.ends_with("timeframe") ? o.market.timeframe : o.market.time_zone) = value.substr(1, value.size() - 2);
        }
        else if (key == "exit.exitOnGuideCrossLongTrend")
        {
            if (value != "true" && value != "false")
                throw std::invalid_argument("Expected boolean: " + key);
            o.exit_on_guide_cross_long_trend = value == "true";
        }
        else
        {
            if (!counts.contains(key) && !numbers.contains(key))
                throw std::invalid_argument("Unknown SADX2 parameter: " + key);
            if (value == "null" || value == "true" || value == "false" || value.front() == '"')
                throw std::invalid_argument("Expected number: " + key);
            const auto v = std::stold(value);
            if (!std::isfinite(v))
                throw std::invalid_argument("Nonfinite SADX2 parameter");
            if (counts.contains(key))
            {
                if (v < 0 || v > 1000000 || std::floor(v) != v)
                    throw std::invalid_argument("Invalid integer: " + key);
                *counts.at(key) = static_cast<std::size_t>(v);
            }
            else
                *numbers.at(key) = v;
        }
    };
    std::string rest(json);
    const auto trim = [&]
    {
        auto n = rest.find_first_not_of(" \t\r\n");
        rest.erase(0, n == std::string::npos ? rest.size() : n);
    };
    const auto take = [&](char c)
    {
        trim();
        if (rest.empty() || rest.front() != c)
            throw std::invalid_argument("Invalid SADX2 JSON");
        rest.erase(0, 1);
    };
    const std::regex key_pattern(R"json("([A-Za-z0-9_]+)"\s*:)json");
    const std::regex value_pattern(
        R"json(("[^"\\]*"|true|false|null|-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?))json");
    std::set<std::string> groups;
    std::function<void(std::string)> object = [&](std::string prefix)
    {
        take('{');
        trim();
        if (!rest.empty() && rest.front() == '}')
        {
            take('}');
            return;
        }
        for (;;)
        {
            std::smatch match;
            trim();
            if (!std::regex_search(rest, match, key_pattern, std::regex_constants::match_continuous))
                throw std::invalid_argument("Expected JSON key");
            auto key = match[1].str();
            rest.erase(0, static_cast<std::size_t>(match.length()));
            trim();
            if (prefix.empty() && (key == "indicators" || key == "entry" || key == "exit" ||
                                   key == "marketTrendSettings" || key == "MarketTrendSettings"))
            {
                if (key == "MarketTrendSettings")
                    key = "marketTrendSettings";
                if (!groups.insert(key).second)
                    throw std::invalid_argument("Duplicate group: " + key);
                object(key + ".");
            }
            else
            {
                if (!std::regex_search(rest, match, value_pattern, std::regex_constants::match_continuous))
                    throw std::invalid_argument("Invalid JSON value");
                assign(prefix + key, match[1].str());
                rest.erase(0, static_cast<std::size_t>(match.length()));
            }
            trim();
            if (!rest.empty() && rest.front() == '}')
            {
                take('}');
                break;
            }
            take(',');
        }
    };
    object("");
    trim();
    if (!rest.empty())
        throw std::invalid_argument("Trailing JSON text");
    o.validate();
    return o;
}
} // namespace fig::collections::sadx2
