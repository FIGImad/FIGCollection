#pragma once
#include "FigDataReader.h"
#include <chrono>

namespace figcharts {
inline void testFigData() {
    using namespace binary;
    auto path = std::filesystem::temp_directory_path() / ("FIGCharts-test-" + std::to_string(std::chrono::steady_clock::now().time_since_epoch().count()) + ".figdata");
    struct Cleanup { std::filesystem::path path; ~Cleanup() { std::error_code ec; std::filesystem::remove(path, ec); } } cleanup{path};
    Header h{}; std::memcpy(h.magic, "FIGDATA\0", 8); h.version = 1; h.headerBytes = h.descriptorBytes = 128;
    h.rows = 3; h.columns = 8; h.columnsOffset = 128; h.firstTimestamp = 100; h.lastTimestamp = 220;
    std::vector<Column> columns(8);
    std::vector<char> bytes(128 + columns.size() * 128);
    auto append = [&](const void* source, size_t length) { auto p = static_cast<const char*>(source); bytes.insert(bytes.end(), p, p + length); };
    const char* names[] = {"Volume", "RawTime", "Open", "High", "Low", "Close", "VariableStudyName", "Reason"};
    const uint32_t roles[] = {6, 1, 2, 3, 4, 5, 7, 7};
    const double values[][3] = {{0,0,0}, {0,0,0}, {2,3,4}, {4,5,6}, {1,2,3}, {3,4,5}, {0,7.5,8.5}};
    const uint32_t volume[]{10,20,30}, codes[]{0,0,0}; const int64_t times[]{100,160,220};
    for (size_t i = 0; i < columns.size(); ++i) {
        auto& c = columns[i]; c.role = roles[i]; c.type = i == 0 ? 3 : i == 1 ? 1 : i == 7 ? 4 : 2;
        c.dataOffset = bytes.size(); c.dataBytes = c.type <= 2 ? 24 : 12;
        if (i == 0) append(volume, 12); else if (i == 1) append(times, 24);
        else if (i == 7) append(codes, 12); else append(values[i], 24);
    }
    columns[6].validityOffset = bytes.size(); columns[6].validityBytes = 1; bytes.push_back(6);
    columns[7].dictionaryOffset = bytes.size();
    uint32_t entries = 1, length = 4; append(&entries, 4); append(&length, 4); append("exit", 4);
    columns[7].dictionaryBytes = 12;
    h.stringsOffset = bytes.size();
    for (size_t i = 0; i < columns.size(); ++i) {
        columns[i].nameOffset = bytes.size() - h.stringsOffset; columns[i].nameBytes = static_cast<uint32_t>(std::strlen(names[i])); append(names[i], columns[i].nameBytes);
    }
    h.stringsBytes = bytes.size() - h.stringsOffset; h.fileBytes = bytes.size();
    std::memcpy(bytes.data(), &h, 128); std::memcpy(bytes.data() + 128, columns.data(), columns.size() * 128);
    auto write = [&] { std::ofstream out(path, std::ios::binary); out.write(bytes.data(), bytes.size()); };
    write(); auto d = CsvData::load(path);
    if (d.time.size() != 3 || d.volume[2] != 30 || d.close[1] != 4 || d.studies.size() != 1 ||
        d.studies[0].name != "VariableStudyName" || d.studies[0].values[0] != Chart::NoValue || d.studies[0].values[2] != 8.5)
        throw std::runtime_error("FIGDATA compatibility regression");
    auto good = bytes;
    auto rejects = [&] {
        write(); bool rejected = false; try { (void)CsvData::load(path); } catch (const std::exception&) { rejected = true; }
        if (!rejected) throw std::runtime_error("Malformed FIGDATA accepted"); bytes = good;
    };
    bytes.resize(bytes.size() - 1); rejects();
    bytes[8] = 2; rejects(); // unsupported version
    uint64_t badOffset = UINT64_MAX; std::memcpy(bytes.data() + 128 + offsetof(Column, dataOffset), &badOffset, 8); rejects();
    bytes[static_cast<size_t>(columns[6].validityOffset)] = 0; write();
    if (CsvData::load(path).studies[0].values[2] != Chart::NoValue) throw std::runtime_error("FIGDATA validity regression");
}
}
