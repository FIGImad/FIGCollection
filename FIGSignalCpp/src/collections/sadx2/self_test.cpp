#include "fig/collections/sadx2/study_col_sadx2.hpp"
#include <cmath>
#include <limits>
#include <stdexcept>

namespace fig::collections::sadx2
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

void signal_test()
{
    Options o;
    o.chop_min_width_atr = 0;
    o.cooldown_bars = 2;
    StudySADX2 signal(o);
    const auto input = [](std::int64_t time)
    {
        SADX2Input i;
        i.price = {.raw_time = time, .open = 98, .high = 100, .low = 96, .close = 98};
        i.price_ma = 98;
        i.guide_middle = 100;
        i.guide_upper = 101;
        i.guide_lower = 99;
        i.active = {25, 20, 30, -10};

        i.shock.atr = 2;
        return i;
    };
    const auto side = [](const StudyBar& b) { return b.studies.at("SADX2_SIDE"); };
    require(side(signal.process(input(1))) == 0, "Missing prior MA must block entry");
    auto b = signal.process(input(2));
    require(side(b) == -1.1L && b.studies.at("SADX2_ShortSide") == -1, "Short continuation failed");
    auto armed = input(3);
    armed.shock.down_atr = 3;
    require(signal.process(armed).studies.at("SADX2_MaxDownATR") == 3, "Downward arming failed");
    auto stop = input(4);
    stop.shock.score = 2;
    b = signal.process(stop);
    require(side(b) == 0 && b.text_studies.at("SADX2_StopReason") == "ARMED_RISE", "Armed rise exit failed");
    require(side(signal.process(input(5))) == 0, "Cooldown failed");
    require(side(signal.process(input(6))) == -1.1L, "Cooldown boundary failed");
    stop = input(7);
    stop.active.pdi = 40;
    stop.shock.score = 10;

    b = signal.process(stop);
    require(b.text_studies.at("SADX2_StopReason") == "ADX_BIAS", "Exit priority failed");
    StudySADX2 pullback(o);
    auto above = input(1);
    above.price_ma = 102;
    above.market_trend = 1;

    (void)pullback.process(above);
    above.price.raw_time = 2;
    require(side(pullback.process(above)) == -1.2L, "Bullish pullback failed");
    above.price.raw_time = 3;
    above.price.low = 102;
    b = pullback.process(above);
    require(side(b) == 0 && b.text_studies.at("SADX2_StopReason") == "LONG_TREND_GUIDE_CROSS", "Guide cross failed");
    above.price.raw_time = 4;
    (void)pullback.process(above);
    above.price.raw_time = 5;
    require(side(pullback.process(above)) == 0, "Guide recovery gate failed");
    above.price.raw_time = 6;
    above.price_ma = 100.5L;
    require(side(pullback.process(above)) == -1.2L, "Guide recovery failed");
}
void parameters_test()
{
    const auto o = Options::from_json("{}");
    require(o.start_bias_short == -0.4L && o.stop_bias_long == 1 && o.market.ema_length == 132,
            "RUN0738 defaults failed");
    const auto aliases = Options::from_json(R"({"StartBias":-2,"ShockRiseATR":3})");
    require(aliases.start_bias_short == -2 && aliases.start_bias_long == -2 && aliases.shock_rise_atr == 3,
            "Aliases failed");
    for (const auto* json : {"[]",
                             R"({"AdxLen":0})",
                             R"({"AdxLen":2.5})",
                             R"({"unknown":1})",
                             R"({"exit":{"exitOnGuideCrossLongTrend":1}})",
                             R"({"StartBias":1,"entry":{"startBiasLongTrend":2}})"})
    {
        bool rejected = false;
        try
        {
            (void)Options::from_json(json);
        }
        catch (const std::invalid_argument&)
        {
            rejected = true;
        }
        require(rejected, "Invalid JSON accepted");
    }
}
void revision_test()
{
    std::vector<PriceBar> prices;
    for (std::int64_t n = 0; n < 240; ++n)
    {
        const Decimal value = 100 + std::sin(static_cast<Decimal>(n) / 7) * 10 + n * 0.05L;
        prices.push_back(
            {.raw_time = 1609459200 + n * 300, .open = value, .high = value + 3, .low = value - 2, .close = value + 1});
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
            o.market.sma_length = 3;
            o.market.slope_lookback = 2;
            o.market.dmi_length = 2;
            o.market.adx_length = 2;
            o.market.ema_length = 2;
            o.market.atr_length = 2;
            StudyColSADX2 live(o, depth), fresh(o, depth);
            for (const auto& bar : prices)
                (void)live.process(bar);
            const auto index = prices.size() - 1 - depth;
            for (std::size_t n = 0; n < index; ++n)
                (void)fresh.process(prices[n]);
            auto revised = prices[index];
            revised.high += 8;
            revised.close += 7;
            require(same(live.process(revised), fresh.process(revised)), "SADX2 past revision differs from replay");
            revised.close -= 2;
            require(same(live.process(revised), fresh.process(revised)), "SADX2 repeated revision differs from replay");
            for (std::size_t n = index + 1; n < prices.size(); ++n)
                require(same(live.process(prices[n]), fresh.process(prices[n])),
                        "SADX2 replay differs from fresh state");
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
                    "SADX2 out-of-depth revision changed state");
        }
    }
}
} // namespace
void self_test()
{
    parameters_test();
    signal_test();
    revision_test();
    StudyRegistry registry;
    register_collections(registry);
    auto collection = registry.create(StudyCollectionConfig{.type = "sadx2"}, Ticker{}, Interval{});
    require(collection->type() == "SADX2", "Registration failed");
}
} // namespace fig::collections::sadx2
