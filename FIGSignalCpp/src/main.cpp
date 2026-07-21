#include "fig/configuration.hpp"
#include "fig/logger.hpp"
#include "fig/price_aggregator.hpp"
#include "fig/studies/study_adx.hpp"
#include "fig/studies/study_bb.hpp"
#include "fig/studies/study_col_adx_trend.hpp"
#include "fig/studies/study_extended.hpp"
#include "fig/studies/study_ma.hpp"
#include "fig/studies/study_sem.hpp"
#include "fig/studies/study_wma.hpp"
#include "fig/study.hpp"
#include <cmath>
#include <filesystem>
#include <iostream>
#include <stdexcept>

namespace
{
int self_test()
{
    using namespace fig;
    const std::vector<PriceBar> minutes{{-1, 1, 60, 10, 12, 9, 11, 3},
                                        {-1, 1, 120, 11, 13, 10, 12, 4},
                                        {-1, 1, 180, 12, 14, 11, 13, 5},
                                        {-1, 1, 240, 13, 15, 12, 14, 6},
                                        {-1, 1, 300, 14, 16, 13, 15, 7}};
    const auto bars = aggregate_prices(minutes, 300);
    if (bars.size() != 2 || bars.front().open != 10 || bars.front().high != 15 || bars.front().low != 9 ||
        bars.front().close != 14 || bars.front().volume != 18)
        throw std::runtime_error("price aggregation parity test failed");
    const std::vector<Decimal> values{1, 2, 3, 4, 5};
    const auto ma = simple_moving_average(values, 3);
    const auto wma = weighted_moving_average(values, 3);
    if (!ma.back() || std::fabs(*ma.back() - 4) > 1e-12L || !wma.back() ||
        std::fabs(*wma.back() - 26.0L / 6.0L) > 1e-12L)
        throw std::runtime_error("study primitive parity test failed");
    studies::StudyMA live_ma(3);
    const auto warmup1 = live_ma.process(PriceBar{.raw_time = 100, .close = 1});
    const auto warmup2 = live_ma.process(PriceBar{.raw_time = 200, .close = 2});
    const auto warmup3 = live_ma.process(PriceBar{.raw_time = 300, .close = 3});
    const auto first_live = live_ma.process(PriceBar{.raw_time = 400, .close = 4});
    const auto revised_live =
        live_ma.process(PriceBar{.raw_time = 400, .close = 7}); // revise current bar, do not append it twice
    const auto next_live =
        live_ma.process(PriceBar{.raw_time = 500, .close = 5}); // march forward from the revised state
    if (warmup1 || warmup2 || !warmup3 || *warmup3 != 2 || !first_live || *first_live != 3 || !revised_live ||
        *revised_live != 4 || !next_live || *next_live != 5 || live_ma.evaluation_count() != 6)
        throw std::runtime_error("incremental/revised MA state test failed");
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
    std::cout << "FIGSignalCpp self-test passed\n";
    return 0;
}
} // namespace

int wmain(int argc, wchar_t** argv)
{
    try
    {
        for (int i = 1; i < argc; ++i)
            if (std::wstring_view(argv[i]) == L"--self-test")
                return self_test();
        fig::Logger logger("FIGSignalCpp.log");
        logger.write(fig::LogLevel::info, "FIGSignalCpp native host starting");
        fig::StudyRegistry registry;
        fig::register_integrated_collections(registry);
        logger.write(fig::LogLevel::warning,
                     "Native service host is under parity construction; use --self-test for the verified core");
        return 0;
    }
    catch (const std::exception& error)
    {
        std::cerr << "FIGSignalCpp fatal: " << error.what() << '\n';
        return 1;
    }
}
