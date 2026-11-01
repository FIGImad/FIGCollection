using FIG.Studies;
using FIGCommon.Models;

namespace FIGMarketLive;

public sealed record EncodedStudyBar(PriceDataRS Price, byte[] Studies,byte[]? StudiesOverride=null);

/// <summary>Reconstructs plugin state from the complete supplied source history; no checkpoints.</summary>
public static class StudyReplay
{
    public static IReadOnlyList<EncodedStudyBar> Calculate(Func<StudyColBase> create,
        IReadOnlyList<PriceDataRS> prices, CancellationToken cancellation = default)
    {
        var collection = StudyLayers.Configure(create()); // Never reuse state advanced by a failed/cancelled publication.
        var results = new List<EncodedStudyBar>(prices.Count);
        using var encoder=new StudyLayers.Encoder();
        long? previous = null;
        foreach (var price in prices)
        {
            cancellation.ThrowIfCancellationRequested();
            if (previous.HasValue && price.RawTime <= previous.Value)
                throw new InvalidDataException("Replay prices must have strictly increasing UTC timestamps.");
            if (price.High < price.Low || price.Open < price.Low || price.Open > price.High ||
                price.Close < price.Low || price.Close > price.High || price.Volume < 0)
                throw new InvalidDataException("Invalid replay OHLCV.");
            previous = price.RawTime;
            var bar = collection.Calc([new PriceDataRS(price)], 0).Single();
            results.Add(encoder.Encode(bar));
        }
        return results;
    }
}
