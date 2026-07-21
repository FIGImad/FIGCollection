#pragma once
#include "fig/models.hpp"
#include <functional>
#include <memory>
#include <span>
#include <unordered_map>

namespace fig
{
class StudyCollection
{
  public:
    virtual ~StudyCollection() = default;
    [[nodiscard]] virtual std::string_view type() const noexcept = 0;
    [[nodiscard]] virtual std::size_t maximum_lookback() const noexcept = 0;
    [[nodiscard]] virtual StudyBar process(const PriceBar& bar) = 0;
    std::vector<StudyBar> replay(std::span<const PriceBar> prices);
};
class StudyRegistry final
{
  public:
    using Factory =
        std::function<std::unique_ptr<StudyCollection>(const StudyCollectionConfig&, const Ticker&, const Interval&)>;
    void add(std::string type, Factory factory);
    [[nodiscard]] std::unique_ptr<StudyCollection> create(const StudyCollectionConfig&,
                                                          const Ticker&,
                                                          const Interval&) const;
    [[nodiscard]] std::vector<std::string> types() const;

  private:
    std::unordered_map<std::string, Factory> factories_;
};
// Startup/backfill helpers. Live processing owns the stateful class from studies/*.hpp.
[[nodiscard]] std::vector<std::optional<Decimal>> simple_moving_average(std::span<const Decimal>, std::size_t);
[[nodiscard]] std::vector<std::optional<Decimal>> weighted_moving_average(std::span<const Decimal>, std::size_t);
[[nodiscard]] std::vector<std::optional<Decimal>> rate_of_change(std::span<const Decimal>, std::size_t);
void register_integrated_collections(StudyRegistry& registry);
} // namespace fig
