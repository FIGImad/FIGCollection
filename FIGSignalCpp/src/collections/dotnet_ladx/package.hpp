#pragma once
#include <Windows.h>
#include <bcrypt.h>
#include <array>
#include <algorithm>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <span>
#include <stdexcept>
#include <string>
#include <vector>

namespace ladx_package
{
inline std::string hash(std::span<const unsigned char> bytes)
{
    BCRYPT_ALG_HANDLE algorithm{};
    if (BCryptOpenAlgorithmProvider(&algorithm, BCRYPT_SHA256_ALGORITHM, nullptr, 0) < 0)
        throw std::runtime_error("Cannot initialize LADX package hash");
    std::array<unsigned char, 32> digest{};
    const auto status = BCryptHash(algorithm, nullptr, 0, const_cast<unsigned char*>(bytes.data()),
                                   static_cast<ULONG>(bytes.size()), digest.data(), static_cast<ULONG>(digest.size()));
    BCryptCloseAlgorithmProvider(algorithm, 0);
    if (status < 0) throw std::runtime_error("Cannot hash LADX package");
    std::string result;
    for (auto byte : digest) { result += "0123456789abcdef"[byte >> 4]; result += "0123456789abcdef"[byte & 15]; }
    return result;
}
inline std::filesystem::path environment(const wchar_t* name)
{
    const auto size = GetEnvironmentVariableW(name, nullptr, 0);
    if (!size) return {};
    std::wstring value(size, L'\0');
    const auto length = GetEnvironmentVariableW(name, value.data(), size);
    if (!length || length >= size) throw std::runtime_error("Cannot read LADX cache location");
    value.resize(length); return value;
}
class Lock
{
    HANDLE handle_{INVALID_HANDLE_VALUE};
  public:
    explicit Lock(const std::filesystem::path& file)
    {
        for (int attempt = 0; attempt < 600; ++attempt)
        {
            handle_ = CreateFileW(file.c_str(), GENERIC_READ | GENERIC_WRITE, 0, nullptr, OPEN_ALWAYS,
                                   FILE_ATTRIBUTE_NORMAL, nullptr);
            if (handle_ != INVALID_HANDLE_VALUE) return;
            const auto error = GetLastError();
            if (error != ERROR_SHARING_VIOLATION && error != ERROR_LOCK_VIOLATION)
                throw std::runtime_error("Cannot lock LADX cache (Windows error " + std::to_string(error) + ")");
            Sleep(100);
        }
        throw std::runtime_error("Timed out waiting for another LADX worker to extract its package");
    }
    ~Lock() { if (handle_ != INVALID_HANDLE_VALUE) CloseHandle(handle_); }
    Lock(const Lock&) = delete;
    Lock& operator=(const Lock&) = delete;
};
inline std::filesystem::path extract(HMODULE module)
{
    auto resource = FindResourceW(module, MAKEINTRESOURCEW(101), RT_RCDATA);
    if (!resource) throw std::runtime_error("Adapter is missing its embedded LADX package; rebuild it");
    auto loaded = LoadResource(module, resource);
    auto data = static_cast<const unsigned char*>(LockResource(loaded));
    const auto size = SizeofResource(module, resource);
    if (!data || !size) throw std::runtime_error("Cannot read embedded LADX package");
    auto root = environment(L"FIG_LADX_CACHE");
    if (root.empty())
    {
        root = environment(L"LOCALAPPDATA");
        if (root.empty()) throw std::runtime_error("LOCALAPPDATA is unavailable; set FIG_LADX_CACHE to a writable cache directory");
        root /= L"FIGSignalCpp/LADX";
    }
    const auto directory = std::filesystem::absolute(root) / hash({data, size});
    std::filesystem::create_directories(directory);
    Lock lock(directory / L"extraction.lock");
    std::span<const unsigned char> remaining(data, size);
    auto take = [&](std::size_t length) {
        if (length > remaining.size()) throw std::runtime_error("Truncated LADX package");
        auto part = remaining.first(length); remaining = remaining.subspan(length); return part;
    };
    auto integer = [&]() { unsigned int value; auto part = take(4); std::memcpy(&value, part.data(), 4); return value; };
    if (integer() != 0x31474446) throw std::runtime_error("Unsupported LADX package format");
    const auto count = integer();
    for (unsigned int i = 0; i < count; ++i)
    {
        const auto name = take(integer());
        std::string utf8(reinterpret_cast<const char*>(name.data()), name.size());
        auto relative = std::filesystem::path(std::u8string_view(reinterpret_cast<const char8_t*>(utf8.data()), utf8.size()));
        if (relative.empty() || relative.is_absolute() || relative.has_root_name() || utf8.find(':') != std::string::npos || utf8.find('\0') != std::string::npos)
            throw std::runtime_error("Invalid LADX package path");
        for (const auto& component : relative)
            if (component == ".." || component == ".") throw std::runtime_error("Invalid LADX package path component");
        auto content = take(integer());
        const auto target = directory / relative;
        bool matches = false;
        if (std::filesystem::is_regular_file(target) && std::filesystem::file_size(target) == content.size())
        {
            std::ifstream input(target, std::ios::binary);
            std::vector<unsigned char> existing(content.size());
            input.read(reinterpret_cast<char*>(existing.data()), static_cast<std::streamsize>(existing.size()));
            matches = input.good() && std::equal(existing.begin(), existing.end(), content.begin(), content.end());
        }
        if (matches) continue;
        std::filesystem::create_directories(target.parent_path());
        auto temporary = target; temporary += L".extracting";
        {
            std::ofstream output(temporary, std::ios::binary | std::ios::trunc);
            output.exceptions(std::ios::badbit | std::ios::failbit);
            output.write(reinterpret_cast<const char*>(content.data()), static_cast<std::streamsize>(content.size()));
            output.close();
        }
        if (!MoveFileExW(temporary.c_str(), target.c_str(), MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH))
            throw std::runtime_error("Cannot install cached LADX file (Windows error " + std::to_string(GetLastError()) + ")");
    }
    if (!remaining.empty()) throw std::runtime_error("Trailing data in LADX package");
    return directory;
}
}
