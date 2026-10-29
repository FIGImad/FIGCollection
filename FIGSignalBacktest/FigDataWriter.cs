using System.Text;
namespace FIGSignalBacktest;

// FIGDATA v1: identical packed 128-byte headers/descriptors and column ordering.
public sealed class FigDataWriter : IDisposable
{
    private sealed class Column(string name, uint type, uint role, uint display, long count)
    {
        public string Name = name; public uint Type = type, Role = role, Display = display, Color;
        public long Offset, Size, Written, ValidityOffset, DictionaryOffset, DictionarySize, NameOffset;
        public bool Missing; public byte[] Validity = new byte[checked((int)((count + 7) / 8))];
        public MemoryStream Buffer = new(8192);
        public Dictionary<string, uint> Codes = new(StringComparer.Ordinal); public List<string> Dictionary = [];
    }
    private readonly FileStream stream; private readonly BinaryWriter writer;
    private readonly List<Column> columns = []; private string[] numeric = [], text = [];
    private readonly long count; private readonly uint interval; private readonly string collection;
    private long rows, first, last, dataEnd; private bool finished;
    private bool expanded;
    private readonly Dictionary<string, Column> studyColumns = new(StringComparer.Ordinal);
    public FigDataWriter(string path, long count, int interval, string collection)
    {
        if (count <= 0 || count > int.MaxValue) throw new ArgumentException("Invalid FIGDATA row count");
        this.count = count; this.interval = checked((uint)interval); this.collection = collection;
        stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None); writer = new(stream, Encoding.UTF8, true); writer.Write(new byte[128]);
    }
    private void Initialize(StudyRow row)
    {
        void Add(string name, uint type, uint role, uint display = 0) => columns.Add(new(name, type, role, display, count));
        Add("Id", 1, 8); Add("DataSetId", 1, 8); Add("RawTime", 1, 1); Add("Open", 2, 2); Add("High", 2, 3); Add("Low", 2, 4); Add("Close", 2, 5); Add("Volume", 1, 6);
        Add("SignalColor", 3, 8); Add("PriceBarColor", 3, 9); Add("BackgroundColor", 3, 10);
        numeric = row.Numbers.Keys.ToArray(); text = row.Text.Keys.ToArray();
        foreach (var name in numeric)
        {
            bool overlay = collection == "LADX2" && name is "LADX2_PriceMA" or "LADX2_GuideMA" or "LADX2_GuideUB" or "LADX2_GuideLB";
            Add(name, 2, 7, overlay ? 1u : 2u); if (overlay) columns[^1].Color = name == "LADX2_PriceMA" ? 0xFF90EE90u : 0xFFEE82EEu;
        }
        foreach (var name in text) Add(name, 4, 7);
        dataEnd = 128 + 128L * columns.Count;
        foreach (var c in columns) { c.Offset = dataEnd; c.Size = count * (c.Type is 1 or 2 ? 8 : 4); dataEnd = checked(dataEnd + c.Size); }
        foreach (var c in columns.Skip(11)) studyColumns.Add(c.Name, c);
    }
    private void EnsureColumn(string name, uint type)
    {
        if (studyColumns.TryGetValue(name, out var existing))
        {
            if (existing.Type != type) throw new InvalidDataException("FIGDATA study type changed: " + name);
            return;
        }
        if (columns.Take(11).Any(c => c.Name == name)) throw new InvalidDataException("Reserved FIGDATA column: " + name);
        var column = new Column(name, type, 7, 2, count)
        {
            Offset = dataEnd, Size = checked(count * (type == 2 ? 8 : 4)),
            Written = checked(rows * (type == 2 ? 8 : 4)), Missing = rows > 0
        };
        // Earlier rows are sparse zero-filled data with unset validity bits.
        dataEnd = checked(dataEnd + column.Size);
        columns.Add(column); studyColumns.Add(name, column); expanded = true;
    }
    private void Flush(Column c) { stream.Position = c.Offset + c.Written; c.Buffer.WriteTo(stream); c.Written += c.Buffer.Length; c.Buffer.SetLength(0); }
    private void Append(int index, ReadOnlySpan<byte> bytes, bool valid = true)
    {
        var c = columns[index]; c.Buffer.Write(bytes);
        if (valid) c.Validity[rows / 8] |= (byte)(1 << (int)(rows % 8)); else c.Missing = true;
        if (c.Buffer.Length >= 8192) Flush(c);
    }
    public void Write(StudyRow row)
    {
        if (finished || rows >= count) throw new InvalidOperationException("Too many FIGDATA rows");
        if (columns.Count == 0) Initialize(row);
        foreach (var name in row.Numbers.Keys) EnsureColumn(name, 2);
        foreach (var name in row.Text.Keys) EnsureColumn(name, 4);
        var p = row.Price; if (rows > 0 && p.Time <= last) throw new InvalidDataException("FIGDATA timestamps must increase");
        if (rows == 0) first = p.Time; last = p.Time;
        Append(0, BitConverter.GetBytes((long)p.Id)); Append(1, BitConverter.GetBytes((long)p.DataSetId)); Append(2, BitConverter.GetBytes(p.Time));
        Append(3, BitConverter.GetBytes(p.Open)); Append(4, BitConverter.GetBytes(p.High)); Append(5, BitConverter.GetBytes(p.Low)); Append(6, BitConverter.GetBytes(p.Close)); Append(7, BitConverter.GetBytes(p.Volume));
        bool colors = collection == "LADX2"; uint color = 0, background = 0;
        if (colors)
        {
            double Value(string key) => row.Numbers.GetValueOrDefault(key) ?? 0;
            double side = Value("LADX2_SIDE"); color = Math.Abs(side - 1.1) < 1e-9 ? 0xFF228B22u : Math.Abs(side - 1.2) < 1e-9 ? 0xFFBEBE00u : 0xFFFFFFFFu;
            double trend = Value("MarketTrend_READY") == 1 ? Value("MarketTrend") : 0; background = trend > 0 ? 0xFF00280Au : trend < 0 ? 0xFF28000Au : 0xFF000000u;
        }
        Append(8, BitConverter.GetBytes(color), colors); Append(9, BitConverter.GetBytes(color), colors); Append(10, BitConverter.GetBytes(background), colors);
        for (int index = 11; index < columns.Count; index++)
        {
            var c = columns[index];
            if (c.Type == 2)
            {
                var v = row.Numbers.GetValueOrDefault(c.Name);
                Append(index, BitConverter.GetBytes(v ?? 0), v.HasValue);
            }
            else if (row.Text.TryGetValue(c.Name, out var value))
            {
                if (!c.Codes.TryGetValue(value, out var code)) { code = checked((uint)c.Dictionary.Count); c.Codes.Add(value, code); c.Dictionary.Add(value); }
                Append(index, BitConverter.GetBytes(code));
            }
            else Append(index, BitConverter.GetBytes(0u), false);
        }
        rows++;
    }
    public void Finish()
    {
        if (finished || rows != count) throw new InvalidOperationException("Incomplete FIGDATA row count");
        foreach (var c in columns) Flush(c); stream.Position = dataEnd;
        // A larger descriptor table cannot overwrite existing column data.
        // FIGDATA v1 explicitly stores its offset in the header.
        long descriptorsOffset = expanded ? dataEnd : 128;
        if (expanded)
        {
            var ordered = columns.Take(11).Concat(columns.Skip(11).OrderBy(c => c.Type).ThenBy(c => c.Name, StringComparer.Ordinal)).ToArray();
            columns.Clear(); columns.AddRange(ordered);
            stream.Position = checked(descriptorsOffset + 128L * columns.Count);
        }
        using var strings = new MemoryStream();
        foreach (var c in columns)
        {
            if (c.Missing) { c.ValidityOffset = stream.Position; writer.Write(c.Validity); }
            if (c.Type == 4) { c.DictionaryOffset = stream.Position; writer.Write((uint)c.Dictionary.Count); foreach (var s in c.Dictionary) { var data = Encoding.UTF8.GetBytes(s); writer.Write((uint)data.Length); writer.Write(data); } c.DictionarySize = stream.Position - c.DictionaryOffset; }
            c.NameOffset = strings.Position; strings.Write(Encoding.UTF8.GetBytes(c.Name));
        }
        long stringsOffset = stream.Position; strings.WriteTo(stream); long fileBytes = stream.Position;
        stream.Position = descriptorsOffset;
        foreach (var c in columns)
        {
            writer.Write((ulong)c.NameOffset); writer.Write((uint)Encoding.UTF8.GetByteCount(c.Name)); writer.Write(c.Type);
            writer.Write(0UL); writer.Write(0u); writer.Write(c.Role);
            writer.Write((ulong)c.Offset); writer.Write((ulong)c.Size); writer.Write((ulong)c.ValidityOffset); writer.Write(c.Missing ? (ulong)c.Validity.Length : 0UL);
            writer.Write((ulong)c.DictionaryOffset); writer.Write((ulong)c.DictionarySize);
            writer.Write(c.Display); writer.Write(c.Color); writer.Write(0u); writer.Write(0u); writer.Write(new byte[32]);
        }
        stream.Position = 0; writer.Write("FIGDATA\0"u8); writer.Write(1u); writer.Write(128u); writer.Write((ulong)count);
        writer.Write((uint)columns.Count); writer.Write(128u); writer.Write((ulong)descriptorsOffset); writer.Write((ulong)stringsOffset); writer.Write((ulong)strings.Length); writer.Write((ulong)fileBytes);
        writer.Write(first); writer.Write(last); writer.Write(interval); writer.Write(0u); writer.Write(new byte[40]); writer.Flush(); stream.Flush(true); finished = true;
    }
    public void Dispose() { writer.Dispose(); stream.Dispose(); foreach (var c in columns) c.Buffer.Dispose(); }
}
