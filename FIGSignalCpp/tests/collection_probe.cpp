// Offline collection runner used by the EFS parity tests. No database access.
#include "fig/studies/collection_plugin.hpp"
#include <cmath>
#include <fstream>
#include <iomanip>
#include <iostream>
#include <limits>
#include <locale>
#include <sstream>
#include <stdexcept>

namespace
{
void quote(std::ostream& out, const std::string& text)
{
    out << '"';
    for (const auto c : text)
    {
        if (c == '"')
            out << '"';
        out << c;
    }
    out << '"';
}
} // namespace
int wmain(int argc, wchar_t** argv)
{
    try
    {
        if (argc != 6)
            throw std::invalid_argument("Usage: FIGCollectionProbe.exe DLL TYPE PARAMETERS.json PRICES.csv OUTPUT.csv");
        const fig::CollectionPlugin plugin(argv[1]);
        fig::StudyRegistry registry;
        plugin.register_collections(registry);
        std::ifstream parameters{std::filesystem::path(argv[3])};
        if (!parameters)
            throw std::invalid_argument("Cannot open parameters");
        auto collection = registry.create(
            fig::StudyCollectionConfig{.type = std::filesystem::path(argv[2]).string(),
                                       .parameters = std::string(std::istreambuf_iterator<char>(parameters), {})},
            fig::Ticker{},
            fig::Interval{});
        std::ifstream input{std::filesystem::path(argv[4])};
        if (!input)
            throw std::invalid_argument("Cannot open input prices");
        std::ofstream output{std::filesystem::path(argv[5])};
        if (!output)
            throw std::invalid_argument("Cannot open output");
        output.exceptions(std::ios::failbit | std::ios::badbit);
        output.imbue(std::locale::classic());
        output << std::setprecision(std::numeric_limits<fig::Decimal>::max_digits10);
        std::string line;
        if (!std::getline(input, line) || line != "RawTime,Open,High,Low,Close,Volume")
            throw std::invalid_argument("Unexpected input price header");
        bool first = true;
        std::vector<std::string> columns, text_columns;
        while (std::getline(input, line))
        {
            std::istringstream row(line);
            row.imbue(std::locale::classic());
            fig::PriceBar price;
            char separators[5]{};
            if (!(row >> price.raw_time >> separators[0] >> price.open >> separators[1] >> price.high >>
                  separators[2] >> price.low >> separators[3] >> price.close >> separators[4] >> price.volume))
                throw std::invalid_argument("Invalid input price row");
            for (const auto c : separators)
                if (c != ',')
                    throw std::invalid_argument("Expected CSV separator");
            row >> std::ws;
            if (!row.eof())
                throw std::invalid_argument("Unexpected text after price row");
            const auto bar = collection->process(price);
            if (first)
            {
                output << "RawTime";
                for (const auto& [name, value] : bar.studies)
                {
                    (void)value;
                    columns.push_back(name);
                    output << ',';
                    quote(output, name);
                }
                for (const auto& [name, value] : bar.text_studies)
                {
                    (void)value;
                    text_columns.push_back(name);
                    output << ',';
                    quote(output, name);
                }
                output << '\n';
                first = false;
            }
            if (columns.size() != bar.studies.size() || text_columns.size() != bar.text_studies.size())
                throw std::runtime_error("Collection output schema changed during replay");
            output << price.raw_time;
            for (const auto& name : columns)
            {
                output << ',';
                if (const auto value = bar.studies.at(name))
                    output << *value;
            }
            for (const auto& name : text_columns)
            {
                output << ',';
                quote(output, bar.text_studies.at(name));
            }
            output << '\n';
        }
        if (!input.eof() || first)
            throw std::runtime_error("Input prices were empty or unreadable");
        return 0;
    }
    catch (const std::exception& error)
    {
        std::cerr << error.what() << '\n';
        return 1;
    }
}
