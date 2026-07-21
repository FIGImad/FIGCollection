namespace FIGBacktestOptimizer;

public sealed class AdxBaseBacktester
{
    private readonly IReadOnlyList<Bar> bars;
    private readonly BacktestSettings settings;
    private readonly ObjectiveSettings objective;

    public AdxBaseBacktester(IReadOnlyList<Bar> bars, BacktestSettings settings, ObjectiveSettings objective)
    {
        this.bars = bars;
        this.settings = settings;
        this.objective = objective;
    }

    public BacktestRun Run(AdxBaseParameters parameters, string segmentName)
    {
        var priceMa = new RollingSma(parameters.FastMALen);
        var guide = new RollingBollinger(parameters.GuideMALen, parameters.GuideStdDev);
        var activeAdx = new WilderAdx(parameters.AdxLen, parameters.AdxLen);
        var confirmAdx = new WilderAdx(parameters.AdxLenConf, parameters.AdxLenConf);
        var shockAtr = new WilderAtr(parameters.ShockATRLen);

        var run = new BacktestRun();
        var state = new StrategyState();
        var cumulativeClosedProfit = 0.0;
        var commissionTotal = 0.0;
        var nav = 100.0;
        var navMax = 100.0;
        var maxDrawdownPts = 0.0;
        var maxDrawdownPct = 0.0;
        Trade? openTrade = null;
        var entryValue = 0.0;

        StudyFrame? previousFrame = null;
        StudyFrame? previousPreviousFrame = null;

        for (var i = 0; i < bars.Count; i++)
        {
            var bar = bars[i];
            var ohlc4 = (bar.Open + bar.High + bar.Low + bar.Close) / 4.0;
            var priceMaValue = priceMa.Update(ohlc4);
            var guideValue = guide.Update(ohlc4);
            var active = activeAdx.Update(bar);
            var confirm = confirmAdx.Update(bar);
            var atrValue = shockAtr.Update(bar);

            var shock = BuildShock(bar, i > 0 ? bars[i - 1] : null, atrValue);
            var frame = new StudyFrame(bar, priceMaValue, guideValue, active, confirm, shock);

            if (frame.IsReady)
            {
                if (openTrade is null)
                {
                    var entryMode = TestStart(parameters, state, frame, previousFrame);
                    if (entryMode is not null)
                    {
                        var fillPrice = ApplySlippage(bar.Close, isBuy: true);
                        var quantity = CalculateQuantity(fillPrice);
                        if (quantity > 0)
                        {
                            openTrade = new Trade
                            {
                                OpenTime = bar.Time,
                                OpenPrice = fillPrice,
                                Quantity = quantity,
                                EntryMode = entryMode
                            };
                            entryValue = fillPrice * quantity * settings.Multiplier;
                            commissionTotal -= quantity * settings.CommissionPerContract;
                            state.BeginLong(frame, entryMode);
                        }
                    }
                }
                else
                {
                    state.UpdateLong(frame, previousFrame, previousPreviousFrame);
                    var stopReason = TestStop(parameters, state, frame, previousFrame, bars, i);
                    if (stopReason is not null)
                    {
                        var fillPrice = ApplySlippage(bar.Close, isBuy: false);
                        var quantity = openTrade.Quantity;
                        commissionTotal -= quantity * settings.CommissionPerContract;
                        var profit = ((fillPrice * quantity * settings.Multiplier) - entryValue)
                            - (2 * quantity * settings.CommissionPerContract);
                        cumulativeClosedProfit += profit;

                        openTrade.CloseTime = bar.Time;
                        openTrade.ClosePrice = fillPrice;
                        openTrade.Profit = profit;
                        openTrade.BarsHeld = state.BarsInTrade;
                        openTrade.ExitReason = stopReason;
                        run.Trades.Add(openTrade);

                        state.MarkExit(stopReason);
                        state.ClearLong();
                        openTrade = null;
                        entryValue = 0;
                    }
                }
            }

            var openProfit = openTrade is null
                ? 0
                : ((bar.Close * openTrade.Quantity * settings.Multiplier) - entryValue);
            var net = cumulativeClosedProfit + openProfit + commissionTotal;
            nav = (settings.InitialCapital + net) * 100.0 / settings.InitialCapital;
            navMax = Math.Max(navMax, nav);
            var drawdownPts = nav - navMax;
            var drawdownPct = navMax == 0 ? 0 : (nav / navMax - 1.0) * 100.0;
            maxDrawdownPts = Math.Min(maxDrawdownPts, drawdownPts);
            maxDrawdownPct = Math.Min(maxDrawdownPct, drawdownPct);

            previousPreviousFrame = previousFrame;
            previousFrame = frame;
        }

        if (openTrade is not null && bars.Count > 0)
        {
            var last = bars[^1];
            var fillPrice = ApplySlippage(last.Close, isBuy: false);
            var quantity = openTrade.Quantity;
            var profit = ((fillPrice * quantity * settings.Multiplier) - entryValue)
                - (2 * quantity * settings.CommissionPerContract);
            openTrade.CloseTime = last.Time;
            openTrade.ClosePrice = fillPrice;
            openTrade.Profit = profit;
            openTrade.BarsHeld = state.BarsInTrade;
            openTrade.ExitReason = "END_OF_DATA";
            run.Trades.Add(openTrade);
            cumulativeClosedProfit += profit;
        }

        run.Metrics = BuildMetrics(segmentName, run.Trades, cumulativeClosedProfit, maxDrawdownPts, maxDrawdownPct);
        return run;
    }

