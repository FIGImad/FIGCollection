#include "fig/studies/study_report.hpp"
#include "fig/prices/price_aggregator.hpp"
#include <iomanip>
#include <limits>
#include <locale>
#include <ostream>
#include <sstream>
#include <stdexcept>

namespace fig
{
StudyCalculator::StudyCalculator(const StudyReportOptions& o)
    : source_(o.source), ma_(o.period, o.source), wma_(o.period, o.source), sem_(o.period, o.source),
      bb_(o.period, 2, o.source), extended_(o.period, o.source), adx_(o.period, o.adx_smoothing), roc_(o.period),
      slope_(3, 0.5L), trend_({o.period, o.period, 2, o.period, o.adx_smoothing, 20})
{
    (void)studies::source_value(PriceBar{}, o.source); // Validate before any database/output work.
}

StudyBar StudyCalculator::process(const PriceBar& bar)
{
    StudyBar result{bar, {}};
    auto& v = result.studies;
    const auto ma = ma_.process(bar);
    v["MA"] = ma;
    v["WMA"] = wma_.process(bar);
    v["SEM"] = sem_.process(bar);
    v["ROC"] = roc_.process(bar.raw_time, studies::source_value(bar, source_));
    const auto bb = bb_.process(bar);
    v["BB_Middle"] = bb.middle;
    v["BB_Upper"] = bb.upper;
    v["BB_Lower"] = bb.lower;
    const auto e = extended_.process(bar);
    v["Extended_MA"] = e.moving_average;
    v["Extended_SEM"] = e.sem;
    v["Extended_Lower1"] = e.lower1;
    v["Extended_Upper1"] = e.upper1;
    v["Extended_Lower2"] = e.lower2;
    v["Extended_Upper2"] = e.upper2;
    const auto a = adx_.process(bar);
    v["ADX"] = a.adx;
    v["PDI"] = a.pdi;
    v["NDI"] = a.ndi;
    const auto s = slope_.process(bar.raw_time, ma);
    // The underlying slope study uses zero for missing inputs; keep warmup blank in the report.
    v["Slope_Direction"] = ma ? std::optional<Decimal>(s.direction) : std::nullopt;
    v["Slope_PerBar"] = ma ? std::optional<Decimal>(s.slope_per_bar) : std::nullopt;
    for (const auto& [name, value] : trend_.process(bar).studies)
        v["ADXTrend_" + name] = value;
    return result;
}

namespace
{
void write_header(std::ostream& output, const StudyBar& bar)
{
    output << "Id,DataSetId,RawTime,Open,High,Low,Close,Volume";
    for (const auto& [name, value] : bar.studies)
    {
        (void)value;
        output << ',' << name;
    }
    for (const auto& [name, value] : bar.text_studies)
    {
        (void)value;
        output << ',' << name;
    }
    output << '\n';
}

void write_row(std::ostream& output, const StudyBar& bar)
{
    const auto& p = bar.price;
    output << p.id << ',' << p.data_set_id << ',' << p.raw_time << ',' << p.open << ',' << p.high << ',' << p.low << ','
           << p.close << ',' << p.volume;
    for (const auto& [name, value] : bar.studies)
    {
        (void)name;
        output << ',';
        if (value)
            output << *value;
    }
    for (const auto& [name, value] : bar.text_studies)
    {
        (void)name;
        output << ",\"";
        for (const char c : value)
        {
            if (c == '\"')
                output << '\"';
            output << c;
        }
        output << '\"';
    }
    output << '\n';
}
} // namespace

std::size_t write_study_report(const MainRepository& repository,
                               int data_set_id,
                               const StudyReportOptions& options,
                               std::ostream& output,
                               int maximum,
                               const StudyCollectionConfig* collection)
{
    if (options.interval_minutes < 1 || options.interval_minutes > std::numeric_limits<int>::max() / 60)
        throw std::invalid_argument("Interval minutes must be positive and fit in seconds");
    PriceAggregator aggregator(options.interval_minutes * 60);
    std::unique_ptr<StudyCalculator> calculator;
    std::unique_ptr<StudyCollection> integrated;
    if (collection)
    {
        StudyRegistry registry;
        register_integrated_collections(registry);
        integrated = registry.create(*collection, Ticker{}, Interval{});
    }
    else
        calculator = std::make_unique<StudyCalculator>(options);
    output.imbue(std::locale::classic());
    output << std::setprecision(std::numeric_limits<Decimal>::max_digits10);
    bool first = true;
    std::size_t count{};
    const auto consume = [&](const PriceBar& price)
    {
        const auto bar = integrated ? integrated->process(price) : calculator->process(price);
        if (first)
        {
            write_header(output, bar);
            first = false;
        }
        write_row(output, bar);
        if (!output)
            throw std::runtime_error("Could not write study report");
        ++count;
    };
    repository.for_each_price(
        data_set_id,
        [&](const PriceBar& price)
        {
            if (options.interval_minutes == 1)
                consume(price);
            else if (const auto completed = aggregator.process(price))
                consume(*completed);
        },
        maximum);
    if (const auto last = aggregator.finish())
        consume(*last);
    if (first)
        throw std::runtime_error("No prices found for the requested DataSetId");
    output.flush();
    if (!output)
        throw std::runtime_error("Could not finish writing study report");
    return count;
}

void study_report_self_test()
{
    StudyCalculator calculator({3, 3, "close"});
    auto result = calculator.process(PriceBar{.raw_time = 1, .close = 1});
    if (result.studies.at("MA"))
        throw std::runtime_error("report warmup test failed");
    std::ostringstream csv;
    write_header(csv, result);
    write_row(csv, result);
    if (csv.str().find("Id,DataSetId,RawTime,Open,High,Low,Close,Volume,") != 0 ||
        csv.str().find(",MA,") == std::string::npos)
        throw std::runtime_error("report CSV header test failed");
    (void)calculator.process(PriceBar{.raw_time = 2, .close = 2});
    result = calculator.process(PriceBar{.raw_time = 3, .close = 3});
    if (result.studies.at("MA") != 2 || result.studies.at("Extended_MA") != 2)
        throw std::runtime_error("report calculation test failed");
    result = calculator.process(PriceBar{.raw_time = 3, .close = 6});
    if (result.studies.at("MA") != 3)
        throw std::runtime_error("report replacement test failed");
}
} // namespace fig
