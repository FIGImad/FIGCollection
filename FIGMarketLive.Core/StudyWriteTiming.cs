using System.Diagnostics;
using System.Globalization;

namespace FIGMarketLive;

public sealed record StudyWriteTiming(int StudyColId,int Bars,int Checkpoints,bool Committed,double TotalMilliseconds,IReadOnlyDictionary<string,double> Stages)
{
    public override string ToString()=>$"Study write StudyCol {StudyColId}: {Bars:N0} bars, {Checkpoints} checkpoints, {(Committed?"committed":"not committed")}, total {TotalMilliseconds.ToString("F1",CultureInfo.InvariantCulture)} ms; "+
        string.Join("; ",Stages.Select(p=>$"{p.Key}={p.Value.ToString("F1",CultureInfo.InvariantCulture)} ms"));
}

internal sealed class StudyWriteProfiler(int id,int bars,int checkpoints,Action<StudyWriteTiming>? report):IDisposable
{
    private readonly long started=Stopwatch.GetTimestamp();
    private readonly Dictionary<string,double> stages=[];
    public bool Committed {get;set;}
    public IDisposable Stage(string name)=>new Measurement(elapsed=>stages[name]=stages.GetValueOrDefault(name)+elapsed);
    public void Dispose()
    {
        try{report?.Invoke(new(id,bars,checkpoints,Committed,Stopwatch.GetElapsedTime(started).TotalMilliseconds,new Dictionary<string,double>(stages)));}
        catch{ /* Diagnostics must not turn a committed write into an application failure. */ }
    }
    private sealed class Measurement(Action<double> complete):IDisposable
    {
        private readonly long started=Stopwatch.GetTimestamp();
        public void Dispose()=>complete(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }
}
