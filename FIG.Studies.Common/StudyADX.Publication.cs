namespace FIG.Studies;

// Dependency inventory includes reads from historical bars and conditional branches.
public partial class StudyADX : IStudyKeyContract
{
    private StudyOutputSelection? publication;
    public IEnumerable<string> ReadStudyKeys => [];
    public IEnumerable<string> WrittenStudyKeys => [
        Tag("ADX"),
        Tag("PDI"),
        Tag("NDI")
    ];
    public void SelectStudyOutputs(IReadOnlySet<string> requiredKeys)
        => publication = new StudyOutputSelection(new string[]
    {
        Tag("ADX"),
        Tag("PDI"),
        Tag("NDI")
    }, requiredKeys);
}