    private BacktestMetrics BuildMetrics(string segmentName, IReadOnlyList<Trade> trades, double netProfit, double maxDrawdownPts, double maxDrawdownPct)
    {
        var winners = trades.Count(trade => trade.Profit > 0);
        var losers = trades.Count(trade => trade.Profit < 0);
        var grossProfit = trades.Where(trade => trade.Profit > 0).Sum(trade => trade.Profit);
        var grossLoss = trades.Where(trade => trade.Profit < 0).Sum(trade => trade.Profit);
        var profitFactor = grossLoss == 0 ? (grossProfit > 0 ? 999.0 : 0.0) : grossProfit / Math.Abs(grossLoss);
        var metrics = new BacktestMetrics
        {
            Segment = segmentName,
            NetProfit = netProfit,
            NetProfitPct = settings.InitialCapital == 0 ? 0 : netProfit * 100.0 / settings.InitialCapital,
            MaxDrawdownPts = maxDrawdownPts,
            MaxDrawdownPct = maxDrawdownPct,
            Trades = trades.Count,
            Winners = winners,
            Losers = losers,
            WinRatePct = trades.Count == 0 ? 0 : winners * 100.0 / trades.Count,
            ProfitFactor = profitFactor,
            AverageTrade = trades.Count == 0 ? 0 : trades.Average(trade => trade.Profit)
        };
        metrics.Score = Score(metrics);
        return metrics;
    }

    public double Score(BacktestMetrics metrics)
    {
        var lowTradePenalty = Math.Max(0, settings.MinTrades - metrics.Trades) * objective.LowTradeCountPenalty;
        var tradePenalty = metrics.Trades * objective.TradeCountPenalty;
        var profitFactorScore = Math.Min(metrics.ProfitFactor, 3.0) * objective.ProfitFactorWeight;
        return (metrics.NetProfitPct * objective.NetProfitPctWeight)
            - (Math.Abs(metrics.MaxDrawdownPct) * objective.MaxDrawdownPctWeight)
            - tradePenalty
            - lowTradePenalty
            + profitFactorScore;
    }

