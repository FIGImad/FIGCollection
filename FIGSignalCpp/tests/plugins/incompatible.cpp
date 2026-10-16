#include "fig/studies/collection_plugin.hpp"

extern "C" __declspec(dllexport) const fig::CollectionPluginApi* __cdecl FIGGetCollectionPlugin() noexcept
{
    static const fig::CollectionPluginApi api{.api_version = fig::collection_plugin_api_version + 1};
    return &api;
}
