#pragma once
#include "fig/collections/sadx2/options.hpp"
#include "fig/studies/study_adx.hpp"
#include "fig/studies/study_sma_slope.hpp"
namespace fig::collections::sadx2
{
// Only completed chart candles enter the aggregate. All state is rollback-capable.
class MarketTrend
{
  public:
    explicit MarketTrend(MarketTrendSettings settings, std::size_t depth);
    StudyBar process(const PriceBar& bar);

  private:
    struct Engine
    {
        explicit Engine(const MarketTrendSettings& o)
            : ma(o.sma_length, "close", 1), slope(o.sma_length, o.slope_lookback, o.slope_flat_points, 1),
              adx(o.dmi_length, o.adx_length, 1)
        {
        }
        studies::StudyMA ma;
        studies::StudySMASlope slope;
        studies::StudyADX adx;
        std::size_t count{}, line_count{};
        Decimal ema_sum{}, atr_sum{};
        std::optional<Decimal> ema, atr, prev_close, swing_high, prior_high, swing_low, prior_low;
        std::deque<PriceBar> swings;
        int line_trend{}, line_candidate{}, last_direction{};
        bool strong{};
        StudyValues values;
    };
    struct State
    {
        std::int64_t raw_time{};
        std::optional<PriceBar> previous, aggregate;
        std::optional<Engine> engine;
        std::int64_t key{};
        bool seen{}, include{};
    };
    void finish(State& state);
    void push(State& state, const PriceBar& bar);
    void advance(State& state, std::int64_t time);
    struct Session
    {
        std::int64_t key;
        bool inside, at_start;
    };
    Session session(std::int64_t time) const;
    MarketTrendSettings options_;
    studies::LiveStateHistory<State> states_;
};
} // namespace fig::collections::sadx2
