#include "fig/studies/study_report.hpp"
#include "fig/prices/price_aggregator.hpp"
#include "fig/studies/collection_plugin.hpp"
#include <iomanip>
#include <limits>
#include <locale>
#include <ostream>
#include <sstream>
#include <stdexcept>

namespace fig
{
StudyCalculator::StudyCalculator(const StudyReportOptions& options)
{
    if (options.source.empty() ||
        options.source.find_first_not_of("abcdefghijklmnopqrstuvwxyz0123456789") != std::string::npos)
        throw std::invalid_argument("Invalid price source");
    StudyRegistry registry;
    CollectionPlugin{options.collection_dll}.register_collections(registry);
    const auto parameters = "{\"period\":" + std::to_string(options.period) +
                            ",\"adx_smoothing\":" + std::to_string(options.adx_smoothing) + ",\"source\":\"" +
                            options.source + "\"}";
    collection_ = registry.create(
        StudyCollectionConfig{.type = "ADXDiagnostics", .parameters = parameters}, Ticker{}, Interval{});
}
StudyBar StudyCalculator::process(const PriceBar& bar)
{
    return collection_->process(bar);
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
    std::unique_ptr<StudyCollection> selected;
    if (collection)
    {
        StudyRegistry registry;
        CollectionPlugin{options.collection_dll}.register_collections(registry);
        selected = registry.create(*collection, Ticker{}, Interval{});
    }
    else
        calculator = std::make_unique<StudyCalculator>(options);
    output.imbue(std::locale::classic());
    output << std::setprecision(std::numeric_limits<Decimal>::max_digits10);
    bool first = true;
    std::size_t count{};
    const auto consume = [&](const PriceBar& price)
    {
        const auto bar = selected ? selected->process(price) : calculator->process(price);
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
