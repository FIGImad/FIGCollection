#include "fig/collections/ladx/options.hpp"
#include <cmath>
#include <map>
#include <regex>
#include <set>
namespace fig::collections::ladx
{
void Options::validate() const
{
    for (const auto n : {adx_length, fast_ma_length, guide_ma_length, shock_atr_length})
        if (!n || n > std::numeric_limits<std::size_t>::max() / 4)
            throw std::invalid_argument("Invalid LADX period");
    for (const auto v : {guide_deviation,
                         start_bias,
                         stop_bias,
                         reentry_shock_max,
                         shock_drop_atr,
                         shock_armed_drop_atr,
                         shock_arm_up_atr,
                         shock_range_atr,
                         chop_min_width_atr,
                         chop_max_slope_atr,
                         continuation_max_shock_atr})
        if (!std::isfinite(v))
            throw std::invalid_argument("LADX parameters must be finite");
    if (guide_deviation < 0.01L || start_bias < -10 || stop_bias < -100)
        throw std::invalid_argument("LADX parameter below EFS lower limit");
    for (const auto v : {reentry_shock_max,
                         shock_drop_atr,
                         shock_armed_drop_atr,
                         shock_arm_up_atr,
                         shock_range_atr,
                         chop_min_width_atr,
                         chop_max_slope_atr,
                         continuation_max_shock_atr})
        if (v < 0)
            throw std::invalid_argument("LADX parameter cannot be negative");
}

Options Options::from_json(std::string_view json)
{
    Options o;
    const std::map<std::string, std::size_t*> lengths{{"AdxLen", &o.adx_length},
                                                      {"FastMALen", &o.fast_ma_length},
                                                      {"GuideMALen", &o.guide_ma_length},
                                                      {"ShockATRLen", &o.shock_atr_length}};
    const std::map<std::string, std::size_t*> counters{{"ReEntryCooldownBars", &o.cooldown_bars},
                                                      {"ContinuationEntryBars", &o.continuation_bars}};
    const std::map<std::string, Decimal*> decimals{{"GuideStdDev", &o.guide_deviation},
                                                   {"StartBias", &o.start_bias},
                                                   {"StopBias", &o.stop_bias},
                                                   {"TrendReEntryShockMax", &o.reentry_shock_max},
                                                   {"ShockDropATR", &o.shock_drop_atr},
                                                   {"ShockArmedDropATR", &o.shock_armed_drop_atr},
                                                   {"ShockArmUpATR", &o.shock_arm_up_atr},
                                                   {"ShockRangeATR", &o.shock_range_atr},
                                                   {"ChopMinGuideWidthATR", &o.chop_min_width_atr},
                                                   {"ChopMaxGuideSlopeATR", &o.chop_max_slope_atr},
                                                   {"ContinuationMaxShockATR", &o.continuation_max_shock_atr}};
    std::string rest(json);
    auto trim = [&]()
    {
        const auto n = rest.find_first_not_of(" \t\r\n");
        rest.erase(0, n == std::string::npos ? rest.size() : n);
    };
    trim();
    if (rest.empty() || rest.front() != '{')
        throw std::invalid_argument("LADX parameters must be a JSON object");
    rest.erase(0, 1);
    trim();
    const std::regex field(
        R"json("([A-Za-z0-9_]+)"\s*:\s*(null|-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?))json");
    std::set<std::string> seen;
    while (!rest.empty() && rest.front() != '}')
    {
        std::smatch match;
        if (!std::regex_search(rest, match, field, std::regex_constants::match_continuous))
            throw std::invalid_argument("Expected a numeric LADX parameter");
        const auto name = match[1].str(), text = match[2].str();
        if (!seen.insert(name).second)
            throw std::invalid_argument("Duplicate LADX parameter: " + name);
        if (!lengths.contains(name) && !counters.contains(name) && !decimals.contains(name))
            throw std::invalid_argument("Unknown LADX parameter: " + name);
        if (text != "null")
        {
            const auto value = std::stold(text);
            if (!std::isfinite(value))
                throw std::invalid_argument("Invalid LADX parameter: " + name);
            if (decimals.contains(name))
                *decimals.at(name) = value;
            else
            {
                if (value < 0 || value > static_cast<Decimal>(std::numeric_limits<std::size_t>::max() / 4) ||
                    (lengths.contains(name) && std::floor(value) != value))
                    throw std::invalid_argument("Invalid bar count: " + name);
                // The signal class explicitly floors its bar-count settings in EFS.
                *(lengths.contains(name) ? lengths.at(name) : counters.at(name)) =
                    static_cast<std::size_t>(std::floor(value));
            }
        }
        rest.erase(0, static_cast<std::size_t>(match.length()));
        trim();
        if (!rest.empty() && rest.front() == ',')
        {
            rest.erase(0, 1);
            trim();
            if (rest.empty() || rest.front() == '}')
                throw std::invalid_argument("Trailing comma in LADX parameters");
        }
        else if (rest.empty() || rest.front() != '}')
            throw std::invalid_argument("Invalid LADX JSON separator");
    }
    if (rest.empty())
        throw std::invalid_argument("Unterminated LADX JSON object");
    rest.erase(0, 1);
    trim();
    if (!rest.empty())
        throw std::invalid_argument("Unexpected text after LADX parameters");
    o.validate();
    return o;
}
} // namespace fig::collections::ladx
