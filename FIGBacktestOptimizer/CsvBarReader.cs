using System.Globalization;
using System.Text;

namespace FIGBacktestOptimizer;

public static class CsvBarReader
{
    public static List<Bar> ReadBars(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Bar data file not found.", path);
        }

        using var reader = new StreamReader(path);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            throw new InvalidOperationException("CSV file is empty.");
        }

        var headers = SplitCsvLine(headerLine);
        var map = BuildHeaderMap(headers);

        var openIndex = Require(map, "open");
        var highIndex = Require(map, "high");
        var lowIndex = Require(map, "low");
        var closeIndex = Require(map, "close");
        var volumeIndex = TryFind(map, "volume", "vol");
        var timestampIndex = TryFind(map, "datetime", "timestamp", "date_time", "time");
        var dateIndex = TryFind(map, "date");
        var timeIndex = TryFind(map, "bar_time", "tod", "time");

        var bars = new List<Bar>();
        var lineNumber = 1;
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = SplitCsvLine(line);
            try
            {
                var time = ParseTime(fields, timestampIndex, dateIndex, timeIndex);
                var bar = new Bar(
                    time,
                    ParseDouble(fields, openIndex),
                    ParseDouble(fields, highIndex),
                    ParseDouble(fields, lowIndex),
                    ParseDouble(fields, closeIndex),
                    volumeIndex >= 0 && volumeIndex < fields.Count ? ParseDouble(fields, volumeIndex, 0) : 0);
                bars.Add(bar);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed parsing CSV line {lineNumber}: {line}", ex);
            }
        }

        return bars
            .OrderBy(bar => bar.Time)
            .ToList();
    }

    private static DateTime ParseTime(IReadOnlyList<string> fields, int timestampIndex, int dateIndex, int timeIndex)
    {
        if (dateIndex >= 0 && timeIndex >= 0 && dateIndex != timeIndex)
        {
            return ParseDateTime($"{fields[dateIndex]} {fields[timeIndex]}");
        }

        if (timestampIndex >= 0)
        {
            return ParseDateTime(fields[timestampIndex]);
        }

        if (dateIndex >= 0)
        {
            return ParseDateTime(fields[dateIndex]);
        }

        throw new InvalidOperationException("CSV must contain DateTime/Timestamp or Date and Time columns.");
    }

    private static DateTime ParseDateTime(string value)
    {
        var styles = DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, styles, out var invariant))
        {
            return invariant;
        }

        if (DateTime.TryParse(value, CultureInfo.CurrentCulture, styles, out var current))
        {
            return current;
        }

        throw new FormatException($"Invalid date/time value '{value}'.");
    }

    private static double ParseDouble(IReadOnlyList<string> fields, int index, double? fallback = null)
    {
        if (index < 0 || index >= fields.Count)
        {
            if (fallback is not null) return fallback.Value;
            throw new IndexOutOfRangeException($"Field index {index} is outside row.");
        }

        var text = fields[index].Trim();
        if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value))
        {
            return value;
        }

        if (fallback is not null) return fallback.Value;
        throw new FormatException($"Invalid numeric value '{text}'.");
    }

    private static int Require(Dictionary<string, int> map, params string[] names)
    {
        var index = TryFind(map, names);
        if (index >= 0) return index;
        throw new InvalidOperationException($"CSV is missing required column: {string.Join(" or ", names)}.");
    }

    private static int TryFind(Dictionary<string, int> map, params string[] names)
    {
        foreach (var name in names)
        {
            if (map.TryGetValue(Normalize(name), out var index))
            {
                return index;
            }
        }

        return -1;
    }

    private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
        {
            var normalized = Normalize(headers[i]);
            if (!map.ContainsKey(normalized))
            {
                map[normalized] = i;
            }
        }

        return map;
    }

    private static string Normalize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else if (ch is '_' or ' ')
            {
                builder.Append('_');
            }
        }

        return builder.ToString().Trim('_');
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        result.Add(current.ToString());
        return result;
    }
}
