#include "fig/studies/study_lbase_adx.hpp"
namespace fig::studies
{
StudyLBaseADXEx::StudyLBaseADXEx(Decimal trigger, Decimal exit, std::size_t depth)
    : trigger_(trigger), bias_exit_(exit), states_(depth)
{
}
LBaseAdxValues StudyLBaseADXEx::process(std::int64_t time, const LBaseAdxInput& i)
{
    auto s = states_.begin(time);
    const auto adx = i.adx.value_or(-1), pdi = i.pdi.value_or(0), ndi = i.ndi.value_or(0),
               gu = i.guide_upper.value_or(0), gl = i.guide_lower.value_or(0), gm = i.guide_middle.value_or(0),
               pm = i.price_ma.value_or(0);
    if (adx == -1 || pdi == 0 || ndi == 0 || gu == 0 || gl == 0)
    {
        s.side = 0;
        s.value = {s.side, pm, gm, gu, gl, pdi - ndi, adx, s.bars_since_close};
        return states_.commit(std::move(s)).value;
    }
    if (s.side == 0 && s.bars_since_close >= 0)
        ++s.bars_since_close;
    const auto bias = pdi - ndi;
    const bool enter = bias > 0 && adx > trigger_ && pm < gu;
    if (s.bars_since_close > 36 || pm > gu)
        s.bars_since_close = -1;
    if (s.side == 0)
        s.side = enter ? 1 : 0;
    else if (bias < bias_exit_)
        s.side = 0;
    s.value = {s.side, pm, gm, gu, gl, bias, adx, s.bars_since_close};
    return states_.commit(std::move(s)).value;
}
} // namespace fig::studies
