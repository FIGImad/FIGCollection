#pragma once
#include "fig/database/models.hpp"
#include <Windows.h>
#include <filesystem>
#include <fstream>
#include <functional>
#include <limits>
#include <stdexcept>

namespace fig
{
// Local, versioned snapshots for the Windows native runner. No connection strings are stored.
inline std::uint64_t price_cache_hash(std::string_view bytes, std::uint64_t hash = 14695981039346656037ULL)
{
    for (unsigned char c : bytes) { hash ^= c; hash *= 1099511628211ULL; }
    return hash;
}

class PriceCacheLock
{
    HANDLE handle_;
  public:
    explicit PriceCacheLock(const std::filesystem::path& path)
    {
        handle_ = CreateFileW(path.c_str(), GENERIC_READ | GENERIC_WRITE, 0, nullptr, OPEN_ALWAYS,
                              FILE_ATTRIBUTE_NORMAL, nullptr);
        if (handle_ == INVALID_HANDLE_VALUE)
            throw std::runtime_error("Price cache is in use or inaccessible. Retry after the other batch loads prices.");
    }
    ~PriceCacheLock() { CloseHandle(handle_); }
    PriceCacheLock(const PriceCacheLock&) = delete;
    PriceCacheLock& operator=(const PriceCacheLock&) = delete;
};

struct PriceCacheHeader
{
    std::uint64_t count{}, source_count{}, checksum{}, decimal_size{};
};
inline constexpr std::uint64_t price_cache_magic = 0x4649475043410001ULL;
inline constexpr std::uint64_t price_cache_record_size = 2 * sizeof(int) + 2 * sizeof(std::int64_t) + 4 * sizeof(Decimal);

inline PriceCacheHeader read_price_cache_header(std::ifstream& stream, const std::filesystem::path& path)
{
    std::uint64_t magic{};
    PriceCacheHeader h;
    auto get = [&](auto& v) { stream.read(reinterpret_cast<char*>(&v), sizeof(v)); };
    get(magic); get(h.count); get(h.source_count); get(h.checksum); get(h.decimal_size);
    const auto size = std::filesystem::file_size(path);
    if (!stream || magic != price_cache_magic || h.decimal_size != sizeof(Decimal) || size < 40 ||
        (size - 40) % price_cache_record_size || h.count != (size - 40) / price_cache_record_size)
        throw std::runtime_error("Invalid price cache. Run with --price-cache-mode refresh to rebuild it.");
    return h;
}
inline PriceCacheHeader price_cache_header(const std::filesystem::path& path)
{
    std::ifstream stream(path, std::ios::binary);
    return read_price_cache_header(stream, path);
}
inline PriceCacheHeader read_price_cache(const std::filesystem::path& path,
                                         const std::function<void(const PriceBar&)>& consume)
{
    std::ifstream stream(path, std::ios::binary);
    const auto h = read_price_cache_header(stream, path);
    auto hash = price_cache_hash("");
    auto get = [&](auto& v)
    {
        stream.read(reinterpret_cast<char*>(&v), sizeof(v));
        if (!stream) throw std::runtime_error("Truncated price cache; refresh required.");
        hash = price_cache_hash({reinterpret_cast<const char*>(&v), sizeof(v)}, hash);
    };
    for (std::uint64_t n = 0; n < h.count; ++n)
    {
        PriceBar p;
        get(p.id); get(p.data_set_id); get(p.raw_time); get(p.open); get(p.high); get(p.low); get(p.close); get(p.volume);
        consume(p);
    }
    if (hash != h.checksum) throw std::runtime_error("Price cache checksum mismatch; refresh required.");
    return h;
}

class PriceCacheWriter
{
    std::filesystem::path path_, temporary_;
    std::ofstream stream_;
    std::uint64_t count_{}, hash_{price_cache_hash("")};
  public:
    explicit PriceCacheWriter(const std::filesystem::path& path)
        : path_(path), temporary_(path.wstring() + L".tmp"), stream_(temporary_, std::ios::binary | std::ios::trunc)
    {
        if (!stream_) throw std::runtime_error("Cannot create price cache.");
        const char empty[40]{};
        stream_.write(empty, 40);
    }
    ~PriceCacheWriter()
    {
        stream_.close();
        std::error_code ignored;
        std::filesystem::remove(temporary_, ignored);
    }
    void add(const PriceBar& p)
    {
        auto put = [&](const auto& v)
        {
            stream_.write(reinterpret_cast<const char*>(&v), sizeof(v));
            hash_ = price_cache_hash({reinterpret_cast<const char*>(&v), sizeof(v)}, hash_);
        };
        put(p.id); put(p.data_set_id); put(p.raw_time); put(p.open); put(p.high); put(p.low); put(p.close); put(p.volume);
        if (!stream_) throw std::runtime_error("Cannot write price cache (check available disk space).");
        ++count_;
    }
    PriceCacheHeader finish(std::uint64_t source_count)
    {
        const PriceCacheHeader h{count_, source_count, hash_, sizeof(Decimal)};
        stream_.seekp(0);
        for (auto v : {price_cache_magic, h.count, h.source_count, h.checksum, h.decimal_size})
            stream_.write(reinterpret_cast<const char*>(&v), sizeof(v));
        stream_.flush();
        if (!stream_) throw std::runtime_error("Cannot finalize price cache.");
        stream_.close();
        if (!MoveFileExW(temporary_.c_str(), path_.c_str(), MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH))
            throw std::runtime_error("Cannot publish price cache.");
        return h;
    }
};
} // namespace fig
