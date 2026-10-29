using System.Text;
using System.Buffers.Binary;
namespace FIGSignalBacktest;

public static class PriceCache
{
    public const ulong Magic = 0x4649475043410001;
    public static ulong Hash(ReadOnlySpan<byte> bytes, ulong hash = 14695981039346656037) { foreach (var b in bytes) hash = unchecked((hash ^ b) * 1099511628211); return hash; }
    public record Header(ulong Count, ulong SourceCount, ulong Checksum);
    public static Header Inspect(string path)
    {
        using var s = File.OpenRead(path); using var r = new BinaryReader(s);
        if (s.Length < 40 || r.ReadUInt64() != Magic) throw new InvalidDataException("Invalid price cache header; refresh required");
        var h = new Header(r.ReadUInt64(), r.ReadUInt64(), r.ReadUInt64());
        if (r.ReadUInt64() != 8 || (s.Length - 40) % 56 != 0 || h.Count != (ulong)((s.Length - 40) / 56)) throw new InvalidDataException("Invalid price cache length/decimal size");
        return h;
    }
    public static List<Price> Read(string path)
    {
        var h = Inspect(path); using var stream = File.OpenRead(path); stream.Position = 40;
        var result = new List<Price>(checked((int)h.Count)); ulong hash = Hash([]); Span<byte> bytes = stackalloc byte[56];
        for (ulong i = 0; i < h.Count; i++)
        {
            stream.ReadExactly(bytes); hash = Hash(bytes, hash);
            result.Add(new(BinaryPrimitives.ReadInt32LittleEndian(bytes), BinaryPrimitives.ReadInt32LittleEndian(bytes[4..]), BinaryPrimitives.ReadInt64LittleEndian(bytes[8..]),
                BinaryPrimitives.ReadDoubleLittleEndian(bytes[16..]), BinaryPrimitives.ReadDoubleLittleEndian(bytes[24..]), BinaryPrimitives.ReadDoubleLittleEndian(bytes[32..]), BinaryPrimitives.ReadDoubleLittleEndian(bytes[40..]), BinaryPrimitives.ReadInt64LittleEndian(bytes[48..])));
        }
        if (hash != h.Checksum) throw new InvalidDataException("Price cache checksum mismatch; refresh required");
        return result;
    }
    public static Header Write(string path, IReadOnlyList<Price> prices, ulong sourceCount)
    {
        string temporary = path + ".tmp"; ulong hash = Hash([]);
        try
        {
            using (var w = new BinaryWriter(File.Create(temporary)))
            {
                w.Write(new byte[40]);
                Span<byte> data = stackalloc byte[56];
                foreach (var p in prices)
                {
                    BinaryPrimitives.WriteInt32LittleEndian(data, p.Id); BinaryPrimitives.WriteInt32LittleEndian(data[4..], p.DataSetId); BinaryPrimitives.WriteInt64LittleEndian(data[8..], p.Time);
                    BinaryPrimitives.WriteDoubleLittleEndian(data[16..], p.Open); BinaryPrimitives.WriteDoubleLittleEndian(data[24..], p.High); BinaryPrimitives.WriteDoubleLittleEndian(data[32..], p.Low); BinaryPrimitives.WriteDoubleLittleEndian(data[40..], p.Close); BinaryPrimitives.WriteInt64LittleEndian(data[48..], p.Volume);
                    hash = Hash(data, hash); w.Write(data);
                }
                w.BaseStream.Position = 0; w.Write(Magic); w.Write((ulong)prices.Count); w.Write(sourceCount); w.Write(hash); w.Write(8UL);
                w.Flush(); ((FileStream)w.BaseStream).Flush(true);
            }
            File.Move(temporary, path, true); return new((ulong)prices.Count, sourceCount, hash);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
public sealed record PriceInput(List<Price> Bars, ulong SourceCount, string Status, string CacheFile);
public static class Prices
{
    public static List<Price> Aggregate(IEnumerable<Price> prices, int minutes, long end = long.MaxValue)
    {
        var result = new List<Price>(); Price? pending = null, previous = null; long seconds = checked(minutes * 60);
        foreach (var p in prices)
        {
            p.Validate();
            if (previous is Price old && (p.Time <= old.Time || p.DataSetId != old.DataSetId)) throw new InvalidDataException("Prices must increase and belong to one dataset");
            previous = p; if (p.Time >= end) continue;
            if (minutes == 1) { result.Add(p); continue; }
            var bucket = p.Time - ((p.Time % seconds + seconds) % seconds);
            if (pending is not Price current || current.Time != bucket) { if (pending.HasValue) result.Add(pending.Value); pending = p with { Id = -1, Time = bucket }; }
            else pending = current with { High = Math.Max(current.High, p.High), Low = Math.Min(current.Low, p.Low), Close = p.Close, Volume = checked(current.Volume + p.Volume) };
        }
        if (pending.HasValue) result.Add(pending.Value); return result;
    }
    public static PriceInput Load(Options o, bool report = false)
    {
        long end = report || o.End == "" ? long.MaxValue : Format.Date(o.End);
        if (!string.IsNullOrWhiteSpace(o.PricesCsv))
        {
            var raw = PriceCsv.Read(o.PricesCsv, o.MaxBars);
            return new(Aggregate(raw, o.Interval, end), (ulong)raw.Count, "csv", "");
        }
        var connectionString = Database.ConnectionString(o.ConnectionString);
        if (o.CacheMode == "off") { var raw = Database.Read(o, connectionString); return new(Aggregate(raw, o.Interval, end), (ulong)raw.Count, "off", ""); }
        Directory.CreateDirectory(o.CacheDirectory);
        var identity = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(connectionString))) + "-d" + o.Dataset + "-n" + o.MaxBars;
        using var gate = new FileStream(Path.Combine(o.CacheDirectory, identity + ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var rawPath = Path.Combine(o.CacheDirectory, identity + "-raw-v1.bin");
        bool fresh = o.CacheMode == "refresh" || !File.Exists(rawPath); List<Price>? rawPrices = null;
        PriceCache.Header header;
        if (fresh) { rawPrices = Database.Read(o, connectionString); header = PriceCache.Write(rawPath, rawPrices, (ulong)rawPrices.Count); }
        else header = PriceCache.Inspect(rawPath);
        var aggregatePath = Path.Combine(o.CacheDirectory, $"{identity}-{header.Checksum}-{header.Count}-i{o.Interval}-e{end}-aggregate-v1.bin");
        if (!fresh && File.Exists(aggregatePath))
        {
            if (PriceCache.Inspect(aggregatePath).SourceCount != header.SourceCount) throw new InvalidDataException("Cache source count mismatch");
            var saved = PriceCache.Read(aggregatePath); foreach (var p in saved) p.Validate();
            return new(saved, header.SourceCount, "aggregate-hit", aggregatePath);
        }
        var bars = Aggregate(rawPrices ?? PriceCache.Read(rawPath), o.Interval, end);
        PriceCache.Write(aggregatePath, bars, header.SourceCount);
        return new(bars, header.SourceCount, fresh ? "database-snapshot" : "raw-hit", aggregatePath);
    }
}


