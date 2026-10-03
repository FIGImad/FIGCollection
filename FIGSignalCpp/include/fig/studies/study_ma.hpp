#pragma once
#include "fig/studies/live_state.hpp"
#include <map>
namespace fig::studies
{
class StudyMA final
{
  public:
    StudyMA(std::size_t period, std::string source = "close", std::size_t revision_depth = default_revision_depth);
    [[nodiscard]] std::optional<Decimal> process(const PriceBar& bar);
    [[nodiscard]] std::size_t evaluation_count() const noexcept
    {
        return evaluations_;
    }

  private:
    struct State
    {
        std::int64_t raw_time{};
        std::deque<Decimal> window;
        Decimal sum{};
        std::optional<Decimal> value;
    };
    std::size_t period_;
    std::string source_;
    std::size_t evaluations_{};
    LiveStateHistory<State> states_;
};
// Register dependencies before streaming. Each unique (period, source) runs once per process call.
// Own one set per collection/dataset/interval; no process-wide mutable cache.
class SharedMovingAverages final
{
  public:
    using Key = std::pair<std::size_t, std::string>;
    SharedMovingAverages(std::vector<Key> requests, std::size_t depth = default_revision_depth);
    void process(const PriceBar& bar);
    [[nodiscard]] std::optional<Decimal> value(std::size_t period, const std::string& source) const;
    [[nodiscard]] std::size_t calculation_count() const noexcept;
    [[nodiscard]] std::size_t size() const noexcept
    {
        return studies_.size();
    }

  private:
    std::map<Key, StudyMA> studies_;
    std::map<Key, std::optional<Decimal>> values_;
};
} // namespace fig::studies