    private int CalculateQuantity(double fillPrice)
    {
        if (settings.Quantity > 0)
        {
            return settings.Quantity;
        }

        if (settings.QuantityPercent <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling(settings.InitialCapital * settings.QuantityPercent / settings.Multiplier / fillPrice);
    }

    private double ApplySlippage(double price, bool isBuy)
    {
        var adjustment = settings.SlippageTicks * settings.TickSize;
        return isBuy ? price + adjustment : price - adjustment;
    }

    private static PriceShockValue BuildShock(Bar bar, Bar? previous, double? atr)
    {
        if (previous is null || atr is null || atr <= 0)
        {
            return new PriceShockValue(0, 0, 0, 0, 0);
        }

        var closeDelta = bar.Close - previous.Close;
        var downPoints = Math.Max(0, -closeDelta);
        var upPoints = Math.Max(0, closeDelta);
        var rangePoints = Math.Max(0, bar.High - bar.Low);
        var pullbackPoints = Math.Max(0, bar.High - bar.Close);
        var downAtr = downPoints / atr.Value;
        var upAtr = upPoints / atr.Value;
        var rangeAtr = rangePoints / atr.Value;
        var pullbackAtr = pullbackPoints / atr.Value;
        return new PriceShockValue(Math.Max(downAtr, pullbackAtr), downAtr, upAtr, rangeAtr, pullbackAtr);
    }

    private static string? TestStart(AdxBaseParameters p, StrategyState state, StudyFrame frame, StudyFrame? previous)
    {
        if (state.IsReEntryCooldownActive(p))
        {
            return null;
        }

        var priceMa = frame.PriceMa!.Value;
        var guide = frame.Guide!.Value;
        var pullbackBelowGuide = priceMa < guide.Middle;
        var continuationAboveGuide = priceMa >= guide.Middle && frame.Bar.Close >= guide.Middle;
        var priceRecovering = previous?.PriceMa is null || priceMa >= previous.PriceMa.Value;
        var previousBias = previous?.ActiveAdx?.Bias;
        var biasNotDeteriorating = previousBias is null || frame.ActiveAdx!.Value.Bias >= previousBias;
        var shockCleared = frame.Shock.Score <= p.TrendReEntryShockMax;
        var confirmOk = p.AdxTriggerConf <= 0
            || (frame.ConfirmAdx?.Adx is not null && frame.ConfirmAdx.Value.Adx >= p.AdxTriggerConf && frame.ConfirmAdx.Value.Bias > 0);

        var pullbackEntry = pullbackBelowGuide && priceRecovering && biasNotDeteriorating;
        var trendReEntry = state.IsRecentProtectiveExit(p) && continuationAboveGuide && priceRecovering && shockCleared;
        var shouldStart = frame.ActiveAdx!.Value.Bias > p.StartBias && (pullbackEntry || trendReEntry) && confirmOk;

        if (!shouldStart)
        {
            return null;
        }

        return trendReEntry ? "TREND_REENTRY" : "PULLBACK";
    }

    private static string? TestStop(AdxBaseParameters p, StrategyState state, StudyFrame frame, StudyFrame? previous, IReadOnlyList<Bar> bars, int index)
    {
        var previousBar = index > 0 ? bars[index - 1] : null;
        var brokeEntryLow = state.EntryLow is not null && previousBar is not null && previousBar.Low < state.EntryLow.Value - p.FailFastBuffer;
        var failedPullback = p.FailFastBars > 0 && state.FailFastWeakBars >= p.FailFastBars && frame.PriceMa!.Value < frame.Guide!.Value.Middle;
        var brokeGuideBand = previous?.Guide is not null && previousBar is not null && previousBar.Close < previous.Guide.Value.Lower - p.FailFastBuffer;
        var shockArmed = state.MaxUpAtr >= p.ShockArmUpATR;
        var rapidDrop = frame.Shock.Score >= p.ShockDropATR;
        var armedDrop = shockArmed && frame.Shock.Score >= p.ShockArmedDropATR;
        var shockRangeDrop = frame.Shock.RangeAtr >= p.ShockRangeATR && frame.Shock.DownAtr > 0;

        if (frame.ActiveAdx!.Value.Bias < p.StopBias) return "ADX_BIAS";
        if (rapidDrop) return "SHOCK_DROP";
        if (armedDrop) return "ARMED_DROP";
        if (shockRangeDrop) return "SHOCK_RANGE";
        if (brokeEntryLow && frame.PriceMa!.Value < frame.Guide!.Value.Middle) return "ENTRY_LOW";
        if (failedPullback) return "PULLBACK_FAIL";
        if (brokeGuideBand) return "GUIDE_LB";
        return null;
    }
}

public readonly record struct PriceShockValue(double Score, double DownAtr, double UpAtr, double RangeAtr, double PullbackAtr);

public sealed class StudyFrame
{
    public StudyFrame(Bar bar, double? priceMa, BollingerValue? guide, AdxValue? activeAdx, AdxValue? confirmAdx, PriceShockValue shock)
    {
        Bar = bar;
        PriceMa = priceMa;
        Guide = guide;
        ActiveAdx = activeAdx;
        ConfirmAdx = confirmAdx;
        Shock = shock;
    }

