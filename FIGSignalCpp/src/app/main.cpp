#include "fig/infrastructure/configuration.hpp"
#include "fig/infrastructure/logger.hpp"
#include "fig/infrastructure/protected_data.hpp"
#include "fig/prices/price_aggregator.hpp"
#include "fig/studies/backtest.hpp"
#include "fig/studies/study.hpp"
#include "fig/studies/study_adx.hpp"
#include "fig/studies/study_bb.hpp"
#include "fig/studies/study_col_adx_trend.hpp"
#include "fig/studies/study_extended.hpp"
#include "fig/studies/study_ma.hpp"
#include "fig/studies/study_report.hpp"
#include "fig/studies/study_roc.hpp"
#include "fig/studies/study_sem.hpp"
#include "fig/studies/study_slope_trend.hpp"
#include "fig/studies/study_wma.hpp"
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
std::string odbc_value(std::string_view value)
{
    std::string result = "{";
    for (const char c : value)
    {
        result += c;
        if (c == '}')
            result += '}';
    }
    return result + "}";
}

std::string database_connection(const std::filesystem::path& settings_path)
{
    const auto configuration = fig::Configuration::load(settings_path);
    const auto dsn = configuration.get("ODBCName");
    if (dsn.empty() || dsn.find_first_not_of(" \t\r\n") == std::string::npos || dsn == "null")
        throw std::invalid_argument("Set ODBCName in the settings file to a registered ODBC data source name");
    if (dsn.find_first_of(";{}\r\n") != std::string::npos || dsn.find('\0') != std::string::npos)
        throw std::invalid_argument("ODBCName contains invalid data source name characters");
    // The Driver Manager resolves DSN names literally; do not brace-quote the name.
    std::string connection_string = "DSN=" + dsn + ";";
    const auto user = configuration.get("ODBCUser");
    const auto encrypted_password = configuration.get("ODBCPassword");
    if (user.empty() != encrypted_password.empty())
        throw std::invalid_argument("Set ODBCUser and encrypted ODBCPassword together in the settings file");
    if (!user.empty())
    {
        auto password = fig::unprotect(encrypted_password);
        connection_string += "UID=" + odbc_value(user) + ";PWD=" + odbc_value(password) + ";";
        SecureZeroMemory(password.data(), password.size());
    }
    return connection_string;
}

int backtest_batch(int argc, wchar_t** argv)
{
    fig::BacktestOptions o;
    std::filesystem::path settings = "signal-settings.json";
    for (int n = 2; n < argc; ++n)
    {
        const std::wstring_view name(argv[n]);
        if (n + 1 >= argc)
            throw std::invalid_argument("Missing backtest argument value");
        const std::wstring value(argv[++n]);
        const auto integer = [&]()
        {
            std::size_t used{};
            const auto v = std::stoi(value, &used);
            if (used != value.size())
                throw std::invalid_argument("Invalid integer option");
            return v;
        };
        const auto decimal = [&]()
        {
            std::size_t used{};
            const auto v = std::stold(value, &used);
            if (used != value.size() || !std::isfinite(v))
                throw std::invalid_argument("Invalid decimal option");
            return v;
        };
        if (name == L"--dataset")
            o.dataset = integer();
        else if (name == L"--interval-minutes")
            o.interval_minutes = integer();
        else if (name == L"--max-bars")
            o.maximum_source_rows = integer();
        else if (name == L"--jobs")
            o.jobs = integer();
        else if (name == L"--quantity")
            o.quantity = integer();
        else if (name == L"--quantity-pct")
            o.quantity_pct = decimal();
        else if (name == L"--capital")
            o.capital = decimal();
        else if (name == L"--multiplier")
            o.multiplier = decimal();
        else if (name == L"--commission")
            o.commission = decimal();
        else if (name == L"--slippage-points")
            o.slippage_points = decimal();
        else if (name == L"--force-close")
        {
            const auto v = integer();
            if (v != 0 && v != 1)
                throw std::invalid_argument("--force-close requires 0 or 1");
            o.force_close = v == 1;
        }
        else if (name == L"--parameters-dir")
            o.parameters_directory = value;
        else if (name == L"--output-dir")
            o.output_directory = value;
        else if (name == L"--settings")
            settings = value;
        else if (name == L"--price-cache-dir")
            o.price_cache_directory = value;
        else if (name == L"--price-cache-mode")
            o.price_cache_mode = std::filesystem::path(value).string();
        else if (name == L"--start")
            o.start_date = std::filesystem::path(value).string();
        else if (name == L"--end")
            o.end_date = std::filesystem::path(value).string();
        else if (name == L"--train-end")
            o.train_end = std::filesystem::path(value).string();
        else if (name == L"--validation-end")
            o.validation_end = std::filesystem::path(value).string();
        else
            throw std::invalid_argument("Unknown backtest argument; use --help");
    }
    // Namespace snapshots by settings location and contents (never persist credentials).
    std::ifstream cache_settings(settings, std::ios::binary);
    o.price_cache_source = std::filesystem::absolute(settings).lexically_normal().string() + "\n" +
                          std::string(std::istreambuf_iterator<char>(cache_settings), {});
    const fig::MainRepository repository(database_connection(settings));
    fig::run_backtest_batch(repository, o);
    return 0;
}

