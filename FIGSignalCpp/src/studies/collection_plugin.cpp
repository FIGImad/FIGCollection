#include "fig/studies/collection_plugin.hpp"
#include <Windows.h>
#include <cstring>
#include <stdexcept>

namespace fig
{
namespace
{
std::filesystem::path default_plugin_path()
{
    std::wstring buffer(32768, L'\0');
    const auto length = GetModuleFileNameW(nullptr, buffer.data(), static_cast<DWORD>(buffer.size()));
    if (!length || length >= buffer.size())
        throw std::runtime_error("Cannot locate the executable for the default collection DLL");
    buffer.resize(length);
    return std::filesystem::path(buffer).parent_path() / L"FIGStudyCollection.ADX.dll";
}

// Copy exceptions into host-owned objects while the DLL is still loaded. In
// particular, a failed registration must not retain a DLL-defined exception.
template <typename Action> decltype(auto) invoke_plugin(Action&& action)
{
    try
    {
        return action();
    }
    catch (const std::out_of_range& error)
    {
        throw std::out_of_range(error.what());
    }
    catch (const std::invalid_argument& error)
    {
        throw std::invalid_argument(error.what());
    }
    catch (const std::exception& error)
    {
        throw std::runtime_error(error.what());
    }
    catch (...)
    {
        throw std::runtime_error("Collection DLL threw an unknown exception");
    }
}

class PinnedCollection final : public StudyCollection
{
  public:
    PinnedCollection(std::shared_ptr<void> module, std::unique_ptr<StudyCollection> instance)
        : module_(std::move(module)), instance_(std::move(instance))
    {
        if (!instance_)
            throw std::runtime_error("Collection DLL returned a null study collection");
    }
    std::string_view type() const noexcept override
    {
        return instance_->type();
    }
    std::size_t maximum_lookback() const noexcept override
    {
        return instance_->maximum_lookback();
    }
    StudyBar process(const PriceBar& bar) override
    {
        return invoke_plugin([&] { return instance_->process(bar); });
    }

  private:
    // Reverse destruction order: destroy all DLL objects before releasing it.
    std::shared_ptr<void> module_;
    std::unique_ptr<StudyCollection> instance_;
};
struct PinnedFactory
{
    std::shared_ptr<void> module;
    StudyRegistry::Factory factory;
    std::unique_ptr<StudyCollection> operator()(const StudyCollectionConfig& c,
                                                const Ticker& t,
                                                const Interval& i) const
    {
        auto instance = invoke_plugin([&] { return factory(c, t, i); });
        return std::make_unique<PinnedCollection>(module, std::move(instance));
    }
};
} // namespace

struct CollectionPlugin::Module
{
    std::filesystem::path path;
    HMODULE handle{};
    const CollectionPluginApi* api{};
    ~Module()
    {
        if (handle)
            FreeLibrary(handle);
    }
};

CollectionPlugin::CollectionPlugin(const std::filesystem::path& path) : module_(std::make_shared<Module>())
{
    module_->path = std::filesystem::absolute(path.empty() ? default_plugin_path() : path).lexically_normal();
    // Use only the selected DLL's directory and system directories for dependencies.
    module_->handle =
        LoadLibraryExW(module_->path.c_str(), nullptr, LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_SYSTEM32);
    if (!module_->handle)
        throw std::runtime_error("Cannot load collection DLL: " + module_->path.string() + " (Windows error " +
                                 std::to_string(GetLastError()) + ")");
    const auto symbol = GetProcAddress(module_->handle, collection_plugin_entry_point);
    if (!symbol)
        throw std::runtime_error("Collection DLL does not export FIGGetCollectionPlugin: " + module_->path.string());
    GetCollectionPlugin entry{};
    static_assert(sizeof(entry) == sizeof(symbol));
    std::memcpy(&entry, &symbol, sizeof(entry));
    module_->api = entry();
    const auto* api = module_->api;
    if (!api || api->api_version != collection_plugin_api_version || api->structure_size != sizeof(CollectionPluginApi))
        throw std::runtime_error("Incompatible collection DLL API: " + module_->path.string());
    if (api->compiler_version != _MSC_FULL_VER || api->iterator_debug_level != _ITERATOR_DEBUG_LEVEL)
        throw std::runtime_error(
            "Collection DLL build does not match the host: " + module_->path.string() + "\nHost: _MSC_FULL_VER=" +
            std::to_string(_MSC_FULL_VER) + ", _ITERATOR_DEBUG_LEVEL=" + std::to_string(_ITERATOR_DEBUG_LEVEL) +
            "\nDLL:  _MSC_FULL_VER=" + std::to_string(api->compiler_version) +
            ", _ITERATOR_DEBUG_LEVEL=" + std::to_string(api->iterator_debug_level) +
            "\nBuild the executable and DLL with the same compiler and Debug/Release configuration."
            " For the default ADX collection, omit --collection-dll to load the DLL beside the executable.");
    if (!api->name || !*api->name || !api->register_collections)
        throw std::runtime_error("Invalid collection DLL descriptor: " + module_->path.string());
}

const std::filesystem::path& CollectionPlugin::path() const noexcept
{
    return module_->path;
}
std::string_view CollectionPlugin::name() const noexcept
{
    return module_->api->name;
}
void CollectionPlugin::register_collections(StudyRegistry& registry) const
{
    StudyRegistry provided;
    invoke_plugin([&] { module_->api->register_collections(provided); });
    if (provided.factories_.empty())
        throw std::runtime_error("Collection DLL registered no collections: " + module_->path.string());
    auto combined = registry;
    for (auto& [type, factory] : provided.factories_)
        combined.add(type, PinnedFactory{module_, std::move(factory)});
    registry.factories_.swap(combined.factories_);
}
void CollectionPlugin::self_test() const
{
    if (!module_->api->self_test)
        throw std::runtime_error("Collection DLL does not provide self-tests: " + module_->path.string());
    invoke_plugin([&] { module_->api->self_test(); });
}
} // namespace fig
