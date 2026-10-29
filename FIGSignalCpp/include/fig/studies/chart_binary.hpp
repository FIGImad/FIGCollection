#pragma once
#include "fig/studies/study.hpp"
#include <cstdint>
#include <filesystem>
#include <memory>

namespace fig::chart
{
enum class ColumnType : uint32_t { Int64 = 1, Float64 = 2, UInt32 = 3, DictionaryText = 4 };
enum class ColumnRole : uint32_t
{
    Timestamp = 1, Open = 2, High = 3, Low = 4, Close = 5, Volume = 6,
    Study = 7, Metadata = 8, BarColor = 9, Background = 10
};
enum class DisplayMode : uint32_t { None = 0, PriceOverlay = 1, StudyPanel = 2 };
#pragma pack(push, 1)
struct FileHeader
{
    char magic[8];
    uint32_t version;
    uint32_t headerBytes;
    uint64_t rowCount;
    uint32_t columnCount;
    uint32_t columnDescriptorBytes;
    uint64_t columnsOffset;
    uint64_t stringsOffset;
    uint64_t stringsBytes;
    uint64_t fileBytes;
    int64_t firstTimestamp;
    int64_t lastTimestamp;
    uint32_t intervalSeconds;
    uint32_t flags;
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
    uint64_t dataOffset;
    uint64_t dataBytes;
    uint64_t validityOffset;
    uint64_t validityBytes;
    uint64_t dictionaryOffset;
    uint64_t dictionaryBytes;
    DisplayMode display;
    uint32_t colorARGB;
    uint32_t panelGroup;
    uint32_t flags;
    uint8_t reserved[32];
};
#pragma pack(pop)
static_assert(sizeof(FileHeader) == 128);
static_assert(sizeof(ColumnDescriptor) == 128);

// Little-endian v1. Validity bits are LSB-first (1 = valid).
// Dictionary: uint32 entryCount, then entryCount repetitions of
// uint32 UTF-8 byteLength + bytes, without terminators. Codes are zero-based.
// Data blocks are contiguous column arrays; offsets are absolute and 8-byte aligned.
class Writer
{
  public:
    Writer(const std::filesystem::path& path, uint64_t rows, uint32_t interval_seconds,
           std::string collection);
    ~Writer();
    Writer(const Writer&) = delete;
    Writer& operator=(const Writer&) = delete;
    void write(const StudyBar& bar);
    void finish();
  private:
    struct Impl;
    std::unique_ptr<Impl> impl_;
};
void self_test();
} // namespace fig::chart