int calculate_studies(int argc, wchar_t** argv)
{
    int dataset = 0, maximum = 0;
    fig::StudyReportOptions options;
    std::filesystem::path output_path;
    std::filesystem::path settings_path = "signal-settings.json";
    std::filesystem::path parameters_path;
    fig::StudyCollectionConfig collection;
    auto number = [](const std::wstring& text)
    {
        std::size_t used{};
        const int value = std::stoi(text, &used);
        if (used != text.size() || value < 0)
            throw std::invalid_argument("Expected a nonnegative integer option");
        return value;
    };
    bool smoothing_supplied = false;
    for (int i = 2; i < argc; ++i)
    {
        const std::wstring_view option(argv[i]);
        if (i + 1 == argc)
            throw std::invalid_argument("Missing value for report option");
        const std::wstring value(argv[++i]);
        if (option == L"--dataset")
            dataset = number(value);
        else if (option == L"--period")
            options.period = static_cast<std::size_t>(number(value));
        else if (option == L"--adx-smoothing")
        {
            options.adx_smoothing = static_cast<std::size_t>(number(value));
            smoothing_supplied = true;
        }
        else if (option == L"--source")
        {
            options.source.clear();
            for (const auto character : value)
            {
                if (character < L'a' || character > L'z')
                {
                    if (character < L'0' || character > L'9')
                        throw std::invalid_argument("Invalid price source");
                }
                options.source.push_back(static_cast<char>(character));
            }
        }
        else if (option == L"--max-bars")
            maximum = number(value);
        else if (option == L"--interval-minutes")
            options.interval_minutes = number(value);
        else if (option == L"--output")
            output_path = value;
        else if (option == L"--settings")
            settings_path = value;
        else if (option == L"--collection")
            collection.type = std::filesystem::path(value).string();
        else if (option == L"--parameters")
            parameters_path = value;
        else
            throw std::invalid_argument("Unknown report option; use --help");
    }
    if (!smoothing_supplied)
        options.adx_smoothing = options.period;
    if (options.interval_minutes < 1 || options.interval_minutes > std::numeric_limits<int>::max() / 60)
        throw std::invalid_argument("--interval-minutes must be positive and fit in seconds");
    if (dataset <= 0 || options.period < 2 || !options.adx_smoothing || output_path.empty())
        throw std::invalid_argument("Specify --dataset (positive), --period (at least 2), and --output");
    if (!parameters_path.empty())
    {
        if (collection.type.empty())
            throw std::invalid_argument("--parameters requires --collection");
        std::ifstream parameters(parameters_path);
        if (!parameters)
            throw std::invalid_argument("Cannot open collection parameters file");
        collection.parameters.assign(std::istreambuf_iterator<char>(parameters), {});
    }
    if (collection.type.empty())
    {
        const fig::StudyCalculator validate(options);
        (void)validate;
    }
    else
    {
        fig::StudyRegistry registry;
        fig::register_integrated_collections(registry);
        (void)registry.create(collection, fig::Ticker{}, fig::Interval{});
    }
    const auto connection_string = database_connection(settings_path);
    std::ofstream output(output_path, std::ios::out | std::ios::trunc);
    if (!output)
        throw std::runtime_error("Cannot create report file");
    const fig::MainRepository repository(connection_string);
    const auto count = fig::write_study_report(
        repository, dataset, options, output, maximum, collection.type.empty() ? nullptr : &collection);
    std::cout << "Calculated " << count << " bars at " << options.interval_minutes << " minute interval for dataset "
              << dataset;
    if (collection.type.empty())
        std::cout << " with period " << options.period << " and ADX smoothing " << options.adx_smoothing;
    else
        std::cout << " using collection " << collection.type;
    std::cout << ". Report: " << output_path.string() << '\n';
    return 0;
}

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

