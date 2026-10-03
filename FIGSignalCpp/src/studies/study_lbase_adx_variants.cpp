#include "fig/studies/study_lbase_adx_variants.hpp"
#include <algorithm>
namespace fig::studies
{
StudyBar StudyLBaseADXVariant::process(const AdxBaseInput& input)
{
    auto s = states_.begin(input.price.raw_time);
    if (new_ && s.side == 0 && s.bars_since_close >= 0)
        ++s.bars_since_close;
    if (input.active.adx)
    {
        s.last_valid = input;
        if (input.active.bias)
        {
            const auto bias = *input.active.bias;
            if (!new_ && bias > 9)
                s.trigger = -0.25L;
            if (new_ && s.side != 0)
            {
                s.max_bias = std::max(s.max_bias, bias);
                s.stop_bias = s.max_bias > 8.5L ? 4 : -0.75L;
            }
        }
        if (new_ && (s.bars_since_close > 36 || input.price_ma > input.guide_upper))
            s.bars_since_close = -1;
    }
    if (s.last_valid && s.last_valid->active.bias)
    {
        const auto& i = *s.last_valid;
        const auto bias = *i.active.bias;
        if (!s.side)
        {
            const bool enter =
                new_ ? bias > 0 && i.price_ma < i.guide_upper + 10 : bias > s.trigger && i.price_ma < i.guide_middle;
            if (enter)
            {
                s.side = 1;
                if (new_)
                    s.max_bias = s.stop_bias = 0;
            }
        }
        else
        {
            const bool stop = new_ ? bias < s.stop_bias || (s.max_bias > 8.5L && i.price_ma < i.stop_ma) : bias < 3;
            if (stop)
            {
                s.side = 0;
                if (new_)
                    s.bars_since_close = 0;
                else
                    s.trigger = bias;
            }
        }
    }
    StudyBar out{input.price, {}};
    auto put = [&](const std::string& name, std::optional<Decimal> value)
    { out.studies["STrend_LBaseADX_" + name] = value; };
    put("Side", s.side);
    put("ADX", input.active.adx);
    const auto* i = s.last_valid ? &*s.last_valid : nullptr;
    put("PriceMA", i ? std::optional<Decimal>(i->price_ma) : std::nullopt);
    put("GuideMA", i ? std::optional<Decimal>(i->guide_middle) : std::nullopt);
    put("GuideUB", i ? std::optional<Decimal>(i->guide_upper) : std::nullopt);
    put("GuideLB", i ? std::optional<Decimal>(i->guide_lower) : std::nullopt);
    put("ADXBias", i ? i->active.bias : std::nullopt);
    put("ADXSlope", i ? i->roc.slope : std::nullopt);
    if (new_)
    {
        put("StopMA", i ? std::optional<Decimal>(i->stop_ma) : std::nullopt);
        put("ADXConf", i ? i->confirm.adx : std::nullopt);
        put("ADXConfBias", i ? i->confirm.bias : std::nullopt);
        put("MAXBias", s.max_bias);
    }
    else
        put("ADXTrigger", s.trigger);
    states_.commit(std::move(s));
    return out;
}
} // namespace fig::studies
