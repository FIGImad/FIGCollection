using System.Globalization;
using FIGCommon.Models;

namespace FIGSignalBacktest;

public readonly record struct Price(int Id, int DataSetId, long Time, double Open, double High, double Low, double Close, long Volume)
{
    public PriceDataRS Managed() => new()
    {
        Id = Id,
        DataSetId = DataSetId,
        RawTime = Time,
        PriceDate = DateTimeOffset.FromUnixTimeSeconds(Time).UtcDateTime,
        Open = Decimal(Open),
        High = Decimal(High),
        Low = Decimal(Low),
        Close = Decimal(Close),
        Volume = checked((int)Volume)
    };
    private static decimal Decimal(double value) => decimal.Parse(value.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);
    public void Validate() { if (!double.IsFinite(Open) || !double.IsFinite(High) || !double.IsFinite(Low) || !double.IsFinite(Close) || High < Low) throw new InvalidDataException("Invalid OHLC data"); }
}
public sealed record StudyRow(Price Price, SortedDictionary<string, double?> Numbers, SortedDictionary<string, string> Text);
public static class Format
{
    public static string N(object? value) => value switch { null => "", double d => d.ToString("G17", CultureInfo.InvariantCulture), bool b => b ? "1" : "0", IFormattable f => f.ToString(null, CultureInfo.InvariantCulture), _ => value.ToString()! };
    public static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
    public static long Date(string text) => new DateTimeOffset(DateTime.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)).ToUnixTimeSeconds();
    public static StreamWriter Writer(string path) => new(path, false, new System.Text.UTF8Encoding(false)) { NewLine = "\n" };
    public static void Row(TextWriter writer, params object?[] values) => writer.WriteLine(string.Join(',', values.Select(N)));
}
