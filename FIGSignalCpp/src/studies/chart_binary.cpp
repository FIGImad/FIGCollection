#include "fig/studies/chart_binary.hpp"
#include <bit>
#include <cmath>
#include <cstring>
#include <fstream>
#include <limits>
#include <map>
#include <stdexcept>
#include <vector>

namespace fig::chart
{
static_assert(std::endian::native == std::endian::little);
static_assert(sizeof(double) == 8 && std::numeric_limits<double>::is_iec559);
namespace
{
constexpr uint64_t chunk_rows = 4096;
uint64_t aligned(uint64_t value) { return (value + 7) & ~uint64_t{7}; }
uint32_t narrow(size_t value)
{
    if (value > std::numeric_limits<uint32_t>::max())
        throw std::runtime_error("Chart string/dictionary exceeds v1 limits");
    return static_cast<uint32_t>(value);
}
struct Column
{
    std::string name;
    ColumnDescriptor descriptor{};
    std::vector<char> buffer;
    std::vector<uint8_t> validity;
    std::map<std::string, uint32_t> codes;
    std::vector<std::string> dictionary;
    bool missing{};
    size_t width() const
    {
        return descriptor.type == ColumnType::Int64 || descriptor.type == ColumnType::Float64 ? 8 : 4;
    }
    template<class T> void add(T value, uint64_t row, bool valid = true)
    {
        if (sizeof(T) != width())
            throw std::logic_error("Chart column type mismatch");
        const auto* bytes = reinterpret_cast<const char*>(&value);
        buffer.insert(buffer.end(), bytes, bytes + sizeof(T));
        if (valid)
            validity.at(static_cast<size_t>(row / 8)) |= static_cast<uint8_t>(1u << (row % 8));
        else
            missing = true;
    }
};
} // namespace
struct Writer::Impl
{
    std::filesystem::path path, temporary;
    std::ofstream output;
    FileHeader header{};
    std::string collection, strings;
    std::vector<Column> columns;
    size_t numeric_count{}, text_count{};
    uint64_t written{}, flushed{}, tail{};
    bool finished{};

