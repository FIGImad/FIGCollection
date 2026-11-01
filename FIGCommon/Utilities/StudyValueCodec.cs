using System.Text;

namespace FIGCommon.Utilities;

/// <summary>FIGS v1: little endian, bounded, self-describing published values; never runtime state.</summary>
public static class StudyValueCodec
{
    public const int MaxPayloadBytes = 16 * 1024 * 1024;
    private const int MaxEntries = 100_000;
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private enum Kind : byte { Null, Double, Decimal, Int64, Boolean, String, Int32, Single }

    public static byte[] Encode(IReadOnlyDictionary<string, object?> studies)
    {using var encoder=new Encoder();return encoder.Encode(studies);}

    public static Dictionary<string,object?> Merge(byte[] studies,byte[]? overrides)
    {
        var result=Decode(studies);
        if(overrides is not null)foreach(var item in Decode(overrides))result[item.Key]=item.Value;
        return result;
    }

    /// <summary>One encoder per replay. Caches only the last field layout; returned payloads remain independent.</summary>
    public sealed class Encoder : IDisposable
    {
        private readonly MemoryStream stream=new(4096);
        private readonly BinaryWriter writer;
        private KeyValuePair<string,object?>[] entries=[];
        private string[] inputKeys=[];
        private Field[] fields=[];
        private bool disposed;
        private sealed record Field(int InputIndex,string Key,byte[] EncodedName);
        public Encoder()=>writer=new BinaryWriter(stream,Utf8,true);
        public byte[] Encode(IReadOnlyDictionary<string,object?> studies)
        {
            ObjectDisposedException.ThrowIf(disposed,this);
            if(studies.Count>MaxEntries)throw new InvalidDataException("Too many study values.");
            bool changed=entries.Length!=studies.Count||inputKeys.Length!=studies.Count;
            if(changed)entries=new KeyValuePair<string,object?>[studies.Count];
            int n=0;
            foreach(var entry in studies)
            {
                if(n>=entries.Length)throw new InvalidDataException("Inconsistent study field count.");
                if(!changed&&!string.Equals(inputKeys[n],entry.Key,StringComparison.Ordinal))changed=true;
                entries[n++]=entry;
            }
            if(n!=entries.Length)throw new InvalidDataException("Inconsistent study field count.");
            if(changed)
            {
                inputKeys=[];fields=[];
                var nextKeys=entries.Select(e=>e.Key).ToArray();
                var nextFields=new Field[entries.Length];int index=0,total=10;
                foreach(int i in Enumerable.Range(0,entries.Length).OrderBy(i=>entries[i].Key,StringComparer.Ordinal))
                {
                    string key=entries[i].Key;int length=Utf8.GetByteCount(key);
                    if(length>MaxPayloadBytes-total-5)throw new InvalidDataException("Study payload exceeds limit.");
                    var encoded=new byte[length+4];System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(encoded,length);
                    Utf8.GetBytes(key.AsSpan(),encoded.AsSpan(4));total+=encoded.Length+1;
                    nextFields[index++]=new(i,key,encoded);
                }
                inputKeys=nextKeys;fields=nextFields;
            }
            stream.SetLength(0);stream.Position=0;
            writer.Write("FIGS"u8);writer.Write((ushort)1);writer.Write(entries.Length);
            foreach(var field in fields)
            {
                writer.Write(field.EncodedName);var value=entries[field.InputIndex].Value;
                switch(value)
                {
                    case null: writer.Write((byte)Kind.Null);break;
                    case double v: writer.Write((byte)Kind.Double);writer.Write(v);break;
                    case decimal v: WriteDecimal(writer,v);break;
                    case long v: writer.Write((byte)Kind.Int64);writer.Write(v);break;
                    case bool v: writer.Write((byte)Kind.Boolean);writer.Write(v);break;
                    case string v: writer.Write((byte)Kind.String);WriteString(writer,v);break;
                    case int v: writer.Write((byte)Kind.Int32);writer.Write(v);break;
                    case float v: writer.Write((byte)Kind.Single);writer.Write(v);break;
                    default: throw new InvalidDataException($"Unsupported study type for '{field.Key}': {value.GetType().FullName}");
                }
                if(stream.Length>MaxPayloadBytes)throw new InvalidDataException("Study payload exceeds limit.");
            }
            writer.Flush();return stream.ToArray();
        }
        private static void WriteDecimal(BinaryWriter writer,decimal value)
        {
            writer.Write((byte)Kind.Decimal);Span<int> bits=stackalloc int[4];decimal.GetBits(value,bits);
            foreach(int bit in bits)writer.Write(bit);
        }
        public void Dispose(){if(disposed)return;disposed=true;writer.Dispose();stream.Dispose();}
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
        if (length > MaxPayloadBytes || writer.BaseStream.Length+4L+length>MaxPayloadBytes) throw new InvalidDataException("String exceeds limit.");
        byte[]? rented=null;
        Span<byte> bytes=length<=512?stackalloc byte[length]:(rented=System.Buffers.ArrayPool<byte>.Shared.Rent(length)).AsSpan(0,length);
        try{Utf8.GetBytes(value.AsSpan(),bytes);writer.Write(length);writer.Write(bytes);}
        finally{if(rented is not null)System.Buffers.ArrayPool<byte>.Shared.Return(rented);}
    }

    private static string ReadString(BinaryReader reader)
    {
        int length = reader.ReadInt32();
        if (length < 0 || length > reader.BaseStream.Length - reader.BaseStream.Position)
            throw new InvalidDataException("Invalid string length.");
        return Utf8.GetString(reader.ReadBytes(length));
    }
}
