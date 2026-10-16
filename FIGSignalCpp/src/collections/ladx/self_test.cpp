#include "fig/collections/ladx/study_col_ladx.hpp"
#include <cmath>
#include <limits>
#include <stdexcept>

namespace fig::collections::ladx
{
namespace
{
void require(bool value, const char* message)
{
    if (!value)
        throw std::runtime_error(message);
}
bool same(const StudyBar& a, const StudyBar& b)
{
    return a.studies == b.studies && a.text_studies == b.text_studies;
}
void parameters_test()
{
    const auto defaults = Options::from_json("{}");
    require(defaults.fast_ma_length == 4 && defaults.guide_ma_length == 8 && defaults.shock_atr_length == 14 &&
                defaults.start_bias == 1 && defaults.stop_bias == -0.75L,
            "LADX constructor defaults differ from EFS");
    const auto parsed =
        Options::from_json(R"({"AdxLen":3,"FastMALen":null,"ReEntryCooldownBars":2.9,"StartBias":2e0})");
    require(parsed.adx_length == 3 && parsed.fast_ma_length == 4 && parsed.cooldown_bars == 2 && parsed.start_bias == 2,
            "LADX parameter parsing failed");
    for (const auto* json : {"[]",
                             "{",
                             "{} extra",
                             R"({"AdxLen":0})",
                             R"({"AdxLen":2.5})",
                             R"({"StartBias":-11})",
                             R"({"StopBias":-101})",
                             R"({"GuideStdDev":0})",
                             R"({"ShockDropATR":-1})",
                             R"({"ReEntryCooldownBars":-1})",
                             R"({"AdxLen":2,})",
                             R"({"AdxLen":2,"AdxLen":3})",
                             R"({"AdxLenConf":3})",
                             R"({"ShockATRLen":"3"})"})
    {
        bool rejected{};
        try
        {
            (void)Options::from_json(json);
        }
        catch (const std::invalid_argument&)
        {
            rejected = true;
        }
        require(rejected, "LADX accepted invalid parameters");
    }
    auto invalid = defaults;
    invalid.shock_drop_atr = std::numeric_limits<Decimal>::quiet_NaN();
    bool rejected{};
    try
    {
        invalid.validate();
    }
    catch (const std::invalid_argument&)
    {
        rejected = true;
    }
    require(rejected, "LADX accepted a non-finite parameter");
}
LADXInput input(std::int64_t time)
{
    LADXInput i;
    i.price = {.raw_time = time, .open = 101, .high = 103, .low = 100, .close = 102};
    i.active = {25, 30, 20, 10};
    i.price_ma = 102;
    i.guide_middle = 100;
    i.guide_upper = 101;
    i.guide_lower = 99;
    i.shock.atr = 2;
    i.shock.score = 0.5L;
    i.guide_slope = {100, 0.2L, 1};
    return i;
}
void signal_test()
{
    Options o;
    o.chop_min_width_atr = 0;
    o.shock_drop_atr = 4;
    o.shock_armed_drop_atr = 2;
    o.shock_arm_up_atr = 3;
    o.shock_range_atr = 3;
    const auto side = [](const StudyBar& bar) { return bar.studies.at("LADX_SIDE"); };
    StudyLADX trend(o);
    require(side(trend.process(input(1))) == 0, "LADX entered without previous price MA");
    auto result = trend.process(input(2));
    require(side(result) == 1.1L && result.studies.at("LADX_LongSide") == 1, "LADX trend entry failed");
    auto armed = input(3);
    armed.shock.up_atr = 4;
    require(trend.process(armed).studies.at("LADX_MaxUpATR") == 4, "LADX arming failed");
    auto stop = input(4);
    stop.active.pdi = 10;
    stop.shock.score = 5;
    result = trend.process(stop);
    require(side(result) == 0 && result.text_studies.at("LADX_StopReason") == "ADX_BIAS",
            "LADX stop priority failed");
    require(result.studies.at("LADX_MaxUpATR") == 0 &&
                result.studies.at("LADX_BarsSinceExit") == 0 &&
                result.studies.at("LADX_GuideWidthATR") == 0 &&
                result.text_studies.at("LADX_LastExitReason").empty(),
            "LADX did not preserve the supplied EFS exit/reporting behavior");
    auto entry = input(5);
    entry.shock.up_atr = 10; // Entry-bar shock does not arm the new trade.
    result = trend.process(entry);
    require(result.studies.at("LADX_MaxUpATR") == 0, "LADX new entry retained armed state");
    require(side(result) == 1.1L && result.text_studies.at("LADX_StopReason") == "ADX_BIAS",
            "LADX EFS re-entry/retained reason behavior changed");
    stop = input(6);
    stop.shock.score = 2;
    result = trend.process(stop);
    require(side(result) == 1.1L && result.studies.at("LADX_MaxUpATR") == 0,
            "LADX previous trade armed a new trade");
    require(same(result, trend.process(stop)), "LADX repeated unarmed bar changed state");
    stop.price.raw_time = 7;
    stop.shock.up_atr = 3; // Equality arms and can trigger the exit on this same bar.
    result = trend.process(stop);
    require(side(result) == 0 && result.text_studies.at("LADX_StopReason") == "ARMED_DROP" &&
                result.studies.at("LADX_MaxUpATR") == 0,
            "LADX per-trade armed exit/reset failed");
    require(same(result, trend.process(stop)), "LADX repeated stop changed state");
    auto revised = stop;
    revised.shock.up_atr = 2.9L;
    result = trend.process(revised);
    require(side(result) == 1.1L && result.studies.at("LADX_MaxUpATR") == 2.9L,
            "LADX revised bar retained stale armed exit");
    result = trend.process(stop);
    require(side(result) == 0 && result.studies.at("LADX_MaxUpATR") == 0,
            "LADX restored armed exit differs from replay");

    for (const bool range : {false, true})
    {
        StudyLADX signal(o);
        (void)signal.process(input(1));
        (void)signal.process(input(2));
        stop = input(3);
        stop.shock.score = range ? 0.5L : 4;
        stop.shock.range_atr = 3;
        stop.shock.down_atr = 0.1L;
        result = signal.process(stop);
        require(side(result) == 0 &&
                    result.text_studies.at("LADX_StopReason") == (range ? "SHOCK_RANGE" : "SHOCK_DROP"),
                "LADX shock exit failed");
    }
    StudyLADX pullback(o);
    auto p = input(1);
    p.guide_middle = 110;
    (void)pullback.process(p);
    p.price.raw_time = 2;
    require(side(pullback.process(p)) == 1.2L, "LADX pullback subtype failed");

    for (int gate = 0; gate < 5; ++gate)
    {
        auto options = o;
        if (gate == 4)
            options.chop_min_width_atr = 2;
        StudyLADX signal(options);
        (void)signal.process(input(1));
        auto next = input(2);
        if (gate == 0)
            next.active.pdi = 29; // Bias deteriorates but remains above StartBias.
        if (gate == 1)
            next.price_ma = 101; // Falling MA.
        if (gate == 2)
            next.price.close = 99; // Above-guide MA but close below guide.
        if (gate == 3)
            next.active.pdi = 21; // StartBias equality does not enter.
        if (gate == 4)
            next.guide_slope.slope = 0; // Narrow, flat guide.
        require(side(signal.process(next)) == 0, "LADX entry gate failed");
    }
    StudyLADX warmup;
    auto missing = input(1);
    missing.guide_slope = {};
    require(warmup.process(missing).studies.at("LADX_Chop") == 1, "LADX missing-input chop behavior changed");
}
void continuation_test()
{
    Options o;
    o.chop_min_width_atr = 0;
    o.continuation_bars = 5;
    for (int entry_age : {5, 6})
    {
        StudyLADX signal(o);
        for (int n = 0; n < entry_age; ++n)
        {
            auto bar = input(n + 1);
            bar.active.pdi = 20; // Hold flat until the requested window boundary.
            (void)signal.process(bar);
        }
        auto bar = input(entry_age + 1);
        bar.shock.score = 2; // Above TrendReEntryShockMax: ADXBase does not gate continuation on shock.
        const auto result = signal.process(bar);
        require(result.studies.at("LADX_SIDE") == (entry_age == 5 ? 1.1L : 0),
                "LADX continuation window boundary failed");
        require(same(result, signal.process(bar)), "Repeated bar advanced breakout window");
        if (entry_age == 6)
        {
            auto below = input(8);
            below.price_ma = 99;
            (void)signal.process(below);
            require(signal.process(input(9)).studies.at("LADX_SIDE") == 1.1L,
                    "LADX new breakout did not reset window");
        }
    }
    o.continuation_bars = 0;
    StudyLADX disabled(o);
    (void)disabled.process(input(1));
    require(disabled.process(input(2)).studies.at("LADX_SIDE") == 0, "Zero window allowed continuation");
    auto pullback = input(3);
    pullback.guide_middle = 110;
    require(disabled.process(pullback).studies.at("LADX_SIDE") == 1.2L, "Zero window blocked pullback");
    require(Options::from_json(R"({"ContinuationEntryBars":5})").continuation_bars == 5,
            "ContinuationEntryBars parsing failed");
}
void cooldown_test()
{
    for (const std::size_t cooldown : {0U, 1U, 2U, 4U})
    {
        for (int reason = 0; reason < 4; ++reason)
        {
            Options o;
            o.cooldown_bars = cooldown;
            o.continuation_bars = 100; // Isolate cooldown timing from breakout-window expiry.
            o.chop_min_width_atr = 0;
            o.shock_drop_atr = 4;
            o.shock_arm_up_atr = 3;
            o.shock_armed_drop_atr = 2;
            o.shock_range_atr = 3;
            StudyLADX signal(o);
            (void)signal.process(input(1));
            require(signal.process(input(2)).studies.at("LADX_SIDE") == 1.1L,
                    "Cooldown blocked initial entry");
            auto stop = input(3);
            if (reason == 0) stop.active.pdi = 10;
            if (reason == 1) stop.shock.score = 4;
            if (reason == 2) { stop.shock.up_atr = 3; stop.shock.score = 2; }
            if (reason == 3) { stop.shock.range_atr = 3; stop.shock.down_atr = 1; }
            const auto exited = signal.process(stop);
            require(exited.studies.at("LADX_SIDE") == 0 && exited.studies.at("LADX_BarsSinceExit") == 0,
                    "LADX exit did not reset cooldown");
            require(same(exited, signal.process(stop)), "Repeated exit advanced cooldown");
            const auto entry_bar = std::max<std::size_t>(1, cooldown);
            for (std::size_t n = 1; n <= entry_bar; ++n)
            {
                const auto bar = input(3 + static_cast<std::int64_t>(n));
                const auto result = signal.process(bar);
                require(result.studies.at("LADX_BarsSinceExit") == static_cast<Decimal>(n),
                        "Cooldown counter advanced incorrectly");
                require(result.studies.at("LADX_SIDE") == (n < entry_bar ? 0 : 1.1L),
                        "LADX cooldown entry boundary differs from ADXBase");
                require(same(result, signal.process(bar)), "Repeated bar advanced cooldown");
            }
            // Replacing the exit with a non-exit must remove its cooldown state on replay.
            auto revised = input(3);
            StudyLADX fresh(o);
            (void)fresh.process(input(1));
            (void)fresh.process(input(2));
            require(same(signal.process(revised), fresh.process(revised)), "Revised exit retained cooldown");
            require(same(signal.process(input(4)), fresh.process(input(4))), "Cooldown rewind differs from replay");
        }
    }
}
void revision_test()
{
    std::vector<PriceBar> prices;
    for (std::int64_t n = 0; n < 240; ++n)
    {
        const Decimal value = 100 + std::sin(static_cast<Decimal>(n) / 7) * 10 + n * 0.05L;
        prices.push_back({.raw_time = n * 300, .open = value, .high = value + 3, .low = value - 2, .close = value + 1});
    }
    for (const std::size_t depth : {1U, 5U, 20U, 32U})
    {
        for (const std::size_t fast : {3U, 7U})
        {
            Options o;
            o.adx_length = 3;
            o.guide_ma_length = 7;
            o.fast_ma_length = fast;
            o.shock_atr_length = 3;
            StudyColLADX live(o, depth), fresh(o, depth);
            for (const auto& bar : prices)
                (void)live.process(bar);
            const auto index = prices.size() - 1 - depth;
            for (std::size_t n = 0; n < index; ++n)
                (void)fresh.process(prices[n]);
            auto revised = prices[index];
            revised.high += 8;
            revised.close += 7;
            require(same(live.process(revised), fresh.process(revised)), "LADX past revision differs from replay");
            revised.close -= 2;
            require(same(live.process(revised), fresh.process(revised)), "LADX repeated revision differs from replay");
            for (std::size_t n = index + 1; n < prices.size(); ++n)
                require(same(live.process(prices[n]), fresh.process(prices[n])),
                        "LADX replay differs from fresh state");
            bool rejected{};
            try
            {
                (void)live.process(prices[index - 1]);
            }
            catch (const std::out_of_range&)
            {
                rejected = true;
            }
            require(rejected && same(live.process(prices.back()), fresh.process(prices.back())),
                    "LADX out-of-depth revision changed state");
        }
    }
}
} // namespace
void self_test()
{
    parameters_test();
    signal_test();
    cooldown_test();
    continuation_test();
    revision_test();
    StudyRegistry registry;
    register_collections(registry);
    auto collection = registry.create(StudyCollectionConfig{.type = "ladx"}, Ticker{}, Interval{});
    require(collection->type() == "LADX" && collection->maximum_lookback() == 100, "LADX registry/lookup failed");
    const auto first = collection->process(PriceBar{.raw_time = 1, .close = 1});
    require(first.studies.size() == 41 && first.text_studies.size() == 2, "LADX output schema changed");
    require(first.studies.at("MAPrice") == 0 && !first.studies.at("Active_ADX") &&
                !first.studies.at("LADX_ADXSlope"),
            "LADX warmup outputs differ from EFS");
}
} // namespace fig::collections::ladx
