using System.Text;

namespace FIGMarketLive;

/// <summary>FIGS v1: little endian, bounded, self-describing published values; never runtime state.</summary>
public static class StudyBinaryCodec
{
    public const int MaxPayloadBytes = 16 * 1024 * 1024;
    private const int MaxEntries = 100_000;
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private enum Kind : byte { Null, Double, Decimal, Int64, Boolean, String, Int32, Single }

    public static byte[] Encode(IReadOnlyDictionary<string, object?> studies)
    {
        if (studies.Count > MaxEntries) throw new InvalidDataException("Too many study values.");
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Utf8, true);
        writer.Write("FIGS"u8); writer.Write((ushort)1); writer.Write(studies.Count);
        foreach (var (key, value) in studies.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            WriteString(writer, key);
            switch (value)
            {
                case null: writer.Write((byte)Kind.Null); break;
                case double v: writer.Write((byte)Kind.Double); writer.Write(v); break;
                case decimal v: writer.Write((byte)Kind.Decimal); foreach (var part in decimal.GetBits(v)) writer.Write(part); break;
                case long v: writer.Write((byte)Kind.Int64); writer.Write(v); break;
                case bool v: writer.Write((byte)Kind.Boolean); writer.Write(v); break;
                case string v: writer.Write((byte)Kind.String); WriteString(writer, v); break;
                case int v: writer.Write((byte)Kind.Int32); writer.Write(v); break;
                case float v: writer.Write((byte)Kind.Single); writer.Write(v); break;
                default: throw new InvalidDataException($"Unsupported study type for '{key}': {value.GetType().FullName}");
            }
            if (stream.Length > MaxPayloadBytes) throw new InvalidDataException("Study payload exceeds limit.");
        }
        return stream.ToArray();
    }

    public static Dictionary<string, object?> Decode(byte[] payload)
    {
        if (payload.Length > MaxPayloadBytes) throw new InvalidDataException("Study payload exceeds limit.");
        using var stream = new MemoryStream(payload, false);
        using var reader = new BinaryReader(stream, Utf8);
        try
        {
            if (!reader.ReadBytes(4).AsSpan().SequenceEqual("FIGS"u8) || reader.ReadUInt16() != 1)
                throw new InvalidDataException("Unsupported study binary header.");
            int count = reader.ReadInt32();
            if (count < 0 || count > MaxEntries) throw new InvalidDataException("Invalid study count.");
            var result = new Dictionary<string, object?>(StringComparer.Ordinal);
            for (int i = 0; i < count; i++)
            {
                var key = ReadString(reader);
                object? value = (Kind)reader.ReadByte() switch
                {
                    Kind.Null => null,
                    Kind.Double => reader.ReadDouble(),
                    Kind.Decimal => new decimal([reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()]),
                    Kind.Int64 => reader.ReadInt64(),
                    Kind.Boolean => ReadBoolean(reader),
                    Kind.String => ReadString(reader),
                    Kind.Int32 => reader.ReadInt32(),
                    Kind.Single => reader.ReadSingle(),
                    _ => throw new InvalidDataException("Unknown study value tag.")
                };
                if (!result.TryAdd(key, value)) throw new InvalidDataException("Duplicate study key.");
            }
            if (stream.Position != stream.Length) throw new InvalidDataException("Trailing study bytes.");
            return result;
        }
        catch (Exception ex) when (ex is EndOfStreamException or DecoderFallbackException or ArgumentException)
        { throw new InvalidDataException("Malformed study payload.", ex); }
    }

    private static bool ReadBoolean(BinaryReader reader) => reader.ReadByte() switch
    { 0 => false, 1 => true, _ => throw new InvalidDataException("Invalid boolean.") };

    private static void WriteString(BinaryWriter writer, string value)
    {
        int length = Utf8.GetByteCount(value);
        if (length > MaxPayloadBytes) throw new InvalidDataException("String exceeds limit.");
        writer.Write(length); writer.Write(Utf8.GetBytes(value));
    }

    private static string ReadString(BinaryReader reader)
    {
        int length = reader.ReadInt32();
        if (length < 0 || length > reader.BaseStream.Length - reader.BaseStream.Position)
            throw new InvalidDataException("Invalid string length.");
        return Utf8.GetString(reader.ReadBytes(length));
    }
}
