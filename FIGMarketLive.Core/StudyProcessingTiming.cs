using System.Diagnostics;
using System.Globalization;

namespace FIGMarketLive;

/// <summary>Processing wall time, independent of SQL timings. Includes allocation/GC pauses in each operation.</summary>
public sealed class StudyProcessingTiming
{
    public double CalculationMs { get; private set; }
    public double EncodingMs { get; private set; }
    public double CheckpointCaptureMs { get; private set; }
    public double CheckpointSerializationMs { get; private set; }
    public int CalculatedBars { get; private set; }
    public int Captures { get; private set; }
    public void Calculation(long start,int bars=1){CalculationMs+=Stopwatch.GetElapsedTime(start).TotalMilliseconds;CalculatedBars+=bars;}
    public void Encoding(long start)=>EncodingMs+=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    public void Capture(long start){CheckpointCaptureMs+=Stopwatch.GetElapsedTime(start).TotalMilliseconds;Captures++;}
    public void Serialization(long start)=>CheckpointSerializationMs+=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    public override string ToString()=>string.Create(CultureInfo.InvariantCulture,$"{CalculatedBars:N0} bars; Calculation={CalculationMs:F1} ms; Binary encoding={EncodingMs:F1} ms; Checkpoint capture={CheckpointCaptureMs:F1} ms; Checkpoint serialization={CheckpointSerializationMs:F1} ms; Captures={Captures}");
}
