using FIGSignalBacktest;
using System.Runtime.Loader;
using FIG.Studies;
using FIGCommon.Utilities;

internal static class ExtendedRegression
{
    public static void Run(string baselinePlugin, string optimizedPlugin, Price[] prices)
    {
        Type Find(string plugin)
        {
            var assembly=AssemblyLoadContext.All.SelectMany(c=>c.Assemblies).Single(a=>a.Location==Path.GetFullPath(plugin));
            return AssemblyLoadContext.GetLoadContext(assembly)!.Assemblies.Single(a=>a.GetName().Name=="FIG.Studies.Common").GetType("FIG.Studies.StudyExtended")!;
        }
        var oldType=Find(baselinePlugin);var newType=Find(optimizedPlugin);
        foreach(int period in new[]{1,14,200}) foreach(string source in new[]{"close","ohlc4","hlc3"})
        {
            BaseStudy Create(Type type)=>(BaseStudy)Activator.CreateInstance(type,period,source,1d,2d,"A")!;
            var left=Create(oldType);var right=Create(newType);
            var history=new CPriceList();var leftBars=new CircularBuffer<BarStudy>(10);var rightBars=new CircularBuffer<BarStudy>(10);
            for(int i=0;i<300;i++)
            {
                if(i==100)
                {
                    var checkpoint=right.CaptureCheckpoint();right=Create(newType);right.RestoreCheckpoint(checkpoint);
                }
                if(i==150) {oldType.GetField("label")!.SetValue(left,"B");newType.GetField("label")!.SetValue(right,"B");}
                var price=prices[i].Managed();history.Add(price);
                var a=new BarStudy(price);var b=new BarStudy(price);leftBars.AddToFront(a);rightBars.AddToFront(b);
                left.Process(price,leftBars,history);right.Process(price,rightBars,history);
                if(!a.Studies.OrderBy(x=>x.Key).SequenceEqual(b.Studies.OrderBy(x=>x.Key))) throw new Exception($"Extended mismatch: {period}/{source}/{i}");
            }
            var state=right.GetParams()!;state.Price.RawTime=12;var copy=state.Clone();state.Price.RawTime=34;
            if(copy.Price.RawTime!=12)throw new Exception("Extended clone shares Price");
        }
        Console.WriteLine("PASS StudyExtended: 3 periods, 3 sources, checkpoint resume, mutable labels, clone independence");
    }
}