    Impl(const std::filesystem::path& target, uint64_t rows, uint32_t interval, std::string type)
        : path(target), temporary(target.string() + ".tmp"), collection(std::move(type))
    {
        if (!rows || rows > static_cast<uint64_t>(std::numeric_limits<int64_t>::max()) / 1024)
            throw std::invalid_argument("Invalid chart row count");
        if (std::filesystem::exists(path) || std::filesystem::exists(temporary))
            throw std::runtime_error("Chart output already exists");
        output.open(temporary, std::ios::binary | std::ios::trunc);
        if (!output)
            throw std::runtime_error("Cannot create binary chart output");
        output.exceptions(std::ios::badbit | std::ios::failbit);
        std::memcpy(header.magic, "FIGDATA\0", 8);
        header.version = 1;
        header.headerBytes = sizeof(FileHeader);
        header.columnDescriptorBytes = sizeof(ColumnDescriptor);
        header.columnsOffset = sizeof(FileHeader);
        header.rowCount = rows;
        header.intervalSeconds = interval;
    }
    ~Impl()
    {
        if (!finished)
        {
            output.exceptions(std::ios::goodbit);
            output.close();
            std::error_code ignored;
            std::filesystem::remove(temporary, ignored);
        }
    }
    void bytes(uint64_t offset, const void* data, uint64_t size)
    {
        output.seekp(static_cast<std::streamoff>(offset));
        output.write(static_cast<const char*>(data), static_cast<std::streamsize>(size));
    }
    void add(std::string name, ColumnType type, ColumnRole role)
    {
        Column c;
        c.name = std::move(name);
        auto& d = c.descriptor;
        d.type = type;
        d.role = role;
        d.nameOffset = strings.size();
        d.nameBytes = narrow(c.name.size());
        strings += c.name;
        if (collection == "LADX2" && role == ColumnRole::Study)
        {
            // Display primary lines only; aliases and diagnostic values remain available.
            if (c.name == "LADX2_PriceMA" || c.name == "LADX2_GuideMA" ||
                c.name == "LADX2_GuideUB" || c.name == "LADX2_GuideLB")
            {
                d.display = DisplayMode::PriceOverlay;
                d.colorARGB = c.name == "LADX2_PriceMA" ? 0xFF90EE90u : 0xFFEE82EEu;
            }
            if (c.name == "LADX2_ADX" || c.name == "LADX2_ADXBias" || c.name == "LADX2_ADXTrigger")
            {
                d.display = DisplayMode::StudyPanel;
                d.panelGroup = 1;
                d.colorARGB = c.name == "LADX2_ADXTrigger" ? 0xFFEE82EEu : 0xFFBEBE00u;
            }
        }
        c.validity.resize(static_cast<size_t>((header.rowCount + 7) / 8));
        c.buffer.reserve(static_cast<size_t>(chunk_rows) * c.width());
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
        numeric_count = bar.studies.size();
        text_count = bar.text_studies.size();
        for (const auto& [name, value] : bar.studies)
            add(name, ColumnType::Float64, ColumnRole::Study);
        for (const auto& [name, value] : bar.text_studies)
            add(name, ColumnType::DictionaryText, ColumnRole::Study);
        header.columnCount = narrow(columns.size());
        header.stringsOffset = header.columnsOffset + columns.size() * sizeof(ColumnDescriptor);
        header.stringsBytes = strings.size();
        tail = aligned(header.stringsOffset + header.stringsBytes);
        for (auto& c : columns)
        {
            c.descriptor.dataOffset = tail;
            c.descriptor.dataBytes = header.rowCount * c.width();
            if (c.descriptor.dataBytes > static_cast<uint64_t>(std::numeric_limits<int64_t>::max()) - tail - 7)
                throw std::runtime_error("Chart file exceeds stream size limit");
            tail = aligned(tail + c.descriptor.dataBytes);
        }
        // No valid header until finish succeeds.
        const FileHeader blank{};
        bytes(0, &blank, sizeof(blank));
        bytes(header.stringsOffset, strings.data(), strings.size());
    }
    void flush()
    {
        for (auto& c : columns)
        {
            bytes(c.descriptor.dataOffset + flushed * c.width(), c.buffer.data(), c.buffer.size());
            c.buffer.clear();
        }
        flushed = written;
    }
    void write(const StudyBar& bar)
    {
        if (finished || written >= header.rowCount)
            throw std::runtime_error("Too many chart rows or chart already finished");
        if (columns.empty())
            initialize(bar);
        if (bar.studies.size() != numeric_count || bar.text_studies.size() != text_count)
            throw std::runtime_error("Chart study schema changed during backtest");
        const auto& p = bar.price;
        if (written && p.raw_time <= header.lastTimestamp)
            throw std::runtime_error("Chart timestamps must increase");
        if (!written)
            header.firstTimestamp = p.raw_time;
        header.lastTimestamp = p.raw_time;
        columns[0].add(int64_t{p.id}, written);
        columns[1].add(int64_t{p.data_set_id}, written);
        columns[2].add(p.raw_time, written);
        columns[3].add(static_cast<double>(p.open), written);
        columns[4].add(static_cast<double>(p.high), written);
        columns[5].add(static_cast<double>(p.low), written);
        columns[6].add(static_cast<double>(p.close), written);
        columns[7].add(p.volume, written);
        const auto value = [&](const std::string& name)
        {
            const auto found = bar.studies.find(name);
            return found == bar.studies.end() ? Decimal{} : found->second.value_or(0);
        };
        const bool colors = collection == "LADX2";
        const auto side = value("LADX2_SIDE");
        const uint32_t color = std::abs(side - 1.1L) < 1e-9L ? 0xFF228B22u :
                               std::abs(side - 1.2L) < 1e-9L ? 0xFFBEBE00u : 0xFFFFFFFFu;
        const auto trend = value("MarketTrend_READY") == 1 ? value("MarketTrend") : 0;
        const uint32_t background = trend > 0 ? 0xFF00280Au : trend < 0 ? 0xFF28000Au : 0xFF000000u;
        columns[8].add(colors ? color : uint32_t{}, written, colors);
        columns[9].add(colors ? color : uint32_t{}, written, colors);
        columns[10].add(colors ? background : uint32_t{}, written, colors);
        size_t index = 11;
        for (; index < 11 + numeric_count; ++index)
        {
            auto& c = columns[index];
            const auto v = bar.studies.at(c.name);
            const double d = v ? static_cast<double>(*v) : 0;
            c.add(std::isfinite(d) ? d : 0, written, v.has_value() && std::isfinite(d));
        }
        for (; index < columns.size(); ++index)
        {
            auto& c = columns[index];
            const auto& text = bar.text_studies.at(c.name);
            auto [found, inserted] = c.codes.try_emplace(text, narrow(c.dictionary.size()));
            if (inserted)
                c.dictionary.push_back(text);
            c.add(found->second, written);
        }
        ++written;
        if (written % chunk_rows == 0)
            flush();
    }
    void finish()
    {
        if (finished || written != header.rowCount)
            throw std::runtime_error("Incomplete or already finished chart");
        flush();
        for (auto& c : columns)
        {
            auto& d = c.descriptor;
            if (c.missing)
            {
                d.validityOffset = tail;
                d.validityBytes = c.validity.size();
                bytes(tail, c.validity.data(), c.validity.size());
                tail = aligned(tail + c.validity.size());
            }
            if (d.type == ColumnType::DictionaryText)
            {
                d.dictionaryOffset = tail;
                const auto count = narrow(c.dictionary.size());
                bytes(tail, &count, sizeof(count));
                tail += sizeof(count);
                for (const auto& text : c.dictionary)
                {
                    const auto size = narrow(text.size());
                    bytes(tail, &size, sizeof(size));
                    tail += sizeof(size);
                    bytes(tail, text.data(), size);
                    tail += size;
                }
                d.dictionaryBytes = tail - d.dictionaryOffset;
                tail = aligned(tail);
            }
        }
        const char zero{};
        bytes(tail - 1, &zero, 1); // Materialize final alignment padding.
        header.fileBytes = tail;
        for (size_t i = 0; i < columns.size(); ++i)
            bytes(header.columnsOffset + i * sizeof(ColumnDescriptor), &columns[i].descriptor, sizeof(ColumnDescriptor));
        bytes(0, &header, sizeof(header));
        output.close();
        std::filesystem::rename(temporary, path);
        finished = true;
    }
};
Writer::Writer(const std::filesystem::path& path, uint64_t rows, uint32_t interval, std::string collection)
    : impl_(std::make_unique<Impl>(path, rows, interval, std::move(collection))) {}
Writer::~Writer() = default;
void Writer::write(const StudyBar& bar) { impl_->write(bar); }
void Writer::finish() { impl_->finish(); }
} // namespace fig::chart
