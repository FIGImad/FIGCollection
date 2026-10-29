namespace FIG.Studies;

// Dependency inventory includes reads from historical bars and conditional branches.
public partial class StudyExtended : IStudyKeyContract
{
    private StudyOutputSelection? publication;
    private HashSet<string>? requiredPublicationKeys;
    private string? publicationLabel;
    public IEnumerable<string> ReadStudyKeys => [];
    public IEnumerable<string> WrittenStudyKeys => [
        Tag("MA"),
        Tag("SEM"),
        Tag("LB"),
        Tag("UB"),
        Tag("MID"),
        Tag("MID2"),
        Tag("LB2"),
        Tag("UB2")
    ];
    public void SelectStudyOutputs(IReadOnlySet<string> requiredKeys)
    {
        requiredPublicationKeys = new HashSet<string>(requiredKeys, StringComparer.Ordinal);
        RefreshPublication();
    }

    private void RefreshPublication()
    {
        publication = new StudyOutputSelection(WrittenStudyKeys, requiredPublicationKeys!);
        publicationLabel = label;
    }
}
