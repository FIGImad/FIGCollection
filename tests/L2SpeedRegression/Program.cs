using System.Diagnostics;
using System.Runtime.Loader;
using FIGSignalBacktest;
using FIG.Studies;
if (args.Length == 5 && args[0] == "--benchmark") { SpeedBenchmark.Run(args); return; }
if (args.Length is < 4 or > 5) throw new ArgumentException("baseline.dll optimized.dll raw-cache.bin parameters.json [bar-count]");
Environment.SetEnvironmentVariable("FIG_L2TEST_PROFILE", "0");
var prices = Prices.Aggregate(PriceCache.Read(args[2]), 5).Take(args.Length == 5 ? int.Parse(args[4]) : 36000).ToArray();
var parameters = File.ReadAllText(args[3]);
var removedPath = Environment.GetEnvironmentVariable("FIG_TEST_REMOVED_KEYS");
var removedKeys = removedPath == null ? new HashSet<string>() : File.ReadAllLines(removedPath).ToHashSet(StringComparer.Ordinal);
using var original = new Plugins(args[0]).Create("L2", parameters, 5);
using var optimized = new Plugins(args[1]).Create("L2", parameters, 5);
var keyAudit = Make(args[1]);
keyAudit.Calc([prices[0].Managed()], 0);
if (keyAudit.GetType().GetProperty("OmittedStudyKeys")?.GetValue(keyAudit) is IEnumerable<string> omitted)
{
    removedKeys.UnionWith(omitted);
    if (removedKeys.Contains("L2_SIDE")) throw new Exception("Selected signal was pruned");
    Console.WriteLine($"Publication contract omits {removedKeys.Count} unused keys.");
}
long oldTicks=0,newTicks=0,oldBytes=0,newBytes=0;
StudyRow Measure(Collection c, Price p, ref long ticks, ref long bytes)
{
    long allocated=GC.GetAllocatedBytesForCurrentThread(), start=Stopwatch.GetTimestamp();
    var row=c.Process(p);
    ticks+=Stopwatch.GetTimestamp()-start;bytes+=GC.GetAllocatedBytesForCurrentThread()-allocated;
    return row;
}
for(int i=0;i<prices.Length;i++)
{
    StudyRow a,b;
    if(i%2==0) {
        if (i == 0 && Environment.GetEnvironmentVariable("FIG_TEST_COMPARE_REUSE") == "1") AppContext.SetSwitch("FIG.Studies.DisableParameterReuse", true);
        a=Measure(original,prices[i],ref oldTicks,ref oldBytes);
        if (i == 0) AppContext.SetSwitch("FIG.Studies.DisableParameterReuse", false);
        b=Measure(optimized,prices[i],ref newTicks,ref newBytes); }
    else { b=Measure(optimized,prices[i],ref newTicks,ref newBytes); a=Measure(original,prices[i],ref oldTicks,ref oldBytes); }
    if(!a.Numbers.Where(x => !removedKeys.Contains(x.Key)).SequenceEqual(b.Numbers.Where(x => !removedKeys.Contains(x.Key))) || !a.Text.Where(x=>!removedKeys.Contains(x.Key)).SequenceEqual(b.Text.Where(x=>!removedKeys.Contains(x.Key)))) { foreach(var key in a.Numbers.Keys.Union(b.Numbers.Keys).Where(k => !removedKeys.Contains(k)).Where(k => !a.Numbers.ContainsKey(k) || !b.Numbers.ContainsKey(k) || a.Numbers[k] != b.Numbers[k]).Take(12)) Console.WriteLine($"Mismatch {key}: {a.Numbers.GetValueOrDefault(key)} vs {b.Numbers.GetValueOrDefault(key)}"); throw new Exception($"Output mismatch at bar {i}, time {prices[i].Time}"); }
    if((i+1)%3000==0) Console.WriteLine($"Matched retained fields: {i+1} bars");
}
// Clone history must retain independent Price objects and all scalar fields.
var assembly=AssemblyLoadContext.All.SelectMany(c=>c.Assemblies).Single(a=>a.Location==Path.GetFullPath(args[1]));
foreach(var name in new[]{"StudyTrend3SEMParams","StudyCrossSEMMAParams","StudyCycleDirParams"})
{
    var type=assembly.GetType("FIG.Studies."+name)!;
    var state=(BaseParams)Activator.CreateInstance(type)!;
    foreach(var field in type.GetFields())
    {
        if(field.FieldType==typeof(int)) field.SetValue(state,17);
        if(field.FieldType==typeof(decimal) || field.FieldType==typeof(decimal?)) field.SetValue(state,12.5m);
    }
    state.Price.RawTime=123;state.IsValid=false;
    var clone=state.Clone();
    if(ReferenceEquals(state.Price,clone.Price)||clone.Price.RawTime!=123||clone.IsValid) throw new Exception("Clone independence: "+name);
    foreach(var field in type.GetFields()) if(!Equals(field.GetValue(state),field.GetValue(clone))) throw new Exception("Clone field: "+field.Name);
    state.Price.RawTime=456;if(clone.Price.RawTime!=123) throw new Exception("Shared clone price");
}
Console.WriteLine($"PASS exact outputs on {prices.Length} bars; clone independence.");
Console.WriteLine($"Baseline: {oldTicks/(double)Stopwatch.Frequency:F2}s, {oldBytes/1e9:F3} GB allocated");
Console.WriteLine($"Optimized: {newTicks/(double)Stopwatch.Frequency:F2}s, {newBytes/1e9:F3} GB allocated");

// Exercise exact decimal/object outputs through rolling corrections, not just host float conversion.
StudyColBase Make(string file)
{
    var asm=AssemblyLoadContext.All.SelectMany(c=>c.Assemblies).Single(a=>a.Location==Path.GetFullPath(file));
    var factory=(IStudyCollectionFactory)Activator.CreateInstance(asm.GetTypes().Single(t=>!t.IsAbstract && typeof(IStudyCollectionFactory).IsAssignableFrom(t)))!;
    return factory.Create(new StudyCollectionContext(new FIGCommon.Models.StudyColRS {ColType="L2",ColParams=parameters},new FIGCommon.Models.TickerRS(),new FIGCommon.Models.IntervalRS {IntervalLen=300,Id="5M"},Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance));
}
var left=Make(args[0]);var right=Make(args[1]);
void Compare(List<BarStudy> a,List<BarStudy> b)
{
    if(a.Count!=b.Count) throw new Exception("Replay count");
    for(int i=0;i<a.Count;i++)
        if(!a[i].Studies.Where(x=>!removedKeys.Contains(x.Key)).OrderBy(x=>x.Key).SequenceEqual(b[i].Studies.Where(x=>!removedKeys.Contains(x.Key)).OrderBy(x=>x.Key))) throw new Exception("Exact replay output mismatch");
}
for(int i=0;i<1000;i++) Compare(left.Calc([prices[i].Managed()],0),right.Calc([prices[i].Managed()],0));
for(int iteration=0;iteration<3;iteration++)
{
    var correction=prices.Skip(997).Take(3).Select(p=>(p with {Close=p.Close+0.01*iteration}).Managed()).ToList();
    Compare(left.Calc(correction,3),right.Calc(correction,3));
}
Console.WriteLine("PASS exact decimal outputs and repeated three-bar rollback/replay");

ExtendedRegression.Run(args[0], args[1], prices);






