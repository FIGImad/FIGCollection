using System.Diagnostics;
using FIGSignalBacktest;
internal static class SpeedBenchmark
{
    public static void Run(string[] args)
    {
        Environment.SetEnvironmentVariable("FIG_L2TEST_PROFILE","0");
        var bars=Prices.Aggregate(PriceCache.Read(args[3]),5).Take(12000).ToArray();
        var parameters=File.ReadAllText(args[4]);
        var plugins=new[]{new Plugins(args[1]),new Plugins(args[2])};
        foreach(var p in plugins){using var warm=p.Create("L2",parameters,5);foreach(var bar in bars.Take(1000))warm.Process(bar);}
        for(int round=0;round<2;round++) foreach(int index in round==0?new[]{0,1}:new[]{1,0})
        {
            GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
            if (Environment.GetEnvironmentVariable("FIG_TEST_COMPARE_REUSE") == "1") AppContext.SetSwitch("FIG.Studies.DisableParameterReuse", index == 0);
            using var study=plugins[index].Create("L2",parameters,5);
            var allocated=GC.GetAllocatedBytesForCurrentThread();var watch=Stopwatch.StartNew();
            foreach(var bar in bars)study.Process(bar);
            watch.Stop();var bytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
            Console.WriteLine($"Round {round+1}, {(index==0?"baseline":"optimized")}: {watch.Elapsed.TotalSeconds:F3}s; {bytes/1e9:F3} GB");
        }
    }
}

