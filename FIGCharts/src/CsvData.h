#pragma once
#include <filesystem>
#include <string>
#include <vector>

namespace figcharts {
struct Series { std::string name; std::vector<double> values; };
struct CsvData {
    std::vector<double> time, open, high, low, close, volume;
    std::vector<Series> studies;
    static CsvData load(const std::filesystem::path& path);
};
void testCsv();
}
