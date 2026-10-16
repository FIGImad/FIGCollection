using System.Text.Json;
using FIGOptimizationRunner.Configuration;
using FIGOptimizationRunner.Search;

namespace FIGOptimizationRunner;

internal static class NestedParameterTests
{
    private static void Check(bool ok, string label)
    {
        if (!ok) throw new InvalidOperationException("Nested JSON self-test failed: " + label);
    }
    private static void Reject(Action action, string label)
    {
        try { action(); } catch (ArgumentException) { return; }
        throw new InvalidOperationException("Nested JSON self-test expected rejection: " + label);
    }
    private static ParameterValue Value(string json)
    {
        using var document = JsonDocument.Parse(json);
        return new(document.RootElement);
    }
    public static void Run()
    {
        const string json = """
            {
              "indicators":{"adxLength":40},
              "entry":{"rules":{"bias":1}},
              "exit":{"enabled":true},
              "market":{"timeframe":"hourly","zone":"UTC"},
              "strategies":[{"weights":[0.5,null]},{"name":"second"}],
              "literal.key":9,"literal":{"key":8},
              "a/b":{"~key":{"":2}},"emptyObject":{},"emptyArray":[],"nullable":null
            }
            """;
        var baseline = ParameterSet.Parse(json);
        Check(ParameterSet.Parse(baseline.ToJson()).Identity == baseline.Identity, "round trip preserves all JSON types");
        Check(baseline.Get("entry.rules.bias") == 1 && baseline.Get("/entry/rules/bias") == 1, "nested object paths");
        Check(baseline.Get("/strategies/0/weights/0") == Value("0.50"), "array traversal");
        Check(baseline.Get("/a~1b/~0key/") == 2, "escaped and empty property names");
        Check(baseline.Get("literal.key") == 9 && baseline.Get("/literal/key") == 8, "literal dotted keys remain compatible");
        var changed = baseline.With(new Dictionary<string, ParameterValue>
        {
            ["entry.rules.bias"] = 2, ["exit.enabled"] = false,
            ["market.timeframe"] = "daily", ["/strategies/0/weights/1"] = 3,
            ["nullable"] = Value("{\"child\":[true,\"x\",null]}")
        });
        Check(changed.Get("entry.rules.bias") == 2 && changed.Get("exit.enabled") == false &&
            changed.Get("market.timeframe") == "daily" && changed.Get("/strategies/0/weights/1") == 3,
            "typed replacements applied at depth");
        Check(changed.Get("/nullable/child/0") == true && changed.Get("market.zone") == "UTC" &&
            changed.Get("emptyObject") == Value("{}") && changed.Get("emptyArray") == Value("[]"), "fixed structures preserved");
        Check(baseline.Get("exit.enabled") == true && baseline.Get("/strategies/0/weights/1") == Value("null"), "baseline immutable");
        Check(ParameterSet.Parse("{\"b\":[1.0,true],\"a\":2e0}").Identity ==
            ParameterSet.Parse("{\"a\":2,\"b\":[1,true]}").Identity, "object order and exact numbers normalized");
        Check(Value("1") != Value("true") && Value("1") != Value("\"1\"") && Value("null") != Value("\"null\""), "types have distinct identities");
        Check(Value("1e1000") == Value("10e999") && Value("-0.0") == Value("0"), "large exponents and signed zero");
        Check(Value("1.000000000000000000000000000001") != Value("1"), "no decimal rounding of identities");
        Check(Value("[1,2]") != Value("[2,1]"), "array ordering preserved");
        Reject(() => ParameterSet.Parse("{\"group\":{\"x\":1,\"x\":2}}"), "nested duplicate keys");
        Reject(() => ParameterSet.Parse("{\"list\":[{\"x\":1,\"x\":2}]}"), "duplicates inside arrays");
        Reject(() => ParameterSet.Parse("[]"), "root must be an object");
        foreach (var path in new[] { "missing.child", "exit.enabled.child", "/strategies/2", "/strategies/01", "/strategies/-1", "/a~2b" })
            Reject(() => baseline.Get(path), "invalid path " + path);

        var generator = new RandomCandidateGenerator();
        var config = new SearchConfiguration
        {
            Collection = "Custom", Samples = 7,
            Ranges = new() { ["entry.rules.bias"] = [1, 2], ["exit.enabled"] = [true, false], ["market.timeframe"] = ["hourly", "daily"] }
        };
        var candidates = generator.Generate(config, baseline, default).ToArray();
        Check(candidates.Length == 8 && candidates[0].Identity == baseline.Identity &&
            candidates.Select(p => p.Identity).Distinct().Count() == 8, "mixed Cartesian space exhausted");
        Check(candidates.Select(p => p.Identity).SequenceEqual(generator.Generate(config, baseline, default).Select(p => p.Identity)), "mixed deterministic generation");
        Check(candidates.All(p => p.Get("market.zone") == "UTC" && p.Get("indicators.adxLength") == 40), "fixed fields stay intact");
        Reject(() => generator.Generate(config with { Samples = 8 }, baseline, default).ToArray(), "mixed oversubscription");
        var outside = baseline.With(new Dictionary<string, ParameterValue> { ["market.timeframe"] = "weekly" });
        Check(generator.Generate(config with { Samples = 8 }, outside, default).Count() == 9, "mixed baseline outside space");
        Reject(() => generator.Generate(config with { Ranges = new() { ["entry.rules.bias"] = [Value("1"), Value("1.0")] } }, baseline, default).ToArray(), "numeric equivalent choices rejected");
        Reject(() => generator.Generate(config with { Ranges = new() { ["exit.enabled"] = [true, true] } }, baseline, default).ToArray(), "duplicate booleans");
        foreach (var ranges in new Dictionary<string, ParameterValue[]>[]
        {
            new() { ["entry.rules.bias"] = [1, 2], ["/entry/rules/bias"] = [1, 2] },
            new() { ["entry"] = [Value("{}")], ["entry.rules.bias"] = [1] },
            new() { ["/strategies/0"] = [Value("{}")], ["/strategies/0/weights/0"] = [1] }
        }) Reject(() => generator.Generate(config with { Ranges = ranges }, baseline, default).ToArray(), "overlapping paths rejected");
        var subtree = config with { Samples = 1, Ranges = new() { ["nullable"] = [Value("null"), Value("{\"nested\":[1,2]}")] } };
        Check(generator.Generate(subtree, baseline, default).Count() == 2, "whole subtree choices");
        var subtreeReloaded = JsonSerializer.Deserialize<SearchConfiguration>(
            JsonSerializer.Serialize(subtree, SearchConfiguration.Json), SearchConfiguration.Json)!;
        Check(generator.Generate(subtreeReloaded, baseline, default).Select(p => p.Identity).SequenceEqual(
            generator.Generate(subtree, baseline, default).Select(p => p.Identity)), "null and object choices survive serialization");
        var serialized = JsonSerializer.Serialize(config, SearchConfiguration.Json);
        var loaded = JsonSerializer.Deserialize<SearchConfiguration>(serialized, SearchConfiguration.Json)!;
        Check(generator.Generate(loaded, baseline, default).Select(p => p.Identity).SequenceEqual(candidates.Select(p => p.Identity)), "mixed manifest round trip");
        var depth = 100;
        var deep = "{\"root\":" + string.Concat(Enumerable.Repeat("{\"child\":", depth)) + "1" + new string('}', depth + 1);
        var deepBase = ParameterSet.Parse(deep);
        var deepPath = "root." + string.Join('.', Enumerable.Repeat("child", depth));
        var deepChanged = deepBase.With(new Dictionary<string, ParameterValue> { [deepPath] = 2 });
        Check(deepChanged.Get(deepPath) == 2 && deepBase.Get(deepPath) == 1 &&
            ParameterSet.Parse(deepChanged.ToJson()).Identity == deepChanged.Identity, "nesting beyond default 64 levels");
    }
}
