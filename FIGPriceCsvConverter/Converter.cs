using System.Globalization;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace FIGPriceCsvConverter;

public enum AmbiguousTimePolicy { Reject, EarlierUtc, LaterUtc }
public readonly record struct PriceRow(long RawTime, decimal Open, decimal High, decimal Low, decimal Close, long Volume);
public sealed record ConversionResult(int Count, DateTimeOffset FirstUtc, DateTimeOffset LastUtc, IReadOnlyList<PriceRow> Preview);

public static class Converter
{
    public static long ToUnixTime(DateTime local, TimeZoneInfo zone, AmbiguousTimePolicy policy)
    {
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(local))
            throw new InvalidDataException($"{local:yyyy-MM-dd HH:mm:ss} does not exist in {zone.Id} because the clock moves forward. Check the source time zone or export UTC from eSignal.");
        if (zone.IsAmbiguousTime(local))
        {
            if (policy == AmbiguousTimePolicy.Reject)
                throw new InvalidDataException($"{local:yyyy-MM-dd HH:mm:ss} occurs twice in {zone.Id}. Choose an ambiguous-time policy only if you know which occurrence the export represents, or export UTC from eSignal.");
            var offsets = zone.GetAmbiguousTimeOffsets(local);
            return new DateTimeOffset(local, policy == AmbiguousTimePolicy.EarlierUtc ? offsets.Max() : offsets.Min()).ToUnixTimeSeconds();
        }
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone)).ToUnixTimeSeconds();
    }

    public static ConversionResult ConvertFile(string source, string target, TimeZoneInfo zone,
        AmbiguousTimePolicy policy, bool overwrite, IProgress<string>? progress = null, CancellationToken cancellation = default)
    {
        source = Path.GetFullPath(source); target = Path.GetFullPath(target);
        if (source.Equals(target, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Choose a target file different from the source.");
        if (!File.Exists(source)) throw new FileNotFoundException("Source CSV not found.", source);
        if (File.Exists(target) && !overwrite) throw new IOException("Target already exists. Choose another file or allow replacement.");
        if (!Directory.Exists(Path.GetDirectoryName(target))) throw new DirectoryNotFoundException("Choose an existing target folder.");
        var rows = new List<PriceRow>();
        progress?.Report("Reading source prices…");
        using (var csv = new TextFieldParser(source) { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true })
        {
            csv.SetDelimiters(",");
            var header = csv.ReadFields() ?? throw new InvalidDataException("CSV has no header.");
            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Length; i++)
                if (!columns.TryAdd(header[i].Trim(), i)) throw new InvalidDataException("Duplicate CSV column: " + header[i]);
            foreach (var name in new[] { "Date", "Time", "Open", "High", "Low", "Close" })
                if (!columns.ContainsKey(name)) throw new InvalidDataException("Missing CSV column: " + name);
            var volumeColumn = columns.ContainsKey("Vol") ? "Vol" : "Volume";
            if (!columns.ContainsKey(volumeColumn)) throw new InvalidDataException("Missing CSV column: Vol (or Volume).");
            while (!csv.EndOfData)
            {
                cancellation.ThrowIfCancellationRequested();
                var line = csv.LineNumber;
                try
                {
                    var fields = csv.ReadFields()!;
                    if (fields.Length != header.Length) throw new InvalidDataException("Wrong number of fields.");
                    string Value(string name) => fields[columns[name]].Trim();
                    var local = DateTime.ParseExact(Value("Date") + " " + Value("Time"),
                        ["yyyy-MM-dd h:mm:ss tt", "yyyy-MM-dd hh:mm:ss tt", "yyyy-MM-dd H:mm:ss", "yyyy-MM-dd HH:mm:ss",
                         "M/d/yyyy h:mm:ss tt", "M/d/yyyy hh:mm:ss tt", "M/d/yyyy H:mm:ss", "M/d/yyyy HH:mm:ss"],
                        CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
                    decimal Price(string name) => decimal.Parse(Value(name), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                    var row = new PriceRow(ToUnixTime(local, zone, policy), Price("Open"), Price("High"), Price("Low"), Price("Close"),
                        long.Parse(Value(volumeColumn), NumberStyles.Integer, CultureInfo.InvariantCulture));
                    if (row.High < row.Low || row.Open < row.Low || row.Open > row.High || row.Close < row.Low || row.Close > row.High || row.Volume < 0)
                        throw new InvalidDataException("Invalid OHLC range or negative volume.");
                    rows.Add(row);
                    if (rows.Count % 100000 == 0) progress?.Report($"Read {rows.Count:N0} prices…");
                }
                catch (Exception ex) when (ex is FormatException or OverflowException or InvalidDataException or MalformedLineException or ArgumentException)
                {
                    throw new InvalidDataException($"Source line {line}: {ex.Message}", ex);
                }
            }
        }
        if (rows.Count == 0) throw new InvalidDataException("CSV contains no price rows.");
        cancellation.ThrowIfCancellationRequested();
        progress?.Report($"Sorting {rows.Count:N0} prices oldest first…");
        rows.Sort((a, b) => a.RawTime.CompareTo(b.RawTime));
        for (int i = 1; i < rows.Count; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            if (rows[i].RawTime == rows[i - 1].RawTime)
                throw new InvalidDataException($"Duplicate UTC timestamp: {DateTimeOffset.FromUnixTimeSeconds(rows[i].RawTime):u}. FIGSignalBacktest requires unique timestamps. If the source includes both fall-back occurrences, export UTC from eSignal.");
        }
        var temporary = Path.Combine(Path.GetDirectoryName(target)!, ".fig-convert-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            progress?.Report("Writing converted prices…");
            using (var writer = new StreamWriter(new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None), new UTF8Encoding(false)))
            {
                writer.WriteLine("RawTime,Open,High,Low,Close,Volume");
                foreach (var row in rows)
                {
                    cancellation.ThrowIfCancellationRequested();
                    writer.WriteLine(FormattableString.Invariant($"{row.RawTime},{row.Open},{row.High},{row.Low},{row.Close},{row.Volume}"));
                }
            }
            cancellation.ThrowIfCancellationRequested();
            File.Move(temporary, target, overwrite);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        return new(rows.Count, DateTimeOffset.FromUnixTimeSeconds(rows[0].RawTime), DateTimeOffset.FromUnixTimeSeconds(rows[^1].RawTime), rows.Take(8).ToArray());
    }
}
