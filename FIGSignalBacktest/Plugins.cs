using System.Reflection;
using System.Runtime.Loader;
using FIG.Studies;
using FIGCommon.Models;
using Microsoft.Extensions.Logging.Abstractions;
namespace FIGSignalBacktest;

public sealed class Plugins
{
    private sealed class Context(string file) : AssemblyLoadContext("Backtest:" + file, isCollectible: false)
    {
        private readonly AssemblyDependencyResolver resolver = new(file);
        protected override Assembly? Load(AssemblyName name)
        {
            if (name.Name is "FIG.Studies.Core" or "FIGCommon" || name.Name?.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal) == true)
                return Default.LoadFromAssemblyName(name);
            var found = resolver.ResolveAssemblyToPath(name);
            if (found == null && name.Name != null) { var adjacent = Path.Combine(Path.GetDirectoryName(file)!, name.Name + ".dll"); if (File.Exists(adjacent)) found = adjacent; }
            return found == null ? null : LoadFromAssemblyPath(found);
        }
        protected override nint LoadUnmanagedDll(string name) { var found = resolver.ResolveUnmanagedDllToPath(name); return found == null ? 0 : LoadUnmanagedDllFromPath(found); }
    }
    public string PathName { get; }
    private readonly Dictionary<string, Type> factories = new(StringComparer.OrdinalIgnoreCase);
    public IEnumerable<string> Types => factories.Keys.Order(StringComparer.Ordinal);
    public Plugins(string path)
    {
        PathName = Path.GetFullPath(path);
        if (!File.Exists(PathName)) throw new FileNotFoundException("Specify a managed study DLL with --collection-dll", PathName);
        var assembly = new Context(PathName).LoadFromAssemblyPath(PathName);
        foreach (var type in assembly.GetTypes().Where(t => !t.IsAbstract && typeof(IStudyCollectionFactory).IsAssignableFrom(t)))
        {
            var factory = (IStudyCollectionFactory)Activator.CreateInstance(type)!;
            if (factory.ApiVersion != StudyPluginApi.CurrentVersion) throw new InvalidDataException($"Incompatible plugin API: {factory.ColType}");
            if (string.IsNullOrWhiteSpace(factory.ColType) || !factories.TryAdd(factory.ColType, type)) throw new InvalidDataException("Empty/duplicate collection type");
        }
        if (factories.Count == 0) throw new InvalidDataException("DLL contains no IStudyCollectionFactory implementations");
    }
    public Collection Create(string name, string parameters, int interval)
    {
        if (!factories.TryGetValue(name, out var type)) throw new ArgumentException("Unsupported collection: " + name);
        var factory = (IStudyCollectionFactory)Activator.CreateInstance(type)!;
        var context = new StudyCollectionContext(new StudyColRS { ColType = factory.ColType, ColParams = parameters }, new TickerRS(), new IntervalRS { IntervalLen = checked(interval * 60), Id = interval + "M" }, NullLoggerFactory.Instance);
        return new(factory.Create(context));
    }
}
public sealed class Collection(StudyColBase study) : IDisposable
{
    // Optional plugin diagnostics: keep the host independent of any collection DLL.
    public bool SaveProfiling(string path)
    {
        var type = study.GetType();
        if (type.GetProperty("ProfilingEnabled")?.GetValue(study) is not true) return false;
        var method = type.GetMethod("GetProfilingSnapshot", Type.EmptyTypes);
        if (method?.Invoke(study, null) is not System.Collections.IEnumerable rows) return false;
        using var writer = Format.Writer(path);
        writer.WriteLine("Index,Study,Calls,TotalMs,AverageMs,MaxMs,AllocatedBytes,ProfiledBars");
        var bars = type.GetProperty("ProfiledBars")?.GetValue(study);
        foreach (var row in rows)
        {
            if (row == null) continue;
            object? Value(string name) => row.GetType().GetProperty(name)?.GetValue(row)
                ?? throw new InvalidDataException("Profiling snapshot is missing " + name);
            Format.Row(writer, Value("Index"), Format.Quote(Convert.ToString(Value("Study"))!),
                Value("Calls"), Value("TotalMs"), Value("AverageMs"), Value("MaxMs"), Value("AllocatedBytes"), bars);
        }
        return true;
    }
    private readonly List<PriceDataRS> input = new(1);
    public int Lookback => study.GetMaxLen();
    public StudyRow Process(Price price)
    {
        input.Clear(); input.Add(price.Managed());
        var result = study.Calc(input, 0);
        if (result.Count != 1) throw new InvalidDataException("Plugin returned an unexpected bar count");
        var numbers = new SortedDictionary<string, double?>(StringComparer.Ordinal);
        var text = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in result[0].Studies)
        {
            if (value is string s) text.Add(key, s);
            else if (value == null) numbers.Add(key, null);
            else { var n = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture); if (!double.IsFinite(n)) throw new InvalidDataException("Nonfinite study value: " + key); numbers.Add(key, n); }
        }
        return new(price, numbers, text);
    }
    public void Dispose() { if (study is IDisposable disposable) disposable.Dispose(); }
}
