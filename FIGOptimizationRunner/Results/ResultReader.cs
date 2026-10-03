using System.Globalization;
using Microsoft.VisualBasic.FileIO;

namespace FIGOptimizationRunner.Results;

public sealed record RunResult(string RunId, double NetProfit, double FinalNAV, double MaxDrawdownPct,
    long ClosedTrades, double? TrainNetProfit, double? ValidationNetProfit, double? TestNetProfit);

public static class ResultReader
{
    public static IReadOnlyList<RunResult> Read(string path)
    {
        using var csv = new TextFieldParser(path);
        csv.TextFieldType = FieldType.Delimited;
        csv.SetDelimiters(",");
        csv.HasFieldsEnclosedInQuotes = true;
        var header = csv.ReadFields() ?? throw new InvalidDataException("Results CSV has no header.");
        var columns = header.Select((name, i) => (name, i)).ToDictionary(x => x.name, x => x.i, StringComparer.Ordinal);
        foreach (var name in new[] { "RunId", "NetProfit", "FinalNAV", "MaxDrawdownPct", "ClosedTrades", "TrainNetProfit", "ValidationNetProfit", "TestNetProfit" })
            if (!columns.ContainsKey(name)) throw new InvalidDataException($"Missing result column: {name}");
        var result = new List<RunResult>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        while (!csv.EndOfData)
        {
            var row = csv.ReadFields()!;
            if (row.Length != header.Length) throw new InvalidDataException("Results CSV row has the wrong number of fields.");
            string Text(string name) => row[columns[name]];
            double Number(string name)
            {
                var value = double.Parse(Text(name), CultureInfo.InvariantCulture);
                if (!double.IsFinite(value)) throw new InvalidDataException($"Nonfinite result: {name}");
                return value;
            }
            double? Optional(string name) => string.IsNullOrWhiteSpace(Text(name)) ? null : Number(name);
            var id = Text("RunId");
            if (!ids.Add(id)) throw new InvalidDataException($"Duplicate result ID: {id}");
            result.Add(new(id, Number("NetProfit"), Number("FinalNAV"), Number("MaxDrawdownPct"),
                long.Parse(Text("ClosedTrades"), CultureInfo.InvariantCulture), Optional("TrainNetProfit"),
                Optional("ValidationNetProfit"), Optional("TestNetProfit")));
        }
        return result;
    }
}
