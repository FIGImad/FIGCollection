#include "fig/collections/ladx2/market_trend.hpp"
#include <algorithm>
#include <chrono>
#include <cmath>
namespace fig::collections::ladx2
{
MarketTrend::MarketTrend(MarketTrendSettings o, std::size_t depth) : options_(std::move(o)), states_(depth)
{
}
MarketTrend::Session MarketTrend::session(std::int64_t time) const
{
    using namespace std::chrono;
    const sys_seconds timestamp{seconds{time}};
    int offset = 0;
    if (options_.time_zone != "UTC")
    {
        const year y = year_month_day{floor<days>(timestamp)}.year();
        if (int(y) < 2007)
            throw std::invalid_argument("LADX2 New York timezone requires 2007 onward, as in EFS");
        const auto start = sys_days{y / March / Sunday[2]} + hours{7};
        const auto end = sys_days{y / November / Sunday[1]} + hours{6};
        offset = timestamp >= start && timestamp < end ? -240 : -300;
    }
    const auto local = timestamp + minutes{offset};
    const auto day = floor<days>(local);
    const auto minute = duration_cast<minutes>(local - day).count();
    const auto elapsed = (minute - static_cast<std::int64_t>(options_.session_start_minute) + 1440) % 1440;
    auto duration = (options_.session_end_minute + 1440 - options_.session_start_minute) % 1440;
    if (!duration)
        duration = 1440;
    const bool hourly = options_.timeframe == "hourly";
    const auto key = hourly ? floor<hours>(timestamp).time_since_epoch().count()
                            : floor<days>(local - minutes{options_.session_start_minute}).time_since_epoch().count();
    return {key,
            elapsed < static_cast<std::int64_t>(duration),
            hourly ? time % 3600 == 0 : elapsed == 0 && duration_cast<seconds>(local - day).count() % 60 == 0};
}
void MarketTrend::finish(State& s)
{
    if (!s.aggregate)
        return;
    if (s.include)
    {
        auto& e = *s.engine;
        auto bar = *s.aggregate;
        bar.raw_time = static_cast<std::int64_t>(e.count + 1);
        const auto ma = e.ma.process(bar);
        const auto slope = e.slope.process(bar.raw_time, ma);
        const auto adx = e.adx.process(bar);
        const auto& o = options_;
        const auto tr =
            e.prev_close
                ? std::max({bar.high - bar.low, std::abs(bar.high - *e.prev_close), std::abs(bar.low - *e.prev_close)})
                : bar.high - bar.low;
        ++e.count;
        if (!e.ema)
        {
            e.ema_sum += bar.close;
            if (e.count == o.ema_length)
                e.ema = e.ema_sum / static_cast<Decimal>(o.ema_length);
        }
        else
            *e.ema += 2 / static_cast<Decimal>(o.ema_length + 1) * (bar.close - *e.ema);
        if (!e.atr)
        {
            e.atr_sum += tr;
            if (e.count == o.atr_length)
                e.atr = e.atr_sum / static_cast<Decimal>(o.atr_length);
        }
        else
            *e.atr += (tr - *e.atr) / static_cast<Decimal>(o.atr_length);
        e.prev_close = bar.close;
        e.swings.push_back(bar);
        if (e.swings.size() > 5)
            e.swings.pop_front();
        if (e.swings.size() == 5)
        {
            const auto& candidate = e.swings[2];
            bool high = true, low = true;
            for (std::size_t n = 0; n < 5; ++n)
                if (n != 2)
                {
                    if (candidate.high <= e.swings[n].high)
                        high = false;
                    if (candidate.low >= e.swings[n].low)
                        low = false;
                }
            if (high)
            {
                e.prior_high = e.swing_high;
                e.swing_high = candidate.high;
            }
            if (low)
            {
                e.prior_low = e.swing_low;
                e.swing_low = candidate.low;
            }
        }
        int swing = 0;
        if (e.prior_high && e.prior_low)
        {
            if (*e.swing_high > *e.prior_high && *e.swing_low > *e.prior_low && bar.close >= *e.swing_low)
                swing = 1;
            else if (*e.swing_high < *e.prior_high && *e.swing_low < *e.prior_low && bar.close <= *e.swing_high)
                swing = -1;
        }
        e.strong = adx.adx && *adx.adx >= (e.strong ? 20 : 25);
        const int dmi = e.strong ? (*adx.pdi > *adx.ndi ? 1 : *adx.ndi > *adx.pdi ? -1 : 0) : 0;
        if (e.ema && e.atr)
        {
            const auto upper = *e.ema + o.atr_multiplier * *e.atr, lower = *e.ema - o.atr_multiplier * *e.atr;
            const int candidate = bar.close > upper ? 1 : bar.close < lower ? -1 : 0;
            if (*e.atr <= 0)
            {
                e.line_trend = 0;
                e.line_candidate = 0;
                e.line_count = 0;
            }
            else if (candidate && candidate != e.line_trend)
            {
                e.line_count = candidate == e.line_candidate ? e.line_count + 1 : 1;
                e.line_candidate = candidate;
                if (e.line_count >= o.line_confirm_bars)
                {
                    e.line_trend = candidate;
                    e.line_count = 0;
                    e.line_candidate = 0;
                }
            }
            else
            {
                e.line_count = 0;
                e.line_candidate = 0;
            }
        }
        const int slope_direction = slope.flag ? static_cast<int>(*slope.flag) : 0;
        int up = 0, down = 0;
        for (auto vote : {slope_direction, swing, dmi, e.line_trend})
        {
            if (vote == 1)
                ++up;
            if (vote == -1)
                ++down;
        }
        const bool ready = e.ema && e.atr && slope.slope && adx.adx;
        const int raw = ready && e.strong ? (up >= 3 && up > down ? 1 : down >= 3 && down > up ? -1 : 0) : 0;
        int direction = 0;
        if (ready)
        {
            direction = raw ? raw : up > down ? 1 : down > up ? -1 : e.last_direction;
            if (!direction)
                direction = *slope.slope < 0 ? -1 : 1;
        }
        e.last_direction = direction;
        e.values = {{"MarketTrend", direction},
                    {"MarketTrend_RAW_TREND", raw},
                    {"MarketTrend_READY", ready ? 1 : 0},
                    {"MarketTrend_BARS", static_cast<Decimal>(e.count)},
                    {"MarketTrend_EMA", e.ema},
                    {"MarketTrend_ATR", e.atr},
                    {"MarketTrend_ADX", adx.adx},
                    {"MarketTrend_SLOPE", slope.slope},
                    {"MarketTrend_SCORE", up - down},
                    {"MarketTrend_UP_VOTES", up},
                    {"MarketTrend_DOWN_VOTES", down},
                    {"MarketTrend_SLOPE_TREND", slope_direction},
                    {"MarketTrend_SWING_TREND", swing},
                    {"MarketTrend_DMI_TREND", dmi},
                    {"MT_Slope_MA", ma},
                    {"MT_Slope_MASlope", slope.flag.value_or(0)},
                    {"MT_Slope_MASlopeValue", slope.slope},
                    {"MT_Slope_BARS", static_cast<Decimal>(e.count)},
                    {"MT_ADX_BARS", static_cast<Decimal>(e.count)},
                    {"MT_Slope_READY", slope.slope ? 1 : 0},
                    {"MT_ADX_ADX", adx.adx},
                    {"MT_ADX_PDI", adx.pdi},
                    {"MT_ADX_NDI", adx.ndi},
                    {"MT_ADX_BIAS", adx.bias},
                    {"MT_ADX_READY", adx.adx ? 1 : 0}};
    }
    s.aggregate.reset();
}
void MarketTrend::advance(State& s, std::int64_t time)
{
    const auto info = session(time);
    if (s.aggregate && (info.key > s.key || (info.key == s.key && !info.inside)))
        finish(s);
}
void MarketTrend::push(State& s, const PriceBar& bar)
{
    advance(s, bar.raw_time);
    const auto info = session(bar.raw_time);
    if (!info.inside)
        return;
    if (!s.aggregate)
    {
        s.aggregate = bar;
        s.key = info.key;
        s.include = s.seen || info.at_start;
        s.seen = true;
    }
    else
    {
        s.aggregate->high = std::max(s.aggregate->high, bar.high);
        s.aggregate->low = std::min(s.aggregate->low, bar.low);
        s.aggregate->close = bar.close;
    }
}
StudyBar MarketTrend::process(const PriceBar& bar)
{
    auto s = states_.begin(bar.raw_time);
    if (!s.engine)
        s.engine.emplace(options_);
    if (s.previous)
    {
        const auto& p = *s.previous;
        if (std::isfinite(p.open) && std::isfinite(p.high) && std::isfinite(p.low) && std::isfinite(p.close) &&
            p.high >= p.low && p.open >= p.low && p.open <= p.high && p.close >= p.low && p.close <= p.high)
            push(s, p);
        else
        {
            s.engine.emplace(options_);
            s.aggregate.reset();
            s.seen = false;
        }
    }
    advance(s, bar.raw_time);
    auto values = s.engine->values;
    for (const auto* key : {"MarketTrend",
                            "MarketTrend_RAW_TREND",
                            "MarketTrend_READY",
                            "MarketTrend_BARS",
                            "MarketTrend_SCORE",
                            "MarketTrend_UP_VOTES",
                            "MarketTrend_DOWN_VOTES",
                            "MarketTrend_SLOPE_TREND",
                            "MarketTrend_SWING_TREND",
                            "MarketTrend_DMI_TREND",
                            "MT_Slope_READY",
                            "MT_ADX_READY",
                            "MT_Slope_BARS",
                            "MT_ADX_BARS",
                            "MT_Slope_MASlope"})
        if (!values.contains(key))
            values[key] = 0;
    for (const auto* key : {"MarketTrend_EMA",
                            "MarketTrend_ATR",
                            "MarketTrend_ADX",
                            "MarketTrend_SLOPE",
                            "MT_Slope_MA",
                            "MT_Slope_MASlope",
                            "MT_Slope_MASlopeValue",
                            "MT_ADX_ADX",
                            "MT_ADX_PDI",
                            "MT_ADX_NDI",
                            "MT_ADX_BIAS"})
        if (!values.contains(key))
            values[key] = std::nullopt;
    values["MarketTrend_DAILY_BARS"] =
        options_.timeframe == "daily" ? values.at("MarketTrend_BARS") : std::optional<Decimal>{0};
    values["MarketTrend_HOURLY_BARS"] =
        options_.timeframe == "hourly" ? values.at("MarketTrend_BARS") : std::optional<Decimal>{0};
    s.previous = bar;
    states_.commit(std::move(s));
    return {bar, std::move(values), {{"MarketTrend_TIMEFRAME", options_.timeframe}}};
}
} // namespace fig::collections::ladx2
