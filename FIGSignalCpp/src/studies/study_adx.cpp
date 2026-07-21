#include "fig/studies/study_adx.hpp"
#include <algorithm>
#include <cmath>
namespace fig::studies
{
StudyADX::StudyADX(std::size_t length, std::size_t smoothing, std::size_t depth)
    : length_(length), smoothing_(smoothing), states_(depth)
{
    if (!length_ || !smoothing_)
        throw std::invalid_argument("Invalid ADX parameters");
}
AdxValues StudyADX::process(const PriceBar& bar)
{
    auto s = states_.begin(bar.raw_time);
    s.value = {};
    if (!s.previous)
    {
        s.previous = bar;
        return states_.commit(std::move(s)).value;
    }
    const auto& p = *s.previous;
    const auto up = bar.high - p.high, down = p.low - bar.low, pdm = (up > down && up > 0) ? up : 0,
               ndm = (down > up && down > 0) ? down : 0,
               tr = std::max({bar.high - bar.low, std::abs(bar.high - p.close), std::abs(bar.low - p.close)});
    s.previous = bar;
    if (s.dm_count < length_)
    {
        s.tr_sum += tr;
        s.pdm_sum += pdm;
        s.ndm_sum += ndm;
        ++s.dm_count;
        if (s.dm_count < length_)
            return states_.commit(std::move(s)).value;
    }
    else
    {
        s.tr_sum = s.tr_sum - s.tr_sum / static_cast<Decimal>(length_) + tr;
        s.pdm_sum = s.pdm_sum - s.pdm_sum / static_cast<Decimal>(length_) + pdm;
        s.ndm_sum = s.ndm_sum - s.ndm_sum / static_cast<Decimal>(length_) + ndm;
    }
    if (s.tr_sum <= 0)
        return states_.commit(std::move(s)).value;
    const auto pdi = 100 * s.pdm_sum / s.tr_sum, ndi = 100 * s.ndm_sum / s.tr_sum, total = pdi + ndi,
               dx = total > 0 ? 100 * std::abs(pdi - ndi) / total : 0;
    s.value.pdi = pdi;
    s.value.ndi = ndi;
    if (!s.last_adx)
    {
        s.dx_sum += dx;
        ++s.dx_count;
        if (s.dx_count >= smoothing_)
        {
            s.last_adx = s.dx_sum / static_cast<Decimal>(smoothing_);
            s.value.adx = s.last_adx;
        }
    }
    else
    {
        s.last_adx = (*s.last_adx * static_cast<Decimal>(smoothing_ - 1) + dx) / static_cast<Decimal>(smoothing_);
        s.value.adx = s.last_adx;
    }
    return states_.commit(std::move(s)).value;
}
} // namespace fig::studies
