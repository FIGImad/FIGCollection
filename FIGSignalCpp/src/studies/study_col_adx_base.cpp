#include "fig/studies/study_col_adx_base.hpp"
#include <algorithm>
namespace fig::studies
{
namespace
{
std::size_t lookback(const AdxBaseOptions& o)
{
    o.validate();
    // Finite warmup bound only. Wilder smoothing and trade state require full chronological replay for exact results.
    return std::max({2 * o.adx_length,
                     2 * o.confirm_length,
                     o.fast_ma_length * 2 + 6,
                     o.slow_ma_length,
                     o.guide_ma_length + 5,
                     o.shock_atr_length + 1,
                     o.adx_length + 11});
}
std::vector<SharedMovingAverages::Key> ma_requests(const AdxBaseOptions& o)
{
    std::vector<SharedMovingAverages::Key> requests{{o.fast_ma_length + 6, "close"}, {o.guide_ma_length, "close"}};
    // The Guide BB already calculates its ohlc4 mean.
    if (o.fast_ma_length != o.guide_ma_length)
        requests.emplace_back(o.fast_ma_length, "ohlc4");
    if (o.slow_ma_length != o.guide_ma_length)
        requests.emplace_back(o.slow_ma_length, "ohlc4");
    return requests;
}
} // namespace
StudyColADXBase::StudyColADXBase(AdxBaseOptions o, std::size_t d)
    : lookback_(lookback(o)), fast_period_(o.fast_ma_length), slow_period_(o.slow_ma_length),
      guide_period_(o.guide_ma_length), moving_averages_(ma_requests(o), d),
      guide_(o.guide_ma_length, o.guide_deviation, "ohlc4", d), active_(o.adx_length, o.adx_length, d),
      confirm_(o.confirm_length, o.confirm_length, d), roc_(10, d), shock_(o.shock_atr_length, d),
      conf_slope_(o.fast_ma_length + 6, o.fast_ma_length, 0.1L, d), guide_slope_(o.guide_ma_length, 5, 0.1L, d),
      long_signal_(o, d), short_signal_(o, d)
{
}
StudyBar StudyColADXBase::process(const PriceBar& bar)
{
    AdxBaseInput i;
    i.price = bar;
    const auto bands = guide_.process(bar);
    moving_averages_.process(bar);
    const auto ohlc_ma = [&](std::size_t period)
    { return (period == guide_period_ ? bands.middle : moving_averages_.value(period, "ohlc4")).value_or(0); };
    i.price_ma = ohlc_ma(fast_period_);
    i.stop_ma = ohlc_ma(slow_period_);
    i.guide_middle = bands.middle.value_or(0);
    i.guide_upper = bands.upper.value_or(0);
    i.guide_lower = bands.lower.value_or(0);
    i.active = active_.process(bar);
    i.confirm = confirm_.process(bar);
    // Despite the ADXROC name, the supplied collection feeds Active_BIAS, not Active_ADX.
    i.roc = roc_.process(bar.raw_time, i.active.bias);
    i.shock = shock_.process(bar);
    i.confirm_slope = conf_slope_.process(bar.raw_time, moving_averages_.value(fast_period_ + 6, "close"));
    i.guide_slope = guide_slope_.process(bar.raw_time, moving_averages_.value(guide_period_, "close"));
    StudyBar out{bar, {}};
    auto& v = out.studies;
    v["MAPrice"] = i.price_ma;
    v["MAStop"] = i.stop_ma;
    v["MIDGuide"] = i.guide_middle;
    v["UBGuide"] = i.guide_upper;
    v["LBGuide"] = i.guide_lower;
    auto adx = [&](const std::string& label, const AdxValues& a)
    {
        v[label + "_ADX"] = a.adx;
        v[label + "_PDI"] = a.pdi;
        v[label + "_NDI"] = a.ndi;
        v[label + "_BIAS"] = a.bias;
    };
    adx("Active", i.active);
    adx("Conf", i.confirm);
    v["Active_ADX_ROC"] = i.roc.roc;
    v["Active_ADX_ROC_PERCENT"] = i.roc.percent;
    v["Active_ADX_SLOPE"] = i.roc.slope;
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
    auto slope = [&](const std::string& label, const SmaSlopeValues& s)
    {
        v[label + "_MA"] = s.ma;
        v[label + "_MASlope"] = s.flag;
        v[label + "_MASlopeValue"] = s.slope;
    };
    slope("SLConf", i.confirm_slope);
    slope("SLGuide", i.guide_slope);
    for (auto signal : {long_signal_.process(i)/*, short_signal_.process(i)*/})
    {
        out.studies.merge(signal.studies);
        out.text_studies.merge(signal.text_studies);
    }
    return out;
}
} // namespace fig::studies
