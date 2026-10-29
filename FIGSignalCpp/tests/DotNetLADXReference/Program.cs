using System.Globalization;
using FIG.Studies;
using FIG.StudyCollections.LADX;
using FIGCommon.Models;
using Microsoft.Extensions.Logging.Abstractions;

// Independent oracle: invoke the plugin through its production collection factory,
// not the bridge entry points. Include same-bar revisions and historical rewinds.
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
if (args.Length != 1) throw new ArgumentException("Expected output directory");
Directory.CreateDirectory(args[0]);
File.WriteAllText(Path.Combine(args[0], "parameters.json"), "{}");
using var prices = new StreamWriter(Path.Combine(args[0], "prices.csv"));
using var expected = new StreamWriter(Path.Combine(args[0], "expected.csv"));
prices.WriteLine("RawTime,Open,High,Low,Close,Volume");
var collection = new LADXFactory().Create(new StudyCollectionContext(
    new StudyColRS { ColType = "LADX", ColParams = "{}" }, new TickerRS(),
    new IntervalRS { IntervalLen = 120 }, NullLoggerFactory.Instance));
string[]? keys = null;
string Quote(string text) => "\"" + text.Replace("\"", "\"\"") + "\"";
void Emit(int index, decimal adjustment = 0, int deleted = 0)
{
    decimal value = 100 + (index % 71) * .25m + adjustment;
    var price = new PriceDataRS { RawTime = 1700000000L + index * 120, Open = value,
        High = value + 1, Low = value - 1, Close = value + .125m, Volume = 100 };
    prices.WriteLine($"{price.RawTime},{price.Open},{price.High},{price.Low},{price.Close},{price.Volume}");
    var output = collection.Calc([price], deleted).Single().Studies;
    if (keys == null)
    {
        keys = output.Keys.Order(StringComparer.Ordinal).ToArray();
        expected.WriteLine("RawTime," + string.Join(',', keys.Select(Quote)));
    }
    expected.WriteLine(price.RawTime + "," + string.Join(',', keys.Select(k =>
        output[k] is string text ? Quote(text) : Convert.ToString(output[k], CultureInfo.InvariantCulture))));
}
for (int i = 0; i < 1600; ++i)
{
    Emit(i);
    if (i % 31 == 0) Emit(i, .5m, 1);
}
Emit(1597, -.75m, 3);
Emit(1598);
Emit(1599);
Console.WriteLine("Generated factory reference with warmup, numeric/text/null outputs and revisions.");
