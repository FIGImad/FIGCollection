#include "fig/studies/collection_plugin.hpp"
#include "fig/studies/study_ma.hpp"
#include <stdexcept>

namespace
{
// A small, independent example. Replace this class with the next collection's
// own studies. There is deliberately no dependency on any ADX collection code.
class ExampleMA final : public fig::StudyCollection
{
  public:
    std::string_view type() const noexcept override
    {
        return "ExampleMA";
    }
    std::size_t maximum_lookback() const noexcept override
    {
        return 3 + fig::studies::default_revision_depth;
    }
    fig::StudyBar process(const fig::PriceBar& bar) override
    {
        const auto average = average_.process(bar);
        return {bar, {{"MA", average}, {"AboveMA", average && bar.close > *average ? 1 : 0}}};
    }

  private:
    fig::studies::StudyMA average_{3};
};
void register_collections(fig::StudyRegistry& registry)
{
    registry.add("ExampleMA",
                 [](const fig::StudyCollectionConfig& config, const fig::Ticker&, const fig::Interval&)
                 {
                     if (config.parameters != "{}")
                         throw std::invalid_argument("ExampleMA has a fixed period of 3; omit --parameters");
                     return std::make_unique<ExampleMA>();
                 });
}
void self_test()
{
    ExampleMA study;
    if (study.process({.raw_time = 1, .close = 1}).studies.at("MA"))
        throw std::runtime_error("ExampleMA warmup failed");
    (void)study.process({.raw_time = 2, .close = 2});
    const auto result = study.process({.raw_time = 3, .close = 3});
    if (result.studies.at("MA") != 2 || result.studies.at("AboveMA") != 1 ||
        study.process({.raw_time = 3, .close = 6}).studies.at("MA") != 3)
        throw std::runtime_error("ExampleMA calculation/replacement failed");
}
} // namespace

extern "C" __declspec(dllexport) const fig::CollectionPluginApi* __cdecl FIGGetCollectionPlugin() noexcept
{
    static const fig::CollectionPluginApi api{
        .name = "Example", .register_collections = register_collections, .self_test = self_test};
    return &api;
}