int self_test()
{
    using namespace fig;
    revision_test();
    study_report_self_test();
    protected_data_self_test();
    backtest_self_test();
    efs_study_self_test();
    const std::vector<PriceBar> minutes{{-1, 1, 60, 10, 12, 9, 11, 3},
                                        {-1, 1, 120, 11, 13, 10, 12, 4},
                                        {-1, 1, 180, 12, 14, 11, 13, 5},
                                        {-1, 1, 240, 13, 15, 12, 14, 6},
                                        {-1, 1, 300, 14, 16, 13, 15, 7}};
    const auto bars = aggregate_prices(minutes, 300);
    if (bars.size() != 2 || bars.front().open != 10 || bars.front().high != 15 || bars.front().low != 9 ||
        bars.front().close != 14 || bars.front().volume != 18)
        throw std::runtime_error("price aggregation parity test failed");
    PriceAggregator stream(300);
    studies::StudyMA aggregated_ma(2);
    for (std::size_t i = 0; i < 4; ++i)
        if (stream.process(minutes[i]))
            throw std::runtime_error("Aggregation emitted before the bucket boundary");
    const auto completed = stream.process(minutes[4]);
    if (!completed || completed->raw_time != 0 || completed->id != -1 || aggregated_ma.process(*completed))
        throw std::runtime_error("Aggregated study warmup failed");
    auto after_gap = minutes[4];
    after_gap.raw_time = 900;
    const auto second = stream.process(after_gap);
    if (!second || second->raw_time != 300 || second->volume != 7 ||
        aggregated_ma.process(*second) != std::optional<Decimal>(14.5L))
        throw std::runtime_error("Studies must consume aggregated closes once per bucket");
    bool duplicate_rejected = false;
    try
    {
        (void)stream.process(after_gap);
    }
    catch (const std::invalid_argument&)
    {
        duplicate_rejected = true;
    }
    const auto tail = stream.finish();
    if (!duplicate_rejected || !tail || tail->raw_time != 900 || tail->volume != 7 || stream.finish())
        throw std::runtime_error("Aggregation gap/final partial bucket handling failed");
    PriceAggregator empty(300);
    if (empty.finish())
        throw std::runtime_error("Empty aggregation must not emit a bar");
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
        if (argc > 1 && std::wstring_view(argv[1]) == L"--calculate-studies")
            return calculate_studies(argc, argv);
        if (argc > 1 && std::wstring_view(argv[1]) == L"--backtest-batch")
            return backtest_batch(argc, argv);
        if (argc > 1 && std::wstring_view(argv[1]) == L"--help")
        {
            std::cout
                << "FIGSignalCpp --self-test\n"
                   "FIGSignalCpp --backtest-batch --parameters-dir DIR --output-dir DIR\n"
                   "  [--dataset 3] [--interval-minutes 5] [--jobs 2] [--max-bars 0]\n"
                   "  [--capital 9255000] [--quantity 15] [--multiplier 20] [--commission 1.60]\n"
                   "  [--slippage-points 0] [--force-close 0] [--settings signal-settings.json]\n"
                   "  [--price-cache-dir DIR] [--price-cache-mode off|use|refresh]\n"
                   "  [--quantity-pct 1] (quantity 0 sizes each entry from initial capital / multiplier / HLC3)\n"
                   "  [--start 2010-01-01] [--end YYYY-MM-DD] [--train-end 2023-01-01] [--validation-end 2025-01-01]\n"
                   "Backtest dates/months are UTC; end boundaries are exclusive.\n"
                   "FIGSignalCpp --calculate-studies --dataset ID --output FILE.csv\n"
                   "  [--period 14] [--adx-smoothing PERIOD] [--source close] [--max-bars 0]\n"
                   "  [--settings signal-settings.json]\n"
                   "  [--interval-minutes 1] (aggregate prices before calculating studies)\n"
                   "  [--collection ADXBase --parameters adx-base-parameters.json]\n"
                   "Reads FIGAutoTrader.dbo.PriceData using the settings file's ODBCName (DSN).\n"
                   "SQL authentication: ODBCUser and encrypted ODBCPassword in settings.\n"
                   "Zero max-bars reads all source rows; a positive value reads the earliest N source rows.\n"
                   "Aggregation includes partial first/last buckets and does not fill gaps.\n";
            return 0;
        }
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
