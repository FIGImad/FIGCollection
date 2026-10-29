#include "fig/studies/collection_plugin.hpp"
#include <Windows.h>
#include "package.hpp"
#include <nethost.h>
#include <hostfxr.h>
#include <coreclr_delegates.h>
#include <array>
#include <cstring>
#include <limits>
#include <stdexcept>

namespace
{
struct Bar { std::int64_t time; double open, high, low, close; std::int64_t volume; };
static_assert(sizeof(Bar) == 48);
using Create = int(__cdecl*)(const char*, int, int, void**, int*, char*, int);
using Process = int(__cdecl*)(void*, const Bar*, char*, int, char*, int);
using Destroy = void(__cdecl*)(void*);

template<class T> T symbol(HMODULE module, const char* name)
{
    auto address = GetProcAddress(module, name);
    if (!address) throw std::runtime_error(std::string("Missing .NET host function: ") + name);
    T result; static_assert(sizeof(result) == sizeof(address));
    std::memcpy(&result, &address, sizeof(result)); return result;
}
std::filesystem::path bridge_directory()
{
    static const wchar_t anchor[] = L"FIG .NET bridge";
    HMODULE module{};
    if (!GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
                           anchor, &module)) throw std::runtime_error("Cannot locate bridge DLL");
    return ladx_package::extract(module);
}
struct Runtime
{
    Create create{}; Process process{}; Destroy destroy{};
    Runtime()
    {
        const auto directory = bridge_directory();
        auto net_host = LoadLibraryExW((directory / L"nethost.dll").c_str(), nullptr,
                                      LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_SYSTEM32);
        if (!net_host) throw std::runtime_error("Cannot load extracted nethost.dll");
        auto locate = symbol<decltype(&get_hostfxr_path)>(net_host, "get_hostfxr_path");
        std::array<wchar_t, 32768> path{}; size_t size = path.size();
        const int locate_status = locate(path.data(), &size, nullptr);
        FreeLibrary(net_host);
        if (locate_status != 0)
            throw std::runtime_error("Install the x64 .NET 10 and ASP.NET Core 10 runtimes to use DotNetLADX.");
        // hostfxr and CoreCLR remain loaded for process lifetime. Managed function
        // pointers must never outlive their runtime; unloading CoreCLR is unsupported.
        auto host = LoadLibraryExW(path.data(), nullptr, LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_SYSTEM32);
        if (!host) throw std::runtime_error("Cannot load hostfxr.dll");
        auto initialize = symbol<hostfxr_initialize_for_runtime_config_fn>(host, "hostfxr_initialize_for_runtime_config");
        auto get_delegate = symbol<hostfxr_get_runtime_delegate_fn>(host, "hostfxr_get_runtime_delegate");
        auto close = symbol<hostfxr_close_fn>(host, "hostfxr_close");

        hostfxr_handle context{};
        const auto config = directory / L"FIG.LADXBridge.runtimeconfig.json";
        int status = initialize(config.c_str(), nullptr, &context);
        if (status < 0 || !context) throw std::runtime_error("Cannot initialize .NET 10: check dotnet-ladx deployment and installed runtimes (" + std::to_string(status) + ")");
        void* loader{};
        status = get_delegate(context, hdt_load_assembly_and_get_function_pointer, &loader);
        close(context);
        if (status != 0 || !loader) throw std::runtime_error("Cannot obtain .NET assembly loader");
        auto load = reinterpret_cast<load_assembly_and_get_function_pointer_fn>(loader);
        const auto assembly = directory / L"FIG.LADXBridge.dll";
        auto bind = [&](const wchar_t* method, auto& target) {
            void* address{};
            int rc = load(assembly.c_str(), L"FIG.LADXBridge.EntryPoints, FIG.LADXBridge", method,
                          UNMANAGEDCALLERSONLY_METHOD, nullptr, &address);
            if (rc != 0 || !address) throw std::runtime_error("Cannot bind managed LADX bridge (" + std::to_string(rc) + ")");
            static_assert(sizeof(target) == sizeof(address)); std::memcpy(&target, &address, sizeof(target));
        };
        bind(L"Create", create); bind(L"Process", process); bind(L"Destroy", destroy);
    }
};
Runtime& runtime() { static Runtime instance; return instance; }

