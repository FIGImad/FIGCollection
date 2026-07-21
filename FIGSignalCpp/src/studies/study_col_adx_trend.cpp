#include "fig/studies/study_col_adx_trend.hpp"
#include <algorithm>
namespace fig::studies
{
StudyColADXTrend::StudyColADXTrend(AdxTrendOptions o, std::size_t d)
    : maximum_lookback_(std::max({o.price_ma_length, o.guide_ma_length, o.adx_length, o.adx_smoothing}) + 20),
      price_ma_(o.price_ma_length, "ohlc4", d), guide_ma_(o.guide_ma_length, "ohlc4", d),
      guide_bands_(o.guide_ma_length, o.guide_deviation, "ohlc4", d), adx_(o.adx_length, o.adx_smoothing, d),
      signal_(o.adx_trigger, -0.75L, d)
{
    if (!o.price_ma_length || !o.guide_ma_length || !o.adx_length || !o.adx_smoothing || o.guide_deviation <= 0)
        throw std::invalid_argument("Invalid ADXTrend options");
}
StudyBar StudyColADXTrend::process(const PriceBar& bar)
{
    const auto fast = price_ma_.process(bar);
    const auto guide = guide_ma_.process(bar);
    const auto bands = guide_bands_.process(bar);
    const auto adx = adx_.process(bar);
    const auto sig =
        signal_.process(bar.raw_time, {adx.adx, adx.pdi, adx.ndi, bands.upper, bands.lower, bands.middle, fast});
    StudyBar out{bar, {}};
    out.studies["MAFast"] = fast;
    out.studies["MAGuide"] = guide;
    out.studies["UBGuide"] = bands.upper;
    out.studies["LBGuide"] = bands.lower;
    out.studies["MIDGuide"] = bands.middle;
    out.studies["Active_ADX"] = adx.adx;
    out.studies["Active_PDI"] = adx.pdi;
    out.studies["Active_NDI"] = adx.ndi;
    out.studies["LBASEADX_SIDE"] = sig.side;
    out.studies["LBASEADX_AdxBias"] = sig.adx_bias;
    out.studies["LBASEADX_BarCountSinceClose"] = static_cast<Decimal>(sig.bars_since_close);
    return out;
}
} // namespace fig::studies
