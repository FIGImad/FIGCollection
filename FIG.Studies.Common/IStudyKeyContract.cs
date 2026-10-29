namespace FIG.Studies;

/// <summary>Current and historical bar dependencies, and optional output publication.
/// Selection never changes a study's calculations or parameter history.</summary>
public interface IStudyKeyContract
{
    IEnumerable<string> ReadStudyKeys { get; }
    IEnumerable<string> WrittenStudyKeys { get; }
    void SelectStudyOutputs(IReadOnlySet<string> requiredKeys);
}

/// <summary>Immutable per-instance keys and flags, prepared before processing bars.</summary>
public sealed class StudyOutputSelection
{
    private readonly string[] keys;
    private readonly bool[] enabled;
    public string Signature { get; }

    public StudyOutputSelection(IEnumerable<string> outputKeys, IReadOnlySet<string> requiredKeys)
    {
        keys = outputKeys.ToArray();
        enabled = keys.Select(requiredKeys.Contains).ToArray();
        Signature = System.Text.Json.JsonSerializer.Serialize(keys.Where((_, i) => enabled[i]));
    }

    public bool IsEnabled(int index) => enabled[index];
    public string Key(int index) => keys[index];
}
