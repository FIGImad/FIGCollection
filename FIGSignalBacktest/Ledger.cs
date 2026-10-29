namespace FIGSignalBacktest;

public sealed record Trade(long EntryTime, long ExitTime, double Entry, double Exit, double Gross, double Fees, string Reason, int Quantity, int Side, double EntrySignal);
public sealed class Month
{
    public int Id, Position; public long LastTime; public double Start, Equity, Realized, Unrealized, Fees, Drawdown, DrawdownPoints; public int Trades, Bars;
}
public sealed class Segment
{
    public double Start, End, Peak, Drawdown; public int Bars, Trades;
    public void Update(double before, double equity, int closed) { if (Bars == 0) Start = Peak = before; End = equity; Peak = Math.Max(Peak, equity); if (Peak > 0) Drawdown = Math.Min(Drawdown, 100 * (equity / Peak - 1)); Bars++; Trades += closed; }
}
public sealed class Ledger(Options options)
{
    public readonly Options Options = options;
    public double Equity = options.Capital, Peak = options.Capital, Realized, Unrealized, Fees, MaxDrawdown, MaxDrawdownPoints, Entry;
    public bool Open; public long EntryTime; public int Side, Quantity; public long? LastTime;
    public readonly List<Month> Months = []; public readonly List<Trade> Trades = []; public readonly Segment[] Segments = [new(), new(), new()];
    private readonly long trainEnd = Format.Date(options.TrainEnd), validationEnd = Format.Date(options.ValidationEnd);
    public static int Signal(double? value) { if (value == null || !double.IsFinite(value.Value)) throw new InvalidDataException("Missing/invalid position signal (expected a finite number)"); return Math.Sign(value.Value); }
    private double entrySignal;
    public void Process(Price bar, double? signal, bool final = false)
    {
        int side = Signal(signal);
        if (side is < -1 or > 1 || (LastTime.HasValue && bar.Time <= LastTime)) throw new InvalidDataException("Invalid side or non-increasing backtest prices");
        var o = Options; var date = DateTimeOffset.FromUnixTimeSeconds(bar.Time); int month = date.Year * 12 + date.Month - 1; double before = Equity;
        if (Months.Count == 0 || month != Months[^1].Id)
        {
            if (Months.Count > 0) for (int id = Months[^1].Id + 1; id < month; id++) Months.Add(new() { Id = id, LastTime = LastTime!.Value, Start = Equity, Equity = Equity, Unrealized = Unrealized, Drawdown = 100 * (Equity / Peak - 1), DrawdownPoints = 100 * (Equity - Peak) / o.Capital, Position = Open ? Side * Quantity : 0 });
            Months.Add(new() { Id = month, Start = Equity });
        }
        var m = Months[^1]; double previousRealized = Realized, previousFees = Fees; int closed = 0;
        void Close(string reason)
        {
            double exit = bar.Close - Side * o.Slippage, gross = (exit - Entry) * Side * Quantity * o.Multiplier, fees = 2.0 * Quantity * o.Commission;
            Realized += gross; Fees += Quantity * o.Commission; Trades.Add(new(EntryTime, bar.Time, Entry, exit, gross, fees, reason, Quantity, Side, entrySignal)); Open = false; closed++;
        }
        if (Open && side != Side) Close(side == 0 ? "SIGNAL" : "REVERSAL");
        if (!Open && side != 0)
        {
            Quantity = o.Quantity;
            if (Quantity == 0)
            {
                var hlc = (bar.High + bar.Low + bar.Close) / 3; if (!double.IsFinite(hlc) || hlc <= 0) throw new InvalidDataException("EFS sizing requires positive finite HLC3");
                var sized = Math.Ceiling(o.Capital * o.QuantityPct / o.Multiplier / hlc); if (!double.IsFinite(sized) || sized < 1 || sized > int.MaxValue) throw new InvalidDataException("EFS quantity outside supported range"); Quantity = (int)sized;
            }
            entrySignal = signal!.Value;
            Side = side; Entry = bar.Close + Side * o.Slippage; EntryTime = bar.Time; Fees += Quantity * o.Commission; Open = true;
        }
        if (Open && final && o.ForceClose) Close("END_OF_DATA");
        Unrealized = Open ? (bar.Close - Entry) * Side * Quantity * o.Multiplier : 0; Equity = o.Capital + Realized + Unrealized - Fees; Peak = Math.Max(Peak, Equity);
        double dd = 100 * (Equity / Peak - 1), points = 100 * (Equity - Peak) / o.Capital;
        MaxDrawdown = Math.Min(MaxDrawdown, dd); MaxDrawdownPoints = Math.Min(MaxDrawdownPoints, points);
        m.Drawdown = Math.Min(m.Drawdown, dd); m.DrawdownPoints = Math.Min(m.DrawdownPoints, points); m.Realized += Realized - previousRealized; m.Fees += Fees - previousFees;
        m.Equity = Equity; m.Unrealized = Unrealized; m.Position = Open ? Side * Quantity : 0; m.LastTime = bar.Time; m.Bars++; m.Trades += closed;
        Segments[bar.Time < trainEnd ? 0 : bar.Time < validationEnd ? 1 : 2].Update(before, Equity, closed); LastTime = bar.Time;
    }
}
