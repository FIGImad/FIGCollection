using FIG.Studies;

namespace FIGSignalExSvc.Plugins;

public interface IStudyPluginCatalog
{
    IReadOnlyCollection<StudyPluginRegistration> Registrations { get; }

    StudyColBase Create(string colType, StudyCollectionContext context);
}
