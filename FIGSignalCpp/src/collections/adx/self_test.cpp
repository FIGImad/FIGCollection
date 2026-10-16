#include "fig/prices/price_aggregator.hpp"
#include "fig/studies/study.hpp"
#include "fig/studies/study_adx.hpp"
#include "fig/studies/study_bb.hpp"
#include "fig/studies/study_col_adx_trend.hpp"
#include "fig/studies/study_extended.hpp"
#include "fig/studies/study_ma.hpp"
#include "fig/studies/study_roc.hpp"
#include "fig/studies/study_sem.hpp"
#include "fig/studies/study_slope_trend.hpp"
#include "fig/studies/study_wma.hpp"
#include "registration.hpp"
#include <cmath>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <limits>
#include <stdexcept>

namespace fig
{
void efs_study_self_test();
}
namespace
{
struct RevisionStudies
{
    fig::studies::StudyMA ma;
    fig::studies::StudyWMA wma;
    fig::studies::StudySEM sem;
    fig::studies::StudyBB bb;
    fig::studies::StudyExtended extended;
    fig::studies::StudyADX adx;
    fig::studies::StudyROC roc;
    fig::studies::StudySlopeTrend slope;
    fig::studies::StudyColADXTrend collection;

    explicit RevisionStudies(std::size_t depth = fig::studies::default_revision_depth)
        : ma(3, "close", depth), wma(3, "close", depth), sem(3, "close", depth), bb(3, 2, "close", depth),
          extended(3, "ohlc4", 1, 2, depth), adx(3, 3, depth), roc(3, depth), slope(3, 0.5L, depth),
          collection({3, 5, 2, 3, 3, 20}, depth)
    {
    }