class Reader
{
    const char* cursor_; std::size_t remaining_;
  public:
    Reader(const char* data, std::size_t length) : cursor_(data), remaining_(length) {}
    template<class T> T read()
    {
        if (remaining_ < sizeof(T)) throw std::runtime_error("Truncated managed result");
        T value; std::memcpy(&value, cursor_, sizeof(T)); cursor_ += sizeof(T); remaining_ -= sizeof(T); return value;
    }
    std::string text()
    {
        auto size = read<std::uint32_t>();
        if (size > remaining_) throw std::runtime_error("Invalid managed string length");
        std::string value(cursor_, size); cursor_ += size; remaining_ -= size; return value;
    }
    bool empty() const { return remaining_ == 0; }
};
class Collection final : public fig::StudyCollection
{
    Runtime& runtime_; void* handle_{}; int lookback_{};
    std::vector<char> buffer_ = std::vector<char>(65536);
  public:
    Collection(const fig::StudyCollectionConfig& config, const fig::Interval& interval) : runtime_(runtime())
    {
        if (config.parameters.size() > static_cast<std::size_t>(std::numeric_limits<int>::max()))
            throw std::invalid_argument("LADX parameters too large");
        std::array<char, 2048> error{};
        if (runtime_.create(config.parameters.data(), static_cast<int>(config.parameters.size()), interval.length_seconds,
                            &handle_, &lookback_, error.data(), static_cast<int>(error.size())) != 0)
            throw std::invalid_argument(std::string(".NET LADX: ") + error.data());
    }
    ~Collection() override { runtime_.destroy(handle_); }
    Collection(const Collection&) = delete;
    Collection& operator=(const Collection&) = delete;
    std::string_view type() const noexcept override { return "LADX"; }
    std::size_t maximum_lookback() const noexcept override { return static_cast<std::size_t>(lookback_); }
    fig::StudyBar process(const fig::PriceBar& price) override
    {
        Bar input{price.raw_time, static_cast<double>(price.open), static_cast<double>(price.high),
                  static_cast<double>(price.low), static_cast<double>(price.close), price.volume};
        std::array<char, 2048> error{};
        int length = runtime_.process(handle_, &input, buffer_.data(), static_cast<int>(buffer_.size()),
                                      error.data(), static_cast<int>(error.size()));
        if (length < 0) throw std::runtime_error(std::string(".NET LADX: ") + error.data());
        if (static_cast<std::size_t>(length) > buffer_.size()) throw std::runtime_error("Invalid managed output length");
        Reader reader(buffer_.data(), static_cast<std::size_t>(length));
        fig::StudyBar result{price, {}};
        const auto count = reader.read<std::uint32_t>();
        for (std::uint32_t i = 0; i < count; ++i)
        {
            auto name = reader.text();
            switch (reader.read<std::uint8_t>())
            {
            case 0: result.studies.emplace(std::move(name), std::nullopt); break;
            case 1: result.studies.emplace(std::move(name), reader.read<double>()); break;
            case 2: result.text_studies.emplace(std::move(name), reader.text()); break;
            default: throw std::runtime_error("Unknown managed result type");
            }
        }
        if (!reader.empty()) throw std::runtime_error("Trailing managed result data");
        return result;
    }
};
void register_collections(fig::StudyRegistry& registry)
{
    registry.add("LADX", [](const fig::StudyCollectionConfig& config, const fig::Ticker&, const fig::Interval& interval) {
        return std::make_unique<Collection>(config, interval);
    });
}
void self_test()
{
    fig::StudyCollectionConfig config{.type = "LADX"};
    fig::Interval interval{.length_seconds = 120};
    Collection first(config, interval), second(config, interval);
    if (!first.maximum_lookback()) throw std::runtime_error("Missing managed warmup");
    for (int i = 0; i < 300; ++i)
    {
        const double value = 100 + (i % 17) * .25;
        fig::PriceBar bar{.raw_time = 1700000000LL + i * 120, .open = value, .high = value + 1,
                          .low = value - 1, .close = value + .25, .volume = 100};
        (void)first.process(bar);
        bar.close = value - .25;
        auto a = first.process(bar), b = second.process(bar);
        if (a.studies != b.studies || a.text_studies != b.text_studies || !a.studies.contains("MAPrice"))
            throw std::runtime_error("Managed revised history differs from clean replay");
    }
    try { Collection invalid(config, fig::Interval{.length_seconds = 61}); }
    catch (const std::invalid_argument&) { return; }
    throw std::runtime_error("Managed interval validation failed");
}
}
extern "C" __declspec(dllexport) const fig::CollectionPluginApi* __cdecl FIGGetCollectionPlugin() noexcept
{
    static const fig::CollectionPluginApi api{.name = "DotNetLADX", .register_collections = register_collections, .self_test = self_test};
    return &api;
}


