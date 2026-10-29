#pragma once
#include "CsvData.h"
#include <chartdir.h>
#include <array>
#include <bit>
#include <cmath>
#include <cstdint>
#include <cstring>
#include <fstream>
#include <limits>
#include <set>
#include <stdexcept>

namespace figcharts::binary {
// FIGSignalCpp figdata.hpp v1 wire layout. In particular, the writer does NOT
// pad column blocks to 8 bytes. Read bytes into aligned storage, never cast them.
#pragma pack(push, 1)
struct Header {
    char magic[8]; uint32_t version, headerBytes; uint64_t rows;
    uint32_t columns, descriptorBytes;
    uint64_t columnsOffset, stringsOffset, stringsBytes, fileBytes;
    int64_t firstTimestamp, lastTimestamp;
    uint32_t intervalSeconds, flags; uint8_t reserved[40];
};
struct Column {
    uint64_t nameOffset; uint32_t nameBytes, type;
    uint64_t titleOffset; uint32_t titleBytes, role;
    uint64_t dataOffset, dataBytes, validityOffset, validityBytes, dictionaryOffset, dictionaryBytes;
    uint32_t display, colorARGB, panelGroup, flags; uint8_t reserved[32];
};
#pragma pack(pop)
static_assert(sizeof(Header) == 128 && sizeof(Column) == 128);
static_assert(std::endian::native == std::endian::little);
static_assert(sizeof(double) == 8 && std::numeric_limits<double>::is_iec559);

inline CsvData load(const std::filesystem::path& path) {
    auto fail = [](const std::string& why) { throw std::runtime_error("Invalid FIGDATA: " + why); };
    std::ifstream in(path, std::ios::binary | std::ios::ate);
    if (!in) throw std::runtime_error("Cannot open FIGDATA file.");
    const auto size = static_cast<uint64_t>(in.tellg());
    auto range = [&](uint64_t offset, uint64_t bytes) {
        if (offset > size || bytes > size - offset) fail("block extends beyond the file (incomplete export?)");
    };
    auto read = [&](uint64_t offset, void* target, uint64_t bytes) {
        range(offset, bytes); in.seekg(static_cast<std::streamoff>(offset));
        in.read(static_cast<char*>(target), static_cast<std::streamsize>(bytes));
        if (!in) fail("could not read block");
    };
    Header h{}; read(0, &h, sizeof(h));
    if (std::memcmp(h.magic, "FIGDATA\0", 8) != 0) fail("signature missing; export may not have finished");
    if (h.version != 1 || h.headerBytes != 128 || h.descriptorBytes != 128 || h.flags) fail("unsupported version/header");
    if (h.fileBytes != size) fail("file size does not match finalized header");
    if (!h.rows || h.rows > INT32_MAX || !h.columns) fail("invalid row/column count");
    range(h.columnsOffset, uint64_t(h.columns) * sizeof(Column)); range(h.stringsOffset, h.stringsBytes);
    std::vector<Column> columns(h.columns); read(h.columnsOffset, columns.data(), columns.size() * sizeof(Column));
    std::set<std::string> names;
    std::array<bool, 7> present{};
    CsvData result;
    for (const auto& c : columns) {
        auto stringAt = [&](uint64_t offset, uint32_t length) {
            if (offset > h.stringsBytes || length > h.stringsBytes - offset || length > 1024 * 1024) fail("invalid column name/title");
            std::string s(length, '\0'); if (length) read(h.stringsOffset + offset, s.data(), length); return s;
        };
        auto name = stringAt(c.nameOffset, c.nameBytes);
        if (name.empty() || !names.insert(name).second) fail("empty or duplicate column name");
        if (c.titleBytes) (void)stringAt(c.titleOffset, c.titleBytes);
        if (c.type < 1 || c.type > 4 || c.role < 1 || c.role > 10 || c.flags) fail("unsupported column descriptor: " + name);
        const uint64_t stride = c.type <= 2 ? 8 : 4;
        if (c.dataBytes != h.rows * stride) fail("wrong data length: " + name);
        range(c.dataOffset, c.dataBytes);
        if (c.validityOffset) {
            if (c.validityBytes != (h.rows + 7) / 8) fail("wrong validity length: " + name);
            range(c.validityOffset, c.validityBytes);
        } else if (c.validityBytes) fail("validity offset missing: " + name);
        if (c.type == 4) {
            if (c.dictionaryBytes < 4) fail("text dictionary missing: " + name);
            range(c.dictionaryOffset, c.dictionaryBytes);
        }
        const bool price = c.role <= 6;
        if (price && (present[c.role] || c.type == 4)) fail("duplicate or nonnumeric price role: " + name);
        if (price) present[c.role] = true;
        // Numeric study columns only. Text, colors and metadata are not plotted.
        if (!price && (c.role != 7 || c.type == 4)) continue;
        std::vector<double>* values = nullptr;
        switch (c.role) {
        case 1: values = &result.time; break; case 2: values = &result.open; break;
        case 3: values = &result.high; break; case 4: values = &result.low; break;
        case 5: values = &result.close; break; case 6: values = &result.volume; break;
        default: result.studies.push_back({name, {}}); values = &result.studies.back().values; break;
        }
        values->resize(static_cast<size_t>(h.rows));
        std::vector<uint8_t> validity(static_cast<size_t>(c.validityBytes));
        if (!validity.empty()) read(c.validityOffset, validity.data(), validity.size());
        std::array<char, 65536> buffer;
        for (uint64_t first = 0; first < h.rows;) {
            auto count = std::min<uint64_t>(buffer.size() / stride, h.rows - first);
            read(c.dataOffset + first * stride, buffer.data(), count * stride);
            for (uint64_t j = 0; j < count; ++j) {
                auto row = static_cast<size_t>(first + j);
                bool valid = validity.empty() || (validity[row / 8] & (1u << (row % 8)));
                if (!valid) {
                    if (price) fail("missing price/volume/timestamp at row " + std::to_string(row + 1));
                    (*values)[row] = Chart::NoValue; continue;
                }
                double value;
                if (c.type == 2) std::memcpy(&value, buffer.data() + j * stride, 8);
                else if (c.type == 1) { int64_t v; std::memcpy(&v, buffer.data() + j * stride, 8); value = static_cast<double>(v); }
                else { uint32_t v; std::memcpy(&v, buffer.data() + j * stride, 4); value = v; }
                if (!std::isfinite(value)) fail("nonfinite numeric value: " + name);
                (*values)[row] = value;
            }
            first += count;
        }
    }
    for (int role = 1; role <= 5; ++role) if (!present[role]) fail("required OHLC/timestamp column missing");
    if (!present[6]) result.volume.resize(static_cast<size_t>(h.rows), 0);
    for (size_t i = 0; i < result.time.size(); ++i) {
        double t = result.time[i], o = result.open[i], hi = result.high[i], lo = result.low[i], cl = result.close[i];
        if (t < 0 || t > 32503680000.0 || std::floor(t) != t || (i && t <= result.time[i - 1])) fail("timestamps must be increasing Unix seconds");
        if (lo > hi || o < lo || o > hi || cl < lo || cl > hi || result.volume[i] < 0) fail("inconsistent OHLC/volume at row " + std::to_string(i + 1));
    }
    if (result.time.front() != static_cast<double>(h.firstTimestamp) || result.time.back() != static_cast<double>(h.lastTimestamp)) fail("timestamp bounds mismatch");
    return result;
}
}
