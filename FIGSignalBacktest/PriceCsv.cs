using System.Globalization;
using Microsoft.VisualBasic.FileIO;

namespace FIGSignalBacktest;

public static class PriceCsv
{
    public static List<Price> Read(string path, int maxBars)
    {
        using var csv = new TextFieldParser(path) { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true };
        csv.SetDelimiters(",");
        var header = csv.ReadFields() ?? throw new InvalidDataException("Price CSV has no header.");
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Length; i++)
            if (!columns.TryAdd(header[i].Trim(), i)) throw new InvalidDataException("Duplicate price CSV column: " + header[i]);
        foreach (var name in new[] { "RawTime", "Open", "High", "Low", "Close", "Volume" })
            if (!columns.ContainsKey(name)) throw new InvalidDataException("Missing price CSV column: " + name);
        var result = new List<Price>();
        while (!csv.EndOfData && (maxBars == 0 || result.Count < maxBars))
        {
            var line = csv.LineNumber;
            try
            {
                var fields = csv.ReadFields()!;
                if (fields.Length != header.Length) throw new InvalidDataException("Wrong number of fields.");
                string Text(string name) => fields[columns[name]];
                double Number(string name) => double.Parse(Text(name), NumberStyles.Float, CultureInfo.InvariantCulture);
                int Optional(string name, int fallback) => columns.ContainsKey(name) ? int.Parse(Text(name), CultureInfo.InvariantCulture) : fallback;
                var price = new Price(Optional("Id", -1), Optional("DataSetId", 0), long.Parse(Text("RawTime"), CultureInfo.InvariantCulture),
                    Number("Open"), Number("High"), Number("Low"), Number("Close"), long.Parse(Text("Volume"), CultureInfo.InvariantCulture));
                price.Validate();
                if (result.Count > 0 && (price.Time <= result[^1].Time || price.DataSetId != result[^1].DataSetId))
                    throw new InvalidDataException("Rows must have increasing RawTime and belong to one dataset.");
                result.Add(price);
            }
            catch (Exception ex) when (ex is FormatException or OverflowException or InvalidDataException or MalformedLineException)
            {
                throw new InvalidDataException($"Price CSV line {line}: {ex.Message}", ex);
            }
        }
        if (result.Count == 0) throw new InvalidDataException("Price CSV has no data rows.");
        return result;
    }
}
