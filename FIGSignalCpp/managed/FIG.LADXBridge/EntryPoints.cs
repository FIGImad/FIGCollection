using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using FIG.Studies;
using FIG.StudyCollections.LADX;
using FIGCommon.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace FIG.LADXBridge;

// ABI v1: x64, cdecl, little endian. Ownership stays on the allocating side.
// A handle is single-threaded; independent handles can run concurrently.
public static unsafe class EntryPoints
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Bar
    {
        public long Time;
        public double Open, High, Low, Close;
        public long Volume;
    }
    private sealed class Instance(LADXEngine engine)
    {
        public readonly LADXEngine Engine = engine;
        public bool Failed;
    }
    private static int Error(Exception ex, byte* error, int capacity)
    {
        if (error != null && capacity > 0)
        {
            var bytes = Encoding.UTF8.GetBytes(ex.Message);
            int length = Math.Min(bytes.Length, capacity - 1);
            bytes.AsSpan(0, length).CopyTo(new Span<byte>(error, length));
            error[length] = 0;
        }
        return -1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static int Create(byte* parameters, int length, int intervalSeconds,
        nint* handle, int* lookback, byte* error, int errorCapacity)
    {
        try
        {
            *handle = 0;
            if (intervalSeconds <= 0 || intervalSeconds % 60 != 0)
                throw new ArgumentException("LADX requires a positive whole-minute interval.");
            var json = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(parameters, length));
            using var document = JsonDocument.Parse(json);
            var options = LADXOptions.FromJson(document.RootElement);
            // Use the real factory's validation and warmup calculation.
            var collection = new LADXFactory().Create(new StudyCollectionContext(
                new StudyColRS { ColType = "LADX", ColParams = json }, new TickerRS(),
                new IntervalRS { IntervalLen = intervalSeconds }, NullLoggerFactory.Instance));
            *lookback = collection.GetMaxLen();
            *handle = GCHandle.ToIntPtr(GCHandle.Alloc(new Instance(new LADXEngine(options))));
            return 0;
        }
        catch (Exception ex) { return Error(ex, error, errorCapacity); }
    }

    // Binary result: int32 count; repeated UTF8 name (int32 length + bytes),
    // byte kind (0=null, 1=float64, 2=UTF8 text), followed by value when present.
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static int Process(nint handle, Bar* bar, byte* output, int capacity,
        byte* error, int errorCapacity)
    {
        Instance? instance = null;
        try
        {
            instance = (Instance)GCHandle.FromIntPtr(handle).Target!;
            if (instance.Failed) throw new InvalidOperationException("Collection failed; create a new instance and replay.");
            // Round-trip text retains the native double's significant decimal digits.
            static decimal Price(double value) => decimal.Parse(value.ToString("R", CultureInfo.InvariantCulture),
                NumberStyles.Float, CultureInfo.InvariantCulture);
            var values = instance.Engine.Process(new PriceDataRS {
                RawTime = bar->Time, Open = Price(bar->Open), High = Price(bar->High),
                Low = Price(bar->Low), Close = Price(bar->Close), Volume = checked((int)bar->Volume)
            });
            var writer = new ResultWriter(new Span<byte>(output, capacity));
            writer.Int(values.Count);
            foreach (var (key, value) in values)
            {
                writer.Text(key);
                if (value == null) writer.Byte(0);
                else if (value is string text) { writer.Byte(2); writer.Text(text); }
                else { writer.Byte(1); writer.Number(Convert.ToDouble(value, CultureInfo.InvariantCulture)); }
            }
            return writer.Length;
        }
        catch (Exception ex)
        {
            if (instance != null) instance.Failed = true;
            return Error(ex, error, errorCapacity);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static void Destroy(nint handle)
    {
        if (handle != 0) GCHandle.FromIntPtr(handle).Free();
    }

    private ref struct ResultWriter(Span<byte> buffer)
    {
        private Span<byte> remaining = buffer;
        public int Length { get; private set; }
        private Span<byte> Take(int length)
        {
            if (length > remaining.Length) throw new InvalidOperationException("LADX result exceeds bridge buffer capacity.");
            var part = remaining[..length]; remaining = remaining[length..]; Length += length; return part;
        }
        public void Int(int value) => BinaryPrimitives.WriteInt32LittleEndian(Take(4), value);
        public void Byte(byte value) => Take(1)[0] = value;
        public void Number(double value) => BinaryPrimitives.WriteDoubleLittleEndian(Take(8), value);
        public void Text(string value)
        {
            int length = Encoding.UTF8.GetByteCount(value); Int(length); Encoding.UTF8.GetBytes(value, Take(length));
        }
    }
}