    public Bar Bar { get; }
    public double? PriceMa { get; }
    public BollingerValue? Guide { get; }
    public AdxValue? ActiveAdx { get; }
    public AdxValue? ConfirmAdx { get; }
    public PriceShockValue Shock { get; }
    public bool IsReady => PriceMa is not null && Guide is not null && ActiveAdx?.Adx is not null;
}

public sealed class StrategyState
{
    public int BarsInTrade { get; private set; }
    public int FailFastWeakBars { get; private set; }
    public double? EntryLow { get; private set; }
    public double MaxUpAtr { get; private set; }
    public int BarsSinceExit { get; private set; } = 1_000_000;
    public string LastExitReason { get; private set; } = "";

    public void BeginLong(StudyFrame frame, string entryMode)
    {
        BarsInTrade = 0;
        FailFastWeakBars = 0;
        EntryLow = frame.Bar.Low;
        MaxUpAtr = 0;
    }

    public void UpdateLong(StudyFrame frame, StudyFrame? previous, StudyFrame? previousPrevious)
    {
        BarsInTrade++;

        var completedBarWeak = previous is not null
            && previousPrevious is not null
            && previous.PriceMa is not null
            && previous.Guide is not null
            && previous.PriceMa.Value < previous.Guide.Value.Middle
            && previous.Bar.Close < previousPrevious.Bar.Close;

        if (completedBarWeak)
        {
            FailFastWeakBars++;
        }
        else if (previous is not null && previousPrevious is not null
            && previous.PriceMa is not null
            && previous.Guide is not null
            && (previous.PriceMa.Value >= previous.Guide.Value.Middle || previous.Bar.Close >= previousPrevious.Bar.Close))
        {
            FailFastWeakBars = 0;
        }

        MaxUpAtr = Math.Max(MaxUpAtr, frame.Shock.UpAtr);
    }

    public void MarkExit(string reason)
    {
        LastExitReason = reason;
        BarsSinceExit = 0;
    }

    public void ClearLong()
    {
        BarsInTrade = 0;
        FailFastWeakBars = 0;
        EntryLow = null;
        MaxUpAtr = 0;
    }

    public bool IsReEntryCooldownActive(AdxBaseParameters p)
    {
        return p.ReEntryCooldownBars > 0 && BarsSinceExit < p.ReEntryCooldownBars;
    }

    public bool IsRecentProtectiveExit(AdxBaseParameters p)
    {
        if (p.TrendReEntryBars <= 0 || BarsSinceExit > p.TrendReEntryBars)
        {
            return false;
        }

        return LastExitReason is "SHOCK_DROP" or "ARMED_DROP" or "SHOCK_RANGE";
    }
}
