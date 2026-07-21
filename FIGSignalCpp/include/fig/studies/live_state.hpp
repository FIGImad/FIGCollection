#pragma once
#include "fig/models.hpp"
#include <deque>
#include <stdexcept>

namespace fig::studies
{
template <class State> class LiveStateHistory
{
  public:
    explicit LiveStateHistory(std::size_t revision_depth = 10) : revision_depth_(revision_depth)
    {
        if (!revision_depth_)
            throw std::invalid_argument("Revision depth cannot be zero");
    }
    [[nodiscard]] State begin(std::int64_t raw_time)
    {
        if (discarded_ && raw_time <= states_.front().raw_time)
            throw std::out_of_range("Bar revision exceeds retained rollback depth");
        while (!states_.empty() && states_.back().raw_time >= raw_time)
            states_.pop_back();
        State state = states_.empty() ? State{} : states_.back();
        state.raw_time = raw_time;
        return state;
    }
    const State& commit(State state)
    {
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
