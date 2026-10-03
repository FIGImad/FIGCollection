#include "fig/studies/study_col_adx_base.hpp"
#include "fig/studies/study_cycle_dir.hpp"
#include "fig/studies/study_lbase_adx_variants.hpp"
#include "fig/studies/study_sem.hpp"
#include "fig/studies/study_volatility_ratio.hpp"
#include <algorithm>
#include <cmath>
#include <limits>

namespace fig
{
namespace
{
void require(bool valid, const char* message)
{
    if (!valid)
        throw std::runtime_error(message);
}
void near(std::optional<Decimal> actual, std::optional<Decimal> expected)
{
    require(actual.has_value() == expected.has_value(), "EFS optional/warmup mismatch");
    if (actual)
        require(std::abs(*actual - *expected) <= 1e-9L * (1 + std::abs(*expected)), "EFS numeric mismatch");
}

// Batch reference for the pasted Study_ADX closed-bar formulas. Recompute from input history,
// independently of LiveStateHistory and incremental StudyADX snapshots.
studies::AdxValues reference_adx(const std::vector<PriceBar>& bars, std::size_t length, std::size_t smoothing)
{
    studies::AdxValues out;
    if (bars.size() <= length)
        return out;
    auto dm = [&](std::size_t n)
    {
        const auto& c = bars[n];
        const auto& p = bars[n - 1];
        const auto up = c.high - p.high, down = p.low - c.low;
        return std::vector<Decimal>{std::max({c.high - c.low, std::abs(c.high - p.close), std::abs(c.low - p.close)}),
                                    up > down && up > 0 ? up : 0,
                                    down > up && down > 0 ? down : 0};
    };
    std::vector<Decimal> sums(3), dx;
    for (std::size_t n = length; n > 0; --n)
    {
        const auto v = dm(n);
        for (std::size_t k = 0; k < 3; ++k)
            sums[k] += v[k];
    }
    std::optional<Decimal> adx;
    for (std::size_t n = length; n < bars.size(); ++n)
    {
        out = {};
        if (n > length)
        {
            const auto v = dm(n);
            for (std::size_t k = 0; k < 3; ++k)
                sums[k] = sums[k] - sums[k] / static_cast<Decimal>(length) + v[k];
        }
        if (sums[0] <= 0)
            continue;
        const auto pdi = 100 * (sums[1] / sums[0]), ndi = 100 * (sums[2] / sums[0]);
        const auto current = pdi + ndi > 0 ? 100 * std::abs(pdi - ndi) / (pdi + ndi) : 0;
        out.pdi = pdi;
        out.ndi = ndi;
        out.bias = pdi - ndi;
        if (!adx)
        {
            dx.insert(dx.begin(), current);
            if (dx.size() == smoothing)
            {
                Decimal sum{};
                for (const auto v : dx)
                    sum += v;
                adx = sum / static_cast<Decimal>(smoothing);
            }
        }
        else
            adx = (*adx * static_cast<Decimal>(smoothing - 1) + current) / static_cast<Decimal>(smoothing);
        out.adx = adx;
    }
    return out;
}

studies::AdxBaseInput signal_input(std::int64_t time, Decimal ma, Decimal bias)
{
    studies::AdxBaseInput i;
    i.price = {.raw_time = time, .open = ma, .high = ma + 1, .low = ma - 1, .close = ma};
    i.price_ma = ma;
    i.stop_ma = ma;
    i.guide_middle = 100;
    i.guide_upper = 110;
    i.guide_lower = 90;
    i.active = {20, 20 + bias, 20, bias};
    i.shock.atr = 10;
    i.guide_slope = {100, 0.1L, 1};
    i.confirm_slope = {ma, 0, 0};
    return i;
}

void signal_tests()
{
    studies::AdxBaseOptions o;
    o.cooldown_bars = 3;
    studies::StudyLBaseADX signal(o);
    (void)signal.process(signal_input(1, 90, 2));
    auto out = signal.process(signal_input(2, 91, 2));
    require(out.studies.at("STrend_LBaseADX_Side") == 1, "Long pullback entry failed");
    require(out.text_studies.at("STrend_LBaseADX_EntryMode") == "PULLBACK", "Wrong long entry mode");
    auto input = signal_input(3, 92, 3);
    out = signal.process(input);
    const auto repeated = signal.process(input);
    require(out.studies == repeated.studies && out.text_studies == repeated.text_studies,
            "Same-bar signal changed counters");
    require(out.studies.at("STrend_LBaseADX_BarsInTrade") == 1, "Trade counter failed");
    input = signal_input(4, 91, 2);
    input.shock.score = 4;
    input.shock.down_atr = 4;
    out = signal.process(input);
    require(out.studies.at("STrend_LBaseADX_Side") == 0 &&
                out.text_studies.at("STrend_LBaseADX_LastExitReason") == "SHOCK_DROP",
            "Shock exit failed");
    for (std::int64_t t = 5; t <= 6; ++t)
    {
        out = signal.process(signal_input(t, 92, 2));
        require(out.text_studies.at("STrend_LBaseADX_StartBlockReason") == "COOLDOWN", "Cooldown duration mismatch");
    }
    out = signal.process(signal_input(7, 93, 2));
    require(out.studies.at("STrend_LBaseADX_Side") == 1, "Cooldown boundary failed");
    input = signal_input(8, 92, -1);
    input.shock.score = 10;
    out = signal.process(input);
    require(out.text_studies.at("STrend_LBaseADX_StopReason") == "ADX_BIAS", "Long exit priority mismatch");

    studies::StudyLBaseADX continuation;
    (void)continuation.process(signal_input(1, 99, 2));
    input = signal_input(2, 101, 2);
    input.guide_slope.slope = -1;
    input.shock.score = 10;
    out = continuation.process(input);
    require(out.studies.at("STrend_LBaseADX_Side") == 1 &&
                out.text_studies.at("STrend_LBaseADX_EntryMode") == "CONTINUATION",
            "Commented EFS continuation/confirmation gates were incorrectly enabled");
    studies::StudyLBaseADX chop;
    (void)chop.process(signal_input(1, 90, 2));
    input = signal_input(2, 91, 2);
    input.shock.atr = 100;
    input.guide_slope.slope = 0;
    out = chop.process(input);
    require(out.studies.at("STrend_LBaseADX_Chop") == 1 && out.studies.at("STrend_LBaseADX_Side") == 0,
            "Chop gate failed");

    studies::StudySBaseADX short_signal;
    input = signal_input(1, 102, 0.5L);
    input.confirm_slope.slope = -2;
    out = short_signal.process(input);
    require(out.studies.at("STrend_SBaseADX_Side") == -1, "EFS short entry must use bias < positive StartBias");
    input = signal_input(2, 101, 0);
    input.confirm_slope.slope = 2;
    input.shock.up_atr = 10;
    out = short_signal.process(input);
    require(out.text_studies.at("STrend_SBaseADX_StopReason") == "CONF_SLOPE", "Short exit priority failed");
    studies::StudySBaseADX short_bias;
    input = signal_input(1, 102, -2);
    input.confirm_slope.slope = -2;
    (void)short_bias.process(input);
    input = signal_input(2, 101, -0.5L);
    input.confirm_slope.slope = 0;
    out = short_bias.process(input);
    require(out.text_studies.at("STrend_SBaseADX_StopReason") == "ADX_BIAS",
            "EFS short exit must use negative StopBias");

    studies::StudyLBaseADXAttempt attempt;
    out = attempt.process(signal_input(1, 90, 2));
    require(out.studies.at("STrend_LBaseADX_Side") == 1, "Attempt entry failed");
    out = attempt.process(signal_input(2, 91, 2));
    require(out.studies.at("STrend_LBaseADX_ADXTrigger") == 2 && out.studies.at("STrend_LBaseADX_Side") == 0,
            "Attempt adaptive trigger failed");
    studies::StudyLBaseADXNew experimental;
    (void)experimental.process(signal_input(1, 90, 2));
    (void)experimental.process(signal_input(2, 92, 10));
    out = experimental.process(signal_input(3, 93, 3));
    require(out.studies.at("STrend_LBaseADX_MAXBias") == 10 && out.studies.at("STrend_LBaseADX_Side") == 0,
            "New variant max-bias exit failed");
}
} // namespace

void efs_study_self_test()
{
    using namespace studies;
    std::vector<PriceBar> bars;
    for (std::int64_t n = 0; n < 260; ++n)
    {
        const auto price = 100 + 20 * std::sin(static_cast<Decimal>(n) / 9) + static_cast<Decimal>((n * 17) % 13);
        bars.push_back(
            {.raw_time = n * 60, .open = price - 1, .high = price + 3, .low = price - 4, .close = price + 1});
    }
    for (const auto length : {1U, 3U, 50U})
    {
        StudyADX adx(length, length);
        std::vector<PriceBar> prefix;
        for (const auto& bar : bars)
        {
            prefix.push_back(bar);
            auto actual = adx.process(bar);
            const auto expected = reference_adx(prefix, length, length);
            near(actual.adx, expected.adx);
            near(actual.pdi, expected.pdi);
            near(actual.ndi, expected.ndi);
            near(actual.bias, expected.bias);
            // Repeated updates during both seeds and the established recursive state.
            auto replacement = bar;
            replacement.close += 1;
            replacement.high += 2;
            prefix.back() = replacement;
            actual = adx.process(replacement);
            const auto revised = reference_adx(prefix, length, length);
            near(actual.adx, revised.adx);
            near(actual.pdi, revised.pdi);
            near(actual.ndi, revised.ndi);
            prefix.back() = bar;
            (void)adx.process(bar);
        }
    }
    StudyADX flat_adx(3, 2);
    for (std::int64_t n = 0; n < 12; ++n)
        require(!flat_adx.process({.raw_time = n, .open = 1, .high = 1, .low = 1, .close = 1}).adx,
                "Flat ADX should stay undefined");
    StudyBB zero_bb(3, 1);
    StudySEM zero_sem(3);
    for (std::int64_t n = 0; n < 3; ++n)
    {
        (void)zero_bb.process({.raw_time = n});
        (void)zero_sem.process({.raw_time = n});
    }
    near(zero_bb.process({.raw_time = 2}).middle, 0);
    near(zero_sem.process({.raw_time = 2}), 0);
    StudySMASlope slope(2, 2, 0.1L);
    for (std::int64_t n = 1; n <= 3; ++n)
        require(!slope.process({.raw_time = n, .close = static_cast<Decimal>(n)}).slope, "Slope warmup mismatch");
    near(slope.process({.raw_time = 4, .close = 4}).slope, 1);
    StudyADXROC roc(2);
    require(!roc.process(1, 0).roc, "ROC warmup failed");
    auto r = roc.process(2, 2);
    near(r.roc, 2);
    require(!r.percent, "ROC divided by zero");
    r = roc.process(3, -2);
    near(r.percent, -200);
    near(r.slope, -1);
    r = roc.process(3, -4);
    near(r.slope, -2);
    StudyATR atr(2);
    require(!atr.process({.raw_time = 1, .high = 11, .low = 9, .close = 10}), "ATR first-bar warmup failed");
    require(!atr.process({.raw_time = 2, .high = 14, .low = 11, .close = 12}), "ATR seed warmup failed");
    near(atr.process({.raw_time = 3, .high = 13, .low = 10, .close = 11}), 3.5L);
    near(atr.process({.raw_time = 4, .high = 14, .low = 9, .close = 12}), 4.25L);
    StudyPriceShock shock(1);
    (void)shock.process({.raw_time = 1, .high = 11, .low = 9, .close = 10});
    auto sh = shock.process({.raw_time = 2, .high = 12, .low = 6, .close = 7});
    near(sh.down_atr, 0.5L);
    near(sh.score, 5.0L / 6);
    StudyVolatilityRatio vol(1, 2);
    (void)vol.process({.raw_time = 1, .high = 11, .low = 9, .close = 10});
    (void)vol.process({.raw_time = 2, .high = 11, .low = 9, .close = 10});
    const auto vr = vol.process({.raw_time = 3, .high = 16, .low = 10, .close = 12});
    near(vr.ratio, 1.5L);
    require(vr.state == 1, "Volatility expansion failed");
    StudyCycleDir cycle;
    (void)cycle.process({.raw_time = 1, .high = 10, .low = 8, .close = 9}, {});
    auto cy = cycle.process({.raw_time = 2, .high = 12, .low = 9, .close = 11}, {10, 11, 9});
    require(cy.direction == -1 && cy.high == 12 && cy.low == 9, "Cycle direction transition failed");
    signal_tests();

    SharedMovingAverages shared({{3, "close"}, {3, "ohlc4"}, {3, "close"}});
    require(shared.size() == 2, "Duplicate SMA dependency was not shared");
    StudyMA separate_close(3, "close"), separate_ohlc(3, "ohlc4");
    StudySMASlope shared_slope(3, 2, 0.1L), separate_slope(3, 2, 0.1L);
    for (const auto& p : bars)
    {
        shared.process(p);
        near(shared.value(3, "close"), separate_close.process(p));
        near(shared.value(3, "ohlc4"), separate_ohlc.process(p));
        const auto before_reads = shared.calculation_count();
        (void)shared.value(3, "close");
        (void)shared.value(3, "close");
        require(shared.calculation_count() == before_reads, "Reading shared SMA recalculated it");
        near(shared_slope.process(p.raw_time, shared.value(3, "close")).slope, separate_slope.process(p).slope);
    }
    require(shared.calculation_count() == 2 * bars.size(), "SMA evaluated more than once per dependency/bar");
    for (const auto distance : {0U, 5U, 20U})
    {
        const auto target = bars.size() - 1 - distance;
        auto replacement = bars[target];
        replacement.close += 30;
        shared.process(replacement);
        near(shared.value(3, "close"), separate_close.process(replacement));
        near(shared.value(3, "ohlc4"), separate_ohlc.process(replacement));
        near(shared_slope.process(replacement.raw_time, shared.value(3, "close")).slope,
             separate_slope.process(replacement).slope);
        for (std::size_t n = target; n < bars.size(); ++n)
        {
            shared.process(bars[n]);
            near(shared.value(3, "close"), separate_close.process(bars[n]));
            near(shared.value(3, "ohlc4"), separate_ohlc.process(bars[n]));
            near(shared_slope.process(bars[n].raw_time, shared.value(3, "close")).slope,
                 separate_slope.process(bars[n]).slope);
        }
    }
    // Exercise both BB-middle reuse and sharing between the two close-SMA slopes.
    for (const auto guide_period : {3U, 9U})
    {
        AdxBaseOptions o;
        o.fast_ma_length = o.slow_ma_length = 3;
        o.guide_ma_length = guide_period;
        StudyColADXBase collection(o);
        StudyMA price_reference(3, "ohlc4");
        StudySMASlope slope_reference(guide_period, 5, 0.1L);
        for (const auto& p : bars)
        {
            const auto result = collection.process(p);
            near(result.studies.at("MAPrice"), price_reference.process(p).value_or(0));
            near(result.studies.at("MAStop"), result.studies.at("MAPrice"));
            near(result.studies.at("SLGuide_MASlopeValue"), slope_reference.process(p).slope);
        }
    }

    StudyRegistry registry;
    register_integrated_collections(registry);
    StudyCollectionConfig config;
    config.type = "adxbase";
    config.parameters = R"({"AdxLen":3,"AdxLenConf":2,"FastMALen":2,"GuideMALen":5,"ShockATRLen":2})";
    auto live = registry.create(config, Ticker{}, Interval{});
    for (const auto& bar : bars)
        (void)live->process(bar);
    for (const auto distance : {0U, 5U, 20U})
    {
        const auto target = bars.size() - 1 - distance;
        auto fresh = registry.create(config, Ticker{}, Interval{});
        for (std::size_t n = 0; n < target; ++n)
            (void)fresh->process(bars[n]);
        auto replacement = bars[target];
        replacement.close += 10;
        replacement.high += 10;
        auto a = live->process(replacement), b = fresh->process(replacement);
        require(a.studies == b.studies && a.text_studies == b.text_studies, "Collection rollback mismatch");
        for (std::size_t n = target; n < bars.size(); ++n)
        {
            a = live->process(bars[n]);
            b = fresh->process(bars[n]);
            require(a.studies == b.studies && a.text_studies == b.text_studies, "Collection replay mismatch");
        }
        // The collection may intentionally omit the short study; verify the required long outputs.
        require(a.studies.contains("STrend_LBaseADX_Side") && a.studies.contains("MIDGuide") &&
                    a.studies.contains("Shock_ATR") && a.text_studies.contains("STrend_LBaseADX_EntryMode"),
                "Required long collection output missing");
    }
    for (const auto invalid :
         {R"({"AdxLen":0})", R"({"AdxLen":-1})", R"({"AdxLen":2.5})", R"({"Unknown":1})", R"({"AdxLen":2,})"})
    {
        bool rejected = false;
        try
        {
            (void)AdxBaseOptions::from_json(invalid);
        }
        catch (const std::exception&)
        {
            rejected = true;
        }
        require(rejected, "Invalid collection parameters were accepted");
    }
}
} // namespace fig
