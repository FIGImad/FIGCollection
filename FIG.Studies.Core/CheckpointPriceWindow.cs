using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using FIGCommon.Models;

namespace FIG.Studies;

// OHLCV and timestamps are the persisted calculation inputs. Database identities
// and decimal scale (1.25 versus 1.2500) must not affect the fingerprint.
public sealed record CheckpointPriceWindow(
    [property: JsonRequired] string Algorithm,
    [property: JsonRequired] int Count,
    [property: JsonRequired] long FirstRawTime,
    [property: JsonRequired] long LastRawTime,
    [property: JsonRequired] string Hash)
{
    public const string CurrentAlgorithm = "sha256-xor-v1";

    public void Validate(int limit, long checkpointRawTime)
    {
        if (Algorithm != CurrentAlgorithm || Count < 1 || Count > limit
            || FirstRawTime > LastRawTime || LastRawTime != checkpointRawTime
            || Hash == null || Hash.Length != 64 || Hash.Any(c => !Uri.IsHexDigit(c)))
            throw new InvalidDataException("Invalid checkpoint price-window reference.");
    }

    public void ValidatePrices(IReadOnlyList<PriceDataRS> prices)
    {
        if (prices.Count != Count || prices.Count == 0 || prices[0].RawTime != FirstRawTime
            || prices[^1].RawTime != LastRawTime)
            throw new InvalidDataException("Checkpoint price history is missing or has different boundaries.");
        var digest = default(PriceWindowDigest);
        for (int i = 0; i < prices.Count; i++)
        {
            if (i > 0 && prices[i].RawTime <= prices[i - 1].RawTime)
                throw new InvalidDataException("Checkpoint price history contains unordered or duplicate bars.");
            digest ^= PriceWindowDigest.ForPrice(prices[i]);
        }
        if (!string.Equals(digest.ToString(), Hash, StringComparison.Ordinal))
            throw new InvalidDataException("Checkpoint price history changed; replay is required.");
    }
}

// Each row hash includes its timestamp. XOR prefix sums let the bounded window
// advance or rewind without rehashing all historical prices on every capture.
// Row count, boundaries and strict timestamp order are validated separately.
internal readonly record struct PriceWindowDigest(ulong A, ulong B, ulong C, ulong D)
{
    public static PriceWindowDigest ForPrice(PriceDataRS price)
    {
        var canonical = string.Create(CultureInfo.InvariantCulture,
            $"{price.RawTime}|{price.Open:G29}|{price.High:G29}|{price.Low:G29}|{price.Close:G29}|{price.Volume}");
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(canonical), hash);
        return new(BinaryPrimitives.ReadUInt64LittleEndian(hash), BinaryPrimitives.ReadUInt64LittleEndian(hash[8..]),
            BinaryPrimitives.ReadUInt64LittleEndian(hash[16..]), BinaryPrimitives.ReadUInt64LittleEndian(hash[24..]));
    }

    public static PriceWindowDigest operator ^(PriceWindowDigest left, PriceWindowDigest right)
        => new(left.A ^ right.A, left.B ^ right.B, left.C ^ right.C, left.D ^ right.D);

    public override string ToString()
    {
        Span<byte> bytes = stackalloc byte[32];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, A);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[8..], B);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[16..], C);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[24..], D);
        return Convert.ToHexString(bytes);
    }
}
