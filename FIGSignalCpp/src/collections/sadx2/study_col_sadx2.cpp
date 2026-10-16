#include "fig/collections/sadx2/study_col_sadx2.hpp"
#include <algorithm>

namespace fig::collections::sadx2
{
namespace
{
std::size_t lookback(const Options& o, int interval)
{
    o.validate();
    const auto minutes = interval / 60;
    if (interval % 60 || minutes < 1 || minutes >= 1440 || 1440 % minutes ||
        o.market.session_start_minute % static_cast<std::size_t>(minutes) ||
        o.market.session_end_minute % static_cast<std::size_t>(minutes) ||
        (o.market.timeframe == "hourly" && (minutes > 60 || 60 % minutes)))
        throw std::invalid_argument("SADX2 requires aligned minute bars; hourly intervals must divide 60");
    const auto candles = std::max({o.market.sma_length + o.market.slope_lookback,
                                   o.market.dmi_length + o.market.adx_length,
                                   o.market.ema_length,
                                   o.market.atr_length});
    const auto bars_per_candle = static_cast<std::size_t>((o.market.timeframe == "hourly" ? 3600 : 86400) / interval);
    return std::max({2 * o.adx_length,
                     o.fast_ma_length,
                     o.guide_ma_length + 5,
                     o.shock_atr_length + 1,
                     (candles + 1) * bars_per_candle + 1});
}
std::vector<studies::SharedMovingAverages::Key> ma_requests(const Options& o)
{
    std::vector<studies::SharedMovingAverages::Key> requests{{o.guide_ma_length, "close"}};
    if (o.fast_ma_length != o.guide_ma_length)
        requests.emplace_back(o.fast_ma_length, "ohlc4");
    return requests;
}
} // namespace
StudyColSADX2::StudyColSADX2(Options o, std::size_t depth, int interval_seconds)
    : lookback_(lookback(o, interval_seconds)), fast_period_(o.fast_ma_length), guide_period_(o.guide_ma_length),
      averages_(ma_requests(o), depth), guide_(o.guide_ma_length, o.guide_deviation, "ohlc4", depth),
      active_(o.adx_length, o.adx_length, depth), shock_(o.shock_atr_length, depth),
      guide_slope_(o.guide_ma_length, 5, 0.1L, depth), signal_(o, depth), market_(o.market, depth)
{
}
StudyBar StudyColSADX2::process(const PriceBar& bar)
{
    SADX2Input i;
    auto market = market_.process(bar);
    i.market_trend = static_cast<int>(market.studies.at("MarketTrend").value_or(0));
    i.price = bar;
    const auto bands = guide_.process(bar);
    averages_.process(bar);
    i.price_ma = (fast_period_ == guide_period_ ? bands.middle : averages_.value(fast_period_, "ohlc4")).value_or(0);
    i.guide_middle = bands.middle.value_or(0);
    i.guide_upper = bands.upper.value_or(0);
    i.guide_lower = bands.lower.value_or(0);
    i.active = active_.process(bar);
    i.shock = shock_.process(bar);
    i.guide_slope = guide_slope_.process(bar.raw_time, averages_.value(guide_period_, "close"));
    auto out = signal_.process(i);
    out.studies.merge(market.studies);
    out.text_studies.merge(market.text_studies);
    auto& v = out.studies;
    v["MAPrice"] = i.price_ma;
    v["MIDGuide"] = i.guide_middle;
    v["UBGuide"] = i.guide_upper;
    v["LBGuide"] = i.guide_lower;
    v["Active_ADX"] = i.active.adx;
    v["Active_PDI"] = i.active.pdi;
    v["Active_NDI"] = i.active.ndi;
    v["Active_BIAS"] = i.active.bias;
    v["Shock_ATR"] = i.shock.atr;
    v["Shock_DownPoints"] = i.shock.down_points;
    v["Shock_UpPoints"] = i.shock.up_points;
    v["Shock_RangePoints"] = i.shock.range_points;
    v["Shock_PullbackPoints"] = i.shock.pullback_points;
    v["Shock_DownATR"] = i.shock.down_atr;
    v["Shock_UpATR"] = i.shock.up_atr;
    v["Shock_RangeATR"] = i.shock.range_atr;
    v["Shock_PullbackATR"] = i.shock.pullback_atr;
    v["Shock_Score"] = i.shock.score;
    v["SLGuide_MA"] = i.guide_slope.ma;
    v["SLGuide_MASlope"] = i.guide_slope.flag;
    v["SLGuide_MASlopeValue"] = i.guide_slope.slope;
    return out;
}
} // namespace fig::collections::sadx2
