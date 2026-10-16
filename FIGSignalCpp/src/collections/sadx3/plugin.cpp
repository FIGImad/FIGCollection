#include "fig/collections/sadx3/study_col_sadx3.hpp"
#include "fig/studies/collection_plugin.hpp"

namespace fig::collections::sadx3
{
void register_collections(StudyRegistry& registry)
{
    registry.add("SADX3",
                 [](const StudyCollectionConfig& config, const Ticker&, const Interval& interval)
                 {
                     return std::make_unique<StudyColSADX3>(Options::from_json(config.parameters),
                                                            studies::default_revision_depth,
                                                            interval.length_seconds ? interval.length_seconds : 300);
                 });
}
} // namespace fig::collections::sadx3

extern "C" __declspec(dllexport) const fig::CollectionPluginApi* __cdecl FIGGetCollectionPlugin() noexcept
{
    static const fig::CollectionPluginApi api{.name = "SADX3",
                                              .register_collections = fig::collections::sadx3::register_collections,
                                              .self_test = fig::collections::sadx3::self_test};
    return &api;
}