    auto process(const fig::PriceBar& bar)
    {
        auto result = collection.process(bar).studies;
        result["MA"] = ma.process(bar);
        result["WMA"] = wma.process(bar);
        result["SEM"] = sem.process(bar);
        result["ROC"] = roc.process(bar.raw_time, bar.close);
        const auto b = bb.process(bar);
        result["BBMiddle"] = b.middle;
        result["BBUpper"] = b.upper;
        result["BBLower"] = b.lower;
        const auto e = extended.process(bar);
        result["ExtendedMA"] = e.moving_average;
        result["ExtendedSEM"] = e.sem;
        result["ExtendedLower1"] = e.lower1;
        result["ExtendedUpper1"] = e.upper1;
        result["ExtendedLower2"] = e.lower2;
        result["ExtendedUpper2"] = e.upper2;
        const auto a = adx.process(bar);
        result["ADX"] = a.adx;
        result["PDI"] = a.pdi;
        result["NDI"] = a.ndi;
        const auto s = slope.process(bar.raw_time, result["MA"]);
        result["SlopeDirection"] = static_cast<fig::Decimal>(s.direction);
        result["SlopeMA"] = s.moving_average;
        result["Slope"] = s.slope_per_bar;
        return result;
    }
};

void revision_test()
{
    using namespace fig;
    struct Snapshot
    {
        std::int64_t raw_time{};
        int total{};
    };
    studies::LiveStateHistory<Snapshot> history(1);
    history.commit({0, 1});
    history.commit({60, 3});
    (void)history.begin(0); // An abandoned calculation must not discard committed bars.
    if (history.begin(120).total != 3)
        throw std::runtime_error("begin must preserve history until commit");
    history.commit({120, 6});
    bool initial_boundary_rejected = false;
    try
    {
        (void)history.begin(0); // Two bars ago exceeds depth one, even before eviction.
    }
    catch (const std::out_of_range&)
    {
        initial_boundary_rejected = true;
    }
    if (!initial_boundary_rejected || history.begin(60).total != 1)
        throw std::runtime_error("initial rollback boundary is incorrect");
    std::vector<PriceBar> prices;
    for (std::int64_t i = 0; i < 80; ++i)
    {
        const auto value = static_cast<Decimal>(100 + i % 7 * 3 - i % 11 * 2);
        prices.push_back(
            PriceBar{.raw_time = i * 60, .open = value, .high = value + 4, .low = value - 3, .close = value + 1});
    }
    for (const std::size_t depth : {1U, 5U, 20U, 32U})
    {
        RevisionStudies live(depth);
        for (const auto& bar : prices)
            (void)live.process(bar);
        // The exact rollback boundary must work after older snapshots were evicted.
        const auto target = prices.size() - 1 - depth;
        RevisionStudies fresh(depth);
        for (std::size_t i = 0; i < target; ++i)
            (void)fresh.process(prices[i]);
        auto revised = prices[target];
        revised.high += 10;
        revised.close += 8;
        if (live.process(revised) != fresh.process(revised))
            throw std::runtime_error("past-bar revision differs from fresh calculation");
        revised.close -= 2;
        if (live.process(revised) != fresh.process(revised))
            throw std::runtime_error("repeated past-bar replacement differs from fresh calculation");
        // Replay discarded bars and check every result, including recursive ADX/signal state.
        for (std::size_t i = target + 1; i < prices.size(); ++i)
            if (live.process(prices[i]) != fresh.process(prices[i]))
                throw std::runtime_error("replayed studies differ from fresh calculation");
        bool rejected = false;
        try
        {
            (void)live.process(prices[target - 1]);
        }
        catch (const std::out_of_range&)
        {
            rejected = true;
        }
        if (!rejected || live.process(prices.back()) != fresh.process(prices.back()))
            throw std::runtime_error("out-of-depth revision must fail without changing state");
    }
    // Exercise the public default, warmup rollback, and an unobserved timestamp between bars.
    studies::StudyMA ma(3);
    for (const auto& bar : prices)
        (void)ma.process(bar);
    const auto target = prices.size() - 21;
    const auto value = ma.process(prices[target]);
    const auto expected = (prices[target - 2].close + prices[target - 1].close + prices[target].close) / 3;
    if (!value || *value != expected)
        throw std::runtime_error("default revision depth must support twenty bars ago");
    studies::StudyMA warmup(3);
    for (std::size_t i = 0; i < 6; ++i)
        (void)warmup.process(prices[i]);
    if (warmup.process(prices[0]) || warmup.process(prices[0]))
        throw std::runtime_error("rewinding to first bar must restore warmup");
    (void)warmup.process(prices[1]);
    (void)warmup.process(prices[2]);
    if (warmup.process(PriceBar{.raw_time = 30, .close = 200}))
        throw std::runtime_error("inserting a past timestamp must discard later bars");
}

} // namespace
namespace fig
{
void adx_collection_self_test()
{
    revision_test();
    efs_study_self_test();
    studies::StudyWMA live_wma(3);
    studies::StudySEM live_sem(3);
    studies::StudyBB live_bb(3, 2);
    studies::StudyExtended live_extended(3);
    studies::StudyADX live_adx(3, 3);
    studies::StudyColADXTrend adx_collection({3, 5, 2, 3, 3, 20});
    std::optional<Decimal> wma_value, sem_value;
    studies::BollingerBands bb_value;
    studies::ExtendedValues extended_value;
    studies::AdxValues adx_value;
    StudyBar collection_value;
    for (std::int64_t i = 1; i <= 30; ++i)
    {
        const PriceBar bar{.raw_time = i * 60,
                           .open = static_cast<Decimal>(i),
                           .high = static_cast<Decimal>(i) + 1,
                           .low = static_cast<Decimal>(i) - 1,
                           .close = static_cast<Decimal>(i) + 0.5L,
                           .volume = 100};
        wma_value = live_wma.process(bar);
        sem_value = live_sem.process(bar);
        bb_value = live_bb.process(bar);
        extended_value = live_extended.process(bar);
        adx_value = live_adx.process(bar);
        collection_value = adx_collection.process(bar);
    }
    const PriceBar revision{.raw_time = 1800, .open = 30, .high = 32, .low = 29, .close = 31, .volume = 110};
    wma_value = live_wma.process(revision);
    sem_value = live_sem.process(revision);
    bb_value = live_bb.process(revision);
    extended_value = live_extended.process(revision);
    adx_value = live_adx.process(revision);
    collection_value = adx_collection.process(revision);
    if (!wma_value || !sem_value || !bb_value.middle || !extended_value.moving_average || !extended_value.sem ||
        !adx_value.adx || !collection_value.studies["Active_ADX"])
        throw std::runtime_error("one or more live study classes failed warmup/revision testing");
}
} // namespace fig
