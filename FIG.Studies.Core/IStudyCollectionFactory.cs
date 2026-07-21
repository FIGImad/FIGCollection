namespace FIG.Studies;

public interface IStudyCollectionFactory
{
    string ColType { get; }
    string Version { get; }
    int ApiVersion { get; }

    StudyColBase Create(StudyCollectionContext context);
}
