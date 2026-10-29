#include "CsvData.h"
#include "FigDataReader.h"
#include <chartdir.h>
#include <fstream>
#include <cwctype>
#include <sstream>
#include <set>
#include <cmath>
#include <stdexcept>
#include <algorithm>

namespace figcharts {
namespace {
std::vector<std::string> record(std::istream& in) {
    std::vector<std::string> cells;
    std::string cell;
    bool quoted = false, any = false;
    char c;
    while (in.get(c)) {
        any = true;
        if (c == '"') {
            if (quoted && in.peek() == '"') { in.get(c); cell += '"'; }
            else quoted = !quoted;
        } else if (c == ',' && !quoted) { cells.push_back(cell); cell.clear(); }
        else if (c == '\n' && !quoted) { cells.push_back(cell); return cells; }
        else if (c != '\r' || quoted) cell += c;
    }
    if (quoted) throw std::runtime_error("Unclosed CSV quote.");
    if (any) cells.push_back(cell);
    return cells;
}
bool number(const std::string& s, double& n) {
    try { size_t end; n = std::stod(s, &end); return s.find_first_not_of(" \t", end) == std::string::npos && std::isfinite(n); }
    catch (...) { return false; }
}
CsvData parse(std::istream& in) {
    auto header = record(in);
    if (!header.empty() && header[0].starts_with("\xef\xbb\xbf")) header[0].erase(0, 3);
    std::set<std::string> unique;
    for (auto& h : header) if (!unique.insert(h).second) throw std::runtime_error("Duplicate CSV column: " + h);
    auto index = [&](const char* name) {
        auto it = std::find(header.begin(), header.end(), name);
        if (it == header.end()) throw std::runtime_error(std::string("Missing ") + name + ". Open prices.csv or a study report, not results/trades/monthly.csv.");
        return size_t(it - header.begin());
    };
    size_t t = index("RawTime"), o = index("Open"), h = index("High"), l = index("Low"), c = index("Close");
    auto vi = std::find(header.begin(), header.end(), "Volume");
    size_t v = size_t(vi - header.begin());
    CsvData data;
    std::vector<size_t> columns;
    for (size_t i = 0; i < header.size(); ++i) {
        if (i == t || i == o || i == h || i == l || i == c || i == v || header[i] == "Id" || header[i] == "DataSetId") continue;
        columns.push_back(i); data.studies.push_back({header[i], {}});
    }
    std::vector<bool> numeric(columns.size(), true), populated(columns.size(), false);
    size_t row = 1;
    for (;;) {
        auto cells = record(in);
        if (cells.empty()) break;
        ++row;
        if (cells.size() == 1 && cells[0].empty()) continue;
        auto fail = [&](const std::string& reason) { throw std::runtime_error("CSV row " + std::to_string(row) + ": " + reason); };
        if (cells.size() != header.size()) fail("column count differs from header");
        auto value = [&](size_t i) { double n; if (!number(cells[i], n)) fail("invalid " + header[i]); return n; };
        double timestamp = value(t), open = value(o), high = value(h), low = value(l), close = value(c);
        if (timestamp < 0 || timestamp > 32503680000.0 || std::floor(timestamp) != timestamp) fail("RawTime must be Unix seconds");
        if (!data.time.empty() && timestamp <= data.time.back()) fail("RawTime must be strictly increasing; export one dataset in chronological order");
        if (low > high || open < low || open > high || close < low || close > high) fail("inconsistent OHLC prices");
        double volume = v < header.size() ? value(v) : 0;
        if (volume < 0) fail("negative volume");
        data.time.push_back(timestamp); data.open.push_back(open); data.high.push_back(high);
        data.low.push_back(low); data.close.push_back(close); data.volume.push_back(volume);
        for (size_t j = 0; j < columns.size(); ++j) {
            double n = Chart::NoValue;
            if (!cells[columns[j]].empty()) {
                if (number(cells[columns[j]], n)) populated[j] = true;
                else { numeric[j] = false; n = Chart::NoValue; }
            }
            data.studies[j].values.push_back(n);
        }
    }
    if (data.time.empty()) throw std::runtime_error("CSV has no price bars.");
    for (size_t j = columns.size(); j-- > 0;) if (!numeric[j] || !populated[j]) data.studies.erase(data.studies.begin() + j);
    return data;
}
}
CsvData CsvData::load(const std::filesystem::path& path) {
    std::ifstream probe(path, std::ios::binary);
    char signature[8]{}; probe.read(signature, sizeof(signature));
    auto extension = path.extension().wstring();
    std::transform(extension.begin(), extension.end(), extension.begin(), [](wchar_t c) { return static_cast<wchar_t>(std::towlower(c)); });
    if (std::memcmp(signature, "FIGDATA\0", 8) == 0 || extension == L".figdata" || extension == L".raw" || extension == L".bin")
        return binary::load(path);
    std::ifstream in(path);
    if (!in) throw std::runtime_error("Cannot open CSV file.");
    return parse(in);
}
void testCsv() {
    std::istringstream good("\xef\xbb\xbfRawTime,Open,High,Low,Close,Volume,MA,Reason\r\n100,2,4,1,3,10,,\"a,b\"\r\n160,3,5,2,4,20,3.5,\"line\ntext\"\r\n");
    auto d = parse(good);
    if (d.time.size() != 2 || d.studies.size() != 1 || d.studies[0].values[0] != Chart::NoValue || d.studies[0].values[1] != 3.5) throw std::runtime_error("CSV regression failed");
    for (auto text : {"RawTime,Open,High,Low,Close\n1,3,2,1,2\n", "RawTime,Open,High,Low,Close\n1,1,2,0,1\n1,1,2,0,1\n", "RunId,NetProfit\nx,2\n", "RawTime,Open,High,Low,Close\n1,nan,2,0,1\n", "RawTime,Open,High,Low,Close\n1,1,2,0\n"}) {
        bool rejected = false; try { std::istringstream bad(text); parse(bad); } catch (...) { rejected = true; }
        if (!rejected) throw std::runtime_error("Invalid CSV accepted");
    }
}
}
