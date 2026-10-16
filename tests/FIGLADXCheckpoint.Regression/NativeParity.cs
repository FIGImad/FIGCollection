using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using FIG.StudyCollections.LADX;
using FIGCommon.Models;

internal static class NativeParity
{
    public static void Run(Action<bool, string> check)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var native = Path.Combine(root, "FIGSignalCpp/x64/Release");
        var directory = Path.Combine(root, "tests/FIGLADXCheckpoint.Regression/obj/native-parity");
        Directory.CreateDirectory(directory);
        var scenarios = new[] {
            new LADXOptions(),
            new LADXOptions { AdxLen = 1, FastMALen = 1, GuideMALen = 1, ShockATRLen = 1 },
            new LADXOptions { FastMALen = 8, GuideMALen = 8, ReEntryCooldownBars = 3 },
            new LADXOptions { AdxLen = 6, ContinuationEntryBars = 0, ChopMaxGuideSlopeATR = 0 },
            new LADXOptions { AdxLen = 6, ContinuationEntryBars = 5, ShockArmUpATR = .3, ShockArmedDropATR = .5 },
            LADXOptions.FromJson(JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "FIGSignalCpp/ladx-parameters.json"))).RootElement)
        };
        int comparisons = 0;
        var subtypes = new HashSet<decimal>();
        foreach (var (options, index) in scenarios.Select((o, i) => (o, i)))
        {
            var parameters = JsonSerializer.SerializeToNode(options)!.AsObject();
            parameters.Remove("RevisionDepth");
            var parameterPath = Path.Combine(directory, "parameters.json");
            File.WriteAllText(parameterPath, parameters.ToJsonString());
            foreach (int priceMode in new[] { 0, 1, 2 })
            {
                bool flat = priceMode == 1;
                var prices = Enumerable.Range(0, 1200).Select(i => {
                    decimal close = flat ? 100 : 100 + (decimal)(Math.Sin(i * .039) * 35 + Math.Sin(i * .41) * 3);
                    if (priceMode != 2) close = Math.Round(close * 4) / 4;
                    return new PriceDataRS { RawTime = 1700000040 + i * 60L, Open = close + (flat ? 0 : priceMode == 2 ? .3m : .25m),
                        High = close + (flat ? 0 : 2), Low = close - (flat ? 0 : 3), Close = close, Volume = 1000 };
                }).ToList();
                var input = Path.Combine(directory, "prices.csv");
                var output = Path.Combine(directory, "native.csv");
                File.WriteAllLines(input, new[] { "RawTime,Open,High,Low,Close,Volume" }.Concat(prices.Select(p =>
                    FormattableString.Invariant($"{p.RawTime},{p.Open},{p.High},{p.Low},{p.Close},{p.Volume}"))));
                var start = new ProcessStartInfo(Path.Combine(native, "FIGCollectionProbe.exe")) { UseShellExecute = false, RedirectStandardError = true };
                foreach (var arg in new[] { Path.Combine(native, "FIGStudyCollection.LADX.dll"), "LADX", parameterPath, input, output }) start.ArgumentList.Add(arg);
                using var process = Process.Start(start)!;
                var errors = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0) throw new Exception(errors);
                var lines = File.ReadAllLines(output);
                var keys = lines[0].Split(',').Skip(1).Select(s => s.Trim('"')).ToArray();
                var engine = new LADXEngine(options);
                for (int row = 0; row < prices.Count; row++)
                {
                    var actual = engine.Process(prices[row]);
                    if (!actual.Keys.Order().SequenceEqual(keys.Order())) throw new Exception("Native schema mismatch");
                    var expected = lines[row + 1].Split(',').Skip(1).ToArray();
                    for (int col = 0; col < keys.Length; col++)
                    {
                        var value = actual[keys[col]];
                        bool match = value is string text ? text == expected[col].Trim('"') :
                            value == null ? expected[col] == "" :
                            decimal.TryParse(expected[col], NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                                && Math.Abs((decimal)value - number) <= .000002m;
                        if (!match) throw new Exception($"Native mismatch scenario {index}, flat={flat}, bar {row}, {keys[col]}: C#={value}, C++={expected[col]}");
                        comparisons++;
                    }
                    subtypes.Add((decimal)actual["LADX_SIDE"]!);
                }
            }
        }
        check(subtypes.SetEquals(new[] { 0m, 1.1m, 1.2m }), "Native parity covers flat and both long entry subtypes");
        check(true, $"All {comparisons} native output comparisons match (numeric tolerance 0.000002)");
        foreach (var json in new[] { "{\"Unknown\":1}", "{\"AdxLen\":1,\"AdxLen\":2}", "{\"adxlen\":2}", "{\"AdxLen\":1.5}" })
        {
            try { LADXOptions.FromJson(JsonDocument.Parse(json).RootElement); throw new Exception("Invalid parameter accepted"); }
            catch (ArgumentException) { check(true, "Invalid native parameter rejected"); }
        }
        var floored = LADXOptions.FromJson(JsonDocument.Parse("{\"AdxLen\":null,\"ReEntryCooldownBars\":2.9,\"ContinuationEntryBars\":3.7}").RootElement);
        check(floored.AdxLen == 50 && floored.ReEntryCooldownBars == 2 && floored.ContinuationEntryBars == 3, "Null defaults and floored counters match native parser");
    }
}
