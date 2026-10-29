namespace FIGSignalBacktest;
// Chronological port of the native ADXDiagnostics report; separate from plugin strategies.
public sealed class Diagnostics
{
    private readonly int period, smoothing; private readonly string source;
    private readonly Window prices, guide; private readonly Adx adx;
    private readonly Queue<double> roc = [], slopes = []; private int side, barsSinceClose;
    public Diagnostics(Options o)
    {
        if (o.Period < 2 || o.Smoothing < 1) throw new ArgumentException("Diagnostic period must be at least 2 and smoothing positive");
        period = o.Period; smoothing = o.Smoothing; source = o.Source; _ = Value(default, source);
        prices = new(period); guide = new(period); adx = new(period, smoothing);
    }
    private static double Value(Price p, string s) => s switch { "open" => p.Open, "high" => p.High, "low" => p.Low, "close" => p.Close, "hl2" => (p.High + p.Low) / 2, "hlc3" => (p.High + p.Low + p.Close) / 3, "ohlc4" => (p.Open + p.High + p.Low + p.Close) / 4, "oc2" => (p.Open + p.Close) / 2, "hlcc4" => (p.High + p.Low + 2 * p.Close) / 4, _ => throw new ArgumentException("Invalid price source: " + s) };
    private sealed class Window(int n)
    {
        private readonly Queue<double> values = []; private double sum, bbSquares, extSum, extXY, extSquares, weighted, wmaSum;
        public double? Mean, Wma, Sem, BbWidth, ExtMean, ExtSem, ExtSd;
        public void Push(double v)
        {
            bool full = values.Count == n; double old = full ? values.Peek() : 0;
            sum += v; bbSquares += v * v; if (full) { sum -= old; bbSquares -= old * old; }
            if (full) { var prior = wmaSum; weighted = weighted - prior + n * v; wmaSum = prior - old + v; } else wmaSum += v;
            if (full) values.Dequeue(); values.Enqueue(v);
            if (!full) { weighted = 0; int i = 0; foreach (var x in values) weighted += x * ++i; }
            if (values.Count < n) return;
            if (!full) { extSum = extXY = extSquares = 0; int i = 0; foreach (var x in values) { extSum += x; extXY += (n - i++) * x; extSquares += x * x; } }
            else { var prior = extSum; extXY += prior + v - old * (n + 1); extSum += v - old; extSquares += v * v - old * old; }
            double count = n, xsum = count * (count + 1) / 2, xx = count * (count + 1) * (2 * count + 1) / 6, den = count * xx - xsum * xsum;
            Mean = sum / count; Wma = weighted / (count * (count + 1) / 2); Sem = (extSum - ((count * extXY - xsum * extSum) / den) * xsum) / count;
            BbWidth = 2 * Math.Sqrt(Math.Max(0, bbSquares / count - Mean.Value * Mean.Value)); ExtMean = extSum / count; ExtSem = Sem; ExtSd = Math.Sqrt(Math.Max(0, extSquares / count - ExtMean.Value * ExtMean.Value));
        }
    }
    private sealed class Adx(int length, int smooth)
    {
        private Price? previous; private int dm, dxCount; private double trSum, pdmSum, ndmSum, dxSum; private double? lastAdx;
        public (double? Adx, double? Pdi, double? Ndi) Push(Price bar)
        {
            var old = previous; previous = bar; if (old == null) return (null, null, null); var p = old.Value;
            double up = bar.High - p.High, down = p.Low - bar.Low, pdm = up > down && up > 0 ? up : 0, ndm = down > up && down > 0 ? down : 0, tr = Math.Max(bar.High - bar.Low, Math.Max(Math.Abs(bar.High - p.Close), Math.Abs(bar.Low - p.Close)));
            if (dm < length) { trSum += tr; pdmSum += pdm; ndmSum += ndm; if (++dm < length) return (null, null, null); }
            else { trSum = trSum - trSum / length + tr; pdmSum = pdmSum - pdmSum / length + pdm; ndmSum = ndmSum - ndmSum / length + ndm; }
            if (trSum <= 0) return (null, null, null);
            double pd = 100 * (pdmSum / trSum), nd = 100 * (ndmSum / trSum), total = pd + nd, dx = total > 0 ? 100 * Math.Abs(pd - nd) / total : 0;
            if (lastAdx == null) { dxSum += dx; if (++dxCount >= smooth) lastAdx = dxSum / smooth; } else lastAdx = (lastAdx.Value * (smooth - 1) + dx) / smooth;
            return (lastAdx, pd, nd);
        }
    }
    public StudyRow Process(Price p)
    {
        double value = Value(p, source); prices.Push(value); guide.Push(Value(p, "ohlc4")); var a = adx.Push(p);
        roc.Enqueue(value); if (roc.Count > period) roc.Dequeue(); double multiplier = value > 0 ? Math.Pow(10, Math.Ceiling(Math.Log10(value))) / 10 : 0;
        slopes.Enqueue(prices.Mean ?? 0); if (slopes.Count > 4) slopes.Dequeue(); double slope = slopes.Count == 4 && (prices.Mean ?? 0) != 0 && slopes.Peek() != 0 ? (prices.Mean!.Value - slopes.Peek()) / 3 : 0;
        var v = new SortedDictionary<string, double?>(StringComparer.Ordinal)
        {
            ["MA"] = prices.Mean,
            ["WMA"] = prices.Wma,
            ["SEM"] = prices.Sem,
            ["ROC"] = value != 0 ? (roc.Peek() - value) * multiplier / value : 0,
            ["BB_Middle"] = prices.Mean,
            ["BB_Upper"] = prices.Mean + prices.BbWidth,
            ["BB_Lower"] = prices.Mean - prices.BbWidth,
            ["Extended_MA"] = prices.ExtMean,
            ["Extended_SEM"] = prices.ExtSem,
            ["Extended_Lower1"] = prices.ExtMean - prices.ExtSd,
            ["Extended_Upper1"] = prices.ExtMean + prices.ExtSd,
            ["Extended_Lower2"] = prices.ExtMean - 2 * prices.ExtSd,
            ["Extended_Upper2"] = prices.ExtMean + 2 * prices.ExtSd,
            ["ADX"] = a.Adx,
            ["PDI"] = a.Pdi,
            ["NDI"] = a.Ndi,
            ["Slope_Direction"] = prices.Mean == null ? null : Math.Abs(slope) <= .5 ? 0 : Math.Sign(slope),
            ["Slope_PerBar"] = prices.Mean == null ? null : slope
        };
        var upper = guide.Mean + guide.BbWidth; var lower = guide.Mean - guide.BbWidth; double bias = (a.Pdi ?? 0) - (a.Ndi ?? 0);
        if (a.Adx == null || (a.Pdi ?? 0) == 0 || (a.Ndi ?? 0) == 0 || (upper ?? 0) == 0 || (lower ?? 0) == 0) side = 0;
        else { if (side == 0 && barsSinceClose >= 0) barsSinceClose++; bool enter = bias > 0 && a.Adx > 20 && guide.Mean < upper; if (barsSinceClose > 36 || guide.Mean > upper) barsSinceClose = -1; if (side == 0) side = enter ? 1 : 0; else if (bias < -.75) side = 0; }
        v["ADXTrend_MAFast"] = guide.Mean; v["ADXTrend_MAGuide"] = guide.Mean; v["ADXTrend_UBGuide"] = upper; v["ADXTrend_LBGuide"] = lower; v["ADXTrend_MIDGuide"] = guide.Mean;
        v["ADXTrend_Active_ADX"] = a.Adx; v["ADXTrend_Active_PDI"] = a.Pdi; v["ADXTrend_Active_NDI"] = a.Ndi; v["ADXTrend_LBASEADX_SIDE"] = side; v["ADXTrend_LBASEADX_AdxBias"] = bias; v["ADXTrend_LBASEADX_BarCountSinceClose"] = barsSinceClose;
        return new(p, v, new(StringComparer.Ordinal));
    }
}

