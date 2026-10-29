#pragma once
#include "fig/database/models.hpp"
#include <bit>
#include <cmath>
#include <cstdint>
#include <cstring>
#include <limits>
#include <map>
#include <ostream>
#include <stdexcept>
#include <vector>

namespace fig::figdata
{
enum class ColumnType : uint32_t { Int64 = 1, Float64 = 2, UInt32 = 3, DictionaryText = 4 };
enum class ColumnRole : uint32_t
{ Timestamp = 1, Open = 2, High = 3, Low = 4, Close = 5, Volume = 6, Study = 7, Metadata = 8, BarColor = 9, Background = 10 };
enum class DisplayMode : uint32_t { None = 0, PriceOverlay = 1, StudyPanel = 2 };
#pragma pack(push, 1)
struct FileHeader
{
    char magic[8];
    uint32_t version, headerBytes;
    uint64_t rowCount;
    uint32_t columnCount, columnDescriptorBytes;
    uint64_t columnsOffset, stringsOffset, stringsBytes, fileBytes;
    int64_t firstTimestamp, lastTimestamp;
    uint32_t intervalSeconds, flags;
    uint8_t reserved[40];
};
struct ColumnDescriptor
{
    uint64_t nameOffset;
    uint32_t nameBytes;
    ColumnType type;
    uint64_t titleOffset;
    uint32_t titleBytes;
    ColumnRole role;
    uint64_t dataOffset, dataBytes, validityOffset, validityBytes, dictionaryOffset, dictionaryBytes;
    DisplayMode display;
    uint32_t colorARGB, panelGroup, flags;
    uint8_t reserved[32];
};
#pragma pack(pop)
static_assert(sizeof(FileHeader) == 128);
static_assert(sizeof(ColumnDescriptor) == 128);
static_assert(std::endian::native == std::endian::little);
static_assert(sizeof(double) == 8 && std::numeric_limits<double>::is_iec559);

// Column buffers keep memory bounded regardless of history length; only validity
// bitmaps and distinct text values scale with the input. Final header is written last.
class Writer
{
    struct Column
    {
        std::string name;
        ColumnDescriptor descriptor{};
        std::vector<char> buffer;
        std::vector<uint8_t> validity;
        std::map<std::string, uint32_t> codes;
        std::vector<std::string> dictionary;
        uint64_t written{};
        bool missing{};
    };
    std::ostream& out;
    FileHeader header{};
    std::vector<Column> columns;
    std::vector<std::string> numeric, text;
    std::string collection;
    uint64_t rows{}, dataEnd{};
    bool finished{};
    static uint32_t size32(size_t n)
    {
        if (n > UINT32_MAX) throw std::runtime_error("FIGDATA string/dictionary too large");
        return static_cast<uint32_t>(n);
    }
    void bytes(const void* p, size_t n) { out.write(static_cast<const char*>(p), static_cast<std::streamsize>(n)); }
    template<class T> void scalar(T v) { bytes(&v, sizeof(v)); }
    void flush(Column& c)
    {
        out.seekp(static_cast<std::streamoff>(c.descriptor.dataOffset + c.written));
        bytes(c.buffer.data(), c.buffer.size());
        c.written += c.buffer.size();
        c.buffer.clear();
    }
    template<class T> void append(size_t index, T v, bool valid = true)
    {
        auto& c = columns.at(index);
        const auto* p = reinterpret_cast<const char*>(&v);
        c.buffer.insert(c.buffer.end(), p, p + sizeof(v));
        if (valid) c.validity[static_cast<size_t>(rows / 8)] |= uint8_t(1u << (rows % 8));
        else c.missing = true;
        if (c.buffer.size() >= 8192) flush(c);
    }
    void add(std::string name, ColumnType type, ColumnRole role, DisplayMode display = DisplayMode::None)
    {
        Column c;
        c.name = std::move(name);
        c.descriptor.type = type;
        c.descriptor.role = role;
        c.descriptor.display = display;
        c.validity.resize(static_cast<size_t>((header.rowCount + 7) / 8));
        columns.push_back(std::move(c));
    }
    void initialize(const StudyBar& bar)
    {
        add("Id", ColumnType::Int64, ColumnRole::Metadata);
        add("DataSetId", ColumnType::Int64, ColumnRole::Metadata);
        add("RawTime", ColumnType::Int64, ColumnRole::Timestamp);
        add("Open", ColumnType::Float64, ColumnRole::Open);
        add("High", ColumnType::Float64, ColumnRole::High);
        add("Low", ColumnType::Float64, ColumnRole::Low);
        add("Close", ColumnType::Float64, ColumnRole::Close);
        add("Volume", ColumnType::Int64, ColumnRole::Volume);
        add("SignalColor", ColumnType::UInt32, ColumnRole::Metadata);
        add("PriceBarColor", ColumnType::UInt32, ColumnRole::BarColor);
        add("BackgroundColor", ColumnType::UInt32, ColumnRole::Background);
        for (const auto& [name, value] : bar.studies)
        {
            numeric.push_back(name);
            const bool overlay = collection == "LADX2" &&
                (name == "LADX2_PriceMA" || name == "LADX2_GuideMA" || name == "LADX2_GuideUB" || name == "LADX2_GuideLB");
            add(name, ColumnType::Float64, ColumnRole::Study, overlay ? DisplayMode::PriceOverlay : DisplayMode::StudyPanel);
            if (overlay) columns.back().descriptor.colorARGB = name == "LADX2_PriceMA" ? 0xFF90EE90u : 0xFFEE82EEu;
        }
        for (const auto& [name, value] : bar.text_studies)
        {
            text.push_back(name);
            add(name, ColumnType::DictionaryText, ColumnRole::Study);
        }
        header.columnCount = size32(columns.size());
        dataEnd = sizeof(FileHeader) + columns.size() * sizeof(ColumnDescriptor);
        for (auto& c : columns)
        {
            c.descriptor.dataOffset = dataEnd;
            c.descriptor.dataBytes = header.rowCount *
                (c.descriptor.type == ColumnType::Int64 || c.descriptor.type == ColumnType::Float64 ? 8 : 4);
            dataEnd += c.descriptor.dataBytes;
        }
    }
  public:
    Writer(std::ostream& stream, uint64_t count, uint32_t interval, std::string type)
        : out(stream), collection(std::move(type))
    {
        if (!count || count > uint64_t(INT64_MAX) / 1024) throw std::runtime_error("Invalid FIGDATA row count");
        std::memcpy(header.magic, "FIGDATA\0", 8);
        header.version = 1;
        header.headerBytes = sizeof(FileHeader);
        header.rowCount = count;
        header.columnDescriptorBytes = sizeof(ColumnDescriptor);
        header.columnsOffset = sizeof(FileHeader);
        header.intervalSeconds = interval;
        FileHeader incomplete{};
        bytes(&incomplete, sizeof(incomplete));
    }
    void write(const StudyBar& bar)
    {
        if (finished || rows >= header.rowCount) throw std::runtime_error("Too many FIGDATA rows");
        if (columns.empty()) initialize(bar);
        if (numeric.size() != bar.studies.size() || text.size() != bar.text_studies.size())
            throw std::runtime_error("FIGDATA study schema changed");
        const auto& p = bar.price;
        if (rows && p.raw_time <= header.lastTimestamp) throw std::runtime_error("FIGDATA timestamps must increase");
        if (!rows) header.firstTimestamp = p.raw_time;
        header.lastTimestamp = p.raw_time;
        append(0, int64_t(p.id)); append(1, int64_t(p.data_set_id)); append(2, int64_t(p.raw_time));
        append(3, double(p.open)); append(4, double(p.high)); append(5, double(p.low));
        append(6, double(p.close)); append(7, int64_t(p.volume));
        const auto value = [&](const char* name) { return bar.studies.at(name).value_or(0); };
        uint32_t color{}, background{};
        const bool colors = collection == "LADX2";
        if (colors)
        {
            const auto side = value("LADX2_SIDE");
            color = std::abs(side - 1.1L) < 1e-9L ? 0xFF228B22u : std::abs(side - 1.2L) < 1e-9L ? 0xFFBEBE00u : 0xFFFFFFFFu;
            const auto trend = value("MarketTrend_READY") == 1 ? value("MarketTrend") : 0;
            background = trend > 0 ? 0xFF00280Au : trend < 0 ? 0xFF28000Au : 0xFF000000u;
        }
        append(8, color, colors); append(9, color, colors); append(10, background, colors);
        size_t index = 11;
        for (const auto& name : numeric)
        {
            const auto v = bar.studies.at(name);
            append(index++, double(v.value_or(0)), v.has_value());
        }
        for (const auto& name : text)
        {
            auto& c = columns[index];
            const auto& v = bar.text_studies.at(name);
            const auto [it, inserted] = c.codes.emplace(v, size32(c.dictionary.size()));
            if (inserted) c.dictionary.push_back(v);
            append(index++, it->second);
        }
        ++rows;
    }
    void finish()
    {
        if (finished || rows != header.rowCount) throw std::runtime_error("Incomplete FIGDATA row count");
        for (auto& c : columns) flush(c);
        out.seekp(static_cast<std::streamoff>(dataEnd));
        std::string strings;
        for (auto& c : columns)
        {
            auto& d = c.descriptor;
            if (c.missing)
            {
                d.validityOffset = static_cast<uint64_t>(out.tellp());
                d.validityBytes = c.validity.size();
                bytes(c.validity.data(), c.validity.size());
            }
            if (d.type == ColumnType::DictionaryText)
            {
                d.dictionaryOffset = static_cast<uint64_t>(out.tellp());
                scalar(size32(c.dictionary.size()));
                for (const auto& s : c.dictionary) { scalar(size32(s.size())); bytes(s.data(), s.size()); }
                d.dictionaryBytes = static_cast<uint64_t>(out.tellp()) - d.dictionaryOffset;
            }
            d.nameOffset = strings.size();
            d.nameBytes = size32(c.name.size());
            strings += c.name;
        }
        header.stringsOffset = static_cast<uint64_t>(out.tellp());
        header.stringsBytes = strings.size();
        bytes(strings.data(), strings.size());
        header.fileBytes = static_cast<uint64_t>(out.tellp());
        out.seekp(sizeof(FileHeader));
        for (const auto& c : columns) bytes(&c.descriptor, sizeof(c.descriptor));
        out.seekp(0);
        bytes(&header, sizeof(header));
        out.flush();
        if (!out) throw std::runtime_error("Could not finish FIGDATA output");
        finished = true;
    }
};
} // namespace fig::figdata
