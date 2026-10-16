#include "fig/studies/collection_plugin.hpp"
#include <Windows.h>
#include <stdexcept>

namespace fig
{
void collection_plugin_self_test()
{
    const auto require = [](bool valid, const char* message)
    {
        if (!valid)
            throw std::runtime_error(message);
    };
    std::filesystem::path path;
    std::unique_ptr<StudyCollection> retained;
    {
        const CollectionPlugin plugin;
        path = plugin.path();
        StudyRegistry registry;
        plugin.register_collections(registry);
        const auto types = registry.types();
        require(types == std::vector<std::string>{"ADXBASE", "ADXDIAGNOSTICS", "ADXTREND"},
                "Unexpected ADX plugin registrations");
        bool duplicate_rejected{};
        try
        {
            plugin.register_collections(registry);
        }
        catch (const std::invalid_argument&)
        {
            duplicate_rejected = true;
        }
        require(duplicate_rejected && registry.types() == types, "Duplicate plugin registration was not atomic");
        auto copy = registry;
        registry = StudyRegistry{};
        retained = copy.create(StudyCollectionConfig{.type = "adxbase"}, Ticker{}, Interval{});
        bool unknown_rejected{};
        try
        {
            (void)copy.create(StudyCollectionConfig{.type = "unknown"}, Ticker{}, Interval{});
        }
        catch (const std::invalid_argument&)
        {
            unknown_rejected = true;
        }
        require(unknown_rejected, "Unknown collection type was accepted");
    }
    // The instance is the only remaining owner of this DLL.
    require(GetModuleHandleW(path.c_str()) != nullptr, "Collection did not retain its DLL");
    require(retained->type() == "ADXBase" && retained->maximum_lookback() > 0, "Plugin dispatch failed");
    const PriceBar bar{.raw_time = 60, .open = 10, .high = 12, .low = 9, .close = 11};
    const auto first = retained->process(bar);
    require(first.studies == retained->process(bar).studies, "Plugin bar replacement failed");
    retained.reset();
    require(GetModuleHandleW(path.c_str()) == nullptr, "Unused collection DLL was not unloaded");
    bool missing_rejected{};
    try
    {
        const CollectionPlugin missing(path.parent_path() / "missing-collection-for-test.dll");
    }
    catch (const std::runtime_error&)
    {
        missing_rejected = true;
    }
    require(missing_rejected, "Missing collection DLL was accepted");
}
} // namespace fig
