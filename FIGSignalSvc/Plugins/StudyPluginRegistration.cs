using FIG.Studies;

namespace FIGSignalExSvc.Plugins;

public sealed record StudyPluginRegistration(
    string ColType,
    string Version,
    string AssemblyPath,
    IStudyCollectionFactory Factory);
