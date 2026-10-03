#pragma once
#include "fig/database/models.hpp"
#include <deque>
#include <limits>
#include <stdexcept>

namespace fig::studies
{
inline constexpr std::size_t default_revision_depth = 20;

// Depth counts bars AFTER the bar being replaced (the current bar is distance zero).
// Keep one extra predecessor snapshot so even the oldest allowed bar can be replaced.
template <class State> class LiveStateHistory
{
  public:
    explicit LiveStateHistory(std::size_t revision_depth = default_revision_depth) : revision_depth_(revision_depth)
    {
        if (!revision_depth_)
            throw std::invalid_argument("Revision depth cannot be zero");
        if (revision_depth_ > std::numeric_limits<std::size_t>::max() - 2)
            throw std::invalid_argument("Revision depth is too large");
    }
    // Calculate from the state strictly before this timestamp without changing history.
    [[nodiscard]] State begin(std::int64_t raw_time) const
    {
        if ((discarded_ || states_.size() == revision_depth_ + 2) && raw_time <= states_.front().raw_time)
            throw std::out_of_range("Bar revision exceeds retained rollback depth");
        auto predecessor = states_.rbegin();
        while (predecessor != states_.rend() && predecessor->raw_time >= raw_time)
            ++predecessor;
        State state = predecessor == states_.rend() ? State{} : *predecessor;
        state.raw_time = raw_time;
        return state;
    }
    const State& commit(State state)
    {
        if ((discarded_ || states_.size() == revision_depth_ + 2) && state.raw_time <= states_.front().raw_time)
            throw std::out_of_range("Bar revision exceeds retained rollback depth");
        // Replacing a past bar invalidates that bar and every later calculation.
        while (!states_.empty() && states_.back().raw_time >= state.raw_time)
            states_.pop_back();
        states_.push_back(std::move(state));
        while (states_.size() > revision_depth_ + 2)
        {
            states_.pop_front();
            discarded_ = true;
        }
        return states_.back();
    }

  private:
    std::size_t revision_depth_;
    bool discarded_{};
    std::deque<State> states_;
};

[[nodiscard]] inline Decimal source_value(const PriceBar& bar, std::string_view source)
{
    if (source == "open")
        return bar.open;
    if (source == "high")
        return bar.high;
    if (source == "low")
        return bar.low;
    if (source == "close")
        return bar.close;
    if (source == "hl2")
        return (bar.high + bar.low) / 2;
    if (source == "oc2")
        return (bar.open + bar.close) / 2;
    if (source == "hlc3")
        return (bar.high + bar.low + bar.close) / 3;
    if (source == "ohlc4")
        return (bar.open + bar.high + bar.low + bar.close) / 4;
    if (source == "hlcc4")
        return (bar.high + bar.low + 2 * bar.close) / 4;
    throw std::invalid_argument("Unsupported price source");
}
} // namespace fig::studies
