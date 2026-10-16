#include "fig/collections/ladx2/study_col_ladx2.hpp"
#include "fig/studies/collection_plugin.hpp"

namespace fig::collections::ladx2
{
void register_collections(StudyRegistry& registry)
{
    registry.add("LADX2",
                 [](const StudyCollectionConfig& config, const Ticker&, const Interval& interval)
                 {
                     return std::make_unique<StudyColLADX2>(Options::from_json(config.parameters),
                                                            studies::default_revision_depth,
                                                            interval.length_seconds ? interval.length_seconds : 300);
                 });
}
} // namespace fig::collections::ladx2

extern "C" __declspec(dllexport) const fig::CollectionPluginApi* __cdecl FIGGetCollectionPlugin() noexcept
{
    static const fig::CollectionPluginApi api{.name = "LADX2",
                                              .register_collections = fig::collections::ladx2::register_collections,
                                              .self_test = fig::collections::ladx2::self_test};
    return &api;
}
