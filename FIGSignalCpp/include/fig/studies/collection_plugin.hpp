#pragma once
#include "fig/studies/study.hpp"
#include <filesystem>

#if !defined(_MSC_VER) || !defined(_DLL)
#error Collection plugins require MSVC and the shared C++ runtime (/MD or /MDd).
#endif

namespace fig
{
// This is a versioned native C++ ABI, not a portable C ABI. Rebuild the host and
// plugins together when changing this interface, study.hpp, or database/models.hpp.
inline constexpr std::uint32_t collection_plugin_api_version = 1;
inline constexpr char collection_plugin_entry_point[] = "FIGGetCollectionPlugin";
struct CollectionPluginApi
{
    std::uint32_t api_version{collection_plugin_api_version};
    std::uint32_t structure_size{sizeof(CollectionPluginApi)};
    std::uint32_t compiler_version{_MSC_FULL_VER};
    std::uint32_t iterator_debug_level{_ITERATOR_DEBUG_LEVEL};
    const char* name{};
    void (*register_collections)(StudyRegistry&){};
    void (*self_test)(){}; // Optional. Must throw on failure.
};
using GetCollectionPlugin = const CollectionPluginApi*(__cdecl*)() noexcept;

// An empty path selects FIGStudyCollection.ADX.dll beside the executable.
// Explicit relative paths are relative to the current working directory.
class CollectionPlugin final
{
  public:
    explicit CollectionPlugin(const std::filesystem::path& path = {});
    [[nodiscard]] const std::filesystem::path& path() const noexcept;
    [[nodiscard]] std::string_view name() const noexcept;
    // Registration is atomic. Factories and instances retain the loaded module,
    // even after this object or the original registry has been destroyed.
    void register_collections(StudyRegistry& registry) const;
    void self_test() const;

  private:
    struct Module;
    std::shared_ptr<Module> module_;
};
void collection_plugin_self_test();
} // namespace fig
