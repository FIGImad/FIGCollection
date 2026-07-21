#pragma once

#include <cstdint>
#include <map>
#include <optional>
#include <string>
#include <vector>

namespace fig
{
using Decimal = long double;
using StudyValues = std::map<std::string, std::optional<Decimal>, std::less<>>;

struct Ticker
{
    int id{-1};
    std::string symbol;
    std::string local_symbol;
    Decimal min_tick{0.01L};
    int period_multiplier{92};
};
struct Interval
{
    std::string id;
    std::string name;
    int sequence{};
    int length_seconds{};
};
struct DataSet
{
    int id{-1};
    int ticker_id{-1};
    std::string interval_id;
};
struct StudyCollectionConfig
{
    int id{-1};
    int ticker_id{-1};
    std::string interval_id;
    std::string type;
    int order_sequence{};
    std::string parameters{"{}"};
    bool enabled{true};
};

struct PriceBar
{
    int id{-1};
    int data_set_id{-1};
    std::int64_t raw_time{};
    Decimal open{};
    Decimal high{};
    Decimal low{};
    Decimal close{};
    std::int64_t volume{};
};

struct StudyBar
{
    PriceBar price;
    StudyValues studies;
};
struct StudyHistory
{
    int id{-1};
    int study_collection_id{-1};
    PriceBar price;
    std::string studies_json;
    StudyValues studies;
};

struct StrategyConfig
{
    int id{-1};
    int study_collection_id{-1};
    std::string name;
    std::string strategy;
    std::string entry_signal;
    std::string exit_signal;
    std::string cancel_signal;
    int direction{};
    bool enabled{true};
};

struct Signal
{
    int id{-1};
    int strategy_config_id{-1};
    std::string strategy_name;
    std::int64_t entry_raw_time{};
    std::optional<std::int64_t> exit_raw_time;
    Decimal entry_price{};
    std::optional<Decimal> exit_price;
    int direction{};
    std::string status;
    std::string details;
};
} // namespace fig
