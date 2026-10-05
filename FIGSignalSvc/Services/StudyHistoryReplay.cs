using FIG.Studies;
using FIGCommon.Models;

namespace FIGSignalExSvc.Services;

public static class StudyHistoryReplay
{
    public static List<BarStudy> Calculate(StudyColBase collection,
        Func<long, List<PriceDataRS>> readPage, int intervalSeconds, long retainFrom,
        Action<BarStudy>? onBarProcessed = null)
    {
        var result = new List<BarStudy>();
        foreach (var price in ReadBars(readPage, intervalSeconds))
        {
            var bar = collection.Calc([price], 0)[0];
            if (bar.Price.RawTime < retainFrom) continue;
            onBarProcessed?.Invoke(bar);
            result.Add(bar);
        }
        return result;
    }

    public static IEnumerable<PriceDataRS> ReadBars(Func<long, List<PriceDataRS>> readPage,
        int intervalSeconds, long start = 0)
    {
        if (intervalSeconds < 60 || intervalSeconds % 60 != 0)
            throw new InvalidDataException("Study replay requires an interval in whole minutes.");
        var pending = new List<PriceDataRS>();
        long cursor = start;
        while (true)
        {
            var page = readPage(cursor);
            if (page.Count == 0)
            {
                foreach (var bar in ExecStudySet.CompilePriceEx(pending, intervalSeconds)) yield return bar;
                yield break;
            }
            if (page[0].RawTime < cursor || page.Zip(page.Skip(1)).Any(pair => pair.First.RawTime >= pair.Second.RawTime))
                throw new InvalidDataException("Price replay must advance in strictly ascending order.");
            cursor = checked(page[^1].RawTime + 1);
            pending.AddRange(page);
            // Hold the final aggregate interval until the next page, including across market gaps.
            long boundary = ExecStudySet.CompilePriceEx(new() { page[^1] }, intervalSeconds)[0].RawTime;
            var completed = pending.Where(p => p.RawTime < boundary).ToList();
            pending = pending.Where(p => p.RawTime >= boundary).ToList();
            foreach (var bar in ExecStudySet.CompilePriceEx(completed, intervalSeconds)) yield return bar;
        }
    }
}
