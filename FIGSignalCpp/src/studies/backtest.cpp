#include "fig/studies/backtest.hpp"
#include "fig/prices/price_aggregator.hpp"
#include "fig/prices/price_cache.hpp"
#include "fig/studies/study.hpp"
#include <algorithm>
#include <atomic>
#include <chrono>
#include <cmath>
#include <fstream>
#include <iomanip>
#include <iostream>
#include <limits>
#include <mutex>
#include <sstream>
#include <thread>

namespace fig
{
namespace
{
using namespace std::chrono;
std::int64_t date_time(const std::string& text)
{
    if (text.size() != 10 || text[4] != '-' || text[7] != '-' ||
        text.find_first_not_of("0123456789-") != std::string::npos)
        throw std::invalid_argument("Expected a UTC date in YYYY-MM-DD format");
    for (std::size_t n = 0; n < text.size(); ++n)
        if (n != 4 && n != 7 && (text[n] < '0' || text[n] > '9'))
            throw std::invalid_argument("Expected a UTC date in YYYY-MM-DD format");
    const year_month_day day{year{std::stoi(text.substr(0, 4))},
                             month{static_cast<unsigned>(std::stoi(text.substr(5, 2)))},
                             std::chrono::day{static_cast<unsigned>(std::stoi(text.substr(8, 2)))}};
    if (!day.ok())
        throw std::invalid_argument("Invalid backtest date");
    return duration_cast<seconds>(sys_days{day}.time_since_epoch()).count();
}
int month_id(std::int64_t raw)
{
    const year_month_day d{floor<days>(sys_seconds{seconds{raw}})};
    return static_cast<int>(d.year()) * 12 + static_cast<int>(static_cast<unsigned>(d.month())) - 1;
}
std::string month_text(int id)
{
    std::ostringstream out;
    out << id / 12 << '-' << std::setw(2) << std::setfill('0') << id % 12 + 1;
    return out.str();
}
std::ofstream file(const std::filesystem::path& path)
{
    std::ofstream out(path);
    if (!out)
        throw std::runtime_error("Cannot create backtest output: " + path.string());
    out.exceptions(std::ios::failbit | std::ios::badbit);
    out.imbue(std::locale::classic());
    out << std::setprecision(std::numeric_limits<Decimal>::max_digits10);
    return out;
}
std::string read(const std::filesystem::path& path)
{
    std::ifstream in(path, std::ios::binary);
    if (!in)
        throw std::invalid_argument("Cannot read parameters: " + path.string());
    return std::string(std::istreambuf_iterator<char>(in), {});
}
std::string csv_quoted(const std::string& value)
{
    std::string out = "\"";
    for (char c : value)
    {
        if (c == '"')
            out += '"';
        out += c;
    }
    return out + '"';
}
struct Trade
{
    std::int64_t entry_time{}, exit_time{};
    Decimal entry{}, exit{}, gross{}, fees{};
    std::string reason;
    int quantity{};
};
struct Month
{
    int id{};
    std::int64_t last_time{};
    Decimal start{}, equity{}, realized{}, unrealized{}, fees{}, drawdown{}, drawdown_points{};
    std::size_t trades{}, bars{};
    int position{};
};
struct Segment
{
    Decimal start{}, end{}, peak{}, drawdown{};
    std::size_t bars{}, trades{};
    void update(Decimal before, Decimal equity, bool closed)
    {
        if (!bars)
            start = peak = before;
        end = equity;
        peak = std::max(peak, equity);
        if (peak > 0)
            drawdown = std::min(drawdown, 100 * (equity / peak - 1));
        ++bars;
        if (closed)
            ++trades;
    }
};
class Ledger
{
  public:
    explicit Ledger(const BacktestOptions& options)
        : o(options), equity(options.capital), peak(options.capital), train_end(date_time(options.train_end)),
          validation_end(date_time(options.validation_end))
    {
    }
    void process(const PriceBar& bar, int side, bool final = false)
    {
        if (side != 0 && side != 1)
            throw std::runtime_error("Long backtest expects Side 0 or 1");
        if (last_time && bar.raw_time <= *last_time)
            throw std::runtime_error("Backtest prices must increase");
        const Decimal before = equity;
        const int month = month_id(bar.raw_time);
        if (months.empty() || month != months.back().id)
        {
            if (!months.empty())
                for (int id = months.back().id + 1; id < month; ++id)
                    months.push_back(Month{.id = id,
                                           .last_time = *last_time,
                                           .start = equity,
                                           .equity = equity,
                                           .unrealized = unrealized,
                                           .drawdown = 100 * (equity / peak - 1),
                                           .drawdown_points = 100 * (equity - peak) / o.capital,
                                           .position = open ? position_quantity : 0});
            months.push_back(Month{.id = month, .start = equity});
        }
        auto& m = months.back();
        const auto previous_realized = realized, previous_fees = fees;
        bool closed = false;
        if (!open && side == 1 && previous_side == 0)
        {
            position_quantity = o.quantity;
            if (o.quantity == 0)
            {
                const auto hlc = (bar.high + bar.low + bar.close) / 3;
                if (!std::isfinite(hlc) || hlc <= 0)
                    throw std::runtime_error("EFS quantity sizing requires positive finite entry HLC3");
                // Same operation order and ceiling as EFS. Initial AUM stays fixed (no reinvestment).
                const auto sized = std::ceil(o.capital * o.quantity_pct / o.multiplier / hlc);
                if (!std::isfinite(sized) || sized < 1 || sized > std::numeric_limits<int>::max())
                    throw std::runtime_error("EFS calculated quantity is outside the supported contract range");
                position_quantity = static_cast<int>(sized);
            }
            entry = bar.close + o.slippage_points;
            entry_time = bar.raw_time;
            fees += position_quantity * o.commission;
            open = true;
        }
        if (open && (side == 0 || (final && o.force_close)))
        {
            const auto exit = bar.close - o.slippage_points;
            const auto gross = (exit - entry) * position_quantity * o.multiplier;
            const auto trade_fees = 2 * static_cast<Decimal>(position_quantity) * o.commission;
            realized += gross;
            fees += position_quantity * o.commission;
            trades.push_back(
                {entry_time, bar.raw_time, entry, exit, gross, trade_fees, side == 0 ? "SIGNAL" : "END_OF_DATA", position_quantity});
            open = false;
            closed = true;
        }
        unrealized = open ? (bar.close - entry) * position_quantity * o.multiplier : 0;
        equity = o.capital + realized + unrealized - fees;
        peak = std::max(peak, equity);
        const auto dd = 100 * (equity / peak - 1);
        const auto dd_points = 100 * (equity - peak) / o.capital;
        max_drawdown = std::min(max_drawdown, dd);
        max_drawdown_points = std::min(max_drawdown_points, dd_points);
        m.drawdown = std::min(m.drawdown, dd);
        m.drawdown_points = std::min(m.drawdown_points, dd_points);
        m.realized += realized - previous_realized;
        m.fees += fees - previous_fees;
        m.equity = equity;
        m.unrealized = unrealized;
        m.position = open ? position_quantity : 0;
        m.last_time = bar.raw_time;
        ++m.bars;
        if (closed)
            ++m.trades;
        segments[bar.raw_time < train_end ? 0 : bar.raw_time < validation_end ? 1 : 2].update(before, equity, closed);
        previous_side = side;
        last_time = bar.raw_time;
    }
    BacktestOptions o;
    Decimal equity{}, peak{}, realized{}, unrealized{}, fees{}, max_drawdown{}, max_drawdown_points{};
    bool open{};
    Decimal entry{};
    std::int64_t entry_time{};
    int previous_side{};
    int position_quantity{};
    std::optional<std::int64_t> last_time;
    std::int64_t train_end{}, validation_end{};
    std::vector<Month> months;
    std::vector<Trade> trades;
    Segment segments[3];
};
struct Candidate
{
    std::string id, json;
};
struct Result
{
    std::string id;
    Decimal profit{}, nav{}, realized{}, unrealized{}, fees{}, dd{}, dd_points{}, win_rate{};
    std::size_t trades{};
    int position{};
    std::optional<Decimal> profit_factor;
    Segment segments[3];
};
Result save(const Candidate& candidate, const Ledger& ledger, const std::filesystem::path& dir)
{
    auto monthly = file(dir / (candidate.id + "-monthly.csv"));
    monthly << "MonthUTC,LastPriceRawTime,Bars,StartEquity,EndEquity,StartNAV,EndNAV,MonthlyNetProfit,MonthlyReturnPct,"
               "RealizedGrossProfit,UnrealizedProfit,Commissions,ClosedTrades,OpenContracts,DrawdownPctFromRunPeak,"
               "DrawdownNAVPoints\n";
    for (const auto& m : ledger.months)
    {
        monthly << month_text(m.id) << ',' << m.last_time << ',' << m.bars << ',' << m.start << ',' << m.equity << ','
                << 100 * m.start / ledger.o.capital << ',' << 100 * m.equity / ledger.o.capital << ','
                << m.equity - m.start << ',';
        if (m.start > 0)
            monthly << 100 * (m.equity / m.start - 1);
        monthly << ',' << m.realized << ',' << m.unrealized << ',' << m.fees << ',' << m.trades << ',' << m.position
                << ',' << m.drawdown << ',' << m.drawdown_points << '\n';
    }
    auto trades = file(dir / (candidate.id + "-trades.csv"));
    trades << "EntryRawTime,ExitRawTime,EntryPrice,ExitPrice,Contracts,Multiplier,GrossProfit,Commissions,NetProfit,"
              "ExitReason\n";
    Decimal wins{}, losses{};
    std::size_t winning{};
    for (const auto& t : ledger.trades)
    {
        const auto net = t.gross - t.fees;
        if (net > 0)
        {
            wins += net;
            ++winning;
        }
        else
            losses -= net;
        trades << t.entry_time << ',' << t.exit_time << ',' << t.entry << ',' << t.exit << ',' << t.quantity
               << ',' << ledger.o.multiplier << ',' << t.gross << ',' << t.fees << ',' << net << ',' << t.reason
               << '\n';
    }
    auto params = file(dir / (candidate.id + "-parameters.json"));
    params << candidate.json << '\n';
    Result r;
    r.id = candidate.id;
    r.profit = ledger.equity - ledger.o.capital;
    r.nav = 100 * ledger.equity / ledger.o.capital;
    r.realized = ledger.realized;
    r.unrealized = ledger.unrealized;
    r.fees = ledger.fees;
    r.dd = ledger.max_drawdown;
    r.dd_points = ledger.max_drawdown_points;
    r.trades = ledger.trades.size();
    r.position = ledger.open ? ledger.position_quantity : 0;
    r.win_rate = r.trades ? 100 * static_cast<Decimal>(winning) / static_cast<Decimal>(r.trades) : 0;
    if (losses > 0)
        r.profit_factor = wins / losses;
    std::copy(std::begin(ledger.segments), std::end(ledger.segments), std::begin(r.segments));
    return r;
}
void validate(const BacktestOptions& o)
{
    if (o.price_cache_mode != "off" && o.price_cache_mode != "use" && o.price_cache_mode != "refresh")
        throw std::invalid_argument("Price cache mode must be off, use, or refresh");
    if (o.price_cache_mode != "off" && (o.price_cache_directory.empty() || o.price_cache_source.empty()))
        throw std::invalid_argument("Caching requires a cache directory and source identity");
    if (o.dataset <= 0 || o.interval_minutes < 1 || o.interval_minutes > std::numeric_limits<int>::max() / 60 ||
        o.maximum_source_rows < 0 || o.quantity < 0 || o.jobs < 1 || o.jobs > 64)
        throw std::invalid_argument("Invalid dataset/interval/maximum/quantity/jobs for backtest");
    if (!std::isfinite(o.capital) || o.capital <= 0 || !std::isfinite(o.multiplier) || o.multiplier <= 0 ||
        !std::isfinite(o.commission) || o.commission < 0 || !std::isfinite(o.slippage_points) || o.slippage_points < 0)
        throw std::invalid_argument("Invalid backtest financial settings");
    if (!std::isfinite(o.quantity_pct) || o.quantity_pct <= 0 || o.quantity_pct > 1)
        throw std::invalid_argument("Quantity percentage must be greater than 0 and at most 1");
    const auto start = date_time(o.start_date), train = date_time(o.train_end),
               validation = date_time(o.validation_end);
    if (start >= train || train >= validation || (!o.end_date.empty() && date_time(o.end_date) <= start))
        throw std::invalid_argument("Dates must satisfy start < train-end < validation-end and end > start");
}
} // namespace

void run_backtest_batch(const MainRepository& repository, const BacktestOptions& o)
{
    validate(o);
    std::vector<std::filesystem::path> paths;
    for (const auto& entry : std::filesystem::directory_iterator(o.parameters_directory))
        if (entry.is_regular_file() && entry.path().extension() == ".json")
            paths.push_back(entry.path());
    std::sort(paths.begin(), paths.end());
    if (paths.empty())
        throw std::invalid_argument("No JSON parameter files in parameters directory");
    StudyRegistry registry;
    register_integrated_collections(registry);
    std::vector<Candidate> candidates;
    for (std::size_t n = 0; n < paths.size(); ++n)
    {
        std::ostringstream id;
        id << "run-" << std::setw(4) << std::setfill('0') << n + 1;
        auto json = read(paths[n]);
        (void)registry.create(StudyCollectionConfig{.type = "ADXBase", .parameters = json}, Ticker{}, Interval{});
        candidates.push_back({id.str(), std::move(json)});
    }
    // Never mix reports from different batches or overwrite an earlier optimization.
    if (o.output_directory.empty() ||
        (std::filesystem::exists(o.output_directory) && !std::filesystem::is_empty(o.output_directory)))
        throw std::invalid_argument("Choose a new or empty backtest output directory");
    std::vector<PriceBar> bars;
    const auto end = o.end_date.empty() ? std::numeric_limits<std::int64_t>::max() : date_time(o.end_date);
    PriceAggregator aggregator(o.interval_minutes * 60);
    const auto aggregate = [&](const PriceBar& p)
        {
            if (p.raw_time >= end)
                return;
            if (o.interval_minutes == 1)
                bars.push_back(p);
            else if (const auto completed = aggregator.process(p))
                bars.push_back(*completed);
        };
    std::size_t source_count{};
    std::string cache_status = "off";
    std::filesystem::path used_cache;
    const auto finish_aggregation = [&]()
    {
        if (const auto tail = aggregator.finish()) bars.push_back(*tail);
    };
    if (o.price_cache_mode == "off")
    {
        std::cout << "Reading and aggregating dataset " << o.dataset << " once..." << std::endl;
        source_count = repository.for_each_price(o.dataset, aggregate, o.maximum_source_rows);
        finish_aggregation();
    }
    else
    {
        std::filesystem::create_directories(o.price_cache_directory);
        const auto identity = std::to_string(price_cache_hash(o.price_cache_source)) + "-d" +
                              std::to_string(o.dataset) + "-n" + std::to_string(o.maximum_source_rows);
        PriceCacheLock lock(o.price_cache_directory / (identity + ".lock"));
        const auto raw_path = o.price_cache_directory / (identity + "-raw-v1.bin");
        PriceCacheHeader raw;
        bool newly_loaded = o.price_cache_mode == "refresh" || !std::filesystem::exists(raw_path);
        if (newly_loaded)
        {
            std::cout << "Loading database prices and saving raw snapshot..." << std::endl;
            PriceCacheWriter writer(raw_path);
            source_count = repository.for_each_price(o.dataset, [&](const PriceBar& p)
                { writer.add(p); aggregate(p); }, o.maximum_source_rows);
            raw = writer.finish(source_count);
            finish_aggregation();
        }
        else raw = price_cache_header(raw_path);
        source_count = static_cast<std::size_t>(raw.source_count);
        // Raw content fingerprint invalidates every interval/end-date derivative after a refresh.
        used_cache = o.price_cache_directory / (identity + "-" + std::to_string(raw.checksum) + "-" +
            std::to_string(raw.count) + "-i" + std::to_string(o.interval_minutes) + "-e" +
            std::to_string(end) + "-aggregate-v1.bin");
        if (!newly_loaded && std::filesystem::exists(used_cache))
        {
            std::cout << "Reusing aggregated price snapshot: " << used_cache.string() << std::endl;
            const auto saved = read_price_cache(used_cache, [&](const PriceBar& p) { bars.push_back(p); });
            if (saved.source_count != raw.source_count) throw std::runtime_error("Cache source count mismatch; refresh required.");
            cache_status = "aggregate-hit";
        }
        else
        {
            if (!newly_loaded)
            {
                std::cout << "Reusing raw price snapshot; aggregating requested interval..." << std::endl;
                read_price_cache(raw_path, aggregate);
                finish_aggregation();
            }
            PriceCacheWriter writer(used_cache);
            for (const auto& p : bars) writer.add(p);
            writer.finish(source_count);
            cache_status = newly_loaded ? "database-snapshot" : "raw-hit";
        }
    }
    if (source_count == 0)
        throw std::runtime_error("No PriceData rows for dataset " + std::to_string(o.dataset) +
                                 " in the configured database");
    const auto start = date_time(o.start_date);
    auto first = std::lower_bound(
        bars.begin(), bars.end(), start, [](const PriceBar& b, std::int64_t t) { return b.raw_time < t; });
    if (first == bars.end())
        throw std::runtime_error("No prices in selected backtest dates");
    for (const auto& p : bars)
        if (!std::isfinite(p.close) || !std::isfinite(p.open) || !std::isfinite(p.high) || !std::isfinite(p.low) ||
            p.high < p.low)
            throw std::runtime_error("Invalid OHLC data in backtest");
    const auto first_index = static_cast<std::size_t>(first - bars.begin());
    if (first->raw_time >= date_time(o.train_end))
        std::cout << "Warning: no training-period bars; training ranks will be tied. Adjust dates before selecting "
                     "parameters."
                  << std::endl;
    std::filesystem::create_directories(o.output_directory);
    {
        auto manifest = file(o.output_directory / "batch-settings.txt");
        manifest
            << "Collection=ADXBase\nSignal=STrend_LBaseADX_Side\nDataset=" << o.dataset
            << "\nIntervalMinutes=" << o.interval_minutes << "\nInitialCapital=" << o.capital
            << "\nContracts=" << o.quantity << "\nMultiplier=" << o.multiplier
            << "\nSizingMode=" << (o.quantity == 0 ? "efs-aum-hlc3" : "fixed")
            << "\nQuantityPct=" << o.quantity_pct << "\nReinvestAUM=0"
            << "\nCommissionPerContractPerSide=" << o.commission << "\nSlippagePointsPerSide=" << o.slippage_points
            << "\nForceClose=" << o.force_close << "\nStartUTC=" << o.start_date << "\nEndExclusiveUTC=" << o.end_date
            << "\nTrainEndExclusiveUTC=" << o.train_end << "\nValidationEndExclusiveUTC=" << o.validation_end
            << "\nSourceRowsRead=" << source_count << "\nAggregatedBars=" << bars.size()
            << "\nPriceCache=" << cache_status << "\nPriceCacheFile=" << used_cache.string()
            << "\nDatabaseRowsReadThisRun=" << ((cache_status == "off" || cache_status == "database-snapshot") ? source_count : 0)
            << "\nFirstRawTime=" << first->raw_time << "\nLastRawTime=" << bars.back().raw_time
            << "\nCandidates=" << candidates.size()
            << "\nFill=signal bar close plus/minus configured slippage\nMonths=UTC\nRanking=training net profit only\n"
            << "Segments=continuous positions/equity, marked at boundaries; not separate restarted strategies\n"
            << "Partial first and final buckets included; prices fetched from live database are not a transaction "
               "snapshot\n";
        auto input = file(o.output_directory / "prices.csv");
        input << "Id,DataSetId,RawTime,Open,High,Low,Close,Volume\n";
        for (const auto& p : bars)
            input << p.id << ',' << p.data_set_id << ',' << p.raw_time << ',' << p.open << ',' << p.high << ',' << p.low
                  << ',' << p.close << ',' << p.volume << '\n';
        auto mapping = file(o.output_directory / "candidate-files.csv");
        mapping << "RunId,InputParameterFile\n";
        for (std::size_t n = 0; n < paths.size(); ++n)
            mapping << candidates[n].id << ',' << csv_quoted(paths[n].string()) << '\n';
    }
    std::cout << bars.size() << " aggregated bars; " << candidates.size() << " candidates; " << o.jobs << " workers."
              << std::endl;
    std::vector<Result> results(candidates.size());
    std::atomic_size_t next{}, done{};
    std::atomic_bool failed{};
    std::exception_ptr error;
    std::mutex output_mutex;
    std::vector<std::jthread> workers;
    for (int worker = 0; worker < o.jobs; ++worker)
        workers.emplace_back(
            [&]
            {
                try
                {
                    while (!failed)
                    {
                        const auto index = next.fetch_add(1);
                        if (index >= candidates.size())
                            break;
                        const auto& candidate = candidates[index];
                        auto collection =
                            registry.create(StudyCollectionConfig{.type = "ADXBase", .parameters = candidate.json},
                                            Ticker{},
                                            Interval{});
                        Ledger ledger(o);
                        for (std::size_t n = 0; n < bars.size(); ++n)
                        {
                            if (failed)
                                return;
                            const auto study = collection->process(bars[n]);
                            if (n < first_index)
                                continue;
                            const auto side = study.studies.at("STrend_LBaseADX_Side");
                            if (!side || (*side != 0 && *side != 1))
                                throw std::runtime_error("Missing/invalid long signal");
                            ledger.process(bars[n], static_cast<int>(*side), n + 1 == bars.size());
                        }
                        results[index] = save(candidate, ledger, o.output_directory);
                        std::lock_guard lock(output_mutex);
                        std::cout << "Completed " << ++done << '/' << candidates.size() << ": " << candidate.id
                                  << " NAV=" << results[index].nav << std::endl;
                    }
                }
                catch (...)
                {
                    failed = true;
                    std::lock_guard lock(output_mutex);
                    if (!error)
                        error = std::current_exception();
                }
            });
    workers.clear(); // jthread joins before reading results.
    if (error)
        std::rethrow_exception(error);
    std::stable_sort(results.begin(),
                     results.end(),
                     [](const Result& a, const Result& b)
                     { return a.segments[0].end - a.segments[0].start > b.segments[0].end - b.segments[0].start; });
    auto summary = file(o.output_directory / "results.csv");
    summary << "TrainingProfitRank,RunId,NetProfit,FinalNAV,RealizedGrossProfit,UnrealizedProfit,Commissions,"
               "MaxDrawdownPct,MaxDrawdownNAVPoints,ClosedTrades,WinRatePct,ProfitFactor,OpenContracts";
    for (const auto* name : {"Train", "Validation", "Test"})
        summary << ',' << name << "Bars," << name << "NetProfit," << name << "ReturnPct," << name << "MaxDrawdownPct,"
                << name << "ClosedTrades";
    summary << ",ParametersJSON\n";
    for (std::size_t n = 0; n < results.size(); ++n)
    {
        const auto& r = results[n];
        summary << n + 1 << ',' << r.id << ',' << r.profit << ',' << r.nav << ',' << r.realized << ',' << r.unrealized
                << ',' << r.fees << ',' << r.dd << ',' << r.dd_points << ',' << r.trades << ',' << r.win_rate << ',';
        if (r.profit_factor)
            summary << *r.profit_factor;
        summary << ',' << r.position;
        for (const auto& s : r.segments)
        {
            summary << ',' << s.bars << ',';
            if (s.bars)
                summary << s.end - s.start;
            summary << ',';
            if (s.bars && s.start > 0)
                summary << 100 * (s.end / s.start - 1);
            summary << ',';
            if (s.bars)
                summary << s.drawdown;
            summary << ',' << s.trades;
        }
        const auto c = std::find_if(
            candidates.begin(), candidates.end(), [&](const Candidate& value) { return value.id == r.id; });
        auto json = c->json;
        json.erase(std::remove(json.begin(), json.end(), '\r'), json.end());
        json.erase(std::remove(json.begin(), json.end(), '\n'), json.end());
        summary << ',' << csv_quoted(json) << '\n';
    }
    std::cout << "Batch complete. Reports: " << o.output_directory.string() << std::endl;
}

void backtest_self_test()
{
    auto check = [](bool ok)
    {
        if (!ok)
            throw std::runtime_error("Backtest accounting regression failed");
    };
    check(csv_quoted("{\"x\":1}") == "\"{\"\"x\"\":1}\"");
    BacktestOptions o;
    Ledger l(o);
    const auto jan = date_time("2022-01-31"), feb = date_time("2022-02-01"), april = date_time("2022-04-01");
    l.process(PriceBar{.raw_time = jan, .close = 100}, 1);
    l.process(PriceBar{.raw_time = jan + 300, .close = 102}, 1);
    check(l.fees == 24 && l.unrealized == 600 && l.trades.empty());
    l.process(PriceBar{.raw_time = feb, .close = 103}, 0);
    check(l.realized == 900 && l.fees == 48 && l.equity == o.capital + 852);
    check(l.trades.size() == 1 && l.months.size() == 2 && l.months[0].equity == l.months[1].start);
    l.process(PriceBar{.raw_time = april, .close = 99}, 0);
    check(l.months.size() == 4 && l.months[2].bars == 0 && l.months[2].equity == l.months[1].equity);
    Decimal monthly_profit{};
    for (const auto& m : l.months)
        monthly_profit += m.equity - m.start;
    check(monthly_profit == l.equity - o.capital);
    o.slippage_points = 0.25L;
    o.force_close = true;
    Ledger forced(o);
    forced.process(PriceBar{.raw_time = jan, .close = 100}, 1);
    forced.process(PriceBar{.raw_time = feb, .close = 101}, 1, true);
    check(!forced.open && forced.realized == 150 && forced.fees == 48 && forced.equity == o.capital + 102);
    o.force_close = false;
    Ledger marked(o);
    marked.process(PriceBar{.raw_time = jan, .close = 100}, 1);
    marked.process(PriceBar{.raw_time = date_time("2023-01-01"), .close = 101}, 1, true);
    check(marked.open && marked.trades.empty() && marked.fees == 24 && marked.unrealized == 225);
    check(marked.segments[0].end - marked.segments[0].start + marked.segments[1].end - marked.segments[1].start ==
          marked.equity - o.capital);
    BacktestOptions sized_options;
    sized_options.capital = 1200000;
    sized_options.quantity = 0;
    validate(sized_options);
    Ledger sized(sized_options);
    const auto check_near = [&](Decimal a, Decimal b) { check(std::abs(a - b) < 0.000001L); };
    // HLC3 is 5000, while close is 4900: proves sizing does not use close alone.
    sized.process(PriceBar{.raw_time = jan, .high = 5200, .low = 4900, .close = 4900}, 1);
    check(sized.position_quantity == 12);
    check_near(sized.fees, 19.2L);
    sized.process(PriceBar{.raw_time = feb, .high = 20000, .low = 20000, .close = 20000}, 1);
    check(sized.position_quantity == 12 && sized.months.back().position == 12);
    check_near(sized.unrealized, 3624000);
    sized.process(PriceBar{.raw_time = feb + 300, .close = 5100}, 0);
    check(sized.trades[0].quantity == 12 && !sized.open);
    check_near(sized.trades[0].gross, 48000);
    check_near(sized.trades[0].fees, 38.4L);
    // Initial AUM is retained despite profits; reinvesting would round up to three.
    sized.process(PriceBar{.raw_time = feb + 600, .high = 31500, .low = 28500, .close = 30000}, 1);
    check(sized.position_quantity == 2);
    sized.process(PriceBar{.raw_time = april, .close = 29900}, 0);
    check(sized.trades[1].quantity == 2 && sized.months[2].position == 2);
    check_near(sized.equity, 1243955.2L);
    Decimal sized_month_profit{};
    for (const auto& m : sized.months) sized_month_profit += m.equity - m.start;
    check_near(sized_month_profit, sized.equity - sized_options.capital);
    sized_options.quantity_pct = 0.5L;
    sized_options.force_close = true;
    Ledger fractional(sized_options);
    fractional.process(PriceBar{.raw_time = jan, .high = 30850, .low = 30850, .close = 30850}, 1);
    check(fractional.position_quantity == 1); // ceil(30000 / 30850), not truncation to zero.
    fractional.process(PriceBar{.raw_time = feb, .close = 30950}, 1, true);
    check(fractional.trades[0].quantity == 1 && !fractional.open);
    check_near(fractional.equity, 1201996.8L);
    bool rejected = false;
    try { Ledger invalid(sized_options); invalid.process(PriceBar{.raw_time = jan}, 1); }
    catch (const std::runtime_error&) { rejected = true; }
    check(rejected);
    rejected = false;
    try { Ledger invalid(sized_options); invalid.process(PriceBar{.raw_time = jan, .high = 1e-20L}, 1); }
    catch (const std::runtime_error&) { rejected = true; }
    check(rejected);
}
} // namespace fig
