namespace FIGSignalBacktest;

public static class SelfTests
{
    static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException("Self-test: " + message); }
    public static void Run()
    {
        Check(Options.Parse(["--backtest-batch", "--fixed-output-dir", "--output-dir", "reports", "--jobs", "3"]).FixedOutputDirectory, "fixed output flag before valued options");
        Check(Options.Parse(["--backtest-batch", "--output-dir", "reports", "--fixed-output-dir"]).FixedOutputDirectory, "fixed output flag last");
        Check(!Options.Parse(["--backtest-batch"]).FixedOutputDirectory, "fixed output defaults off");
        var o = new Options(); long jan = Format.Date("2022-01-31"), feb = Format.Date("2022-02-01"), april = Format.Date("2022-04-01");
        Price P(long t, double close) => new(-1, 3, t, close, close + 1, close - 1, close, 100);
        var l = new Ledger(o); l.Process(P(jan, 100), 1); l.Process(P(jan + 300, 102), 1); Check(l.Fees == 24 && l.Unrealized == 600 && l.Trades.Count == 0, "open position");
        l.Process(P(feb, 103), 0); Check(l.Realized == 900 && l.Fees == 48 && l.Equity == o.Capital + 852, "long accounting");
        l.Process(P(april, 99), 0); Check(l.Months.Count == 4 && l.Months[2].Bars == 0, "missing month");
        var s = new Ledger(o); s.Process(P(jan, 100), -1); s.Process(P(feb, 98), -1); s.Process(P(april, 97), 0); Check(s.Realized == 900 && s.Fees == 48 && s.Trades[0].Side == -1, "short accounting");
        var r = new Ledger(o); r.Process(P(jan, 100), 1); r.Process(P(jan + 300, 102), -1); r.Process(P(jan + 600, 103), 1); r.Process(P(jan + 900, 104), 0); Check(r.Realized == 600 && r.Fees == 144 && r.Trades.Count == 3, "reversals");
        o = new() { Slippage = .25, ForceClose = true }; var f = new Ledger(o); f.Process(P(jan, 100), 1); f.Process(P(feb, 101), 1, true); Check(!f.Open && f.Realized == 150 && f.Fees == 48, "slippage/force close");
        o = new() { Capital = 1200000, Quantity = 0 }; var sized = new Ledger(o); sized.Process(new(-1, 3, jan, 5000, 5200, 4900, 4900, 100), 1); Check(sized.Quantity == 12, "HLC3 sizing");
        Check(Ledger.Signal(1.1) == 1 && Ledger.Signal(-1.2) == -1, "signal normalization");
        var coded = new Ledger(new Options { ForceClose = true });
        coded.Process(P(jan, 100), -1.9847);
        coded.Process(P(jan + 300, 99), -4.8546);
        Check(coded.Trades.Count == 0, "same direction signal change does not close trade");
        coded.Process(P(jan + 600, 98), 7.7498);
        coded.Process(P(jan + 900, 99), 7.6597, true);
        Check(coded.Trades.Count == 2 && coded.Trades[0].EntrySignal == -1.9847 && coded.Trades[1].EntrySignal == 7.7498,
            "entry signals preserved through subtype changes, reversal and forced close");
        Check(coded.Realized == 900 && coded.Fees == 96, "coded signals preserve direction-based accounting");
        var reportDir = Path.Combine(Path.GetTempPath(), "fig-side-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(reportDir);
        try
        {
            Reports.Save(new(new Candidate("side-test", "{}", ""), coded), reportDir);
            var lines = File.ReadAllLines(Path.Combine(reportDir, "side-test-trades.csv"));
            Check(lines[1].Split(',')[10] == "-1.9847" && lines[2].Split(',')[10] == "7.7498", "CSV Side contains four-decimal entry signal");
        }
        finally { Directory.Delete(reportDir, true); }
        var bars = Prices.Aggregate([P(61, 100), P(120, 102), P(601, 99)], 5); Check(bars.Count == 2 && bars[0].Time == 0 && bars[0].Id == -1 && bars[0].Volume == 200 && bars[0].Close == 102, "partial aggregation/gaps");
        bool rejected = false; try { Ledger.Signal(null); } catch (InvalidDataException) { rejected = true; }
        Check(rejected, "missing signal rejected");
        Configuration();
        CsvInput();
        Binary();
        DynamicBinary();
        Console.WriteLine("FIGSignalBacktest self-test passed");
    }
    private static void Configuration()
    {
        const string configured = "Data Source=localhost;Initial Catalog=FIGBacktestTest;Integrated Security=True";
        var options = Options.Parse(["--backtest-batch", "--dataset", "3", "--connection-string", configured]);
        options.Validate(true);
        var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Database.ConnectionString(options.ConnectionString));
        Check(connection.DataSource == "localhost" && connection.InitialCatalog == "FIGBacktestTest" && connection.IntegratedSecurity, "direct SQL connection option");
        bool rejected = false;
        try { Database.ConnectionString(""); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "missing SQL connection rejected");
        rejected = false;
        try { Options.Parse(["--backtest-batch", "--dataset", "3"]).Validate(true); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "database input requires connection option");
        rejected = false;
        try { Options.Parse(["--backtest-batch", "--settings", "old.json"]); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "removed settings option rejected");
        var cacheDirectory = Directory.CreateTempSubdirectory("fig-connection-cache-").FullName;
        options.CacheDirectory = cacheDirectory;
        options.CacheMode = "use";
        var resolved = Database.ConnectionString(configured);
        var identity = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(resolved))) + "-d3-n0";
        try
        {
            PriceCache.Write(Path.Combine(cacheDirectory, identity + "-raw-v1.bin"), [new(-1, 3, 60, 100, 102, 99, 101, 10)], 1);
            var input = Prices.Load(options);
            Check(input.Status == "raw-hit" && input.Bars.Count == 1, "direct connection uses matching offline cache without settings");
            Check(Prices.Load(options).Status == "aggregate-hit", "direct connection aggregate cache reused");
        }
        finally
        {
            foreach (var file in Directory.GetFiles(cacheDirectory)) File.Delete(file);
            Directory.Delete(cacheDirectory);
        }
    }
    private static void CsvInput()
    {
        var path = Path.GetTempFileName();
        try
        {
            bool rejected = false;
            try { new Options().Validate(true); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "database dataset required");
            var options = Options.Parse(["--backtest-batch", "--prices-csv", path, "--interval-minutes", "5"]);

            options.Validate(true);
            File.WriteAllText(path, "RawTime,Open,High,Low,Close,Volume\n60,100,103,99,102,10\n120,102,105,101,104,20\n360,104,106,103,105,30\n");
            var input = Prices.Load(options);
            Check(input.Status == "csv" && input.SourceCount == 3 && input.Bars.Count == 2 && input.Bars[0].Volume == 30 && input.Bars[0].Close == 104, "CSV aggregates without dataset or settings");
            options.MaxBars = 1;
            Check(Prices.Load(options).SourceCount == 1, "CSV maximum source bars");
            File.WriteAllText(path, "\"Id\",\"DataSetId\",\"RawTime\",Open,High,Low,Close,Volume\n1,7,60,100,103,99,102,10\n");
            Check(PriceCsv.Read(path, 0)[0].DataSetId == 7, "exported CSV and quoted headers supported");
            foreach (var content in new[] {
                "Open,High,Low,Close,Volume\n100,103,99,102,10\n",
                "RawTime,Open,High,Low,Close,Volume\n60,NaN,103,99,102,10\n",
                "RawTime,Open,High,Low,Close,Volume\n60,100,103,99,102,10\n60,100,103,99,102,10\n",
                "RawTime,Open,High,Low,Close,Volume\n"
            })
            {
                File.WriteAllText(path, content);
                rejected = false;
                try { PriceCsv.Read(path, 0); } catch (InvalidDataException) { rejected = true; }
                Check(rejected, "invalid CSV rejected");
            }
        }
        finally { File.Delete(path); }
    }
    private static void Binary()
    {
        string file=Path.GetTempFileName();
        try {
            var numbers=new SortedDictionary<string,double?>(StringComparer.Ordinal) { ["LADX2_SIDE"]=1.1,["LADX2_PriceMA"]=null,["MarketTrend_READY"]=1,["MarketTrend"]=1 };
            var text=new SortedDictionary<string,string>(StringComparer.Ordinal) { ["Reason"]="first" };
            using(var writer=new FigDataWriter(file,2,300,"LADX2")) {
                writer.Write(new(new(-1,3,100,1,2,0,1,100),numbers,text));
                numbers["LADX2_SIDE"]=1.2;numbers["LADX2_PriceMA"]=2;numbers["MarketTrend"]=-1;text["Reason"]="second";
                writer.Write(new(new(-1,3,400,1,2,0,1,100),numbers,text));writer.Finish();
            }
            var data=File.ReadAllBytes(file);
            Check(BitConverter.ToInt64(data,16)==2 && BitConverter.ToInt64(data,64)==100 && BitConverter.ToInt64(data,72)==400,"FIGDATA header");
            int Descriptor(int index)=>128+index*128;
            int Offset(int index,int field)=>checked((int)BitConverter.ToInt64(data,Descriptor(index)+field));
            Check(BitConverter.ToUInt32(data,Offset(8,32))==0xFF228B22 && BitConverter.ToUInt32(data,Offset(8,32)+4)==0xFFBEBE00,"FIGDATA signal colors");
            Check(BitConverter.ToUInt32(data,Offset(10,32))==0xFF00280A && BitConverter.ToUInt32(data,Offset(10,32)+4)==0xFF28000A,"FIGDATA background colors");
            Check(data[Offset(11,48)]==2 && BitConverter.ToUInt32(data,Descriptor(11)+80)==1,"FIGDATA null validity/overlay");
            Check(BitConverter.ToUInt32(data,Offset(15,64))==2,"FIGDATA text dictionary");
        } finally { File.Delete(file); }
    }
    private static void DynamicBinary()
    {
        var file = Path.GetTempFileName();
        try
        {
            using (var writer = new FigDataWriter(file, 3, 300, "L2"))
            {
                writer.Write(new(new(1, 1, 100, 1, 2, 0, 1, 1), new() { ["A"] = 1 }, new()));
                writer.Write(new(new(2, 1, 400, 1, 2, 0, 1, 1), new() { ["A"] = 2, ["L2_SIDE"] = 0 }, new() { ["Reason"] = "ready" }));
                writer.Write(new(new(3, 1, 700, 1, 2, 0, 1, 1), new() { ["L2_SIDE"] = 1 }, new()));
                writer.Finish();
            }
            var bytes = File.ReadAllBytes(file);
            int table = checked((int)BitConverter.ToUInt64(bytes, 32));
            Check(table > 128 && BitConverter.ToUInt32(bytes, 24) == 14, "dynamic descriptor table");
            int Offset(int column, int field) => checked((int)BitConverter.ToUInt64(bytes, table + column * 128 + field));
            Check(bytes[Offset(11, 48)] == 3 && bytes[Offset(12, 48)] == 6 && bytes[Offset(13, 48)] == 2, "dynamic validity masks");
            Check(BitConverter.ToDouble(bytes, Offset(12, 32) + 16) == 1, "late signal data preserved");
            Check(BitConverter.ToUInt32(bytes, Offset(13, 64)) == 1, "late string dictionary");
        }
        finally { File.Delete(file); }
    }
    public static void Plugin(Options o)
    {
        o.Validate(false, requireData: false); var plugins = new Plugins(o.Plugin); string json = o.Parameters == "" ? "{}" : File.ReadAllText(o.Parameters);
        using var a = plugins.Create(o.Collection, json, o.Interval); using var b = plugins.Create(o.Collection, json, o.Interval);
        for (int i = 0; i < 400; i++)
        {
            double v = 100 + (i % 17) * .25; var p = new Price(i, 3, 1700000000L + i * (long)o.Interval * 60, v, v + 1, v - 1, v + .125, 100);
            var x = a.Process(p); var y = b.Process(p); Check(x.Numbers.SequenceEqual(y.Numbers) && x.Text.SequenceEqual(y.Text), "plugin instance isolation");
        }
        var profilePath = Path.GetTempFileName();
        try
        {
            if (a.SaveProfiling(profilePath))
            {
                var lines = File.ReadAllLines(profilePath);
                Check(lines.Length > 1 && lines[0].EndsWith(",ProfiledBars"), "profiling rows exported");
                Check(lines.Skip(1).All(line => line.EndsWith(",400")), "profiling includes every processed bar");
                Console.WriteLine("Profiling export self-test passed");
            }
        }
        finally { File.Delete(profilePath); }
        Console.WriteLine($"Collection self-test passed: {o.Collection}; lookback={a.Lookback}");
    }
}
