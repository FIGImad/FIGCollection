#include "fig/collections/ladx/study_col_ladx.hpp"
#include "fig/studies/collection_plugin.hpp"

namespace fig::collections::ladx
{
void register_collections(StudyRegistry& registry)
{
    registry.add("LADX",
                 [](const StudyCollectionConfig& config, const Ticker&, const Interval&)
                 { return std::make_unique<StudyColLADX>(Options::from_json(config.parameters)); });
}
} // namespace fig::collections::ladx

extern "C" __declspec(dllexport) const fig::CollectionPluginApi* __cdecl FIGGetCollectionPlugin() noexcept
{
    static const fig::CollectionPluginApi api{.name = "LADX",
                                              .register_collections = fig::collections::ladx::register_collections,
                                              .self_test = fig::collections::ladx::self_test};
    return &api;
}
