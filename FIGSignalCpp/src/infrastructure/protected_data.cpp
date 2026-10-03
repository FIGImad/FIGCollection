#include "fig/infrastructure/protected_data.hpp"
#include <Windows.h>
#include <bcrypt.h>
#include <filesystem>
#include <fstream>
#include <limits>
#include <optional>
#include <stdexcept>
#include <vector>
#include <wincrypt.h>

namespace fig
{
namespace
{
struct SecretBytes
{
    std::vector<unsigned char> data;
    ~SecretBytes()
    {
        if (!data.empty())
            SecureZeroMemory(data.data(), data.size());
    }
};

std::vector<unsigned char> decode(std::string_view text)
{
    if (text.empty() || text.size() > std::numeric_limits<DWORD>::max())
        throw std::invalid_argument("Invalid encrypted password or master key encoding");
    DWORD size{};
    constexpr DWORD flags = CRYPT_STRING_BASE64 | CRYPT_STRING_STRICT;
    if (!CryptStringToBinaryA(text.data(), static_cast<DWORD>(text.size()), flags, nullptr, &size, nullptr, nullptr))
        throw std::invalid_argument("Invalid encrypted password or master key encoding");
    std::vector<unsigned char> bytes(size);
    if (!CryptStringToBinaryA(
            text.data(), static_cast<DWORD>(text.size()), flags, bytes.data(), &size, nullptr, nullptr))
        throw std::invalid_argument("Invalid encrypted password or master key encoding");
    bytes.resize(size);
    return bytes;
}

std::optional<std::string> registry_key(HKEY root, const char* path)
{
    DWORD size{};
    constexpr DWORD flags = RRF_RT_REG_SZ | RRF_RT_REG_EXPAND_SZ | RRF_NOEXPAND;
    auto status = RegGetValueA(root, path, "FIG_MASTER_KEY", flags, nullptr, nullptr, &size);
    if (status == ERROR_FILE_NOT_FOUND || status == ERROR_PATH_NOT_FOUND)
        return std::nullopt;
    if (status != ERROR_SUCCESS)
        throw std::runtime_error("Cannot read FIG_MASTER_KEY from Windows environment settings");
    std::string value(size, '\0');
    status = RegGetValueA(root, path, "FIG_MASTER_KEY", flags, nullptr, value.data(), &size);
    if (status != ERROR_SUCCESS)
        throw std::runtime_error("Cannot read FIG_MASTER_KEY from Windows environment settings");
    value.resize(size);
    if (!value.empty() && value.back() == '\0')
        value.pop_back();
    return value;
}

std::vector<unsigned char> master_key()
{
    std::string encoded;
    if (std::filesystem::exists("/run/secrets/FIG_MASTER_KEY"))
    {
        std::ifstream secret("/run/secrets/FIG_MASTER_KEY", std::ios::binary);
        if (!secret)
            throw std::runtime_error("Cannot read FIG_MASTER_KEY secret file");
        encoded.assign(std::istreambuf_iterator<char>(secret), {});
    }
    if (encoded.find_first_not_of(" \t\r\n") == std::string::npos)
    {
        auto setting =
            registry_key(HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment");
        if (!setting)
            setting = registry_key(HKEY_CURRENT_USER, "Environment");
        if (setting)
            encoded = std::move(*setting);
        else
        {
            const auto size = GetEnvironmentVariableA("FIG_MASTER_KEY", nullptr, 0);
            if (size)
            {
                encoded.resize(size);
                const auto written = GetEnvironmentVariableA("FIG_MASTER_KEY", encoded.data(), size);
                if (!written || written >= size)
                    throw std::runtime_error("Cannot read FIG_MASTER_KEY process environment");
                encoded.resize(written);
            }
        }
    }
    if (encoded.find_first_not_of(" \t\r\n") == std::string::npos)
        throw std::runtime_error("FIG_MASTER_KEY is not configured");
    SecretBytes decoded;
    try
    {
        decoded.data = decode(encoded);
    }
    catch (...)
    {
        SecureZeroMemory(encoded.data(), encoded.size());
        throw;
    }
    SecureZeroMemory(encoded.data(), encoded.size());
    if (decoded.data.size() != 32)
        throw std::runtime_error("FIG_MASTER_KEY must decode to exactly 32 bytes");
    return std::move(decoded.data);
}

struct AesHandles
{
    BCRYPT_ALG_HANDLE algorithm{};
    BCRYPT_KEY_HANDLE key{};
    ~AesHandles()
    {
        if (key)
            BCryptDestroyKey(key);
        if (algorithm)
            BCryptCloseAlgorithmProvider(algorithm, 0);
    }
};

std::string decrypt(std::string_view encrypted, std::vector<unsigned char>& key)
{
    auto blob = decode(encrypted);
    if (blob.size() < 28 || key.size() != 32)
        throw std::invalid_argument("Invalid encrypted password or master key length");
    AesHandles handles;
    if (BCryptOpenAlgorithmProvider(&handles.algorithm, BCRYPT_AES_ALGORITHM, nullptr, 0) < 0 ||
        BCryptSetProperty(handles.algorithm,
                          BCRYPT_CHAINING_MODE,
                          reinterpret_cast<PUCHAR>(const_cast<wchar_t*>(BCRYPT_CHAIN_MODE_GCM)),
                          sizeof(BCRYPT_CHAIN_MODE_GCM),
                          0) < 0 ||
        BCryptGenerateSymmetricKey(
            handles.algorithm, &handles.key, nullptr, 0, key.data(), static_cast<ULONG>(key.size()), 0) < 0)
        throw std::runtime_error("Cannot initialize AES-256-GCM");
    const auto length = static_cast<ULONG>(blob.size() - 28);
    BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO info;
    BCRYPT_INIT_AUTH_MODE_INFO(info);
    info.pbNonce = blob.data();
    info.cbNonce = 12;
    info.pbTag = blob.data() + 12 + length;
    info.cbTag = 16;
    SecretBytes plain{std::vector<unsigned char>(length ? length : 1)};
    ULONG written{};
    if (BCryptDecrypt(handles.key,
                      blob.data() + 12,
                      length,
                      &info,
                      nullptr,
                      0,
                      plain.data.data(),
                      static_cast<ULONG>(plain.data.size()),
                      &written,
                      0) < 0 ||
        written != length)
        throw std::runtime_error("Cannot decrypt ODBCPassword: wrong FIG_MASTER_KEY or damaged encrypted value");
    return std::string(reinterpret_cast<const char*>(plain.data.data()), written);
}
} // namespace

std::string unprotect(std::string_view encrypted)
{
    if (encrypted.empty())
        return {};
    SecretBytes key{master_key()};
    return decrypt(encrypted, key.data);
}

void protected_data_self_test()
{
    // NIST AES-256-GCM vector: zero key/nonce, sixteen zero plaintext bytes.
    SecretBytes key{std::vector<unsigned char>(32)};
    const std::string vector = "AAAAAAAAAAAAAAAAzqdAPU1ga24HTsXTuvOdGNDRyKeZmWvwJluYtdSKuRk=";
    if (decrypt(vector, key.data) != std::string(16, '\0'))
        throw std::runtime_error("AES-256-GCM known-answer test failed");
    bool rejected = false;
    key.data[0] = 1;
    try
    {
        (void)decrypt(vector, key.data);
    }
    catch (const std::runtime_error&)
    {
        rejected = true;
    }
    if (!rejected)
        throw std::runtime_error("AES-256-GCM authentication test failed");
}
} // namespace fig
