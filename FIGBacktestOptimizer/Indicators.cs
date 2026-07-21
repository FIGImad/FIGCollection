namespace FIGBacktestOptimizer;

public sealed class RollingSma
{
    private readonly int period;
    private readonly Queue<double> values = new();
    private double sum;

    public RollingSma(int period)
    {
        if (period <= 0) throw new ArgumentOutOfRangeException(nameof(period));
        this.period = period;
    }

    public double? Current { get; private set; }

    public double? Update(double value)
    {
        values.Enqueue(value);
        sum += value;

        if (values.Count > period)
        {
            sum -= values.Dequeue();
        }

        Current = values.Count == period ? sum / period : null;
        return Current;
    }
}

public sealed class RollingBollinger
{
    private readonly int period;
    private readonly double stdDev;
    private readonly Queue<double> values = new();
    private double sum;
    private double sumSquares;

    public RollingBollinger(int period, double stdDev)
    {
        if (period <= 0) throw new ArgumentOutOfRangeException(nameof(period));
        this.period = period;
        this.stdDev = stdDev;
    }

    public BollingerValue? Update(double value)
    {
        values.Enqueue(value);
        sum += value;
        sumSquares += value * value;

        if (values.Count > period)
        {
            var old = values.Dequeue();
            sum -= old;
            sumSquares -= old * old;
        }

        if (values.Count < period)
        {
            return null;
        }

        var mean = sum / period;
        var variance = Math.Max(0, (sumSquares / period) - (mean * mean));
        var width = stdDev * Math.Sqrt(variance);
        return new BollingerValue(mean, mean + width, mean - width);
    }
}

public readonly record struct BollingerValue(double Middle, double Upper, double Lower);

public sealed class WilderAtr
{
    private readonly int period;
    private readonly Queue<double> warmup = new();
    private double? previousClose;
    private double? atr;

    public WilderAtr(int period)
    {
        if (period <= 0) throw new ArgumentOutOfRangeException(nameof(period));
        this.period = period;
    }

    public double? Current => atr;

    public double? Update(Bar bar)
    {
        if (previousClose is null)
        {
            previousClose = bar.Close;
            return null;
        }

        var tr = TrueRange(bar.High, bar.Low, previousClose.Value);
        previousClose = bar.Close;

        if (atr is null)
        {
            warmup.Enqueue(tr);
            if (warmup.Count < period)
            {
                return null;
            }

            atr = warmup.Average();
            warmup.Clear();
            return atr;
        }

        atr = ((atr.Value * (period - 1)) + tr) / period;
        return atr;
    }

    public static double TrueRange(double high, double low, double previousClose)
    {
        return Math.Max(high - low, Math.Max(Math.Abs(high - previousClose), Math.Abs(low - previousClose)));
    }
}

public sealed class WilderAdx
{
    private readonly int length;
    private readonly int smoothing;
    private readonly Queue<double> trWarmup = new();
    private readonly Queue<double> pDmWarmup = new();
    private readonly Queue<double> nDmWarmup = new();
    private readonly Queue<double> dxWarmup = new();

    private Bar? previousBar;
    private bool dmInitialized;
    private double trSum;
    private double pDmSum;
    private double nDmSum;
    private double? adx;

    public WilderAdx(int length, int smoothing)
    {
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
        if (smoothing <= 0) throw new ArgumentOutOfRangeException(nameof(smoothing));
        this.length = length;
        this.smoothing = smoothing;
    }

    public AdxValue? Update(Bar bar)
    {
        if (previousBar is null)
        {
            previousBar = bar;
            return null;
        }

        var upMove = bar.High - previousBar.High;
        var downMove = previousBar.Low - bar.Low;
        var pDm = upMove > downMove && upMove > 0 ? upMove : 0;
        var nDm = downMove > upMove && downMove > 0 ? downMove : 0;
        var tr = WilderAtr.TrueRange(bar.High, bar.Low, previousBar.Close);
        previousBar = bar;

        if (!dmInitialized)
        {
            trWarmup.Enqueue(tr);
            pDmWarmup.Enqueue(pDm);
            nDmWarmup.Enqueue(nDm);

            if (trWarmup.Count < length)
            {
                return null;
            }

            trSum = trWarmup.Sum();
            pDmSum = pDmWarmup.Sum();
            nDmSum = nDmWarmup.Sum();
            dmInitialized = true;
            trWarmup.Clear();
            pDmWarmup.Clear();
            nDmWarmup.Clear();
        }
        else
        {
            trSum = trSum - (trSum / length) + tr;
            pDmSum = pDmSum - (pDmSum / length) + pDm;
            nDmSum = nDmSum - (nDmSum / length) + nDm;
        }

        if (trSum <= 0)
        {
            return null;
        }

        var pdi = 100 * pDmSum / trSum;
        var ndi = 100 * nDmSum / trSum;
        var diSum = pdi + ndi;
        var dx = diSum > 0 ? 100 * Math.Abs(pdi - ndi) / diSum : 0;

        if (adx is null)
        {
            dxWarmup.Enqueue(dx);
            if (dxWarmup.Count < smoothing)
            {
                return new AdxValue(null, pdi, ndi, pdi - ndi);
            }

            adx = dxWarmup.Average();
            dxWarmup.Clear();
        }
        else
        {
            adx = ((adx.Value * (smoothing - 1)) + dx) / smoothing;
        }

        return new AdxValue(adx, pdi, ndi, pdi - ndi);
    }
}

public readonly record struct AdxValue(double? Adx, double Pdi, double Ndi, double Bias);
