using System.Text.Json;
using FIG.Studies;
using FIGCommon.Models;
using FIGCommon.Utilities;

static class BaseSignalRegression
{
    public record Input(decimal High, decimal Low, decimal MA, int MAT, int Side, decimal PostHigh, decimal PostLow);

    public static void Run(string fixturePath)
    {
        var fixtures = JsonSerializer.Deserialize<Input[]>(File.ReadAllText(fixturePath))!;
        var study = new Study_BaseSignal("L2");
        var restored = new Study_BaseSignal("L2");
        var assigned = new Study_BaseSignal("C1");
        AppContext.SetSwitch("FIG.Studies.DisableParameterReuse", true);
        var withoutReuse = new Study_BaseSignal("L2");
        AppContext.SetSwitch("FIG.Studies.DisableParameterReuse", false);

        string Json<T>(T obj) => JsonSerializer.Serialize(obj, StudyStateJson.Options);
        BarStudy Process(Study_BaseSignal target, Input f, int index)
        {
            var price = new PriceDataRS { RawTime = 1700000000L + index * 300L,
                High = f.High, Low = f.Low, Open = (f.High + f.Low) / 2, Close = (f.High + f.Low) / 2 };
            var bar = new BarStudy(price);
            bar[StudyMA.Label("L2")] = f.MA;
            bar[StudyLineTrendEx.Label("L2", "MAT")] = f.MAT;
            var bars = new CircularBuffer<BarStudy>(10); bars.AddToFront(bar);
            var prices = new CPriceList(10); prices.Add(price);
            target.Process(price, bars, prices);
            return bar;
        }
        void Check(Study_BaseSignal target, BarStudy bar, Input f, int index)
        {
            if (target.Side != f.Side || target.postSide_H != f.PostHigh || target.postSide_L != f.PostLow
                || (int?)bar[target.Tag("SIGNAL")] != f.Side
                || (decimal?)bar[target.Tag("postSide_H")] != f.PostHigh
                || (decimal?)bar[target.Tag("postSide_L")] != f.PostLow)
                throw new Exception($"Base signal differs from EFS at fixture {index}: {Json(bar.Studies)} versus {Json(f)}");
        }

        for (int i = 0; i < fixtures.Length; i++)
        {
            var f = fixtures[i];
            Check(study, Process(study, f, i), f, i);
            Check(restored, Process(restored, f, i), f, i);
            Check(withoutReuse, Process(withoutReuse, f, i), f, i);
            if (Json(study.CaptureCheckpoint()) != Json(restored.CaptureCheckpoint())
                || Json(study.CaptureCheckpoint()) != Json(withoutReuse.CaptureCheckpoint()))
                throw new Exception($"Continuation/reuse state mismatch at {i}");

            // Re-evaluate the current bar with different prices, then restore it.
            // The final state must match the uninterrupted calculation exactly.
            if (i % 23 == 0)
            {
                Process(restored, f with { High = f.High + 30, Low = f.Low + 30 }, i);
                Check(restored, Process(restored, f, i), f, i);
                if (Json(study.CaptureCheckpoint()) != Json(restored.CaptureCheckpoint()))
                    throw new Exception($"Same-bar correction mismatch at {i}");
            }
            if (i % 173 == 0)
            {
                var checkpoint = JsonSerializer.Deserialize<StudyCheckpoint>(Json(study.CaptureCheckpoint()), StudyStateJson.Options)!;
                restored = new Study_BaseSignal("L2");
                restored.RestoreCheckpoint(checkpoint);
                assigned.Assign(study);
                if (Json(assigned.CaptureCheckpoint()) != Json(study.CaptureCheckpoint()))
                    throw new Exception($"Assign lost history at {i}");
            }
        }

        // Replay the last three revised bars and compare against a clean calculation.
        var revised = fixtures.Select((f, i) => i >= fixtures.Length - 3
            ? f with { High = f.High + 20, Low = f.Low + 20 } : f).ToArray();
        var fresh = new Study_BaseSignal("L2");
        for (int i = 0; i < revised.Length; i++) Process(fresh, revised[i], i);
        for (int i = revised.Length - 3; i < revised.Length; i++) Process(study, revised[i], i);
        if (Json(study.CaptureCheckpoint()) != Json(fresh.CaptureCheckpoint()))
            throw new Exception("Three-bar correction mismatch");

        var prefixed = new Study_BaseSignal("L2", "L2_BASE");
        if (prefixed.Tag("SIGNAL") != "L2_BASE_SIGNAL"
            || prefixed.WrittenStudyKeys.Intersect(new Study_L2().WrittenStudyKeys).Any())
            throw new Exception("Base signal outputs collide with strategy outputs");
        prefixed.SelectStudyOutputs(new HashSet<string> { "L2_BASE_SIGNAL" });
        var output = Process(prefixed, fixtures[1], 0);
        if (!output.ContainsKey("L2_BASE_SIGNAL") || output.ContainsKey("L2_BASE_POSTSIDE_L"))
            throw new Exception("Output selection was not honored");
        Console.WriteLine($"Base signal: {fixtures.Length} EFS reference bars passed; checkpoint continuation, same-bar updates, three-bar corrections, state reuse, assignment and output naming passed.");
    }
}
