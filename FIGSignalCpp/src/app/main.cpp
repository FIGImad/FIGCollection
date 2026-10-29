#include "fig/infrastructure/configuration.hpp"
#include "fig/infrastructure/logger.hpp"
#include "fig/infrastructure/protected_data.hpp"
#include "fig/prices/price_aggregator.hpp"
#include "fig/studies/backtest.hpp"
#include "fig/studies/collection_plugin.hpp"
#include "fig/studies/study.hpp"
#include "fig/studies/study_adx.hpp"
#include "fig/studies/study_bb.hpp"
#include "fig/studies/study_extended.hpp"
#include "fig/studies/study_ma.hpp"
#include "fig/studies/study_report.hpp"
#include "fig/studies/study_roc.hpp"
#include "fig/studies/study_sem.hpp"
#include "fig/studies/study_wma.hpp"
#include <algorithm>
#include <cctype>
#include <cmath>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <limits>
#include <stdexcept>

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
        else if (name == L"--collection")
            o.collection = std::filesystem::path(value).string();
        else if (name == L"--collection-dll")
            o.collection_dll = value;
        else if (name == L"--signal")
            o.signal = std::filesystem::path(value).string();
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
    // Resolve the selected DLL/type before database credentials or output work.
    fig::StudyRegistry registry;
    fig::CollectionPlugin{o.collection_dll}.register_collections(registry);
    const auto types = registry.types();
    std::string selected = o.collection;
    std::ranges::transform(
        selected, selected.begin(), [](unsigned char c) { return static_cast<char>(std::toupper(c)); });
    if (std::ranges::find(types, selected) == types.end())
        throw std::invalid_argument("Unsupported collection type in the loaded DLL: " + o.collection);
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
        else if (option == L"--collection-dll")
            options.collection_dll = value;
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
        fig::CollectionPlugin{options.collection_dll}.register_collections(registry);
        (void)registry.create(collection, fig::Ticker{}, fig::Interval{.length_seconds = options.interval_minutes * 60});
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

std::filesystem::path plugin_path_argument(int argc, wchar_t** argv)
{
    if (argc == 2)
        return {};
    if (argc == 4 && std::wstring_view(argv[2]) == L"--collection-dll")
        return argv[3];
    throw std::invalid_argument("Expected [--collection-dll PATH]; use --help");
}
int inspect_plugin(int argc, wchar_t** argv, bool test)
{
    const fig::CollectionPlugin plugin(plugin_path_argument(argc, argv));
    fig::StudyRegistry registry;
    plugin.register_collections(registry);
    std::cout << "Collection DLL: " << plugin.path().string() << " (" << plugin.name() << ")\n";
    for (const auto& type : registry.types())
        std::cout << "  " << type << '\n';
    if (test)
    {
        plugin.self_test();
        std::cout << "Collection self-test passed\n";
    }
    return 0;
}

int self_test()
{
    using namespace fig;
    study_report_self_test();
    protected_data_self_test();
    backtest_self_test();
    CollectionPlugin{}.self_test();
    collection_plugin_self_test();
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
        if (argc > 1 && std::wstring_view(argv[1]) == L"--list-collections")
            return inspect_plugin(argc, argv, false);
        if (argc > 1 && std::wstring_view(argv[1]) == L"--test-collection")
            return inspect_plugin(argc, argv, true);
        if (argc > 1 && std::wstring_view(argv[1]) == L"--help")
        {
            std::cout
                << "FIGSignalCpp --self-test\n"
                   "FIGSignalCpp --list-collections [--collection-dll PATH]\n"
                   "FIGSignalCpp --test-collection [--collection-dll PATH]\n"
                   "FIGSignalCpp --backtest-batch --parameters-dir DIR --output-dir DIR\n"
                   "  [--collection-dll PATH] [--collection ADXBase] [--signal STrend_LBaseADX_Side]\n"
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
                   "  [--collection-dll PATH] [--collection ADXBase --parameters adx-base-parameters.json]\n"
                   "Default DLL: FIGStudyCollection.ADX.dll beside the executable.\n"
                   "Omitting --collection in a report selects the legacy ADXDiagnostics report.\n"
                   "Reads FIGAutoTrader.dbo.PriceData using the settings file's ODBCName (DSN).\n"
                   "SQL authentication: ODBCUser and encrypted ODBCPassword in settings.\n"
                   "Zero max-bars reads all source rows; a positive value reads the earliest N source rows.\n"
                   "Aggregation includes partial first/last buckets and does not fill gaps.\n";
            return 0;
        }
        if (argc == 2 && std::wstring_view(argv[1]) == L"--self-test")
            return self_test();
        if (argc != 1)
            throw std::invalid_argument("Unknown command or arguments; use --help");
        fig::Logger logger("FIGSignalCpp.log");
        logger.write(fig::LogLevel::info, "FIGSignalCpp native host starting");
        fig::StudyRegistry registry;
        fig::CollectionPlugin{}.register_collections(registry);
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
